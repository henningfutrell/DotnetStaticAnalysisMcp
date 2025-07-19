namespace TestLibrary;

/// <summary>
/// A valid class with no compilation errors
/// </summary>
public class ValidClass
{
    public string Name { get; set; } = string.Empty;
    
    public int Value { get; set; }
    
    public ValidClass()
    {
    }
    
    public ValidClass(string name, int value)
    {
        Name = name;
        Value = value;
    }
    
    public string GetDescription()
    {
        return $"Name: {Name}, Value: {Value}";
    }
    
    public override string ToString()
    {
        return GetDescription();
    }
}
