#!/usr/bin/env python3
"""
Fix remaining xUnit analyzer warnings
"""

import re
import glob

def fix_xunit_warnings(file_path):
    with open(file_path, 'r') as f:
        content = f.read()
    
    original = content
    
    # Fix xUnit2012: Use Assert.Contains instead of Assert.True for collection checks
    # Pattern: Assert.True(collection.Contains(item)) -> Assert.Contains(item, collection)
    content = re.sub(r'Assert\.True\(([^.]+)\.Contains\(([^)]+)\)\);', 
                    r'Assert.Contains(\2, \1);', content)
    
    # Fix xUnit2013: Use Assert.Empty instead of Assert.Equal(0, collection.Count)
    content = re.sub(r'Assert\.Equal\(0, ([^.]+)\.Count\);', 
                    r'Assert.Empty(\1);', content)
    content = re.sub(r'Assert\.Equal\(0, ([^.]+)\.Length\);', 
                    r'Assert.Empty(\1);', content)
    
    # Fix xUnit2000: Parameter order issues
    # Fix cases where string literals are in wrong position
    content = re.sub(r'Assert\.Equal\(([^,]+\.GetProperty\([^)]+\)\.GetString\(\)), ("([^"]*)")\);', 
                    r'Assert.Equal(\2, \1);', content)
    
    # Fix cases where the pattern is more complex
    content = re.sub(r'Assert\.Equal\(([^,]+), ("([^"]*)")\);', 
                    r'Assert.Equal(\2, \1);', content)
    
    if content != original:
        with open(file_path, 'w') as f:
            f.write(content)
        print(f"Fixed {file_path}")
        return True
    return False

# Fix all test files
test_files = glob.glob("/home/henning/Workbench/MCP/MCP.Tests/*.cs")
skip_files = ['InMemoryAnalysisService.cs', 'InMemoryProjectGenerator.cs', 'TestSetup.cs']
test_files = [f for f in test_files if not any(skip in f for skip in skip_files)]

fixed_count = 0
for file_path in test_files:
    try:
        if fix_xunit_warnings(file_path):
            fixed_count += 1
    except Exception as e:
        print(f"Error fixing {file_path}: {e}")

print(f"Fixed {fixed_count} files")
print("Done!")
