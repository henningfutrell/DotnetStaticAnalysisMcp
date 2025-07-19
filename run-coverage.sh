#!/bin/bash

# Script to run code coverage analysis for the MCP .NET Static Analysis Server
# This script ensures we get proper coverage of the production code

echo "🔍 Running Code Coverage Analysis for MCP .NET Static Analysis Server"
echo "======================================================================="

# Clean previous builds
echo "🧹 Cleaning previous builds..."
dotnet clean MCP.Server
dotnet clean MCP.Tests

# Restore packages
echo "📦 Restoring packages..."
dotnet restore MCP.Server
dotnet restore MCP.Tests

# Build projects
echo "🔨 Building projects..."
dotnet build MCP.Server --no-restore
dotnet build MCP.Tests --no-restore

# Run tests with coverage using Coverlet
echo "🧪 Running tests with coverage analysis..."
echo ""

# Method 1: Using Coverlet collector
echo "📊 Method 1: Using Coverlet Collector"
dotnet test MCP.Tests \
    --collect:"XPlat Code Coverage" \
    --results-directory:"./TestResults" \
    --logger:"console;verbosity=detailed" \
    --settings:MCP.Tests/coverage.runsettings \
    -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover

echo ""

# Method 2: Using Coverlet MSBuild integration
echo "📊 Method 2: Using Coverlet MSBuild"
dotnet test MCP.Tests \
    /p:CollectCoverage=true \
    /p:CoverletOutputFormat=opencover \
    /p:CoverletOutput="./TestResults/coverage.opencover.xml" \
    /p:Include="[MCP.Server]*" \
    /p:Exclude="[MCP.Tests]*,[*]System.*,[*]Microsoft.*" \
    --logger:"console;verbosity=detailed"

echo ""

# Method 3: Using dotnet-coverage (if available)
echo "📊 Method 3: Using dotnet-coverage tool"
if command -v dotnet-coverage &> /dev/null; then
    dotnet-coverage collect \
        --settings MCP.Tests/coverage.runsettings \
        --output "./TestResults/coverage.cobertura.xml" \
        --output-format cobertura \
        "dotnet test MCP.Tests --logger:console;verbosity=detailed"
else
    echo "⚠️  dotnet-coverage tool not installed. Skipping this method."
    echo "   To install: dotnet tool install --global dotnet-coverage"
fi

echo ""

# Method 4: Focus on Real Code Coverage Tests
echo "📊 Method 4: Running Real Code Coverage Tests Only"
dotnet test MCP.Tests \
    --filter "FullyQualifiedName~RealCodeCoverageTests" \
    /p:CollectCoverage=true \
    /p:CoverletOutputFormat=opencover \
    /p:CoverletOutput="./TestResults/real-coverage.opencover.xml" \
    /p:Include="[MCP.Server]*" \
    /p:Exclude="[MCP.Tests]*,[*]System.*,[*]Microsoft.*" \
    --logger:"console;verbosity=detailed"

echo ""
echo "📈 Coverage Analysis Complete!"
echo "==============================="

# Check if coverage files were generated
echo "🔍 Checking for generated coverage files..."
if [ -d "./TestResults" ]; then
    echo "✅ TestResults directory found:"
    find ./TestResults -name "*.xml" -o -name "*.json" -o -name "*.coverage" | head -10
    echo ""
    
    # Try to display coverage summary if available
    if [ -f "./TestResults/coverage.opencover.xml" ]; then
        echo "📊 Coverage Summary (from coverage.opencover.xml):"
        if command -v grep &> /dev/null; then
            grep -E "(sequenceCoverage|branchCoverage)" "./TestResults/coverage.opencover.xml" | head -5
        fi
    fi
    
    if [ -f "./TestResults/real-coverage.opencover.xml" ]; then
        echo "📊 Real Code Coverage Summary (from real-coverage.opencover.xml):"
        if command -v grep &> /dev/null; then
            grep -E "(sequenceCoverage|branchCoverage)" "./TestResults/real-coverage.opencover.xml" | head -5
        fi
    fi
else
    echo "❌ No TestResults directory found. Coverage may not have been collected properly."
fi

echo ""
echo "💡 Tips for better coverage:"
echo "   1. Run tests that exercise the real RoslynAnalysisService class"
echo "   2. Use the RealCodeCoverageTests to ensure production code is tested"
echo "   3. Check that MCP.Server.dll is being instrumented"
echo "   4. Verify that tests are calling production code, not just mocks"

echo ""
echo "🎯 To view detailed coverage reports:"
echo "   - Install ReportGenerator: dotnet tool install --global dotnet-reportgenerator-globaltool"
echo "   - Generate HTML report: reportgenerator -reports:./TestResults/*.xml -targetdir:./CoverageReport"
echo "   - Open ./CoverageReport/index.html in a browser"

echo ""
echo "✅ Coverage analysis script completed!"
