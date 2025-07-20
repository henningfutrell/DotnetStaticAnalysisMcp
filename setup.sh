#!/bin/bash

# .NET Static Analysis MCP Server Setup Script

set -e

echo "🚀 Setting up .NET Static Analysis MCP Server..."

# Check if .NET SDK is installed
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET SDK not found. Please install .NET 9.0 SDK or later."
    echo "   Download from: https://dotnet.microsoft.com/download"
    exit 1
fi

# Check .NET version
DOTNET_VERSION=$(dotnet --version)
echo "✅ Found .NET SDK version: $DOTNET_VERSION"

# Restore dependencies
echo "📦 Restoring NuGet packages..."
dotnet restore

# Build the solution
echo "🔨 Building the solution..."
dotnet build

# Run tests to verify everything works
echo "🧪 Running tests..."
dotnet test --verbosity minimal

echo ""
echo "✅ Setup complete!"
echo ""
echo "📋 Next steps:"
echo "1. Update the path in mcp-config-example.json to point to your MCP directory"
echo "2. Add the configuration to your MCP client config file"
echo "3. Restart your MCP client"
echo ""
echo "📁 Current directory: $(pwd)"
echo "🔧 Example config file: $(pwd)/mcp-config-example.json"
echo ""
echo "🎉 Your .NET Static Analysis MCP Server is ready to use!"
