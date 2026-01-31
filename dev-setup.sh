#!/bin/bash
# Development setup script for StudentRegistrar

echo "Setting up StudentRegistrar development environment..."

# Check if dotnet is installed
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET SDK is not installed. Please install .NET 10 SDK first."
    exit 1
fi

# Check if Docker is installed
if ! command -v docker &> /dev/null; then
    echo "❌ Docker is not installed. Please install Docker first."
    exit 1
fi

# Check if Node.js is installed
if ! command -v node &> /dev/null; then
    echo "❌ Node.js is not installed. Please install Node.js first."
    exit 1
fi

echo "✅ Prerequisites check passed"

# Install frontend dependencies
echo "Installing frontend dependencies..."
cd frontend && npm install && cd ..

# Restore local .NET tools (dotnet-ef)
echo "Restoring .NET tools..."
dotnet tool restore

# Initialize user secrets if not already done
echo "Initializing user secrets..."
dotnet user-secrets init --project src/StudentRegistrar.AppHost

# Create EF migrations if they don't exist
if [ ! -d "src/StudentRegistrar.Data/Migrations" ]; then
    echo "Creating Entity Framework migrations..."
    dotnet ef migrations add InitialCreate --project src/StudentRegistrar.Data --startup-project src/StudentRegistrar.Api
    echo "✅ Entity Framework migrations created"
fi

echo ""
echo "🎉 Development environment setup complete!"
echo ""
echo "To start the application:"
echo "  dotnet run --project src/StudentRegistrar.AppHost"
echo ""
echo "To access the application:"
echo "  - Aspire Dashboard: http://localhost:15888"
echo "  - API: http://localhost:5000"
echo "  - Frontend: http://localhost:3001"
echo ""
echo "Note: All service passwords are auto-generated securely by Aspire and rotate on each run."
echo "Run 'dotnet user-secrets list --project src/StudentRegistrar.AppHost' to view configured secrets."
