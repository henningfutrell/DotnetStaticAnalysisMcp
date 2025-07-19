#!/usr/bin/env python3
"""
Fix remaining xUnit conversion issues
"""

import os
import re
import glob

def fix_remaining_assertions(file_path):
    """Fix remaining assertion patterns that weren't converted"""
    print(f"Fixing {file_path}...")
    
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    original_content = content
    
    # Fix remaining Assert.That patterns
    content = re.sub(r'Assert\.That\(([^)]+)\)\.IsEqualTo\(([^)]+)\)', r'Assert.Equal(\2, \1)', content)
    content = re.sub(r'Assert\.That\(([^)]+)\)\.IsNotEqualTo\(([^)]+)\)', r'Assert.NotEqual(\2, \1)', content)
    content = re.sub(r'Assert\.That\(([^)]+)\)\.IsNotNull\(\)', r'Assert.NotNull(\1)', content)
    content = re.sub(r'Assert\.That\(([^)]+)\)\.IsNull\(\)', r'Assert.Null(\1)', content)
    content = re.sub(r'Assert\.That\(([^)]+)\)\.IsTrue\(\)', r'Assert.True(\1)', content)
    content = re.sub(r'Assert\.That\(([^)]+)\)\.IsFalse\(\)', r'Assert.False(\1)', content)
    content = re.sub(r'Assert\.That\(([^)]+)\)\.Contains\(([^)]+)\)', r'Assert.Contains(\2, \1)', content)
    content = re.sub(r'Assert\.That\(([^)]+)\)\.IsEmpty\(\)', r'Assert.Empty(\1)', content)
    content = re.sub(r'Assert\.That\(([^)]+)\)\.IsNotEmpty\(\)', r'Assert.NotEmpty(\1)', content)
    
    # Fix xUnit analyzer warnings - parameter order issues (xUnit2000)
    # Pattern: Assert.Equal(actual, expected) -> Assert.Equal(expected, actual)
    
    # Fix string literals in wrong position
    content = re.sub(r'Assert\.Equal\(([^,]+), ("([^"]*)")\)', r'Assert.Equal(\2, \1)', content)
    content = re.sub(r'Assert\.Equal\(([^,]+), (\'([^\']*)\'))', r'Assert.Equal(\2, \1)', content)
    
    # Fix numeric literals in wrong position
    content = re.sub(r'Assert\.Equal\(([^,]+), (\d+)\)', r'Assert.Equal(\2, \1)', content)
    
    # Fix enum values in wrong position
    content = re.sub(r'Assert\.Equal\(([^,]+), (JsonValueKind\.[A-Za-z]+)\)', r'Assert.Equal(\2, \1)', content)
    
    # Fix boolean literals in wrong position
    content = re.sub(r'Assert\.Equal\(([^,]+), (true|false)\)', r'Assert.Equal(\2, \1)', content)
    
    # Fix xUnit2012 - Use Assert.Contains instead of Assert.True for collection checks
    content = re.sub(r'Assert\.True\(([^.]+)\.Contains\(([^)]+)\)\)', r'Assert.Contains(\2, \1)', content)
    
    # Fix xUnit2013 - Use Assert.Empty instead of Assert.Equal(0, collection.Count)
    content = re.sub(r'Assert\.Equal\(0, ([^.]+)\.Count\)', r'Assert.Empty(\1)', content)
    
    # Fix xUnit2009 - Use Assert.StartsWith/EndsWith instead of Assert.True for string checks
    content = re.sub(r'Assert\.True\(([^.]+)\.StartsWith\(([^)]+)\)\)', r'Assert.StartsWith(\2, \1)', content)
    content = re.sub(r'Assert\.True\(([^.]+)\.EndsWith\(([^)]+)\)\)', r'Assert.EndsWith(\2, \1)', content)
    
    # Additional specific fixes for common patterns
    
    # Fix .GetProperty().GetString() patterns
    content = re.sub(r'Assert\.Equal\(([^,]+)\.GetProperty\("([^"]+)"\)\.GetString\(\), ("([^"]*)")\)', 
                    r'Assert.Equal(\3, \1.GetProperty("\2").GetString())', content)
    
    # Fix .GetProperty().GetInt32() patterns  
    content = re.sub(r'Assert\.Equal\(([^,]+)\.GetProperty\("([^"]+)"\)\.GetInt32\(\), (\d+)\)', 
                    r'Assert.Equal(\3, \1.GetProperty("\2").GetInt32())', content)
    
    if content != original_content:
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"  ✅ Fixed {file_path}")
    else:
        print(f"  ⏭️  No changes needed for {file_path}")

def main():
    """Fix remaining xUnit issues in all test files"""
    test_dir = "/home/henning/Workbench/MCP/MCP.Tests"
    
    # Find all .cs files except infrastructure files
    cs_files = glob.glob(os.path.join(test_dir, "*.cs"))
    
    # Skip infrastructure files
    skip_files = ['InMemoryAnalysisService.cs', 'InMemoryProjectGenerator.cs', 'TestSetup.cs']
    cs_files = [f for f in cs_files if os.path.basename(f) not in skip_files]
    
    print(f"🔧 Fixing remaining xUnit issues in {len(cs_files)} test files...")
    
    for file_path in cs_files:
        try:
            fix_remaining_assertions(file_path)
        except Exception as e:
            print(f"  ❌ Error fixing {file_path}: {e}")
    
    print("🎉 Fixes complete!")
    print("📝 Next step: dotnet build MCP.Tests")

if __name__ == "__main__":
    main()
