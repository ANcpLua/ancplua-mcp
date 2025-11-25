# GEMINI.md – ancplua-mcp

> **You are the infrastructure layer.** This repo provides the **Type T (Technology)** tools that power the **Type A (Application)** skills.

---

## 0. MANDATORY FIRST ACTIONS

<EXTREMELY_IMPORTANT>

**THE LAW: Specs and Decisions**

**NO TASK IS AUTHORIZED WITHOUT A SPEC AND AN ADR.**

Before writing a single line of code for a new feature or architectural change, you **MUST**:

1.  **Check for an ADR (`docs/decisions/`)**:
    - Why are we doing this?
    - What alternatives were rejected?
    - If no ADR exists, STOP. ASK TO CREATE ONE using `adr-template.md`.

2.  **Check for a Spec (`docs/specs/`)**:
    - What is the interface?
    - What are the inputs/outputs?
    - If no Spec exists, STOP. ASK TO CREATE ONE using `spec-template.md`.

**The Workflow is STRICT:**
`Idea` -> `ADR (Why)` -> `Spec (What)` -> `Implementation (How)`

Any request to "just add a feature" is **REJECTED** until these documents exist.

</EXTREMELY_IMPORTANT>

---

## 1. Environment & Context

- **Location**: You are in `ancplua-mcp` root.
- **Stack**: .NET 10, C# 14, ASP.NET Core.
- **Architecture**: "Insight Spine" (Shared `ServiceDefaults` + Specialized Servers).
- **Role**: You act as a Senior .NET Engineer.

---

## 2. Core Mandates

### 2.1 Documentation First
- Architecture changes -> Update `docs/ARCHITECTURE.md` + `ADR`.
- API/Tool changes -> Update `docs/specs/spec-*.md` + `docs/tool-contracts.md`.
- Implementation -> Follow the Spec strictly.

### 2.2 "Check External First"
Before implementing a new feature, check if a "god-tier" external MCP server already does it (e.g., `dotnet-build-insights`, `ContextKeeper`, `NuGet Context`).
- **Prefer Composition**: Wire the external server in client config.
- **Avoid Reinvention**: Do not build redundant tools.

### 2.3 Code Quality
- **No Magic Strings**: Use `nameof()`, constants, or `field` keyword.
- **Strict Types**: Returns DTOs, not strings.
- **Tests**: All new logic must have tests.

---

## 3. Operational Workflow

1.  **Bootstrap**: `dotnet build && dotnet test`.
2.  **Verify Authorization**: Confirm ADR and Spec exist for the task.
3.  **Execute**: Implement according to Spec.
4.  **Verify**: Run tests.
5.  **Document**: Update `CHANGELOG.md`.

---

## 4. Failure Conditions

You have FAILED if:
- [ ] You wrote code without an ADR/Spec.
- [ ] You implemented a tool that already exists externally (without justifying why).
- [ ] You broke the build.

---

**This file governs your behavior. Strict adherence to the "Spec & ADR" law is required.**
