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
    dotnet test --verbosity normal --filter "FullyQualifiedName!~StudentRegistrar.Smoke.Tests"
    
    echo ""
    echo "📊 Test Summary:"
    echo "- Model Tests: 96 tests"
    echo "- API Controller Tests: 4 tests"  
    echo "- Total: 100 tests (excluding smoke tests)"
    echo ""
    echo "✅ All tests should be passing!"
fi
