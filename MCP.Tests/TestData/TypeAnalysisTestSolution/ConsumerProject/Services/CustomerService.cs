using CoreLibrary.Models;

namespace ConsumerProject.Services;

/// <summary>
/// Service for managing customers
/// Demonstrates various usage patterns of <see cref="Customer"/> type
/// </summary>
public class CustomerService : ICustomerService
{
    private readonly List<Customer> _customers = new();
    private readonly Dictionary<int, Customer> _customerCache = new();
    
    /// <summary>
    /// Event raised when a customer is created
    /// </summary>
    public event Action<Customer>? CustomerCreated;
    
    /// <summary>
    /// Creates a new customer
    /// </summary>
    /// <param name="name">The customer name</param>
    /// <param name="email">The customer email</param>
    /// <returns>The created customer</returns>
    public Customer CreateCustomer(string name, string email)
    {
        var customer = new Customer(name)
        {
            Email = email
        };
        
        _customers.Add(customer);
        _customerCache[customer.Id] = customer;
        
        CustomerCreated?.Invoke(customer);
        
        return customer;
    }
    
    /// <summary>
    /// Creates a customer with address
    /// </summary>
    /// <param name="name">The customer name</param>
    /// <param name="email">The customer email</param>
    /// <param name="address">The customer address</param>
    /// <returns>The created customer</returns>
    public Customer CreateCustomerWithAddress(string name, string email, Address address)
    {
        var customer = CreateCustomer(name, email);
        customer.Address = address;
        return customer;
    }
    
    /// <summary>
    /// Gets a customer by ID
    /// </summary>
    /// <param name="id">The customer ID</param>
    /// <returns>The customer or null if not found</returns>
    public Customer? GetCustomer(int id)
    {
        return _customerCache.TryGetValue(id, out Customer? customer) ? customer : null;
    }
    
    /// <summary>
    /// Gets all customers
    /// </summary>
    /// <returns>Collection of customers</returns>
    public IEnumerable<Customer> GetAllCustomers()
    {
        return _customers.AsReadOnly();
    }
    
    /// <summary>
    /// Gets customers by type
    /// </summary>
    /// <param name="type">The customer type</param>
    /// <returns>Customers of the specified type</returns>
    public IEnumerable<Customer> GetCustomersByType(CustomerType type)
    {
        // This is a simplified example - in reality you'd have a Type property on Customer
        return _customers.Where(c => DetermineCustomerType(c) == type);
    }
    
    /// <summary>
    /// Updates a customer
    /// </summary>
    /// <param name="customer">The customer to update</param>
    public void UpdateCustomer(Customer customer)
    {
        if (customer == null)
            throw new ArgumentNullException(nameof(customer));
            
        var existingCustomer = GetCustomer(customer.Id);
        if (existingCustomer != null)
        {
            existingCustomer.Name = customer.Name;
            existingCustomer.Email = customer.Email;
            existingCustomer.Address = customer.Address;
        }
    }
    
    /// <summary>
    /// Deletes a customer
    /// </summary>
    /// <param name="id">The customer ID</param>
    /// <returns>True if deleted, false if not found</returns>
    public bool DeleteCustomer(int id)
    {
        var customer = GetCustomer(id);
        if (customer != null)
        {
            _customers.Remove(customer);
            _customerCache.Remove(id);
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Gets customers with orders
    /// </summary>
    /// <returns>Customers who have placed orders</returns>
    public IEnumerable<Customer> GetCustomersWithOrders()
    {
        return _customers.Where(c => c.Orders.Any());
    }
    
    /// <summary>
    /// Validates customer data
    /// </summary>
    /// <param name="customer">The customer to validate</param>
    /// <returns>Validation result</returns>
    public CustomerValidationResult ValidateCustomer(Customer customer)
    {
        var result = new CustomerValidationResult();
        
        if (customer == null)
        {
            result.IsValid = false;
            result.Errors.Add("Customer cannot be null");
            return result;
        }
        
        if (!customer.IsValid())
        {
            result.IsValid = false;
            result.Errors.Add("Customer data is invalid");
        }
        
        if (customer.Address != null && !customer.Address.IsValid())
        {
            result.IsValid = false;
            result.Errors.Add("Customer address is invalid");
        }
        
        return result;
    }
    
    /// <summary>
    /// Processes customer orders
    /// </summary>
    /// <param name="customer">The customer</param>
    public void ProcessCustomerOrders(Customer customer)
    {
        foreach (Order order in customer.Orders)
        {
            if (order.Status == OrderStatus.Pending)
            {
                order.Process();
            }
        }
    }
    
    /// <summary>
    /// Determines the customer type based on order history
    /// </summary>
    /// <param name="customer">The customer</param>
    /// <returns>The customer type</returns>
    private CustomerType DetermineCustomerType(Customer customer)
    {
        var totalValue = customer.GetTotalOrderValue();
        
        return totalValue switch
        {
            >= 10000 => CustomerType.VIP,
            >= 5000 => CustomerType.Premium,
            >= 1000 => CustomerType.Corporate,
            _ => CustomerType.Regular
        };
    }
}

/// <summary>
/// Interface for customer service implementations
/// </summary>
public interface ICustomerService
{
    event Action<Customer>? CustomerCreated;
    
    Customer CreateCustomer(string name, string email);
    Customer CreateCustomerWithAddress(string name, string email, Address address);
    Customer? GetCustomer(int id);
    IEnumerable<Customer> GetAllCustomers();
    IEnumerable<Customer> GetCustomersByType(CustomerType type);
    void UpdateCustomer(Customer customer);
    bool DeleteCustomer(int id);
    IEnumerable<Customer> GetCustomersWithOrders();
    CustomerValidationResult ValidateCustomer(Customer customer);
    void ProcessCustomerOrders(Customer customer);
}

/// <summary>
/// Result of customer validation
/// </summary>
public class CustomerValidationResult
{
    public bool IsValid { get; set; } = true;
    public List<string> Errors { get; set; } = new();
}
