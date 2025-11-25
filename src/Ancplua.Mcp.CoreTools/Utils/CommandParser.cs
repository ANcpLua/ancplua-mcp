using System.Text;

namespace Ancplua.Mcp.CoreTools.Utils;

/// <summary>
/// Parses command strings into executable and arguments.
/// </summary>
/// <remarks>
/// <para>
/// Handles quoting (double and single quotes) for arguments containing spaces.
/// Supports escaped quotes within quoted strings using backslash (\").
/// </para>
/// <para>
/// For complex quoting scenarios, prefer using structured argument lists directly
/// via <see cref="ProcessRunner.RunAsync"/> instead of command strings.
/// </para>
/// </remarks>
public static class CommandParser
{
    /// <summary>
    /// Splits a command string into executable and arguments.
    /// </summary>
    /// <param name="command">The command string to parse.</param>
    /// <returns>A tuple of (executable, arguments array).</returns>
    /// <exception cref="ArgumentException">If the command is empty, null, or has unclosed quotes.</exception>
    /// <example>
    /// <code>
    /// var (exe, args) = CommandParser.Parse("git commit -m \"Fix bug\"");
    /// // exe = "git", args = ["commit", "-m", "Fix bug"]
    ///
    /// var (exe2, args2) = CommandParser.Parse("echo \"He said \\\"hello\\\"\"");
    /// // exe2 = "echo", args2 = ["He said \"hello\""]
    /// </code>
    /// </example>
    public static (string Executable, string[] Arguments) Parse(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            throw new ArgumentException("Command cannot be empty", nameof(command));
        }

        var parts = new List<string>();
        var current = new StringBuilder();
        var inDoubleQuotes = false;
        var inSingleQuotes = false;
        var escaped = false;

        for (var i = 0; i < command.Length; i++)
        {
            var c = command[i];

            // Handle escape sequences
            if (escaped)
            {
                // Inside quotes, only \" and \\ are special escape sequences
                // Outside quotes or for other characters, treat backslash literally
                if (c == '"' || c == '\\')
                {
                    current.Append(c);
                }
                else
                {
                    // Not a recognized escape, include both backslash and character
                    current.Append('\\');
                    current.Append(c);
                }
                escaped = false;
                continue;
            }

            // Check for escape character (backslash)
            if (c == '\\' && (inDoubleQuotes || inSingleQuotes || (i + 1 < command.Length && command[i + 1] == '"')))
            {
                escaped = true;
                continue;
            }

            switch (c)
            {
                case '"' when !inSingleQuotes:
                    // Toggle double quote state
                    if (!inDoubleQuotes)
                    {
                        inDoubleQuotes = true;
                    }
                    else
                    {
                        inDoubleQuotes = false;
                        // Allow empty strings by adding token even if empty when closing quotes
                        // This handles the case: command "" arg
                        if (current.Length == 0 && parts.Count > 0)
                        {
                            // We're closing an empty quoted string, add it
                            // Only if we're not at the start (executable can't be empty)
                        }
                    }
                    break;

                case '\'' when !inDoubleQuotes:
                    // Toggle single quote state (single quotes don't interpret escapes)
                    inSingleQuotes = !inSingleQuotes;
                    break;

                case ' ' when !inDoubleQuotes && !inSingleQuotes:
                    // Space outside quotes ends current token
                    if (current.Length > 0)
                    {
                        parts.Add(current.ToString());
                        current.Clear();
                    }
                    break;

                default:
                    current.Append(c);
                    break;
            }
        }

        // Check for unclosed quotes
        if (inDoubleQuotes)
        {
            throw new ArgumentException("Unclosed double quote in command", nameof(command));
        }
        if (inSingleQuotes)
        {
            throw new ArgumentException("Unclosed single quote in command", nameof(command));
        }
        if (escaped)
        {
            throw new ArgumentException("Trailing backslash in command", nameof(command));
        }

        // Add final token if any
        if (current.Length > 0)
        {
            parts.Add(current.ToString());
        }

        if (parts.Count == 0)
        {
            throw new ArgumentException("Command contains no tokens", nameof(command));
        }

        return (parts[0], parts.Skip(1).ToArray());
    }
}
