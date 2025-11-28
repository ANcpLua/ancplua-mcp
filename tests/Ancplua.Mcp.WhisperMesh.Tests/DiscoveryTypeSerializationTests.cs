using System.Text.Json;
using Ancplua.Mcp.Libraries.WhisperMesh.Discoveries;

namespace Ancplua.Mcp.WhisperMesh.Tests;

/// <summary>
/// Tests for discovery type JSON serialization round-trip.
/// Validates compliance with WhisperMesh Protocol Specification v1.0 §4.
/// </summary>
public class DiscoveryTypeSerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    [Fact]
    public void CodeLocation_SerializesCorrectly()
    {
        // Arrange
        var location = new CodeLocation
        {
            File = "src/Core/Processor.cs",
            Line = 142,
            Column = 5,
            Symbol = "ProcessData()"
        };

        // Act
        var json = JsonSerializer.Serialize(location, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<CodeLocation>(json, JsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("src/Core/Processor.cs", deserialized.File);
        Assert.Equal(142, deserialized.Line);
        Assert.Equal(5, deserialized.Column);
        Assert.Equal("ProcessData()", deserialized.Symbol);
    }

    [Fact]
    public void CodeLocation_SerializesWithoutOptionalColumn()
    {
        // Arrange
        var location = new CodeLocation
        {
            File = "src/Core/Handler.cs",
            Line = 42,
            Symbol = "Handle()"
        };

        // Act
        var json = JsonSerializer.Serialize(location, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<CodeLocation>(json, JsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("src/Core/Handler.cs", deserialized.File);
        Assert.Equal(42, deserialized.Line);
        Assert.Null(deserialized.Column);
        Assert.Equal("Handle()", deserialized.Symbol);
    }

    [Fact]
    public void ArchitectureViolation_SerializesCorrectly()
    {
        // Arrange
        var violation = new ArchitectureViolation
        {
            Location = new CodeLocation
            {
                File = "src/Tools/NewTool.cs",
                Line = 10,
                Symbol = "NewToolMethod"
            },
            Rule = "CLAUDE.md#section-2",
            Severity = 0.9,
            Finding = "Missing ADR for new tool contract. All tool changes require ADR per CLAUDE.md §2.",
            Agent = "ARCH-Agent"
        };

        // Act
        var json = JsonSerializer.Serialize(violation, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<ArchitectureViolation>(json, JsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("ArchitectureViolation", deserialized.Type);
        Assert.Equal("src/Tools/NewTool.cs", deserialized.Location.File);
        Assert.Equal(10, deserialized.Location.Line);
        Assert.Equal("CLAUDE.md#section-2", deserialized.Rule);
        Assert.Equal(0.9, deserialized.Severity);
        Assert.Contains("Missing ADR", deserialized.Finding);
        Assert.Equal("ARCH-Agent", deserialized.Agent);
    }

    [Fact]
    public void ArchitectureViolation_TypeFieldIsConstant()
    {
        // Arrange
        var violation = new ArchitectureViolation
        {
            Location = new CodeLocation { File = "test.cs", Line = 1, Symbol = "Test" },
            Rule = "test-rule",
            Severity = 0.5,
            Finding = "test finding",
            Agent = "test-agent"
        };

        // Act
        var json = JsonSerializer.Serialize(violation, JsonOptions);

        // Assert - type field should always be "ArchitectureViolation"
        Assert.Contains("\"type\": \"ArchitectureViolation\"", json);
    }

    [Fact]
    public void ImplementationIssue_SerializesCorrectly()
    {
        // Arrange
        var issue = new ImplementationIssue
        {
            Location = new CodeLocation
            {
                File = "src/Data/QueryBuilder.cs",
                Line = 78,
                Column = 12,
                Symbol = "BuildQuery()"
            },
            Category = "security",
            Severity = 0.95,
            Finding = "Potential SQL injection: user input concatenated into query without parameterization.",
            Agent = "IMPL-Agent",
            Suggestion = "Use parameterized queries: cmd.Parameters.AddWithValue(\"@userId\", userId)"
        };

        // Act
        var json = JsonSerializer.Serialize(issue, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<ImplementationIssue>(json, JsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("ImplementationIssue", deserialized.Type);
        Assert.Equal("src/Data/QueryBuilder.cs", deserialized.Location.File);
        Assert.Equal(78, deserialized.Location.Line);
        Assert.Equal(12, deserialized.Location.Column);
        Assert.Equal("security", deserialized.Category);
        Assert.Equal(0.95, deserialized.Severity);
        Assert.Contains("SQL injection", deserialized.Finding);
        Assert.Equal("IMPL-Agent", deserialized.Agent);
        Assert.Contains("parameterized queries", deserialized.Suggestion);
    }

    [Fact]
    public void ImplementationIssue_SerializesWithoutOptionalSuggestion()
    {
        // Arrange
        var issue = new ImplementationIssue
        {
            Location = new CodeLocation { File = "src/Core/Logic.cs", Line = 99, Symbol = "Calculate()" },
            Category = "performance",
            Severity = 0.6,
            Finding = "N+1 query problem detected in loop.",
            Agent = "IMPL-Agent"
        };

        // Act
        var json = JsonSerializer.Serialize(issue, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<ImplementationIssue>(json, JsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("ImplementationIssue", deserialized.Type);
        Assert.Equal("performance", deserialized.Category);
        Assert.Null(deserialized.Suggestion);
    }

    [Fact]
    public void ImplementationIssue_TypeFieldIsConstant()
    {
        // Arrange
        var issue = new ImplementationIssue
        {
            Location = new CodeLocation { File = "test.cs", Line = 1, Symbol = "Test" },
            Category = "correctness",
            Severity = 0.5,
            Finding = "test finding",
            Agent = "test-agent"
        };

        // Act
        var json = JsonSerializer.Serialize(issue, JsonOptions);

        // Assert - type field should always be "ImplementationIssue"
        Assert.Contains("\"type\": \"ImplementationIssue\"", json);
    }

    [Fact]
    public void DiscoveryTypes_MatchWhisperMeshProtocolSchema()
    {
        // Arrange - create both discovery types
        var archViolation = new ArchitectureViolation
        {
            Location = new CodeLocation { File = "test.cs", Line = 1, Symbol = "Test" },
            Rule = "ADR-001",
            Severity = 0.8,
            Finding = "Architecture test",
            Agent = "ARCH-Agent"
        };

        var implIssue = new ImplementationIssue
        {
            Location = new CodeLocation { File = "test.cs", Line = 2, Symbol = "Test2" },
            Category = "security",
            Severity = 0.9,
            Finding = "Security test",
            Agent = "IMPL-Agent"
        };

        // Act - serialize both
        var archJson = JsonSerializer.Serialize(archViolation, JsonOptions);
        var implJson = JsonSerializer.Serialize(implIssue, JsonOptions);

        // Assert - both must have "type" field (§4.1 requirement)
        Assert.Contains("\"type\":", archJson);
        Assert.Contains("\"type\":", implJson);

        // Assert - both must have location with required fields
        Assert.Contains("\"location\":", archJson);
        Assert.Contains("\"location\":", implJson);
        Assert.Contains("\"file\":", archJson);
        Assert.Contains("\"file\":", implJson);
        Assert.Contains("\"line\":", archJson);
        Assert.Contains("\"line\":", implJson);

        // Assert - both must have severity
        Assert.Contains("\"severity\":", archJson);
        Assert.Contains("\"severity\":", implJson);
    }
}
