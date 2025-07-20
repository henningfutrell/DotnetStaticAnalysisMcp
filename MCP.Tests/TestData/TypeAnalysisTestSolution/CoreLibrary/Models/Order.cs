using System.ComponentModel.DataAnnotations;

namespace CoreLibrary.Models;

/// <summary>
/// Represents an order in the system
/// </summary>
[CustomerRelated("Order belongs to a customer")]
public class Order
{
    /// <summary>
    /// Gets or sets the order ID
    /// </summary>
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// Gets or sets the order date
    /// </summary>
    [Required]
    public DateTime OrderDate { get; set; }
    
    /// <summary>
    /// Gets or sets the total amount
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal TotalAmount { get; set; }
    
    /// <summary>
    /// Gets or sets the customer who placed this order
    /// </summary>
    public Customer? Customer { get; set; }
    
    /// <summary>
    /// Gets or sets the customer ID
    /// </summary>
    public int? CustomerId { get; set; }
    
    /// <summary>
    /// Gets the collection of order items
    /// </summary>
    public List<OrderItem> Items { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the order status
    /// </summary>
    public OrderStatus Status { get; set; }
    
    /// <summary>
    /// Creates a new order
    /// </summary>
    public Order()
    {
        OrderDate = DateTime.UtcNow;
        Status = OrderStatus.Pending;
    }
    
    /// <summary>
    /// Creates a new order for the specified customer
    /// </summary>
    /// <param name="customer">The customer</param>
    public Order(Customer customer) : this()
    {
        Customer = customer;
        CustomerId = customer?.Id;
    }
    
    /// <summary>
    /// Adds an item to the order
    /// </summary>
    /// <param name="item">The item to add</param>
    public void AddItem(OrderItem item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));
            
        item.Order = this;
        Items.Add(item);
        RecalculateTotal();
    }
    
    /// <summary>
    /// Recalculates the total amount
    /// </summary>
    private void RecalculateTotal()
    {
        TotalAmount = Items.Sum(i => i.Quantity * i.UnitPrice);
    }
    
    /// <summary>
    /// Processes the order
    /// </summary>
    public void Process()
    {
        if (Status == OrderStatus.Pending)
        {
            Status = OrderStatus.Processing;
        }
    }
}

/// <summary>
/// Order status enumeration
/// </summary>
public enum OrderStatus
{
    Pending,
    Processing,
    Shipped,
    Delivered,
    Cancelled
}

/// <summary>
/// Represents an item in an order
/// </summary>
public class OrderItem
{
    /// <summary>
    /// Gets or sets the item ID
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Gets or sets the product name
    /// </summary>
    [Required]
    [StringLength(200)]
    public string ProductName { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the quantity
    /// </summary>
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
    
    /// <summary>
    /// Gets or sets the unit price
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }
    
    /// <summary>
    /// Gets or sets the order this item belongs to
    /// </summary>
    public Order? Order { get; set; }
    
    /// <summary>
    /// Gets or sets the order ID
    /// </summary>
    public int? OrderId { get; set; }
    
    /// <summary>
    /// Gets the total price for this item
    /// </summary>
    public decimal TotalPrice => Quantity * UnitPrice;
}
