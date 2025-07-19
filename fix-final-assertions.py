#!/usr/bin/env python3
"""
Fix the final remaining Assert.That patterns
"""

import re

def fix_file(filename):
    with open(filename, 'r') as f:
        content = f.read()
    
    original = content
    
    # Fix remaining Assert.That patterns
    content = re.sub(r'await Assert\.That\(([^)]+\.GetProperty\([^)]+\)\.GetInt32\(\))\)\.IsGreaterThan\((\d+)\);', 
                    r'Assert.True(\1 > \2);', content)
    
    content = re.sub(r'await Assert\.That\(([^)]+\.GetProperty\([^)]+\)\.GetInt32\(\))\)\.IsGreaterThanOrEqualTo\((\d+)\);', 
                    r'Assert.True(\1 >= \2);', content)
    
    content = re.sub(r'await Assert\.That\(([^)]+\.GetProperty\([^)]+\)\.GetString\(\))\)\.Contains\(([^)]+)\);', 
                    r'Assert.Contains(\2, \1);', content)
    
    content = re.sub(r'await Assert\.That\(([^)]+)\)\.Contains\(([^)]+)\);', 
                    r'Assert.Contains(\2, \1);', content)
    
    # Fix parameter order issues
    content = re.sub(r'Assert\.Equal\(([^,]+\.GetProperty\([^)]+\)\.GetString\(\)), ("([^"]*)")\);', 
                    r'Assert.Equal(\2, \1);', content)
    
    content = re.sub(r'Assert\.Equal\(([^,]+\.GetProperty\([^)]+\)\.GetInt32\(\)), (\d+)\);', 
                    r'Assert.Equal(\2, \1);', content)
    
    content = re.sub(r'Assert\.Equal\(([^,]+\.GetProperty\([^)]+\)\.GetBoolean\(\)), (true|false)\);', 
                    r'Assert.Equal(\2, \1);', content)
    
    content = re.sub(r'Assert\.Equal\(([^,]+), (JsonValueKind\.[A-Za-z]+)\);', 
                    r'Assert.Equal(\2, \1);', content)
    
    # Fix collection assertions
    content = re.sub(r'Assert\.True\(([^.]+)\.Contains\(([^)]+)\)\);', 
                    r'Assert.Contains(\2, \1);', content)
    
    content = re.sub(r'Assert\.Equal\(0, ([^.]+)\.Count\);', 
                    r'Assert.Empty(\1);', content)
    
    # Fix string assertions
    content = re.sub(r'Assert\.True\(([^.]+)\.StartsWith\(([^)]+)\)\);', 
                    r'Assert.StartsWith(\2, \1);', content)
    
    content = re.sub(r'Assert\.True\(([^.]+)\.EndsWith\(([^)]+)\)\);', 
                    r'Assert.EndsWith(\2, \1);', content)
    
    if content != original:
        with open(filename, 'w') as f:
            f.write(content)
        print(f"Fixed {filename}")
    else:
        print(f"No changes needed for {filename}")

# Fix all remaining files
files = [
    "/home/henning/Workbench/MCP/MCP.Tests/SimpleInMemoryTests.cs",
    "/home/henning/Workbench/MCP/MCP.Tests/Tests2.cs", 
    "/home/henning/Workbench/MCP/MCP.Tests/Tests3.cs",
    "/home/henning/Workbench/MCP/MCP.Tests/CodeSuggestionsErrorHandlingTests.cs",
    "/home/henning/Workbench/MCP/MCP.Tests/WorkingTests.cs",
    "/home/henning/Workbench/MCP/MCP.Tests/RealCodeCoverageTests.cs",
    "/home/henning/Workbench/MCP/MCP.Tests/ProductionCodeTests.cs",
    "/home/henning/Workbench/MCP/MCP.Tests/CodeSuggestionsIntegrationTests.cs",
    "/home/henning/Workbench/MCP/MCP.Tests/CodeSuggestionsTests.cs",
    "/home/henning/Workbench/MCP/MCP.Tests/SimpleTests.cs",
    "/home/henning/Workbench/MCP/MCP.Tests/InMemoryTests.cs"
]

for file in files:
    try:
        fix_file(file)
    except Exception as e:
        print(f"Error fixing {file}: {e}")

print("Done!")
