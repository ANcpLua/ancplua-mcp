# Spec-003: AI Services Integration via MCP

## Overview

This specification defines MCP tools for integrating multiple AI services (Claude, Jules, Gemini, ChatGPT, Copilot, CodeRabbit, Codecov) to enable cross-service communication and orchestration.

## Problem Statement

Multiple AI services are installed on the repository but cannot communicate with each other:
- Claude (CLI + GitHub App)
- Jules (Google AI)
- Gemini Code Assist
- ChatGPT Codex Connector
- GitHub Copilot
- CodeRabbit
- Codecov AI

Each service operates independently, creating silos instead of collaborative workflows.

## Proposed Solution

Create an **AI Services MCP Server** that provides tools for:

1. **Service Discovery** - List available AI services and their capabilities
2. **Inter-Service Messaging** - Send requests between AI services
3. **Workflow Orchestration** - Chain multiple AI services together
4. **Shared Context** - Maintain conversation context across services
5. **Result Aggregation** - Combine results from multiple AI services

## MCP Tools

### 1. Service Management

#### `ListAIServices`
```csharp
[McpServerTool]
[Description("Lists all configured AI services and their status")]
public static async Task<AIServiceInfo[]> ListAIServices()
```

**Returns:**
```json
[
  {
    "name": "claude",
    "type": "conversational",
    "status": "active",
    "capabilities": ["code-review", "generation", "refactoring"],
    "apiEndpoint": "https://api.anthropic.com"
  },
  {
    "name": "jules",
    "type": "task-automation",
    "status": "active",
    "capabilities": ["pr-review", "code-fixes", "cleanup"],
    "apiEndpoint": "https://jules.google.com/api"
  }
]
```

#### `GetServiceCapabilities`
```csharp
[McpServerTool]
[Description("Gets detailed capabilities of a specific AI service")]
public static async Task<ServiceCapabilities> GetServiceCapabilities(
    [Description("Service name (claude, jules, gemini, etc.)")] string serviceName)
```

### 2. Inter-Service Communication

#### `SendToService`
```csharp
[McpServerTool]
[Description("Sends a request to a specific AI service")]
public static async Task<ServiceResponse> SendToService(
    [Description("Target service name")] string serviceName,
    [Description("Request prompt or query")] string prompt,
    [Description("Optional context from other services")] string? context = null)
```

**Example Usage:**
```csharp
// Claude asks Jules to review code
var result = await SendToService(
    serviceName: "jules",
    prompt: "Review this PR for security issues",
    context: "Claude identified potential SQL injection in UserController.cs:42"
);
```

#### `BroadcastToServices`
```csharp
[McpServerTool]
[Description("Broadcasts a request to multiple AI services simultaneously")]
public static async Task<ServiceResponse[]> BroadcastToServices(
    [Description("Service names (comma-separated)")] string serviceNames,
    [Description("Request prompt")] string prompt)
```

### 3. Workflow Orchestration

#### `CreateAIWorkflow`
```csharp
[McpServerTool]
[Description("Creates a multi-service AI workflow")]
public static async Task<WorkflowDefinition> CreateAIWorkflow(
    [Description("Workflow name")] string name,
    [Description("Workflow steps in JSON")] string stepsJson)
```

**Example Workflow JSON:**
```json
{
  "name": "comprehensive-pr-review",
  "steps": [
    {
      "service": "claude",
      "action": "analyze-architecture",
      "input": "pr-diff"
    },
    {
      "service": "codecov",
      "action": "check-coverage",
      "input": "pr-files",
      "depends_on": []
    },
    {
      "service": "coderabbit",
      "action": "review-code-quality",
      "input": "pr-diff",
      "depends_on": []
    },
    {
      "service": "jules",
      "action": "suggest-fixes",
      "input": "{coderabbit.output}",
      "depends_on": ["coderabbit"]
    },
    {
      "service": "copilot",
      "action": "generate-tests",
      "input": "pr-files",
      "depends_on": ["codecov"]
    }
  ]
}
```

#### `ExecuteWorkflow`
```csharp
[McpServerTool]
[Description("Executes a defined AI workflow")]
public static async Task<WorkflowResult> ExecuteWorkflow(
    [Description("Workflow ID or name")] string workflowId,
    [Description("Input data for the workflow")] string inputJson)
```

