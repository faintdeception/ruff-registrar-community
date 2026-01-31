#!/bin/bash

# Student Registrar Setup Script

set -e

echo "🎓 Setting up Student Registrar..."

# Check if Docker is installed
if ! command -v docker &> /dev/null; then
    echo "❌ Docker is not installed. Please install Docker first."
    exit 1
fi

# Check if Docker Compose is installed
if ! command -v docker-compose &> /dev/null; then
    echo "❌ Docker Compose is not installed. Please install Docker Compose first."
    exit 1
fi

# Check if .NET SDK is installed
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET SDK is not installed. Please install .NET 10 SDK first."
    exit 1
fi

# Check if Node.js is installed
if ! command -v node &> /dev/null; then
    echo "❌ Node.js is not installed. Please install Node.js 18+ first."
    exit 1
fi

echo "✅ Prerequisites check passed!"

# Restore .NET packages
echo "📦 Restoring .NET packages..."
dotnet restore

# Restore local .NET tools (dotnet-ef)
echo "🛠️ Restoring .NET tools..."
dotnet tool restore

# Install frontend dependencies
echo "📦 Installing frontend dependencies..."
cd frontend
npm install
cd ..

# Start infrastructure services
echo "🚀 Starting infrastructure services..."
docker-compose up -d postgres keycloak

echo "⏳ Waiting for services to be ready..."
sleep 10

# Default connection string for local Docker Compose Postgres
export ConnectionStrings__studentregistrar="Host=localhost;Database=studentregistrar;Username=postgres;Password=${POSTGRES_PASSWORD:-changeme-in-production}"

# Create initial migration if none exist
if [ ! -d "src/StudentRegistrar.Data/Migrations" ]; then
    echo "🗄️ Creating initial database migration..."
    dotnet ef migrations add InitialCreate --project src/StudentRegistrar.Data --startup-project src/StudentRegistrar.Api
else
    echo "🗄️ Migrations already exist. Skipping creation."
fi

# Apply migration
echo "🗄️ Applying database migration..."
dotnet ef database update --project src/StudentRegistrar.Data --startup-project src/StudentRegistrar.Api

echo "✅ Setup complete!"
echo ""
echo "🌟 Next steps:"
echo "1. Configure Keycloak at http://localhost:8080 (admin/admin)"
echo "2. Create a realm and client for the application"
echo "3. Run the application:"
echo "   - For development: dotnet run --project src/StudentRegistrar.AppHost"
echo "   - Or separately: dotnet run --project src/StudentRegistrar.Api (API) and npm run dev (frontend)"
echo "   - For production: docker-compose up --build"
echo ""
echo "📱 Application URLs:"
echo "- Frontend: http://localhost:3000"
echo "- API: http://localhost:5000"
echo "- Keycloak: http://localhost:8080"
echo ""
echo "Happy coding! 🚀"
