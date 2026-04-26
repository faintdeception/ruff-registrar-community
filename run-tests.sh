#!/bin/bash

# Test runner script for the Student Registrar project
# Usage: ./run-tests.sh [--watch]

echo "🧪 Running Student Registrar Tests"
echo "================================="

if [ "$1" = "--watch" ]; then
    echo "Running in watch mode..."
    dotnet watch test
else
    echo "Running all tests..."
    dotnet test --verbosity normal --filter "FullyQualifiedName!~StudentRegistrar.Smoke.Tests&FullyQualifiedName!~StudentRegistrar.E2E.Tests"
    
    echo ""
    echo "📊 Test Summary:"
    echo "- Model and API tests"
    echo "- Browser E2E and smoke tests are excluded by default"
    echo ""
    echo "✅ All tests should be passing!"
fi
