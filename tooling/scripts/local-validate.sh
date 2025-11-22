#!/bin/bash
# Local validation script for ancplua-mcp
# Run this before committing to ensure everything works

set -e  # Exit on any error

echo "========================================"
echo "ancplua-mcp Local Validation"
echo "========================================"
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print success
success() {
    echo -e "${GREEN}✓${NC} $1"
}

# Function to print error
error() {
    echo -e "${RED}✗${NC} $1"
}

# Function to print info
info() {
    echo -e "${YELLOW}→${NC} $1"
}

# Check if dotnet is installed
info "Checking for .NET SDK..."
if ! command -v dotnet &> /dev/null; then
    error ".NET SDK is not installed"
    exit 1
fi
success ".NET SDK found: $(dotnet --version)"
echo ""

# Restore dependencies
info "Restoring dependencies..."
if dotnet restore; then
    success "Dependencies restored"
else
    error "Failed to restore dependencies"
    exit 1
fi
echo ""

# Build the solution
info "Building solution..."
if dotnet build --no-restore; then
    success "Build succeeded"
else
    error "Build failed"
    exit 1
fi
echo ""

# Run tests
info "Running tests..."
if dotnet test --no-build --verbosity normal; then
    success "All tests passed"
else
    error "Tests failed"
    exit 1
fi
echo ""

# Check for common issues
info "Checking for common issues..."

# Check for TODO or FIXME comments
if grep -r "TODO\|FIXME" --include="*.cs" WorkstationServer/ HttpServer/ tests/ 2>/dev/null | grep -v "Binary file"; then
    echo -e "${YELLOW}⚠${NC} Found TODO/FIXME comments (review before release)"
else
    success "No TODO/FIXME comments found"
fi

echo ""
echo "========================================"
echo -e "${GREEN}All validations passed!${NC}"
echo "========================================"
