#pragma warning disable CA1707
using Ancplua.Mcp.CoreTools.Utils;

namespace Ancplua.Mcp.CoreTools.Tests;

public class CommandParserTests
{
    [Fact]
    public void Parse_SimpleCommand_ReturnsExecutableAndArgs()
    {
        // Act
        var (exe, args) = CommandParser.Parse("git status");

        // Assert
        Assert.Equal("git", exe);
        Assert.Single(args);
        Assert.Equal("status", args[0]);
    }

    [Fact]
    public void Parse_CommandWithQuotedArgument_ParsesCorrectly()
    {
        // Act
        var (exe, args) = CommandParser.Parse("git commit -m \"Fix bug\"");

        // Assert
        Assert.Equal("git", exe);
        Assert.Equal(3, args.Length);
        Assert.Equal("commit", args[0]);
        Assert.Equal("-m", args[1]);
        Assert.Equal("Fix bug", args[2]);
    }

    [Fact]
    public void Parse_EscapedQuotesInQuotedString_ParsesCorrectly()
    {
        // Act
        var (exe, args) = CommandParser.Parse("echo \"He said \\\"hello\\\"\"");

        // Assert
        Assert.Equal("echo", exe);
        Assert.Single(args);
        Assert.Equal("He said \"hello\"", args[0]);
    }

    [Fact]
    public void Parse_SingleQuotes_ParsesCorrectly()
    {
        // Act
        var (exe, args) = CommandParser.Parse("echo 'hello world'");

        // Assert
        Assert.Equal("echo", exe);
        Assert.Single(args);
        Assert.Equal("hello world", args[0]);
    }

    [Fact]
    public void Parse_MixedQuotes_ParsesCorrectly()
    {
        // Double quotes inside single quotes
        var (exe, args) = CommandParser.Parse("echo '\"quoted\"'");

        Assert.Equal("echo", exe);
        Assert.Single(args);
        Assert.Equal("\"quoted\"", args[0]);
    }

    [Fact]
    public void Parse_UnclosedDoubleQuote_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            CommandParser.Parse("git commit -m \"unclosed"));

        Assert.Contains("Unclosed double quote", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_UnclosedSingleQuote_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            CommandParser.Parse("echo 'unclosed"));

        Assert.Contains("Unclosed single quote", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_TrailingBackslashEscapingClosingQuote_ThrowsUnclosedQuote()
    {
        // When input is: echo "test\ (backslash escapes the closing quote)
        // This should throw unclosed quote since the backslash escapes the quote
        // Note: In C# string literal, \" is escaped quote, \\ is escaped backslash
        // So "echo \"test\\\"" becomes: echo "test\"
        var ex = Assert.Throws<ArgumentException>(() =>
            CommandParser.Parse("echo \"test\\\""));

        // The backslash escapes the closing quote, leaving it unclosed
        Assert.Contains("Unclosed double quote", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_EmptyCommand_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CommandParser.Parse(""));
        Assert.Throws<ArgumentException>(() => CommandParser.Parse("   "));
        Assert.Throws<ArgumentException>(() => CommandParser.Parse(null!));
    }

    [Fact]
    public void Parse_MultipleSpaces_CollapsesCorrectly()
    {
        // Act
        var (exe, args) = CommandParser.Parse("git    status    --short");

        // Assert
        Assert.Equal("git", exe);
        Assert.Equal(2, args.Length);
        Assert.Equal("status", args[0]);
        Assert.Equal("--short", args[1]);
    }

    [Fact]
    public void Parse_ExecutableOnly_ReturnsEmptyArgs()
    {
        // Act
        var (exe, args) = CommandParser.Parse("ls");

        // Assert
        Assert.Equal("ls", exe);
        Assert.Empty(args);
    }

    [Fact]
    public void Parse_QuotedExecutable_ParsesCorrectly()
    {
        // Act
        var (exe, args) = CommandParser.Parse("\"Program Files/app.exe\" --help");

        // Assert
        Assert.Equal("Program Files/app.exe", exe);
        Assert.Single(args);
        Assert.Equal("--help", args[0]);
    }

    [Fact]
    public void Parse_EscapedBackslash_ParsesCorrectly()
    {
        // Act - double backslash should result in single backslash
        var (exe, args) = CommandParser.Parse("echo \"path\\\\to\\\\file\"");

        // Assert
        Assert.Equal("echo", exe);
        Assert.Single(args);
        Assert.Equal("path\\to\\file", args[0]);
    }

    [Fact]
    public void Parse_BackslashNonSpecial_PreservesBackslash()
    {
        // Backslash followed by non-special character should preserve both
        var (exe, args) = CommandParser.Parse("echo \"path\\nvalue\"");

        Assert.Equal("echo", exe);
        Assert.Single(args);
        // \n is not a special escape, so both chars preserved
        Assert.Equal("path\\nvalue", args[0]);
    }

    [Fact]
    public void Parse_ComplexGitCommand_ParsesCorrectly()
    {
        // Real-world git command with complex message
        // In the C# string: \\n means literal backslash + n
        // The parser sees: \n which is NOT a special escape, so it stays as \n
        var (exe, args) = CommandParser.Parse(
            "git commit -m \"feat: Add new feature\\n\\nThis is a longer description.\"");

        Assert.Equal("git", exe);
        Assert.Equal(3, args.Length);
        Assert.Equal("commit", args[0]);
        Assert.Equal("-m", args[1]);
        // \n in input stays as \n (backslash + n), represented in C# as \\n
        Assert.Equal("feat: Add new feature\\n\\nThis is a longer description.", args[2]);
    }
}
