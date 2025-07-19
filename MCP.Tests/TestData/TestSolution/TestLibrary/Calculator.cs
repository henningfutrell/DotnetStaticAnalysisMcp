namespace TestLibrary;

public class Calculator
{
    public int Add(int a, int b)
    {
        return a + b;
    }
    
    public int Subtract(int a, int b)
    {
        return a - b;
    }
    
    // This method has a warning - unused parameter
    public void DoSomething(int unusedParameter)
    {
        Console.WriteLine("Doing something...");
    }
    
    // This will cause CS1002 error - missing semicolon
    public void BrokenMethod()
    {
        var x = 5
        Console.WriteLine(x);
    }
}
