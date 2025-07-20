using System;
using System.Collections.Generic;
using System.Linq;

namespace MCP.TestProject
{
    // This class contains deliberate issues for testing the MCP server
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Testing MCP Server Issue Detection");
            
            // COMPILATION ERRORS (should be detected):
            
            // 1. Missing semicolon (CS1002)
            var message = "Hello World"  // Missing semicolon
            
            // 2. Undefined variable (CS0103)
            Console.WriteLine(undefinedVariable);
            
            // 3. Unknown type (CS0246)
            UnknownType unknownInstance = new UnknownType();
            
            // 4. Method with no return statement (CS0161)
            var result = MethodWithNoReturn();
            
            // CODE STYLE ISSUES (should trigger suggestions):
            
            // 5. Old-style object creation (should suggest target-typed new)
            List<string> oldStyleList = new List<string>();
            Dictionary<string, int> oldStyleDict = new Dictionary<string, int>();
            
            // 6. String concatenation in loop (performance issue)
            string concatenated = "";
            for (int i = 0; i < 100; i++)
            {
                concatenated += i.ToString(); // Should suggest StringBuilder
            }
            
            // 7. LINQ inefficiency (should suggest better approach)
            var numbers = Enumerable.Range(1, 1000).ToList();
            var firstEven = numbers.Where(x => x % 2 == 0).First(); // Should suggest FirstOrDefault
            
            // 8. Unused variable (should trigger warning)
            var unusedVariable = "This is never used";
            
            // 9. Magic numbers (should suggest constants)
            var area = 3.14159 * radius * radius; // Should suggest Math.PI
            
            // 10. Inefficient string comparison (should suggest StringComparison)
            if (message.ToLower() == "hello world")
            {
                Console.WriteLine("Match found");
            }
            
            // 11. Potential null reference (should suggest null check)
            string? nullableString = GetNullableString();
            Console.WriteLine(nullableString.Length); // Potential null reference
            
            // 12. Async method called synchronously (should suggest await)
            var data = GetDataAsync().Result; // Should suggest await
            
            Console.WriteLine("Test completed");
        }
        
        // Method with missing return statement (CS0161)
        public static int MethodWithNoReturn()
        {
            Console.WriteLine("This method should return an int");
            // Missing return statement
        }
        
        // Method for testing suggestions
        public static string? GetNullableString()
        {
            return DateTime.Now.Millisecond % 2 == 0 ? "Hello" : null;
        }
        
        // Async method for testing
        public static async Task<string> GetDataAsync()
        {
            await Task.Delay(100);
            return "Data";
        }
        
        // Variable used in magic number example
        private static double radius = 5.0;
    }
    
    // Additional class with more issues
    public class TestClass
    {
        // 13. Field should be readonly (should trigger suggestion)
        private string constantValue = "This never changes";
        
        // 14. Method can be static (should trigger suggestion)
        public void MethodThatCanBeStatic()
        {
            Console.WriteLine("This method doesn't use instance members");
        }
        
        // 15. Empty catch block (should trigger warning)
        public void MethodWithEmptyCatch()
        {
            try
            {
                throw new Exception("Test exception");
            }
            catch
            {
                // Empty catch block - bad practice
            }
        }
        
        // 16. Inefficient exception handling (should suggest specific exception)
        public void MethodWithGenericCatch()
        {
            try
            {
                int.Parse("not a number");
            }
            catch (Exception ex) // Should suggest FormatException
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
        
        // 17. Method with too many parameters (should suggest object parameter)
        public void MethodWithTooManyParameters(string param1, string param2, string param3, 
            string param4, string param5, string param6, string param7, string param8)
        {
            Console.WriteLine($"{param1} {param2} {param3} {param4} {param5} {param6} {param7} {param8}");
        }
        
        // 18. Dispose pattern not implemented (should suggest IDisposable)
        public class ResourceClass
        {
            private FileStream? fileStream;
            
            public void OpenFile(string path)
            {
                fileStream = new FileStream(path, FileMode.Open);
            }
            
            // Missing Dispose implementation
        }
    }
}
