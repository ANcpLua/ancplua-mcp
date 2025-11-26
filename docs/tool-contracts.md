# MCP Tool Contracts

This document defines the public API contracts for all MCP tools exposed by ancplua-mcp servers.
Tool names and signatures are **public API** - breaking changes require an ADR.

**Last Updated**: 2025-11-25

---

## Table of Contents

- [DebugTools (Shared)](#debugtools-shared)
- [GitHubAppsServer Tools](#githubappsserver-tools)
- [AIServicesServer Tools](#aiservicesserver-tools)
- [RoslynMetricsServer Tools](#roslynmetricsserver-tools)
- [WorkstationServer Tools](#workstationserver-tools)
- [HttpServer Tools](#httpserver-tools)
- [Side Effect Legend](#side-effect-legend)

---

## DebugTools (Shared)

Debug and introspection tools available in all servers.

### debug_print_env

Returns all environment variables with sensitive values masked.

- **Input**: None
- **Output**: `EnvironmentInfo`
  ```json
  {
    "variables": { "PATH": "/usr/bin:...", "API_KEY": "***MASKED***" },
    "totalCount": 42,
    "maskedCount": 3
  }
  ```
- **Side effects**: None (read-only)
- **Availability**: All servers

### debug_get_server_info

Returns server metadata including version, transport type, and runtime info.

- **Input**: None
- **Output**: `ServerInfo`
  ```json
  {
    "serverName": "Ancplua.Mcp.WorkstationServer",
    "version": "1.0.0",
    "transport": "stdio",
    "dotNetVersion": "10.0.0",
    "operatingSystem": "Unix 15.1.0",
    "processorCount": 8,
    "workingDirectory": "/path/to/repo",
    "uptime": "00:05:23.1234567",
    "processId": 12345
  }
  ```
- **Side effects**: None (read-only)
- **Availability**: All servers

### debug_get_http_headers

Returns HTTP request headers. Only available on HTTP transport.

- **Input**: None
- **Output**: `HttpContextInfo`
  ```json
  {
    "available": true,
    "headers": { "Content-Type": "application/json", "Authorization": "***MASKED***" },
    "method": "POST",
    "path": "/mcp/v1/tools",
    "queryString": "?foo=bar",
    "remoteIpAddress": "127.0.0.1"
  }
  ```
  On stdio transport:
  ```json
  {
    "available": false,
    "message": "No HTTP context available. This tool requires HTTP transport."
  }
  ```
- **Side effects**: None (read-only)
- **Availability**: HTTP servers (gracefully degrades on stdio)

### debug_get_user_claims

Returns authenticated user claims. Only available on HTTP transport with authentication.

- **Input**: None
- **Output**: `UserClaimsInfo`
  ```json
  {
    "available": true,
    "isAuthenticated": true,
    "authenticationType": "Bearer",
    "claims": { "sub": "user123", "email": "user@example.com" }
  }
  ```
  When not authenticated:
  ```json
  {
    "available": true,
    "isAuthenticated": false,
    "message": "User is not authenticated."
  }
  ```
- **Side effects**: None (read-only)
- **Availability**: HTTP servers (gracefully degrades on stdio)

### debug_get_all

Returns all debug information in a single call.

- **Input**: None
- **Output**: Combined JSON with `environment`, `server`, `httpContext`, `userClaims`
- **Side effects**: None (read-only)
- **Availability**: All servers

---

## GitHubAppsServer Tools

Tools for interacting with GitHub Apps and AI code review services.

### GeminiCodeAssistTools

Tools for interacting with Gemini Code Assist.

| Tool | Parameters | Return Type | Side Effects |
|------|------------|-------------|--------------|
| `InvokeGeminiReview` | `owner: string`, `repo: string`, `prNumber: int` | `Task<string>` | Read-only (returns instructions) |
| `ConfigureGemini` | `owner: string`, `repo: string` | `Task<string>` | Read-only (returns instructions) |

### JulesTools

Tools for interacting with Jules (Google Labs AI).

| Tool | Parameters | Return Type | Side Effects |
|------|------------|-------------|--------------|
| `InvokeJules` | `owner: string`, `repo: string`, `prNumber: int`, `request?: string` | `Task<string>` | Read-only (returns instructions) |
| `CheckJulesConfig` | `owner: string`, `repo: string` | `Task<string>` | Read-only (returns instructions) |

### AIOrchestrationTools

Tools for orchestrating multiple AI code review services.

| Tool | Parameters | Return Type | Side Effects |
|------|------------|-------------|--------------|
| `TriggerAllReviewers` | `owner: string`, `repo: string`, `prNumber: int` | `Task<string>` | Read-only (returns instructions) |
| `GetAiReviewSummary` | `owner: string`, `repo: string`, `prNumber: int` | `Task<string>` | Read-only (returns instructions) |
| `CompareAiReviewers` | (none) | `Task<string>` | Read-only |

### CodeRabbitTools

Tools for interacting with CodeRabbit AI.

| Tool | Parameters | Return Type | Side Effects |
|------|------------|-------------|--------------|
| `TriggerCodeRabbitReview` | `owner: string`, `repo: string`, `prNumber: int` | `Task<string>` | Read-only (returns instructions) |
| `AskCodeRabbit` | `owner: string`, `repo: string`, `prNumber: int`, `question: string` | `Task<string>` | Read-only (returns instructions) |

### CodecovTools

Tools for interacting with Codecov and Codecov AI.

| Tool | Parameters | Return Type | Side Effects |
|------|------------|-------------|--------------|
| `GetCoverage` | `owner: string`, `repo: string`, `branch?: string` | `Task<string>` | External API call (Codecov API) |
| `TriggerCodecovAiReview` | `owner: string`, `repo: string`, `prNumber: int` | `Task<string>` | Read-only (returns instructions) |

---

## AIServicesServer Tools

### ServiceDiscoveryTools

MCP tools for discovering and querying AI services.

| Tool | Parameters | Return Type | Side Effects |
|------|------------|-------------|--------------|
| `ListAiServices` | (none) | `Task<string>` (JSON) | Read-only |
| `GetServiceCapabilities` | `serviceName: string` | `Task<string>` (JSON) | Read-only |

**Supported Services**: claude, jules, gemini, chatgpt, copilot, coderabbit, codecov

**ListAiServices Response Schema**:
```json
[{
  "name": "claude",
  "type": "conversational",
  "status": "active",
  "capabilities": ["code-review", "generation", "refactoring", "analysis"],
  "apiEndpoint": "https://api.anthropic.com",
  "description": "Anthropic Claude AI assistant"
}]
```

---

## RoslynMetricsServer Tools

### NuGetTools

Tools for searching and querying NuGet packages.

| Tool | Parameters | Return Type | Side Effects |
|------|------------|-------------|--------------|
| `SearchAsync` | `query: string`, `take: int = 20`, `source?: string` | `Task<object>` (JSON array) | External API call (NuGet) |
| `GetVersionsAsync` | `id: string`, `includePrerelease: bool = true`, `source?: string` | `Task<object>` (JSON) | External API call (NuGet) |
| `GetLatestAsync` | `id: string`, `includePrerelease: bool = true`, `source?: string` | `Task<object>` (JSON) | External API call (NuGet) |

**SearchAsync Response Schema**:
```json
[{
  "id": "Newtonsoft.Json",
  "version": "13.0.3",
  "description": "Json.NET is a popular high-performance JSON framework",
  "authors": "James Newton-King",
  "totalDownloads": 1234567890
}]
```

### ArchitectureTools

Tools for analyzing C# project architecture.

| Tool | Parameters | Return Type | Side Effects |
|------|------------|-------------|--------------|
| `AnalyzeProjectArchitecture` | `files: FileSpec[]`, `projectName: string = "AnalysisProject"` | `Task<object>` | Read-only (Roslyn analysis) |

**FileSpec**: `{ name: string, code: string }`

**Response Schema**:
```json
{
  "project": "AnalysisProject",
  "impactedCount": 0,
  "isApp": false,
  "outputKind": "DynamicallyLinkedLibrary",
  "types": 5
}
```

### RoslynMetricsTools

Tools for analyzing code metrics using Roslyn.

| Tool | Parameters | Return Type | Side Effects |
|------|------------|-------------|--------------|
| `AnalyzeCSharp` | `code: string`, `assemblyName?: string` | `Task<object>` | Read-only |
| `GenerateCSharpReport` | `code: string`, `assemblyName?: string` | `Task<string>` (Markdown) | Read-only |
| `AnalyzeVb` | `code: string`, `assemblyName?: string` | `Task<object>` | Read-only |
| `QueryMetrics` | `code: string`, `minComplexity?: int`, `maxComplexity?: int`, `minMaintainability?: int`, `kind?: string`, `take: int = 25` | `Task<string>` (Markdown table) | Read-only |

**AnalyzeCSharp Response Schema**:
```json
{
  "symbol": "Assembly",
  "complexity": 10,
  "maintainability": 85,
  "sourceLines": 150,
  "methods": 12,
  "types": 3,
  "namespaces": 1,
  "summary": "Assembly: 3 types, 12 methods, MI=85, CC=10"
}
```

---

## WorkstationServer Tools

Tools for local development workstation operations (stdio transport).

### GitTools

| Tool | Parameters | Return Type | Side Effects |
|------|------------|-------------|--------------|
| `GetStatusAsync` | `repositoryPath?: string` | `Task<string>` | Read-only (git status) |
| `GetLogAsync` | `repositoryPath?: string`, `maxCount: int = 10` | `Task<string>` | Read-only (git log) |
| `GetDiffAsync` | `repositoryPath?: string` | `Task<string>` | Read-only (git diff) |
| `ListBranchesAsync` | `repositoryPath?: string` | `Task<string>` | Read-only (git branch) |
| `GetCurrentBranchAsync` | `repositoryPath?: string` | `Task<string>` | Read-only |
| `AddAsync` | `files: string`, `repositoryPath?: string` | `Task` | **Mutating** (git add) |
| `CommitAsync` | `message: string`, `repositoryPath?: string` | `Task` | **Mutating** (git commit) |

### FileSystemTools

| Tool | Parameters | Return Type | Side Effects |
|------|------------|-------------|--------------|
| `ReadFileAsync` | `path: string` | `Task<string>` | Read-only |
| `WriteFileAsync` | `path: string`, `content: string` | `Task` | **Mutating** (file write) |
| `ListDirectory` | `path: string` | `IEnumerable<string>` | Read-only |
| `DeleteFile` | `path: string` | `void` | **Mutating** (file delete) |
| `CreateDirectory` | `path: string` | `void` | **Mutating** (directory create) |
| `FileExists` | `path: string` | `bool` | Read-only |
| `DirectoryExists` | `path: string` | `bool` | Read-only |

### CiTools

| Tool | Parameters | Return Type | Side Effects |
|------|------------|-------------|--------------|
| `BuildAsync` | `projectPath?: string` | `Task<string>` | Shell execution (dotnet build) |
| `RunTestsAsync` | `projectPath?: string` | `Task<string>` | Shell execution (dotnet test) |
| `RestoreAsync` | `projectPath?: string` | `Task<string>` | Shell execution (dotnet restore) |
| `RunCommandAsync` | `command: string`, `workingDirectory?: string` | `Task<string>` | **Shell execution** (arbitrary command) |
| `GetDiagnostics` | (none) | `string` | Read-only |

### WhisperAggregatorTools

WhisperMesh aggregation tools for multi-agent collaboration (Dolphin Pod workflow).

#### AggregateDiscoveries

Aggregates and deduplicates WhisperMesh discoveries from multiple agents.

- **Input**: `AggregationRequestDto`
  ```json
  {
    "tiers": ["lightning", "storm"],
    "topicPatterns": ["security.*", "architecture", "code-quality"],
    "timeWindowMinutes": 5,
    "minSeverity": 0.5,
    "maxDiscoveries": 1000
  }
  ```
  - `tiers`: Array of tier names (`lightning`, `storm`, or both)
  - `topicPatterns`: Array of NATS topic patterns (supports wildcards: `*` and `>`)
  - `timeWindowMinutes`: Time window to collect discoveries (default: 5)
  - `minSeverity`: Minimum severity threshold 0.0-1.0 (default: 0.0)
  - `maxDiscoveries`: Maximum discoveries to collect (default: 1000)

- **Output**: `AggregatedWhisperReportDto`
  ```json
  {
    "discoveries": [
      {
        "messageId": "uuid",
        "agent": "ARCH-Agent",
        "tier": "lightning",
        "topic": "architecture",
        "severity": 0.95,
        "message": "Missing ADR for tool contract change",
        "discovery": { "type": "ArchitectureViolation", "location": {...}, "rule": "ADR-006", ... },
        "metadata": { "project": "ancplua-mcp", "commit": "abc123", ... },
        "timestamp": "2025-11-25T23:00:00Z"
      }
    ],
    "totalCount": 15,
    "deduplicatedCount": 12,
    "lightningCount": 8,
    "stormCount": 4,
    "criticalCount": 3,
    "highCount": 5,
    "mediumCount": 3,
    "lowCount": 1,
    "agentCounts": {
      "ARCH-Agent": 5,
      "IMPL-Agent": 4,
      "Security-Agent": 3
    },
    "aggregatedAt": "2025-11-25T23:05:00Z",
    "timeWindowMinutes": 5
  }
  ```
  - `discoveries`: Deduplicated and sorted discoveries (Lightning tier first, then by severity descending)
  - `totalCount`: Total discoveries collected before deduplication
  - `deduplicatedCount`: Discoveries after deduplication
  - `lightningCount`: Number of Lightning tier discoveries
  - `stormCount`: Number of Storm tier discoveries
  - `criticalCount`: Severity >= 0.8
  - `highCount`: 0.6 <= severity < 0.8
  - `mediumCount`: 0.4 <= severity < 0.6
  - `lowCount`: Severity < 0.4
  - `agentCounts`: Discovery count per agent
  - `aggregatedAt`: Timestamp when aggregation completed
  - `timeWindowMinutes`: Time window used for aggregation

- **Side effects**: External system (NATS JetStream subscription)
- **Availability**: WorkstationServer
- **Use case**: Dolphin Pod orchestrator aggregates findings from ARCH, IMPL, and Security agents

**Deduplication Logic:**
- Discoveries with the same `CodeLocation` (file + line) and category are deduplicated
- The discovery with the highest severity is kept
- Unique key format: `{file}:{line}:{category}`

**Sorting Order:**
1. Tier (Lightning before Storm)
2. Severity (descending: 1.0 → 0.0)

---

## HttpServer Tools

Mirror of WorkstationServer tools accessible via HTTP transport.
Same contracts as WorkstationServer plus HTTP context in DebugTools.

---

## Side Effect Legend

| Category | Description |
|----------|-------------|
| Read-only | No external effects, safe to call repeatedly |
| **Mutating** | Modifies local state (files, git index) |
| External API call | Makes HTTP request to external service |
| Shell execution | Spawns subprocess (dotnet, git, etc.) |

---

## Masked Environment Variable Patterns

The following patterns trigger value masking in `debug_print_env`:

```
TOKEN, KEY, SECRET, PASSWORD, CREDENTIAL
API_KEY, APIKEY, PRIVATE, AUTH
CONNECTION_STRING, CONNECTIONSTRING
```

---

**Related**: [spec-005](specs/spec-005-debug-mcp-tools.md), [ADR-003](decisions/adr-003-debug-mcp-tools.md)
