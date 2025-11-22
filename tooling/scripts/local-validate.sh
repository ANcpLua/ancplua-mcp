#!/usr/bin/env bash
set -euo pipefail

echo "🔍 Running ancplua-mcp local validation..."

# Restore and build all projects
echo "➡️  dotnet restore"
dotnet restore

echo "➡️  dotnet build (Release)"
dotnet build --configuration Release --no-restore

# Run tests if any test projects exist
if ls tests/*.sln tests/*/*.csproj >/dev/null 2>&1; then
  echo "➡️  dotnet test (Release)"
  dotnet test --configuration Release --no-build
else
  echo "ℹ️  No test projects found under tests/. Skipping dotnet test."
fi

# Optional: lint shell scripts if shellcheck is available
if command -v shellcheck >/dev/null 2>&1; then
  echo "➡️  shellcheck on tooling/scripts/*.sh"
  shopt -s nullglob || true
  SCRIPTS=(tooling/scripts/*.sh)
  if [ "${#SCRIPTS[@]}" -gt 0 ]; then
    shellcheck "${SCRIPTS[@]}"
  else
    echo "ℹ️  No shell scripts found under tooling/scripts/. Skipping shellcheck."
  fi
else
  echo "⚠️  'shellcheck' not found. Skipping shell script checks."
fi

# Optional: lint markdown if markdownlint is available
if command -v markdownlint >/dev/null 2>&1; then
  echo "➡️  markdownlint on docs/**/*.md and root *.md"
  markdownlint "docs/**/*.md" "*.md" || true
else
  echo "⚠️  'markdownlint' not found. Skipping markdown checks."
fi

echo "✅ ancplua-mcp local validation completed."
