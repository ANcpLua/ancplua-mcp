using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Ancplua.Mcp.Servers.Stdio.RoslynMetrics;

/// <summary>
/// Core package inspection logic for AI agents to learn unknown APIs.
/// Downloads packages, extracts API surfaces, compares versions, detects breaking changes.
/// </summary>
internal sealed class PackageInspector
{
    private static readonly Lazy<IReadOnlyList<string>> RuntimeAssemblies = new(() =>
        ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))?.Split(Path.PathSeparator,
            StringSplitOptions.RemoveEmptyEntries) ?? []);

    private readonly SourceRepository _repository;

    public PackageInspector(string? sourceUrl = null)
    {
        var source = new PackageSource(sourceUrl ?? "https://api.nuget.org/v3/index.json");
        _repository = Repository.Factory.GetCoreV3(source);
    }

    /// <summary>
    /// Compare APIs between two package versions. Returns breaking changes, additions, deprecations.
    /// </summary>
    public async Task<ApiDiffResult> CompareVersionsAsync(
        string packageId, string fromVersion, string toVersion, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(fromVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(toVersion);

        var resource = await _repository.GetResourceAsync<FindPackageByIdResource>(ct).ConfigureAwait(false);
        using var cache = new SourceCacheContext();

        var oldVer = NuGetVersion.Parse(fromVersion);
        var newVer = NuGetVersion.Parse(toVersion);
        var changes = new ApiChanges();

        try
        {
            var (isMetaPackage, dependencies) = await CheckIfMetaPackageAsync(resource, cache, packageId, newVer, ct).ConfigureAwait(false);
            changes.IsMetaPackage = isMetaPackage;
            if (isMetaPackage) changes.MetaDependencies.AddRange(dependencies);

            // Use DTOs that capture data while MLC is still valid
            var oldTypes = await LoadTypesForComparisonAsync(resource, cache, packageId, oldVer, ct).ConfigureAwait(false);
            var newTypes = await LoadTypesForComparisonAsync(resource, cache, packageId, newVer, ct).ConfigureAwait(false);

            var oldTypeDict = oldTypes.GroupBy(t => t.FullName).ToDictionary(g => g.Key, g => g.First());
            var newTypeDict = newTypes.GroupBy(t => t.FullName).ToDictionary(g => g.Key, g => g.First());

            foreach (var (typeName, oldType) in oldTypeDict)
            {
                if (!newTypeDict.TryGetValue(typeName, out var newType))
                {
                    changes.RemovedTypes.Add(typeName);
                    continue;
                }
                CompareTypeInfo(oldType, newType, changes);
            }

            foreach (var typeName in newTypeDict.Keys.Where(k => !oldTypeDict.ContainsKey(k)))
                changes.AddedTypes.Add(typeName);
        }
        catch (InvalidOperationException ex) { changes.ComparisonError = ex.Message; }
        catch (IOException ex) { changes.ComparisonError = ex.Message; }
        catch (BadImageFormatException ex) { changes.ComparisonError = ex.Message; }

        return new ApiDiffResult(packageId, fromVersion, toVersion, changes);
    }

    /// <summary>
    /// Extract full API surface from a package version.
    /// </summary>
    public async Task<ApiSurfaceResult> ExtractApiAsync(
        string packageId, string version, bool includePrivate = false, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        var resource = await _repository.GetResourceAsync<FindPackageByIdResource>(ct).ConfigureAwait(false);
        using var cache = new SourceCacheContext();

        var ver = NuGetVersion.Parse(version);
        var apiTypes = await LoadAndExtractTypesAsync(resource, cache, packageId, ver, includePrivate, ct).ConfigureAwait(false);

        return new ApiSurfaceResult(packageId, version, apiTypes);
    }

    /// <summary>
    /// Decompile a type from a package to C# source.
    /// </summary>
    public async Task<DecompileResult> DecompileAsync(
        string packageId, string version, string? typeName = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        var resource = await _repository.GetResourceAsync<FindPackageByIdResource>(ct).ConfigureAwait(false);
        using var cache = new SourceCacheContext();

        var ver = NuGetVersion.Parse(version);
        var assemblyPath = await DownloadPrimaryAssemblyAsync(resource, cache, packageId, ver, ct).ConfigureAwait(false);

        if (assemblyPath is null)
            return new DecompileResult(packageId, version, typeName, null, "No assemblies found in package");

        try
        {
            var decompiler = new ICSharpCode.Decompiler.CSharp.CSharpDecompiler(
                assemblyPath, new() { ThrowOnAssemblyResolveErrors = false });

            string source;
            if (!string.IsNullOrWhiteSpace(typeName))
            {
                var type = decompiler.TypeSystem.MainModule.TypeDefinitions
                    .FirstOrDefault(t => string.Equals(t.FullName, typeName, StringComparison.Ordinal) ||
                                         string.Equals(t.ReflectionName, typeName, StringComparison.Ordinal));
                source = type is null ? $"// Type '{typeName}' not found" : decompiler.DecompileAsString(type.MetadataToken);
            }
            else
            {
                source = decompiler.DecompileWholeModuleAsString();
            }

            return new DecompileResult(packageId, version, typeName, source, null);
        }
        catch (InvalidOperationException ex) { return new DecompileResult(packageId, version, typeName, null, ex.Message); }
        catch (IOException ex) { return new DecompileResult(packageId, version, typeName, null, ex.Message); }
        catch (BadImageFormatException ex) { return new DecompileResult(packageId, version, typeName, null, ex.Message); }
    }

    /// <summary>
    /// Compares two TypeComparisonInfo DTOs (data already extracted from MLC).
    /// </summary>
    private static void CompareTypeInfo(TypeComparisonInfo oldType, TypeComparisonInfo newType, ApiChanges changes)
    {
        if (newType.ObsoleteMessage is not null)
            changes.ObsoleteTypes.Add(string.IsNullOrEmpty(newType.ObsoleteMessage) ? newType.Name : $"{newType.Name}: {newType.ObsoleteMessage}");

        if (!string.Equals(oldType.Namespace, newType.Namespace, StringComparison.Ordinal))
            changes.NamespaceChanges.Add($"{oldType.FullName} → {newType.FullName}");

        var oldInterfaces = oldType.InterfaceNames.ToHashSet();
        var newInterfaces = newType.InterfaceNames.ToHashSet();
        foreach (var removed in oldInterfaces.Except(newInterfaces))
            changes.RemovedInterfaces.Add($"{oldType.Name} no longer implements {removed}");
        foreach (var added in newInterfaces.Except(oldInterfaces))
            changes.AddedInterfaces.Add($"{oldType.Name} now implements {added}");

        if (!string.Equals(oldType.BaseTypeName, newType.BaseTypeName, StringComparison.Ordinal) &&
            oldType.BaseTypeName is not null && newType.BaseTypeName is not null)
            changes.BaseClassChanges.Add($"{oldType.Name}: {oldType.BaseTypeName} → {newType.BaseTypeName}");

        var oldMethods = oldType.Methods.Select(m => m.Signature).ToHashSet();
        var newMethods = newType.Methods.Select(m => m.Signature).ToHashSet();

        foreach (var oldSig in oldMethods.Where(o => !newMethods.Contains(o)))
        {
            changes.RemovedMethods.Add($"{oldType.Name}.{oldSig}");
            var oldMethod = oldType.Methods.FirstOrDefault(m => string.Equals(m.Signature, oldSig, StringComparison.Ordinal));
            if (oldMethod is not null && !oldMethod.IsAsync)
            {
                var asyncName = oldMethod.Name + "Async";
                if (newType.Methods.Any(m => string.Equals(m.Name, asyncName, StringComparison.Ordinal) && m.IsAsync))
                    changes.AsyncChanges.Add($"{oldType.Name}.{oldMethod.Name} → {asyncName} (sync to async)");
            }
        }

        foreach (var newSig in newMethods.Where(n => !oldMethods.Contains(n)))
            changes.AddedMethods.Add($"{newType.Name}.{newSig}");

        foreach (var method in newType.Methods.Where(m => m.ObsoleteMessage is not null))
        {
            changes.ObsoleteMethods.Add(string.IsNullOrEmpty(method.ObsoleteMessage)
                ? $"{newType.Name}.{method.Name}"
                : $"{newType.Name}.{method.Name}: {method.ObsoleteMessage}");
        }

        var oldProps = oldType.PropertyNames.ToHashSet();
        var newProps = newType.PropertyNames.ToHashSet();
        foreach (var old in oldProps.Where(o => !newProps.Contains(o)))
            changes.RemovedProperties.Add($"{oldType.Name}.{old}");
        foreach (var newProp in newProps.Where(n => !oldProps.Contains(n)))
            changes.AddedProperties.Add($"{newType.Name}.{newProp}");
    }

    private static bool IsAsyncMethod(MethodInfo method) =>
        method.ReturnType.FullName == "System.Threading.Tasks.Task" ||
        (method.ReturnType.IsGenericType && method.ReturnType.FullName?.StartsWith("System.Threading.Tasks.Task`1", StringComparison.Ordinal) == true);

    private static string GetMethodSignature(MethodInfo m) =>
        string.Create(CultureInfo.InvariantCulture, $"{m.Name}({string.Join(",", m.GetParameters().Select(p => p.ParameterType.Name))})");

    /// <summary>
    /// Gets obsolete message using GetCustomAttributesData() which works with MetadataLoadContext.
    /// GetCustomAttribute&lt;T&gt;() throws InvalidOperationException on MLC-loaded types.
    /// </summary>
    private static string? GetObsoleteMessage(MemberInfo member)
    {
        try
        {
            var obsoleteAttr = member.GetCustomAttributesData()
                .FirstOrDefault(a => a.AttributeType.FullName == "System.ObsoleteAttribute");

            if (obsoleteAttr is null) return null;

            // ObsoleteAttribute has constructor: ObsoleteAttribute(string? message) or ObsoleteAttribute(string? message, bool error)
            if (obsoleteAttr.ConstructorArguments.Count > 0)
            {
                var msg = obsoleteAttr.ConstructorArguments[0].Value as string;
                return string.IsNullOrEmpty(msg) ? "(deprecated)" : msg;
            }

            return "(deprecated)";
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (TypeLoadException)
        {
            return null;
        }
    }

    private static string GetTypeKind(Type t) => t switch
    {
        { IsInterface: true } => "interface",
        { IsEnum: true } => "enum",
        { IsValueType: true } => "struct",
        { IsClass: true, IsAbstract: true, IsSealed: true } => "static class",
        { IsClass: true, IsAbstract: true } => "abstract class",
        { IsClass: true, IsSealed: true } => "sealed class",
        { IsClass: true } => "class",
        _ => "type"
    };

    /// <summary>
    /// Loads types from a package and extracts API info while MetadataLoadContext is still valid.
    /// This is critical - Type objects become invalid after MLC disposal.
    /// Note: Uses GetCustomAttributesData() since GetCustomAttribute() doesn't work with MLC.
    /// </summary>
    private static async Task<ApiTypeInfo[]> LoadAndExtractTypesAsync(
        FindPackageByIdResource resource, SourceCacheContext cache,
        string packageId, NuGetVersion version, bool includePrivate, CancellationToken ct)
    {
        var assemblyPaths = await DownloadAssembliesAsync(resource, cache, packageId, version, ct).ConfigureAwait(false);
        if (assemblyPaths.Count == 0) return [];

        var resolver = new PathAssemblyResolver(RuntimeAssemblies.Value.Concat(assemblyPaths));
        using var mlc = new MetadataLoadContext(resolver);

        var bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | (includePrivate ? BindingFlags.NonPublic : 0);
        var apiTypes = new List<ApiTypeInfo>();

        foreach (var path in assemblyPaths)
        {
            if (!IsManagedAssembly(path)) continue;
            try
            {
                var types = mlc.LoadFromAssemblyPath(path).GetExportedTypes();
                foreach (var t in types.Where(t => t.FullName is not null && (includePrivate || t.IsPublic)))
                {
                    // Extract ALL data while MLC is valid
                    // Use GetCustomAttributesData() since GetCustomAttribute<T>() doesn't work with MLC
                    apiTypes.Add(new ApiTypeInfo(
                        FullName: t.FullName!,
                        Namespace: t.Namespace,
                        Name: t.Name,
                        Kind: GetTypeKind(t),
                        IsPublic: t.IsPublic,
                        BaseType: t.BaseType?.FullName,
                        Interfaces: [.. t.GetInterfaces().Select(i => i.FullName).OfType<string>()],
                        Methods: [.. t.GetMethods(bindingFlags)
                            .Where(m => !m.IsSpecialName)
                            .Select(m => new ApiMethodInfo(
                                m.Name, m.ReturnType.FullName ?? "void", m.IsStatic,
                                GetObsoleteMessage(m),
                                [.. m.GetParameters().Select(p => new ApiParameterInfo(p.Name ?? "arg", p.ParameterType.FullName ?? "object", p.HasDefaultValue))]))],
                        Properties: [.. t.GetProperties(bindingFlags)
                            .Select(p => new ApiPropertyInfo(p.Name, p.PropertyType.FullName ?? "object", p.CanRead, p.CanWrite))],
                        ObsoleteMessage: GetObsoleteMessage(t)));
                }
            }
            catch (BadImageFormatException) { /* Skip */ }
            catch (FileLoadException) { /* Skip */ }
        }

        return [.. apiTypes];
    }

    /// <summary>
    /// Loads types and extracts comparison-relevant data while MLC is valid.
    /// Returns lightweight DTOs for version comparison.
    /// Note: Uses GetObsoleteMessage() which works with MetadataLoadContext.
    /// </summary>
    private static async Task<List<TypeComparisonInfo>> LoadTypesForComparisonAsync(
        FindPackageByIdResource resource, SourceCacheContext cache,
        string packageId, NuGetVersion version, CancellationToken ct)
    {
        var assemblyPaths = await DownloadAssembliesAsync(resource, cache, packageId, version, ct).ConfigureAwait(false);
        if (assemblyPaths.Count == 0) return [];

        var resolver = new PathAssemblyResolver(RuntimeAssemblies.Value.Concat(assemblyPaths));
        using var mlc = new MetadataLoadContext(resolver);

        var result = new List<TypeComparisonInfo>();
        var bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;

        foreach (var path in assemblyPaths)
        {
            if (!IsManagedAssembly(path)) continue;
            try
            {
                var types = mlc.LoadFromAssemblyPath(path).GetExportedTypes();
                foreach (var t in types.Where(t => t.FullName is not null))
                {
                    // Extract all comparison-relevant data while MLC is valid
                    // Use GetObsoleteMessage() since GetCustomAttribute<T>() doesn't work with MLC
                    var methods = t.GetMethods(bindingFlags).Where(m => !m.IsSpecialName).ToList();
                    result.Add(new TypeComparisonInfo(
                        FullName: t.FullName!,
                        Name: t.Name,
                        Namespace: t.Namespace,
                        BaseTypeName: t.BaseType?.Name,
                        InterfaceNames: [.. t.GetInterfaces().Select(i => i.Name)],
                        ObsoleteMessage: GetObsoleteMessage(t),
                        Methods: [.. methods.Select(m => new MethodComparisonInfo(
                            Name: m.Name,
                            Signature: GetMethodSignature(m),
                            IsAsync: IsAsyncMethod(m),
                            ObsoleteMessage: GetObsoleteMessage(m)))],
                        PropertyNames: [.. t.GetProperties(bindingFlags).Select(p => p.Name)]));
                }
            }
            catch (BadImageFormatException) { /* Skip */ }
            catch (FileLoadException) { /* Skip */ }
        }

        return result;
    }

    /// <summary>
    /// Downloads assemblies from a package to temp directory.
    /// </summary>
    private static async Task<List<string>> DownloadAssembliesAsync(
        FindPackageByIdResource resource, SourceCacheContext cache,
        string packageId, NuGetVersion version, CancellationToken ct)
    {
        var packageStream = new MemoryStream();
        await using (packageStream.ConfigureAwait(false))
        {
            await resource.CopyNupkgToStreamAsync(packageId, version, packageStream, cache, NullLogger.Instance, ct).ConfigureAwait(false);
            packageStream.Seek(0, SeekOrigin.Begin);

            using var reader = new PackageArchiveReader(packageStream, leaveStreamOpen: true);
            var dllEntries = reader.GetFiles()
                .Where(f => f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) && f.Contains("/net", StringComparison.OrdinalIgnoreCase))
                .GroupBy(Path.GetFileNameWithoutExtension)
                .Select(g => g.OrderByDescending(TfmPriority).First())
                .ToList();

            if (dllEntries.Count == 0) return [];

            var tempDir = Path.Combine(Path.GetTempPath(), "pkg-inspector", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            var assemblyPaths = new List<string>();
            foreach (var entry in dllEntries)
            {
                ct.ThrowIfCancellationRequested();
                var dest = Path.Combine(tempDir, Path.GetFileName(entry));
                var entryStream = reader.GetStream(entry);
                await using (entryStream.ConfigureAwait(false))
                {
                    var fileStream = File.Create(dest);
                    await using (fileStream.ConfigureAwait(false))
                    {
                        await entryStream.CopyToAsync(fileStream, ct).ConfigureAwait(false);
                    }
                }
                assemblyPaths.Add(dest);
            }

            return assemblyPaths;
        }
    }

    private static async Task<string?> DownloadPrimaryAssemblyAsync(
        FindPackageByIdResource resource, SourceCacheContext cache,
        string packageId, NuGetVersion version, CancellationToken ct)
    {
        var packageStream = new MemoryStream();
        await using (packageStream.ConfigureAwait(false))
        {
            await resource.CopyNupkgToStreamAsync(packageId, version, packageStream, cache, NullLogger.Instance, ct).ConfigureAwait(false);
            packageStream.Seek(0, SeekOrigin.Begin);

            using var reader = new PackageArchiveReader(packageStream, leaveStreamOpen: true);
            var dllEntry = reader.GetFiles()
                .Where(f => f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) && f.Contains("/net", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(TfmPriority)
                .FirstOrDefault();

            if (dllEntry is null) return null;

            var tempDir = Path.Combine(Path.GetTempPath(), "pkg-inspector", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            var dest = Path.Combine(tempDir, Path.GetFileName(dllEntry));

            var entryStream = reader.GetStream(dllEntry);
            await using (entryStream.ConfigureAwait(false))
            {
                var fileStream = File.Create(dest);
                await using (fileStream.ConfigureAwait(false))
                {
                    await entryStream.CopyToAsync(fileStream, ct).ConfigureAwait(false);
                }
            }

            return dest;
        }
    }

    private static async Task<(bool isMetaPackage, List<string> dependencies)> CheckIfMetaPackageAsync(
        FindPackageByIdResource resource, SourceCacheContext cache,
        string packageId, NuGetVersion version, CancellationToken ct)
    {
        try
        {
            var packageStream = new MemoryStream();
            await using (packageStream.ConfigureAwait(false))
            {
                await resource.CopyNupkgToStreamAsync(packageId, version, packageStream, cache, NullLogger.Instance, ct).ConfigureAwait(false);
                packageStream.Seek(0, SeekOrigin.Begin);

                using var reader = new PackageArchiveReader(packageStream, leaveStreamOpen: true);
                var hasLibDlls = reader.GetFiles().Any(f =>
                    f.StartsWith("lib/", StringComparison.OrdinalIgnoreCase) &&
                    f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));

                if (hasLibDlls) return (false, []);

                var deps = reader.NuspecReader.GetDependencyGroups()
                    .SelectMany(g => g.Packages).Select(p => p.Id).Distinct().ToList();

                return (true, deps);
            }
        }
        catch (InvalidOperationException) { return (false, []); }
        catch (IOException) { return (false, []); }
    }

    private static int TfmPriority(string path)
    {
        var p = path.ToUpperInvariant();
        return p switch
        {
            _ when p.Contains("NET10", StringComparison.Ordinal) => 110,
            _ when p.Contains("NET9", StringComparison.Ordinal) => 105,
            _ when p.Contains("NET8", StringComparison.Ordinal) => 100,
            _ when p.Contains("NET7", StringComparison.Ordinal) => 90,
            _ when p.Contains("NET6", StringComparison.Ordinal) => 80,
            _ when p.Contains("NETSTANDARD2.1", StringComparison.Ordinal) => 70,
            _ when p.Contains("NETSTANDARD2.0", StringComparison.Ordinal) => 60,
            _ when p.Contains("NET4", StringComparison.Ordinal) => 50,
            _ => 0
        };
    }

    private static bool IsManagedAssembly(string path)
    {
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var peReader = new PEReader(fs);
            return peReader.HasMetadata && peReader.GetMetadataReader().IsAssembly;
        }
        catch (BadImageFormatException) { return false; }
        catch (IOException) { return false; }
    }
}

// ============================================================================
// DTOs for Package Inspector
// ============================================================================

#pragma warning disable CA1812 // Instantiated via object initializers

internal sealed record ApiDiffResult(string PackageId, string FromVersion, string ToVersion, ApiChanges Changes);
internal sealed record ApiSurfaceResult(string PackageId, string Version, ApiTypeInfo[] Types);
internal sealed record DecompileResult(string PackageId, string Version, string? TypeName, string? Source, string? Error);

internal sealed record ApiTypeInfo(
    string FullName, string? Namespace, string Name, string Kind, bool IsPublic,
    string? BaseType, string[] Interfaces, ApiMethodInfo[] Methods, ApiPropertyInfo[] Properties, string? ObsoleteMessage);

internal sealed record ApiMethodInfo(string Name, string ReturnType, bool IsStatic, string? ObsoleteMessage, ApiParameterInfo[] Parameters);
internal sealed record ApiPropertyInfo(string Name, string Type, bool CanRead, bool CanWrite);
internal sealed record ApiParameterInfo(string Name, string Type, bool HasDefault);

// DTOs for version comparison (captures data while MLC is valid)
internal sealed record TypeComparisonInfo(
    string FullName,
    string Name,
    string? Namespace,
    string? BaseTypeName,
    string[] InterfaceNames,
    string? ObsoleteMessage,
    MethodComparisonInfo[] Methods,
    string[] PropertyNames);

internal sealed record MethodComparisonInfo(
    string Name,
    string Signature,
    bool IsAsync,
    string? ObsoleteMessage);

#pragma warning restore CA1812

/// <summary>Tracks API changes between package versions.</summary>
internal sealed class ApiChanges
{
    public List<string> RemovedTypes { get; } = [];
    public List<string> AddedTypes { get; } = [];
    public List<string> RemovedMethods { get; } = [];
    public List<string> AddedMethods { get; } = [];
    public List<string> RemovedProperties { get; } = [];
    public List<string> AddedProperties { get; } = [];
    public List<string> RemovedInterfaces { get; } = [];
    public List<string> AddedInterfaces { get; } = [];
    public List<string> BaseClassChanges { get; } = [];
    public List<string> ObsoleteTypes { get; } = [];
    public List<string> ObsoleteMethods { get; } = [];
    public List<string> AsyncChanges { get; } = [];
    public List<string> NamespaceChanges { get; } = [];
    public bool IsMetaPackage { get; set; }
    public List<string> MetaDependencies { get; } = [];
    public string? ComparisonError { get; set; }

    public bool HasBreakingChanges =>
        RemovedTypes.Count > 0 || RemovedMethods.Count > 0 || RemovedProperties.Count > 0 ||
        RemovedInterfaces.Count > 0 || BaseClassChanges.Count > 0 || AsyncChanges.Count > 0;

    public bool HasAdditions =>
        AddedTypes.Count > 0 || AddedMethods.Count > 0 || AddedProperties.Count > 0 || AddedInterfaces.Count > 0;

    public string ToMarkdown()
    {
        var sb = new StringBuilder();

        if (HasBreakingChanges)
        {
            sb.AppendLine("## Breaking Changes");
            AppendList(sb, RemovedTypes, "Removed Types");
            AppendList(sb, RemovedMethods, "Removed Methods");
            AppendList(sb, RemovedProperties, "Removed Properties");
            AppendList(sb, RemovedInterfaces, "Removed Interfaces");
            AppendList(sb, BaseClassChanges, "Base Class Changes");
            AppendList(sb, AsyncChanges, "Sync → Async Migrations");
            AppendList(sb, NamespaceChanges, "Namespace Changes");
        }

        if (ObsoleteTypes.Count > 0 || ObsoleteMethods.Count > 0)
        {
            sb.AppendLine("## Deprecations");
            AppendList(sb, ObsoleteTypes, "Obsolete Types");
            AppendList(sb, ObsoleteMethods, "Obsolete Methods");
        }

        if (HasAdditions)
        {
            sb.AppendLine("## New Features");
            AppendList(sb, AddedTypes, "New Types");
            AppendList(sb, AddedMethods, "New Methods");
            AppendList(sb, AddedProperties, "New Properties");
            AppendList(sb, AddedInterfaces, "New Interfaces");
        }

        if (IsMetaPackage)
        {
            sb.AppendLine("## Meta-Package");
            sb.AppendLine("This package contains no assemblies, only dependencies:");
            foreach (var dep in MetaDependencies.Take(20))
                sb.Append("- ").AppendLine(dep);
            if (MetaDependencies.Count > 20)
                sb.Append("... and ").Append(MetaDependencies.Count - 20).AppendLine(" more");
        }

        if (ComparisonError is not null)
            sb.Append("\n**Warning:** ").AppendLine(ComparisonError);

        return sb.ToString();
    }

    private static void AppendList(StringBuilder sb, List<string> items, string header)
    {
        if (items.Count == 0) return;
        sb.Append("### ").Append(header).Append(" (").Append(items.Count).AppendLine(")");
        foreach (var item in items.Take(10))
            sb.Append("- ").AppendLine(item);
        if (items.Count > 10)
            sb.Append("- ... and ").Append(items.Count - 10).AppendLine(" more");
        sb.AppendLine();
    }
}
