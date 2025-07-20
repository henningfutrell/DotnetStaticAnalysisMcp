using System;
using TestLibrary;

namespace TestProject;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        
        // This will cause CS0103 error - undeclared variable
        var result = undeclaredVariable + 5;
        
        // This will cause CS0246 error - unknown type
        UnknownType unknown = new UnknownType();
        
        var calculator = new Calculator();
        var sum = calculator.Add(10, 20);
        Console.WriteLine($"Sum: {sum}");
        
        // This will cause CS0161 error - not all code paths return a value
        var value = GetValue();
        Console.WriteLine(value);
    }
    
    static int GetValue()
    {
        var random = new Random();
        if (random.Next(0, 2) == 0)
        {
            return 42;
        }
        // Missing return statement for else case
    }
}
