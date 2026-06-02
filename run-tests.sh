#!/bin/bash

# Test runner script for the Student Registrar project
# Usage: ./run-tests.sh [--watch]

set -euo pipefail

echo "🧪 Running Student Registrar Tests"
echo "================================="

if [ "${1:-}" = "--watch" ]; then
    echo "Running in watch mode..."
    dotnet watch test
else
    echo "Running model and API test projects..."
    dotnet test ./tests/StudentRegistrar.Models.Tests/StudentRegistrar.Models.Tests.csproj --verbosity normal
    dotnet test ./tests/StudentRegistrar.Api.Tests/StudentRegistrar.Api.Tests.csproj --verbosity normal
    
    echo ""
    echo "📊 Test Summary:"
    echo "- Model and API test projects"
    echo "- Browser E2E and smoke projects are not invoked by default"
    echo ""
    echo "✅ All tests should be passing!"
fi
