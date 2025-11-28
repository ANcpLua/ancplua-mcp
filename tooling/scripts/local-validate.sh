#!/usr/bin/env bash
set -euo pipefail

DUAL_MODE=false
SIBLING_REPO="$HOME/WebstormProjects/ancplua-claude-plugins"

# Parse arguments
for arg in "$@"; do
  case $arg in
    --dual)
      DUAL_MODE=true
      shift
      ;;
  esac
done

echo "🔍 Running ancplua-mcp local validation..."

# Tooling sanity: show SDK info and ensure we're on the pinned SDK
echo "➡️  dotnet --info"
dotnet --info

if command -v dotnet >/dev/null 2>&1 && dotnet sdk check >/dev/null 2>&1; then
  echo "➡️  dotnet sdk check"
  dotnet sdk check
fi

# Restore tools (if present)
if [ -f "dotnet-tools.json" ] || [ -f ".config/dotnet-tools.json" ]; then
  echo "➡️  dotnet tool restore"
  dotnet tool restore
fi

# Restore and build solution
echo "➡️  dotnet restore ancplua-mcp.sln"
dotnet restore ancplua-mcp.sln

echo "➡️  dotnet format --verify-no-changes"
dotnet format --verify-no-changes --no-restore ancplua-mcp.sln || {
  echo "❌ Formatting issues detected. Run 'dotnet format ancplua-mcp.sln' to fix." >&2
  exit 1
}

echo "➡️  dotnet build ancplua-mcp.sln (Release, warn as error)"
dotnet build ancplua-mcp.sln --configuration Release --no-restore -warnaserror

# Run tests if any test projects exist
if ls tests/*.sln tests/*/*.csproj >/dev/null 2>&1; then
  echo "➡️  dotnet test ancplua-mcp.sln (Release)"
  dotnet test ancplua-mcp.sln --configuration Release --no-build
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

# Dual-repo validation
if [ "$DUAL_MODE" = true ]; then
  echo ""
  echo "═══════════════════════════════════════════════════════════"
  echo "🔗 DUAL-REPO MODE: Validating sibling repository..."
  echo "═══════════════════════════════════════════════════════════"

  if [ -d "$SIBLING_REPO" ] && [ -x "$SIBLING_REPO/tooling/scripts/local-validate.sh" ]; then
    "$SIBLING_REPO/tooling/scripts/local-validate.sh"
  else
    echo "⚠️  Sibling repo not found at: $SIBLING_REPO"
    echo "   Expected: ancplua-claude-plugins with tooling/scripts/local-validate.sh"
  fi
fi
