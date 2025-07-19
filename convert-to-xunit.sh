#!/bin/bash

# Script to convert TUnit tests to xUnit tests
echo "🔄 Converting TUnit tests to xUnit..."

# Function to convert a single file
convert_file() {
    local file="$1"
    echo "Converting $file..."
    
    # Create backup
    cp "$file" "$file.backup"
    
    # Convert TUnit syntax to xUnit syntax
    sed -i 's/using TUnit.Core;/using Xunit;/g' "$file"
    sed -i 's/using TUnit.Assertions;/using Xunit;/g' "$file"
    sed -i 's/using TUnit.Assertions.Extensions;/using Xunit;/g' "$file"
    sed -i 's/\[Test\]/[Fact]/g' "$file"
    sed -i 's/\[TestMethod\]/[Fact]/g' "$file"
    sed -i 's/await Assert\.That(\([^)]*\))\.IsEqualTo(\([^)]*\));/Assert.Equal(\2, \1);/g' "$file"
    sed -i 's/await Assert\.That(\([^)]*\))\.IsNotEqualTo(\([^)]*\));/Assert.NotEqual(\2, \1);/g' "$file"
    sed -i 's/await Assert\.That(\([^)]*\))\.IsNotNull();/Assert.NotNull(\1);/g' "$file"
    sed -i 's/await Assert\.That(\([^)]*\))\.IsNull();/Assert.Null(\1);/g' "$file"
    sed -i 's/await Assert\.That(\([^)]*\))\.IsTrue();/Assert.True(\1);/g' "$file"
    sed -i 's/await Assert\.That(\([^)]*\))\.IsFalse();/Assert.False(\1);/g' "$file"
    sed -i 's/await Assert\.That(\([^)]*\))\.IsGreaterThan(\([^)]*\));/Assert.True(\1 > \2);/g' "$file"
    sed -i 's/await Assert\.That(\([^)]*\))\.IsGreaterThanOrEqualTo(\([^)]*\));/Assert.True(\1 >= \2);/g' "$file"
    sed -i 's/await Assert\.That(\([^)]*\))\.IsLessThan(\([^)]*\));/Assert.True(\1 < \2);/g' "$file"
    sed -i 's/await Assert\.That(\([^)]*\))\.IsLessThanOrEqualTo(\([^)]*\));/Assert.True(\1 <= \2);/g' "$file"
    sed -i 's/await Assert\.That(\([^)]*\))\.Contains(\([^)]*\));/Assert.Contains(\2, \1);/g' "$file"
    sed -i 's/await Assert\.That(\([^)]*\))\.DoesNotContain(\([^)]*\));/Assert.DoesNotContain(\2, \1);/g' "$file"
    sed -i 's/await Assert\.That(\([^)]*\))\.IsEmpty();/Assert.Empty(\1);/g' "$file"
    sed -i 's/await Assert\.That(\([^)]*\))\.IsNotEmpty();/Assert.NotEmpty(\1);/g' "$file"
    sed -i 's/await Assert\.That(\([^)]*\))\.HasCount(\([^)]*\));/Assert.Equal(\2, \1.Count);/g' "$file"
    
    # Remove async from test methods that no longer need it
    sed -i 's/public async Task \([^(]*\)()/public void \1()/g' "$file"
    
    # Convert remaining async test methods to async Task (xUnit style)
    sed -i 's/public async Task \([^(]*\)(/public async Task \1(/g' "$file"
    
    echo "✅ Converted $file"
}

# Convert all test files in MCP.Tests
cd /home/henning/Workbench/MCP/MCP.Tests

for file in *.cs; do
    if [[ "$file" != "InMemoryAnalysisService.cs" && "$file" != "InMemoryProjectGenerator.cs" && "$file" != "TestSetup.cs" ]]; then
        convert_file "$file"
    fi
done

echo "🎉 Conversion complete!"
echo "📝 Backup files created with .backup extension"
echo "🧪 Ready to test with xUnit!"
