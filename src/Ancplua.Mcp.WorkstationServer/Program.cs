using Ancplua.Mcp.WorkstationServer.Tools;

// WorkstationServer - MCP Server for stdio communication
// Exposes filesystem, git, and CI tools to MCP clients like Claude Desktop

Console.Error.WriteLine("WorkstationServer MCP started");

// Read stdin line by line for MCP protocol messages
string? line;
while ((line = Console.ReadLine()) != null)
{
    // Basic MCP protocol handling would go here
    // For now, this is a placeholder for the MCP protocol implementation
    Console.Error.WriteLine($"Received: {line}");
}

Console.Error.WriteLine("WorkstationServer MCP stopped");
