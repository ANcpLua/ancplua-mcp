#!/usr/bin/env bash
set -euo pipefail
echo "🔍 Running ancplua-mcp local validation..."
dotnet --info
dotnet restore
dotnet build --no-restore --configuration Release
# dotnet test --no-build --configuration Release # Uncomment when tests exist
echo "✅ Validation complete."
