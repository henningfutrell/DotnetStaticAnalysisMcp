using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CoreLibrary.Models;

/// <summary>
/// Represents a customer in the system.
/// See also: <see cref="Order"/> and <see cref="Address"/>
/// </summary>
[Serializable]
public class Customer : ICustomer, INotifyPropertyChanged
{
    private string _name = string.Empty;
    private Address? _address;
    
    /// <summary>
    /// Gets or sets the customer ID
    /// </summary>
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// Gets or sets the customer name
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name 
    { 
        get => _name;
        set
        {
            _name = value;
            OnPropertyChanged(nameof(Name));
        }
    }
    
    /// <summary>
    /// Gets or sets the customer's email address
    /// </summary>
    [EmailAddress]
    public string? Email { get; set; }
    
    /// <summary>
    /// Gets or sets the customer's address
    /// </summary>
    public Address? Address 
    { 
        get => _address;
        set
        {
            _address = value;
            OnPropertyChanged(nameof(Address));
        }
    }
    
    /// <summary>
    /// Gets the collection of orders for this customer
    /// </summary>
    public List<Order> Orders { get; set; } = new();
    
    /// <summary>
    /// Event raised when a property changes
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;
    
    /// <summary>
    /// Creates a new customer instance
    /// </summary>
    public Customer()
    {
    }
    
    /// <summary>
    /// Creates a new customer with the specified name
    /// </summary>
    /// <param name="name">The customer name</param>
    public Customer(string name)
    {
        Name = name;
    }
    
    /// <summary>
    /// Adds an order to this customer
    /// </summary>
    /// <param name="order">The order to add</param>
    public void AddOrder(Order order)
    {
        if (order == null)
            throw new ArgumentNullException(nameof(order));
            
        order.Customer = this;
        Orders.Add(order);
    }
    
    /// <summary>
    /// Gets the total value of all orders
    /// </summary>
    /// <returns>The total order value</returns>
    public decimal GetTotalOrderValue()
    {
        return Orders.Sum(o => o.TotalAmount);
    }
    
    /// <summary>
    /// Validates the customer data
    /// </summary>
    /// <returns>True if valid, false otherwise</returns>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Name) && 
               (Email == null || Email.Contains('@'));
    }
    
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Interface for customer implementations
/// </summary>
public interface ICustomer
{
    int Id { get; set; }
    string Name { get; set; }
    string? Email { get; set; }
    Address? Address { get; set; }
    List<Order> Orders { get; set; }
    
    void AddOrder(Order order);
    decimal GetTotalOrderValue();
    bool IsValid();
}

/// <summary>
/// Customer types enumeration
/// </summary>
public enum CustomerType
{
    Regular,
    Premium,
    VIP,
    Corporate
}

/// <summary>
/// Attribute for marking customer-related classes
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
public class CustomerRelatedAttribute : Attribute
{
    public string Description { get; set; } = string.Empty;
    
    public CustomerRelatedAttribute(string description)
    {
        Description = description;
    }
}
