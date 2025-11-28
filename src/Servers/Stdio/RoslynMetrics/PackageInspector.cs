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

            var oldTypes = await LoadTypesAsync(resource, cache, packageId, oldVer, ct).ConfigureAwait(false);
            var newTypes = await LoadTypesAsync(resource, cache, packageId, newVer, ct).ConfigureAwait(false);

            var oldTypeDict = oldTypes.Where(t => t.FullName is not null)
                .GroupBy(t => t.FullName!).ToDictionary(g => g.Key, g => g.First());
            var newTypeDict = newTypes.Where(t => t.FullName is not null)
                .GroupBy(t => t.FullName!).ToDictionary(g => g.Key, g => g.First());

            foreach (var (typeName, oldType) in oldTypeDict)
            {
                if (!newTypeDict.TryGetValue(typeName, out var newType))
                {
                    changes.RemovedTypes.Add(typeName);
                    continue;
                }
                CompareTypeMembers(oldType, newType, changes);
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
        var types = await LoadTypesAsync(resource, cache, packageId, ver, ct).ConfigureAwait(false);

        var bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | (includePrivate ? BindingFlags.NonPublic : 0);
        var apiTypes = types
            .Where(t => t.FullName is not null && (includePrivate || t.IsPublic))
            .Select(t => new ApiTypeInfo(
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
                        m.GetCustomAttribute<ObsoleteAttribute>()?.Message,
                        [.. m.GetParameters().Select(p => new ApiParameterInfo(p.Name ?? "arg", p.ParameterType.FullName ?? "object", p.HasDefaultValue))]))],
                Properties: [.. t.GetProperties(bindingFlags)
                    .Select(p => new ApiPropertyInfo(p.Name, p.PropertyType.FullName ?? "object", p.CanRead, p.CanWrite))],
                ObsoleteMessage: t.GetCustomAttribute<ObsoleteAttribute>()?.Message))
            .ToArray();

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

    private static void CompareTypeMembers(Type oldType, Type newType, ApiChanges changes)
    {
        var newObsolete = newType.GetCustomAttribute<ObsoleteAttribute>();
        if (newObsolete is not null)
            changes.ObsoleteTypes.Add(string.IsNullOrEmpty(newObsolete.Message) ? newType.Name : $"{newType.Name}: {newObsolete.Message}");

        if (!string.Equals(oldType.Namespace, newType.Namespace, StringComparison.Ordinal))
            changes.NamespaceChanges.Add($"{oldType.FullName} → {newType.FullName}");

        var oldInterfaces = oldType.GetInterfaces().Select(i => i.Name).ToHashSet();
        var newInterfaces = newType.GetInterfaces().Select(i => i.Name).ToHashSet();
        foreach (var removed in oldInterfaces.Except(newInterfaces))
            changes.RemovedInterfaces.Add($"{oldType.Name} no longer implements {removed}");
        foreach (var added in newInterfaces.Except(oldInterfaces))
            changes.AddedInterfaces.Add($"{oldType.Name} now implements {added}");

        var oldBase = oldType.BaseType?.Name;
        var newBase = newType.BaseType?.Name;
        if (!string.Equals(oldBase, newBase, StringComparison.Ordinal) && oldBase is not null && newBase is not null)
            changes.BaseClassChanges.Add($"{oldType.Name}: {oldBase} → {newBase}");

        var oldMethods = GetMethodSignatures(oldType);
        var newMethods = GetMethodSignatures(newType);
        var oldMethodInfos = oldType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Where(m => !m.IsSpecialName).ToList();
        var newMethodInfos = newType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Where(m => !m.IsSpecialName).ToList();

        foreach (var old in oldMethods.Where(o => !newMethods.Contains(o)))
        {
            changes.RemovedMethods.Add($"{oldType.Name}.{old}");
            var oldMethod = oldMethodInfos.FirstOrDefault(m => string.Equals(GetMethodSignature(m), old, StringComparison.Ordinal));
            if (oldMethod is not null && !IsAsyncMethod(oldMethod))
            {
                var asyncName = oldMethod.Name + "Async";
                if (newMethodInfos.Any(m => string.Equals(m.Name, asyncName, StringComparison.Ordinal) && IsAsyncMethod(m)))
                    changes.AsyncChanges.Add($"{oldType.Name}.{oldMethod.Name} → {asyncName} (sync to async)");
            }
        }

        foreach (var newMethod in newMethods.Where(n => !oldMethods.Contains(n)))
            changes.AddedMethods.Add($"{newType.Name}.{newMethod}");

        foreach (var method in newMethodInfos)
        {
            var obsolete = method.GetCustomAttribute<ObsoleteAttribute>();
            if (obsolete is not null)
                changes.ObsoleteMethods.Add(string.IsNullOrEmpty(obsolete.Message) ? $"{newType.Name}.{method.Name}" : $"{newType.Name}.{method.Name}: {obsolete.Message}");
        }

        var oldProps = GetPropertyNames(oldType);
        var newProps = GetPropertyNames(newType);
        foreach (var old in oldProps.Where(o => !newProps.Contains(o)))
            changes.RemovedProperties.Add($"{oldType.Name}.{old}");
        foreach (var newProp in newProps.Where(n => !oldProps.Contains(n)))
            changes.AddedProperties.Add($"{newType.Name}.{newProp}");
    }

    private static bool IsAsyncMethod(MethodInfo method) =>
        method.ReturnType == typeof(Task) ||
        (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>));

    private static string GetMethodSignature(MethodInfo m) =>
        string.Create(CultureInfo.InvariantCulture, $"{m.Name}({string.Join(",", m.GetParameters().Select(p => p.ParameterType.Name))})");

    private static HashSet<string> GetMethodSignatures(Type type) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Where(m => !m.IsSpecialName).Select(GetMethodSignature).ToHashSet();

    private static HashSet<string> GetPropertyNames(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Select(p => p.Name).ToHashSet();

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

    private static async Task<List<Type>> LoadTypesAsync(
        FindPackageByIdResource resource, SourceCacheContext cache,
        string packageId, NuGetVersion version, CancellationToken ct)
    {
        var packageStream = new MemoryStream();
        await using (packageStream.ConfigureAwait(false))
        {
            await resource.CopyNupkgToStreamAsync(packageId, version, packageStream, cache, NullLogger.Instance, ct).ConfigureAwait(false);
            packageStream.Seek(0, SeekOrigin.Begin);
        }

        packageStream = new MemoryStream();
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

        var resolver = new PathAssemblyResolver(RuntimeAssemblies.Value.Concat(assemblyPaths));
        using var mlc = new MetadataLoadContext(resolver);

        var types = new List<Type>();
        foreach (var path in assemblyPaths)
        {
            if (!IsManagedAssembly(path)) continue;
            try { types.AddRange(mlc.LoadFromAssemblyPath(path).GetExportedTypes()); }
            catch (BadImageFormatException) { /* Skip */ }
            catch (FileLoadException) { /* Skip */ }
        }

        return types;
    }

    private static async Task<string?> DownloadPrimaryAssemblyAsync(
        FindPackageByIdResource resource, SourceCacheContext cache,
        string packageId, NuGetVersion version, CancellationToken ct)
    {
        var packageStream = new MemoryStream();
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

    private static async Task<(bool isMetaPackage, List<string> dependencies)> CheckIfMetaPackageAsync(
        FindPackageByIdResource resource, SourceCacheContext cache,
        string packageId, NuGetVersion version, CancellationToken ct)
    {
        try
        {
            var packageStream = new MemoryStream();
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
