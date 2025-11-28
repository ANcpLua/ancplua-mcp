using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Ancplua.Mcp.DebugTools.Tools;

/// <summary>
/// Represents the classification result of a binary inspection.
/// </summary>
public sealed record BinaryClassification(
    string OutputKind,
    bool IsManaged,
    bool HasEntryPoint,
    Subsystem Subsystem,
    bool IsDll,
    bool IsAppContainer,
    string FileExtension,
    string? AssemblyName,
    Version? AssemblyVersion,
    IReadOnlyList<string> AssemblyReferences,
    IReadOnlyList<string> DetectedFrameworks);

public static class BinaryInspector
{
    public static BinaryClassification Inspect(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        return Inspect(stream, Path.GetExtension(filePath));
    }

    public static BinaryClassification Inspect(Stream stream, string fileExtension)
    {
        ArgumentNullException.ThrowIfNull(fileExtension);

        using var peReader = new PEReader(stream, PEStreamOptions.LeaveOpen);
        var headers = peReader.PEHeaders;

        var isManaged = peReader.HasMetadata;
        var hasEntryPoint = DetectEntryPoint(headers.CorHeader);

        string? assemblyName = null;
        Version? assemblyVersion = null;
        List<string> references = [];

        if (isManaged)
        {
            var metadataReader = peReader.GetMetadataReader();

            if (metadataReader.IsAssembly)
            {
                var assemblyDef = metadataReader.GetAssemblyDefinition();
                assemblyName = metadataReader.GetString(assemblyDef.Name);
                assemblyVersion = assemblyDef.Version;

                foreach (var refHandle in metadataReader.AssemblyReferences)
                {
                    var reference = metadataReader.GetAssemblyReference(refHandle);
                    references.Add(metadataReader.GetString(reference.Name));
                }
            }
        }

        var subsystem = headers.PEHeader?.Subsystem ?? Subsystem.Unknown;
        var isAppContainer = headers.PEHeader?.DllCharacteristics.HasFlag(DllCharacteristics.AppContainer) ?? false;
        var normalizedExtension = fileExtension.ToUpperInvariant();
        var outputKind = ClassifyBinary(normalizedExtension, headers, isManaged, hasEntryPoint);
        var frameworks = DetectFrameworks(references);

        return new BinaryClassification(
            OutputKind: outputKind,
            IsManaged: isManaged,
            HasEntryPoint: hasEntryPoint,
            Subsystem: subsystem,
            IsDll: headers.IsDll,
            IsAppContainer: isAppContainer,
            FileExtension: normalizedExtension,
            AssemblyName: assemblyName,
            AssemblyVersion: assemblyVersion,
            AssemblyReferences: references,
            DetectedFrameworks: frameworks
        );
    }

    public static string FormatReport(BinaryClassification classification, string? filePath = null)
    {
        ArgumentNullException.ThrowIfNull(classification);

        var lines = new List<string>
        {
            "BINARY INSPECTION REPORT",
            "========================",
            ""
        };

        if (filePath is not null)
        {
            lines.Add($"File Path:            {filePath}");
        }

        lines.AddRange([
            $"Output Kind:          {classification.OutputKind}",
            $"Is Managed:           {classification.IsManaged}",
            $"Has Entry Point:      {classification.HasEntryPoint}",
            $"Subsystem:            {classification.Subsystem}",
            $"Is DLL:               {classification.IsDll}",
            $"Is AppContainer:      {classification.IsAppContainer}",
            $"File Extension:       {classification.FileExtension}"
        ]);

        if (classification.AssemblyName is not null)
        {
            lines.Add("");
            lines.Add($"Assembly Name:        {classification.AssemblyName}");
            lines.Add($"Assembly Version:     {classification.AssemblyVersion}");
        }

        if (classification.DetectedFrameworks.Count > 0)
        {
            lines.Add("");
            lines.Add("Detected Frameworks:");
            lines.AddRange(classification.DetectedFrameworks.Select(fw => $"  • {fw}"));
        }

        if (classification.AssemblyReferences.Count > 0)
        {
            lines.Add("");
            lines.Add($"Assembly References ({classification.AssemblyReferences.Count}):");
            lines.AddRange(classification.AssemblyReferences.Select(r => $"  • {r}"));
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static bool DetectEntryPoint(CorHeader? corHeader)
    {
        if (corHeader is null)
        {
            return false;
        }

        const int methodTokenMask = 0x06000000;
        return (corHeader.EntryPointTokenOrRelativeVirtualAddress & 0xFF000000) == methodTokenMask;
    }

    private static string ClassifyBinary(
        string normalizedExtension,
        PEHeaders headers,
        bool isManaged,
        bool hasEntryPoint)
    {
        return normalizedExtension switch
        {
            ".NETMODULE" => "NetModule",
            ".WINMD" or ".WINMDOBJ" => "Windows Runtime Metadata",
            _ => ClassifyByHeaders(headers, isManaged, hasEntryPoint)
        };
    }

    private static string ClassifyByHeaders(
        PEHeaders headers,
        bool isManaged,
        bool hasEntryPoint)
    {
        if (headers.IsDll)
        {
            return "Dynamically Linked Library";
        }

        var subsystem = headers.PEHeader?.Subsystem ?? Subsystem.Unknown;
        var isAppContainer = headers.PEHeader?.DllCharacteristics.HasFlag(DllCharacteristics.AppContainer) ?? false;

        if (isAppContainer && subsystem == Subsystem.WindowsGui)
        {
            return "Windows Runtime Application";
        }

        return subsystem switch
        {
            Subsystem.WindowsGui => "Windows Application",
            Subsystem.WindowsCui => "Console Application",
            _ when !isManaged => "Native Executable",
            _ when hasEntryPoint => "Console Application",
            _ => "Unknown Managed Binary"
        };
    }

    private static List<string> DetectFrameworks(IReadOnlyList<string> references)
    {
        var frameworks = new List<string>();
        var referenceSet = references.ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (FrameworkDefinitions.WpfAssemblies.Any(referenceSet.Contains))
        {
            frameworks.Add("WPF");
        }

        if (referenceSet.Contains("System.Windows.Forms"))
        {
            frameworks.Add("Windows Forms");
        }

        if (references.Any(r => r.StartsWith("Microsoft.AspNetCore", StringComparison.OrdinalIgnoreCase)))
        {
            frameworks.Add("ASP.NET Core");
        }

        if (referenceSet.Contains("System.ServiceProcess"))
        {
            frameworks.Add("Windows Service");
        }

        if (references.Any(r => r.StartsWith("Microsoft.Maui", StringComparison.OrdinalIgnoreCase)))
        {
            frameworks.Add(".NET MAUI");
        }

        if (referenceSet.Contains("Avalonia.Base"))
        {
            frameworks.Add("Avalonia");
        }

        return frameworks;
    }
}

public static class FrameworkDefinitions
{
    public static readonly IReadOnlySet<string> WpfAssemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "PresentationFramework",
        "PresentationCore",
        "WindowsBase"
    };
}