### 4. Context Management

#### `CreateSharedContext`
```csharp
[McpServerTool]
[Description("Creates a shared context that multiple AI services can access")]
public static async Task<ContextId> CreateSharedContext(
    [Description("Context name")] string name,
    [Description("Initial data")] string data)
```

#### `UpdateSharedContext`
```csharp
[McpServerTool]
[Description("Updates shared context with new information from a service")]
public static async Task UpdateSharedContext(
    [Description("Context ID")] string contextId,
    [Description("Service name adding data")] string serviceName,
    [Description("Data to add")] string data)
```

#### `GetSharedContext`
```csharp
[McpServerTool]
[Description("Retrieves shared context for a service")]
public static async Task<ContextData> GetSharedContext(
    [Description("Context ID")] string contextId,
    [Description("Service requesting context")] string serviceName)
```

### 5. Result Aggregation

#### `AggregateResults`
```csharp
[McpServerTool]
[Description("Aggregates and synthesizes results from multiple AI services")]
public static async Task<AggregatedResult> AggregateResults(
    [Description("Result IDs to aggregate")] string[] resultIds,
    [Description("Aggregation strategy (merge, vote, prioritize)")] string strategy)
```

## Service Configurations

### Configuration File Structure

`config/ai-services.json`:
```json
{
  "services": {
    "claude": {
      "type": "anthropic",
      "apiKey": "${ANTHROPIC_API_KEY}",
      "model": "claude-sonnet-4.5",
      "capabilities": ["code-review", "generation", "refactoring", "analysis"]
    },
    "jules": {
      "type": "google-jules",
      "apiKey": "${JULES_API_KEY}",
      "capabilities": ["pr-review", "code-fixes", "cleanup", "refactoring"]
    },
    "gemini": {
      "type": "google-gemini",
      "apiKey": "${GEMINI_API_KEY}",
      "model": "gemini-3.0-pro",
      "capabilities": ["code-review", "generation", "analysis"]
    },
    "chatgpt": {
      "type": "openai",
      "apiKey": "${OPENAI_API_KEY}",
      "model": "gpt-4-turbo",
      "capabilities": ["code-review", "generation", "explanation"]
    },
    "copilot": {
      "type": "github-copilot",
      "authentication": "github-app",
      "capabilities": ["code-completion", "generation", "chat"]
    },
    "coderabbit": {
      "type": "github-app",
      "installation": "repository",
      "capabilities": ["pr-review", "code-quality", "security"]
    },
    "codecov": {
      "type": "codecov",
      "apiKey": "${CODECOV_API_KEY}",
      "capabilities": ["coverage-analysis", "test-quality"]
    }
  }
}
```

## Example Use Cases

### Use Case 1: Multi-Service PR Review
```csharp
// Create shared context for PR #22
var contextId = await CreateSharedContext(
    name: "pr-22-review",
    data: JsonSerializer.Serialize(new { prNumber = 22, branch = "fix/jules-automation" })
);

// Broadcast review request to all services
var results = await BroadcastToServices(
    serviceNames: "claude,coderabbit,codecov,jules",
    prompt: "Review PR #22 for code quality, security, and test coverage"
);

// Aggregate results
var summary = await AggregateResults(
    resultIds: results.Select(r => r.Id).ToArray(),
    strategy: "merge"
);
```

### Use Case 2: Code Generation Pipeline
```csharp
// 1. Copilot generates initial code
var code = await SendToService("copilot", "Generate UserService with CRUD operations");

// 2. Claude reviews architecture
var review = await SendToService("claude", $"Review this code: {code.Output}");

// 3. Jules applies fixes
var fixes = await SendToService("jules", $"Apply fixes: {review.Suggestions}");

// 4. CodeRabbit validates quality
var validation = await SendToService("coderabbit", $"Validate: {fixes.Output}");
```

