namespace CoverageTestProject;

/// <summary>
/// A simple calculator class for demonstrating code coverage
/// </summary>
public class Calculator
{
    /// <summary>
    /// Adds two numbers
    /// </summary>
    /// <param name="a">First number</param>
    /// <param name="b">Second number</param>
    /// <returns>Sum of the two numbers</returns>
    public int Add(int a, int b)
    {
        return a + b;
    }

    /// <summary>
    /// Subtracts two numbers
    /// </summary>
    /// <param name="a">First number</param>
    /// <param name="b">Second number</param>
    /// <returns>Difference of the two numbers</returns>
    public int Subtract(int a, int b)
    {
        return a - b;
    }

    /// <summary>
    /// Multiplies two numbers
    /// </summary>
    /// <param name="a">First number</param>
    /// <param name="b">Second number</param>
    /// <returns>Product of the two numbers</returns>
    public int Multiply(int a, int b)
    {
        return a * b;
    }

    /// <summary>
    /// Divides two numbers
    /// </summary>
    /// <param name="a">Dividend</param>
    /// <param name="b">Divisor</param>
    /// <returns>Quotient of the division</returns>
    /// <exception cref="DivideByZeroException">Thrown when divisor is zero</exception>
    public double Divide(int a, int b)
    {
        if (b == 0)
        {
            throw new DivideByZeroException("Cannot divide by zero");
        }
        return (double)a / b;
    }

    /// <summary>
    /// Calculates the factorial of a number
    /// </summary>
    /// <param name="n">The number to calculate factorial for</param>
    /// <returns>Factorial of the number</returns>
    /// <exception cref="ArgumentException">Thrown when number is negative</exception>
    public long Factorial(int n)
    {
        if (n < 0)
        {
            throw new ArgumentException("Factorial is not defined for negative numbers");
        }

        if (n == 0 || n == 1)
        {
            return 1;
        }

        long result = 1;
        for (int i = 2; i <= n; i++)
        {
            result *= i;
        }
        return result;
    }

    /// <summary>
    /// Checks if a number is even
    /// </summary>
    /// <param name="number">The number to check</param>
    /// <returns>True if even, false if odd</returns>
    public bool IsEven(int number)
    {
        return number % 2 == 0;
    }

    /// <summary>
    /// Finds the maximum of two numbers
    /// </summary>
    /// <param name="a">First number</param>
    /// <param name="b">Second number</param>
    /// <returns>The larger of the two numbers</returns>
    public int Max(int a, int b)
    {
        if (a > b)
        {
            return a;
        }
        else
        {
            return b;
        }
    }

    /// <summary>
    /// Calculates the absolute value of a number
    /// </summary>
    /// <param name="number">The number</param>
    /// <returns>Absolute value of the number</returns>
    public int Abs(int number)
    {
        // This method is intentionally not covered by tests
        if (number < 0)
        {
            return -number;
        }
        return number;
    }

    /// <summary>
    /// Calculates the power of a number
    /// </summary>
    /// <param name="baseNumber">The base number</param>
    /// <param name="exponent">The exponent</param>
    /// <returns>Base raised to the power of exponent</returns>
    public double Power(double baseNumber, int exponent)
    {
        // This method is also intentionally not covered by tests
        if (exponent == 0)
        {
            return 1;
        }

        if (exponent < 0)
        {
            return 1.0 / Power(baseNumber, -exponent);
        }

        double result = 1;
        for (int i = 0; i < exponent; i++)
        {
            result *= baseNumber;
        }
        return result;
    }
}
