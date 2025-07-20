using Xunit;
using CoverageTestProject;

namespace CoverageTestProject.Tests;

/// <summary>
/// Unit tests for the Calculator class
/// Note: Some methods are intentionally not tested to demonstrate coverage gaps
/// </summary>
public class CalculatorTests
{
    private readonly Calculator _calculator = new();

    [Fact]
    public void Add_ShouldReturnCorrectSum()
    {
        // Arrange
        int a = 5;
        int b = 3;

        // Act
        int result = _calculator.Add(a, b);

        // Assert
        Assert.Equal(8, result);
    }

    [Fact]
    public void Add_WithNegativeNumbers_ShouldReturnCorrectSum()
    {
        // Arrange
        int a = -5;
        int b = 3;

        // Act
        int result = _calculator.Add(a, b);

        // Assert
        Assert.Equal(-2, result);
    }

    [Fact]
    public void Subtract_ShouldReturnCorrectDifference()
    {
        // Arrange
        int a = 10;
        int b = 4;

        // Act
        int result = _calculator.Subtract(a, b);

        // Assert
        Assert.Equal(6, result);
    }

    [Fact]
    public void Multiply_ShouldReturnCorrectProduct()
    {
        // Arrange
        int a = 6;
        int b = 7;

        // Act
        int result = _calculator.Multiply(a, b);

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public void Divide_ShouldReturnCorrectQuotient()
    {
        // Arrange
        int a = 15;
        int b = 3;

        // Act
        double result = _calculator.Divide(a, b);

        // Assert
        Assert.Equal(5.0, result);
    }

    [Fact]
    public void Divide_ByZero_ShouldThrowDivideByZeroException()
    {
        // Arrange
        int a = 10;
        int b = 0;

        // Act & Assert
        Assert.Throws<DivideByZeroException>(() => _calculator.Divide(a, b));
    }

    [Fact]
    public void Factorial_OfZero_ShouldReturnOne()
    {
        // Act
        long result = _calculator.Factorial(0);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public void Factorial_OfPositiveNumber_ShouldReturnCorrectValue()
    {
        // Act
        long result = _calculator.Factorial(5);

        // Assert
        Assert.Equal(120, result);
    }

    [Fact]
    public void Factorial_OfNegativeNumber_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _calculator.Factorial(-1));
    }

    [Fact]
    public void IsEven_WithEvenNumber_ShouldReturnTrue()
    {
        // Act
        bool result = _calculator.IsEven(4);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsEven_WithOddNumber_ShouldReturnFalse()
    {
        // Act
        bool result = _calculator.IsEven(5);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Max_FirstNumberLarger_ShouldReturnFirstNumber()
    {
        // Act
        int result = _calculator.Max(10, 5);

        // Assert
        Assert.Equal(10, result);
    }

    [Fact]
    public void Max_SecondNumberLarger_ShouldReturnSecondNumber()
    {
        // Act
        int result = _calculator.Max(3, 8);

        // Assert
        Assert.Equal(8, result);
    }

    // Note: Abs and Power methods are intentionally not tested
    // to demonstrate uncovered code in coverage reports
}
