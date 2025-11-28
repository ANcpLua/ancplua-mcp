# Development Guide

## NuGet Dependency Management

This repository uses **Central Package Management (CPM)** with **per-project lock files**.

### Key Files

| File | Purpose |
|------|---------|
| `Directory.Packages.props` | Centralized package versions |
| `Directory.Build.props` | Enables lock files, locked restore in CI |
| `**/packages.lock.json` | Per-project lock files (committed) |

### How It Works

1. All package versions defined in `Directory.Packages.props`
2. `.csproj` files use `<PackageReference Include="..." />` without `Version`
3. Lock files generated per project and committed
4. CI enforces locked restore (fails if lock files would change)

### Developer Workflow

```bash
# Normal build
dotnet restore
dotnet build

# Adding/updating packages regenerates lock files automatically
# Commit the updated packages.lock.json with your changes
```

### Updating Dependencies

1. Update version in `Directory.Packages.props`
2. Run `dotnet restore` (lock files regenerate)
3. Run `dotnet build && dotnet test`
4. Commit `.props` changes AND updated `packages.lock.json` files

### CI Behavior

CI runs with `RestoreLockedMode=true`. If dependencies change without updated lock files, CI fails.

## Local Validation

Mirror CI locally:

```bash
./tooling/scripts/local-validate.sh
```

## Code Style

- C# 14 / .NET 10 patterns
- `ConfigureAwait(false)` on all async calls
- `CancellationToken` propagation
- CA analyzer compliance (see `.editorconfig`)
