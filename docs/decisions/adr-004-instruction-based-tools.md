# ADR-001: Instruction-Based Tools vs API Integration

## Status
**ACCEPTED** (Temporary - will be superseded by ADR-003 in v2.0)

## Context

The AIServicesServer exposes tools for orchestrating GitHub Apps (Gemini Code Assist, CodeRabbit, Jules, Codecov AI, GitHub Copilot). We had two implementation options:

### Option A: API-Integrated Tools (Full Automation)
- Make actual API calls to GitHub, Codecov, etc.
- Authenticate via tokens/OAuth
- Programmatically trigger reviews and fetch results
- Full automation layer

**Advantages**:
- True automation
- Programmatic verification of results
- Result aggregation possible
- Professional production-ready implementation

**Disadvantages**:
- Requires Octokit.NET, REST clients, OAuth flows
- Complex token management
- Rate limiting complexity
- Longer development time (2-3 weeks)
- Higher testing requirements

### Option B: Instruction-Based Tools (Educational Guidance)
- Return instruction strings explaining how to use GitHub Apps
- No actual API calls
- Users manually execute suggested commands
- Serves as educational/guidance layer

**Advantages**:
- Fast MVP delivery (1-2 days)
- No API authentication complexity
- Works across all platforms without tokens
- Educational value for users learning GitHub Apps
- Lower testing requirements

**Disadvantages**:
- Not true automation
- Users must manually execute suggestions
- Cannot verify results programmatically
- Less professional perception

## Decision

**We will implement Option B (Instruction-Based Tools) for v1.0**, with a clear migration path to Option A (API Integration) in v2.0.

## Rationale

1. **Time to Value**: Instruction-based tools can be shipped immediately, providing value while API integration is developed
2. **Educational Value**: Many users don't know how to trigger GitHub Apps - instructions teach correct usage
3. **Platform Independence**: Works without any tokens or authentication setup
4. **Clear Migration Path**: Tool signatures remain the same - only implementation changes in v2.0
5. **Risk Mitigation**: Start simple, add complexity incrementally

## Consequences

### Positive
- ✅ Quick deployment and user feedback
- ✅ No API token management required
- ✅ Works across all platforms immediately
- ✅ Educational value for GitHub Apps beginners
- ✅ Validates tool contracts before investing in API integration

### Negative
- ❌ Not true automation (requires manual steps)
- ❌ Users must manually execute suggestions
- ❌ Cannot programmatically verify results
- ❌ May be perceived as incomplete

### Neutral
- ⚠️ Temporary implementation - will be replaced in v2.0
- ⚠️ Sets expectation that v1.0 is guidance-focused

## Implementation Details

### Example: TriggerAllReviewers (v1.0)

```csharp
[McpServerTool]
[Description("Trigger all AI reviewers on a pull request")]
public static Task<string> TriggerAllReviewers(
    [Description("Repository owner")] string owner,
    [Description("Repository name")] string repo,
    [Description("Pull request number")] int prNumber)
{
    return Task.FromResult($@"
To invoke all AI reviewers on PR #{prNumber} in {owner}/{repo}:

1. **Gemini Code Assist**: Add comment '@gemini-code-assist'
2. **CodeRabbit**: Add comment '@coderabbitai review'
3. **Jules**: Add comment '/jules-review'
4. **Codecov AI**: Add comment '@codecov-ai-reviewer review'

Visit: https://github.com/{owner}/{repo}/pull/{prNumber}
    ");
}
```

## Migration Plan to v2.0

### Phase 1: Documentation (Current)
- [x] Document instruction-based approach
- [x] Create ADR-001
- [x] Update ROADMAP.md

### Phase 2: API Integration Design
- [ ] Create API_INTEGRATION.md specification
- [ ] Design tool contracts for API calls
- [ ] Plan authentication strategy

### Phase 3: Implementation
- [ ] Add Octokit.NET dependency
- [ ] Implement GitHub API client wrapper
- [ ] Convert tools to API calls
- [ ] Add rate limiting and error handling

### Phase 4: Testing & Release
- [ ] Comprehensive testing
- [ ] Security review
- [ ] Release v2.0

## Versioning Strategy

- **v1.0**: Instruction-based tools (current)
- **v2.0**: API-integrated tools (planned)
- **Tool Signatures**: Remain unchanged (same parameters, compatible response format)

## User Communication

Documentation must clearly state:
- v1.0 tools return instructions (not automated)
- v2.0 will provide full API integration
- Tool signatures will remain compatible

## References

- GitHub API Documentation: https://docs.github.com/en/rest
- Octokit.NET: https://octokitnet.readthedocs.io/
- ROADMAP.md: Phase 3 (API Integration)
- API_INTEGRATION.md: Technical specifications

## Review Date

This decision will be reviewed when starting Phase 3 (API Integration) development, estimated 2-3 weeks from v1.0 release.
