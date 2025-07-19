#!/usr/bin/env python3
"""
Complete TUnit to xUnit conversion script for MCP.Tests project
This script handles all the conversion patterns needed to migrate from TUnit to xUnit
"""

import os
import re
import glob

def convert_file(file_path):
    """Convert a single file from TUnit to xUnit syntax"""
    print(f"Converting {file_path}...")
    
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Skip files that don't need conversion
    if not any(pattern in content for pattern in ['[Test]', 'await Assert.That', 'TUnit']):
        print(f"  Skipping {file_path} - no TUnit syntax found")
        return
    
    # 1. Update using statements
    content = re.sub(r'using TUnit\.Core;?\s*\n', '', content)
    content = re.sub(r'using TUnit\.Assertions;?\s*\n', '', content)
    content = re.sub(r'using TUnit\.Assertions\.Extensions;?\s*\n', '', content)
    
    # Ensure xUnit using is present if we have test attributes
    if '[Test]' in content or '[Fact]' in content:
        if 'using Xunit;' not in content:
            # Add xUnit using after other using statements
            lines = content.split('\n')
            insert_index = 0
            for i, line in enumerate(lines):
                if line.startswith('using ') and not line.startswith('using Xunit'):
                    insert_index = i + 1
                elif line.startswith('namespace '):
                    break
            lines.insert(insert_index, 'using Xunit;')
            content = '\n'.join(lines)
    
    # 2. Convert test attributes
    content = re.sub(r'\[Test\]', '[Fact]', content)
    content = re.sub(r'\[TestMethod\]', '[Fact]', content)
    
    # 3. Convert async test method signatures
    # Remove async from void methods that don't need it
    content = re.sub(r'public async void (\w+)\(\)', r'public void \1()', content)
    
    # Convert async Task methods to proper xUnit format
    content = re.sub(r'public async Task (\w+)\(\)', r'public async Task \1()', content)
    
    # 4. Convert TUnit assertions to xUnit assertions
    
    # Basic equality assertions
    content = re.sub(r'await Assert\.That\(([^)]+)\)\.IsEqualTo\(([^)]+)\);', r'Assert.Equal(\2, \1);', content)
    content = re.sub(r'await Assert\.That\(([^)]+)\)\.IsNotEqualTo\(([^)]+)\);', r'Assert.NotEqual(\2, \1);', content)
    
    # Null assertions
    content = re.sub(r'await Assert\.That\(([^)]+)\)\.IsNotNull\(\);', r'Assert.NotNull(\1);', content)
    content = re.sub(r'await Assert\.That\(([^)]+)\)\.IsNull\(\);', r'Assert.Null(\1);', content)
    
    # Boolean assertions
    content = re.sub(r'await Assert\.That\(([^)]+)\)\.IsTrue\(\);', r'Assert.True(\1);', content)
    content = re.sub(r'await Assert\.That\(([^)]+)\)\.IsFalse\(\);', r'Assert.False(\1);', content)
    
    # Comparison assertions
    content = re.sub(r'await Assert\.That\(([^)]+)\)\.IsGreaterThan\(([^)]+)\);', r'Assert.True(\1 > \2);', content)
    content = re.sub(r'await Assert\.That\(([^)]+)\)\.IsGreaterThanOrEqualTo\(([^)]+)\);', r'Assert.True(\1 >= \2);', content)
    content = re.sub(r'await Assert\.That\(([^)]+)\)\.IsLessThan\(([^)]+)\);', r'Assert.True(\1 < \2);', content)
    content = re.sub(r'await Assert\.That\(([^)]+)\)\.IsLessThanOrEqualTo\(([^)]+)\);', r'Assert.True(\1 <= \2);', content)
    
    # Collection assertions
    content = re.sub(r'await Assert\.That\(([^)]+)\)\.Contains\(([^)]+)\);', r'Assert.Contains(\2, \1);', content)
    content = re.sub(r'await Assert\.That\(([^)]+)\)\.DoesNotContain\(([^)]+)\);', r'Assert.DoesNotContain(\2, \1);', content)
    content = re.sub(r'await Assert\.That\(([^)]+)\)\.IsEmpty\(\);', r'Assert.Empty(\1);', content)
    content = re.sub(r'await Assert\.That\(([^)]+)\)\.IsNotEmpty\(\);', r'Assert.NotEmpty(\1);', content)
    
    # Count assertions - convert to xUnit preferred style
    content = re.sub(r'await Assert\.That\(([^)]+)\)\.HasCount\(([^)]+)\);', r'Assert.Equal(\2, \1.Count);', content)
    content = re.sub(r'Assert\.Equal\(0, ([^)]+)\.Count\);', r'Assert.Empty(\1);', content)
    content = re.sub(r'Assert\.Equal\(1, ([^)]+)\.Count\);', r'Assert.Single(\1);', content)
    
    # Type assertions
    content = re.sub(r'await Assert\.That\(([^)]+)\)\.IsTypeOf<([^>]+)>\(\);', r'Assert.IsType<\2>(\1);', content)
    content = re.sub(r'await Assert\.That\(([^)]+)\)\.IsAssignableFrom<([^>]+)>\(\);', r'Assert.IsAssignableFrom<\2>(\1);', content)
    
    # 5. Handle complex multi-line assertions
    # This is a more complex pattern for assertions that span multiple lines
    def replace_multiline_assertions(match):
        full_match = match.group(0)
        # For complex cases, just convert the basic pattern
        if '.IsEqualTo(' in full_match:
            return full_match.replace('await Assert.That(', 'Assert.Equal(').replace(').IsEqualTo(', ', ')
        elif '.IsNotNull()' in full_match:
            return full_match.replace('await Assert.That(', 'Assert.NotNull(').replace(').IsNotNull();', ');')
        elif '.IsTrue()' in full_match:
            return full_match.replace('await Assert.That(', 'Assert.True(').replace(').IsTrue();', ');')
        elif '.IsFalse()' in full_match:
            return full_match.replace('await Assert.That(', 'Assert.False(').replace(').IsFalse();', ');')
        return full_match
    
    # Handle remaining await Assert.That patterns
    content = re.sub(r'await Assert\.That\([^;]+\);', replace_multiline_assertions, content)
    
    # 6. Remove unnecessary async/await keywords where they're no longer needed
    # If a method only has Assert calls and no actual async operations, remove async
    lines = content.split('\n')
    for i, line in enumerate(lines):
        if 'public async Task' in line and '()' in line:
            # Check if this method actually needs to be async
            method_start = i
            method_end = method_start
            brace_count = 0
            found_start = False
            
            for j in range(method_start, len(lines)):
                if '{' in lines[j]:
                    brace_count += lines[j].count('{')
                    found_start = True
                if '}' in lines[j]:
                    brace_count -= lines[j].count('}')
                if found_start and brace_count == 0:
                    method_end = j
                    break
            
            # Check if method has any await calls
            method_content = '\n'.join(lines[method_start:method_end+1])
            if 'await ' not in method_content:
                # Remove async and change Task to void
                lines[i] = lines[i].replace('public async Task', 'public void')
    
    content = '\n'.join(lines)
    
    # 7. Fix any remaining syntax issues
    # Remove duplicate using statements
    lines = content.split('\n')
    seen_usings = set()
    filtered_lines = []
    for line in lines:
        if line.startswith('using '):
            if line not in seen_usings:
                seen_usings.add(line)
                filtered_lines.append(line)
        else:
            filtered_lines.append(line)
    content = '\n'.join(filtered_lines)
    
    # Write the converted content back
    with open(file_path, 'w', encoding='utf-8') as f:
        f.write(content)
    
    print(f"  ✅ Converted {file_path}")

def main():
    """Convert all test files in the MCP.Tests directory"""
    test_dir = "/home/henning/Workbench/MCP/MCP.Tests"
    
    # Find all .cs files except infrastructure files
    cs_files = glob.glob(os.path.join(test_dir, "*.cs"))
    
    # Skip infrastructure files
    skip_files = ['InMemoryAnalysisService.cs', 'InMemoryProjectGenerator.cs', 'TestSetup.cs']
    cs_files = [f for f in cs_files if os.path.basename(f) not in skip_files]
    
    print(f"🔄 Converting {len(cs_files)} test files from TUnit to xUnit...")
    
    for file_path in cs_files:
        try:
            convert_file(file_path)
        except Exception as e:
            print(f"  ❌ Error converting {file_path}: {e}")
    
    print("🎉 Conversion complete!")
    print("📝 Next steps:")
    print("  1. Build the project: dotnet build MCP.Tests")
    print("  2. Run the tests: dotnet test MCP.Tests")
    print("  3. Check for any remaining compilation errors")

if __name__ == "__main__":
    main()