### Use Case 3: Test Coverage Workflow
```csharp
var workflow = await CreateAIWorkflow(
    name: "improve-test-coverage",
    stepsJson: @"{
        ""steps"": [
            {""service"": ""codecov"", ""action"": ""analyze-coverage""},
            {""service"": ""claude"", ""action"": ""identify-untested-paths""},
            {""service"": ""copilot"", ""action"": ""generate-missing-tests""},
            {""service"": ""jules"", ""action"": ""review-test-quality""}
        ]
    }"
);

var result = await ExecuteWorkflow(workflow.Id, inputJson: "{\"branch\": \"main\"}");
```

## Implementation Details

### Project Structure
```
src/Ancplua.Mcp.AIServicesServer/
├── AIServicesServer.csproj
├── Program.cs
├── Tools/
│   ├── ServiceDiscoveryTools.cs      # ListAIServices, GetServiceCapabilities
│   ├── InterServiceTools.cs          # SendToService, BroadcastToServices
│   ├── WorkflowTools.cs              # CreateAIWorkflow, ExecuteWorkflow
│   ├── ContextTools.cs               # Shared context management
│   └── AggregationTools.cs           # Result aggregation
├── Services/
│   ├── IServiceConnector.cs          # Interface for service connectors
│   ├── ClaudeConnector.cs
│   ├── JulesConnector.cs
│   ├── GeminiConnector.cs
│   ├── ChatGPTConnector.cs
│   ├── CopilotConnector.cs
│   ├── CodeRabbitConnector.cs
│   └── CodecovConnector.cs
├── Models/
│   ├── AIServiceInfo.cs
│   ├── ServiceResponse.cs
│   ├── WorkflowDefinition.cs
│   └── ContextData.cs
└── Config/
    └── ai-services.json
```

### Dependencies
```xml
<ItemGroup>
  <PackageReference Include="ModelContextProtocol" />
  <PackageReference Include="Microsoft.Extensions.Configuration.Json" />
  <PackageReference Include="Anthropic.SDK" />              <!-- Claude API -->
  <PackageReference Include="Google.Cloud.AIPlatform.V1" /> <!-- Gemini API -->
  <PackageReference Include="OpenAI" />                     <!-- ChatGPT API -->
  <PackageReference Include="Octokit" />                    <!-- GitHub Apps -->
</ItemGroup>
```

## Security Considerations

1. **API Key Management**
   - All API keys stored in environment variables
   - Never logged or exposed in responses
   - Rotation policy: 90 days

2. **Service Authentication**
   - OAuth2 for GitHub Apps (Copilot, CodeRabbit)
   - API keys for Claude, Jules, Gemini, ChatGPT
   - Token-based auth for Codecov

3. **Rate Limiting**
   - Respect each service's rate limits
   - Implement exponential backoff
   - Queue requests when necessary

4. **Data Privacy**
   - Code never sent to services without explicit permission
   - Shared context encrypted at rest
   - Automatic cleanup of old contexts (7 days)

## Testing Strategy

1. **Unit Tests**
   - Mock each service connector
   - Test workflow execution logic
   - Validate context management

2. **Integration Tests**
   - Real API calls to each service (using test accounts)
   - End-to-end workflow tests
   - Service failure handling

3. **Performance Tests**
   - Concurrent service calls
   - Large result aggregation
   - Workflow timeout handling

## Future Enhancements

1. **Service Health Monitoring**
   - Track service uptime and response times
   - Automatic failover to alternative services
   - Health dashboard

2. **Learning from Results**
   - Store successful workflows
   - Recommend workflows based on patterns
   - Auto-optimize service selection

3. **Custom Service Plugins**
   - Allow adding new AI services dynamically
   - Plugin SDK for service connectors
   - Community marketplace

## Status

- **Status**: Proposed
- **Target Version**: 0.2.0
- **Dependencies**: Spec-001 (MCP SDK Integration)
- **Author**: Claude + ANcpLua
- **Date**: 2025-11-22

## References

- [Anthropic Claude API](https://docs.anthropic.com/claude/reference)
- [Google Jules API](https://jules.google.com/docs)
- [Google Gemini API](https://ai.google.dev/docs)
- [OpenAI API](https://platform.openai.com/docs/api-reference)
- [GitHub Apps](https://docs.github.com/en/apps)
- [Codecov API](https://docs.codecov.com/reference)
