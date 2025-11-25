# ADR-002: Docker MCP Registry Submission Timing

## Status
**ACCEPTED**

## Context

The Docker MCP Registry (https://github.com/docker/mcp-registry) offers official distribution for MCP servers with significant benefits:

### Benefits of Docker MCP Registry
- 🐳 **Docker-built images** with cryptographic signatures
- 📋 **Provenance tracking** and SBOMs (Software Bill of Materials)
- 🔄 **Automatic security updates** maintained by Docker
- 🌍 **Global distribution** via Docker Hub CDN
- 📦 **Official listing** in Docker Desktop MCP Toolkit
- ✅ **Trust and credibility** from Docker's review process

### Submission Requirements (from CONTRIBUTING.md)
1. Working Dockerfile in source repository
2. Production-ready implementation
3. Comprehensive documentation
4. Tools must be functional (no placeholder/instruction stubs)
5. Security review passed
6. SBOM and vulnerability scanning

## Decision

**WAIT until Phase 4 completion (v2.0 release) before submitting to docker/mcp-registry.**

Do NOT submit while AIServicesServer uses instruction-based tools (v1.0).

## Rationale

### Technical Reasons

1. **Instruction Stubs Are Not Production-Ready**
   - Current tools return guidance strings, not actual functionality
   - Docker expects working tools that perform real operations
   - Submitting instruction-based tools would be misleading

2. **Testing Requirements**
   - Docker expects >80% test coverage
   - Current implementation has minimal tests
   - Need comprehensive integration tests with real APIs

3. **Documentation Requirements**
   - Need complete API documentation
   - User guides for API-integrated features
   - Security guidelines for token management

4. **Security Concerns**
   - API integration requires token management
   - Need security review before public distribution
   - Must demonstrate secure secret handling

### Strategic Reasons

1. **Reputation Risk**
   - Submitting incomplete work damages credibility
   - Docker's review process expects production quality
   - Better to wait and submit excellent work than rush

2. **Review Process Overhead**
   - Docker team will request changes for instruction-based tools
   - Multiple review cycles waste everyone's time
   - One submission with complete work is more efficient

3. **User Expectations**
   - Users expect MCP servers from Docker registry to "just work"
   - Instruction-based tools may disappoint users
   - Better user experience when we have real automation

## Requirements Before Submission

### Mandatory (MUST COMPLETE)
- [ ] **Full API Integration** - No instruction stubs remaining
- [ ] **Test Coverage >80%** - Comprehensive unit + integration tests
- [ ] **Complete Documentation** - README, API docs, user guides
- [ ] **Security Review** - Secrets management, input validation
- [ ] **Dockerfile** - Multi-stage build, security best practices
- [ ] **SBOM Generation** - Software bill of materials

### Recommended (SHOULD COMPLETE)
- [ ] Performance benchmarks
- [ ] Load testing results
- [ ] User acceptance testing
- [ ] Production deployment examples

## Timeline

### Current State (Week 3)
- ✅ Phase 1 complete: Basic infrastructure
- 🔄 Phase 2 in progress: Documentation

### Before Submission
- Week 4-5: Phase 3 (API Integration)
- Week 6: Phase 4 (Testing)
- Week 7-10: Phase 5 (Docker Registry submission + review)

**Estimated Submission Date**: 6-8 weeks from now

## Consequences

### Positive
- ✅ Submit high-quality, production-ready work
- ✅ Pass Docker review process smoothly
- ✅ Users get fully functional tools immediately
- ✅ Avoid reputation risk from incomplete work
- ✅ Better documentation and testing

### Negative
- ❌ Delayed official distribution (6-8 weeks)
- ❌ Users must build from source in meantime
- ❌ No automatic security updates until listed

### Neutral
- ⚠️ v1.0 users can still use via direct Docker build
- ⚠️ No dependency on Docker registry for development

## Alternative Distribution (Interim)

While waiting for registry submission, we can:

### GitHub Container Registry (GHCR)
```bash
docker pull ghcr.io/ancplua/ancplua-mcp:latest
```
**Advantages**:
- Automated via GitHub Actions
- Free for public repositories
- No approval process

### Docker Hub (Personal Namespace)
```bash
docker pull ancplua/ancplua-mcp:latest
```
**Advantages**:
- Familiar to users
- Free tier available

### Direct Build from Source
```bash
git clone https://github.com/ANcpLua/ancplua-mcp
cd ancplua-mcp
docker build -t ancplua-mcp .
```

## Submission Process (When Ready)

### Step 1: Pre-Submission Checklist
- [ ] All requirements met (see above)
- [ ] Local testing with `task wizard` (from mcp-registry)
- [ ] Catalog generation successful
- [ ] Docker Desktop MCP Toolkit import works

### Step 2: Fork and Prepare
```bash
cd /Users/ancplua/mcp-registry  # Already forked
mkdir -p servers/ancplua-ai-services
# Create server.yaml, tools.json, readme.md
```

### Step 3: Submit PR
```bash
git checkout -b add-ancplua-ai-services
git add servers/ancplua-ai-services/
git commit -m "feat: add ancplua AI Services MCP server"
git push origin add-ancplua-ai-services
# Create PR on GitHub
```

### Step 4: Share Test Credentials
Fill out: https://forms.gle/6Lw3nsvu2d6nFg8e6
(Required for Docker team to test)

### Step 5: Address Review Feedback
- Respond to Docker team comments
- Make requested changes
- Update PR

### Step 6: Approval & Publication
- Docker team merges PR
- Images published to `mcp/ancplua-ai-services` within 24 hours
- Available in Docker Desktop MCP Toolkit

## Monitoring Criteria

We will know we're ready when:
- ✅ `dotnet test` shows >80% coverage
- ✅ All tools make real API calls (no instruction stubs)
- ✅ Security review completed with no critical findings
- ✅ Documentation is comprehensive and accurate
- ✅ Local `task build` and `task catalog` succeed

## Review Date

This decision will be reviewed at the start of Phase 5 (Docker Registry Submission), estimated 6-8 weeks from now.

## References

- Docker MCP Registry: https://github.com/docker/mcp-registry
- Contributing Guide: https://github.com/docker/mcp-registry/blob/main/CONTRIBUTING.md
- ROADMAP.md: Phase 5 (Docker Registry Submission)
- /Users/ancplua/mcp-registry: Local fork (already prepared)

## Success Metrics

**Submission Success**:
- PR accepted within 2 weeks
- Docker review completed with minimal changes requested
- Published to registry within 24 hours of approval

**Post-Publication Success**:
- >100 pulls within first week
- Positive user feedback
- No critical bugs reported
- Featured in Docker Desktop catalog
