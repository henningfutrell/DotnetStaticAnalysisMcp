using System.ComponentModel.DataAnnotations;

namespace CoreLibrary.Models;

/// <summary>
/// Represents an address
/// </summary>
[CustomerRelated("Address is used by customers")]
public class Address
{
    /// <summary>
    /// Gets or sets the street address
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Street { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the city
    /// </summary>
    [Required]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the state or province
    /// </summary>
    [StringLength(50)]
    public string? State { get; set; }
    
    /// <summary>
    /// Gets or sets the postal code
    /// </summary>
    [Required]
    [StringLength(20)]
    public string PostalCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the country
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Country { get; set; } = string.Empty;
    
    /// <summary>
    /// Creates a new address
    /// </summary>
    public Address()
    {
    }
    
    /// <summary>
    /// Creates a new address with the specified details
    /// </summary>
    /// <param name="street">The street address</param>
    /// <param name="city">The city</param>
    /// <param name="postalCode">The postal code</param>
    /// <param name="country">The country</param>
    public Address(string street, string city, string postalCode, string country)
    {
        Street = street;
        City = city;
        PostalCode = postalCode;
        Country = country;
    }
    
    /// <summary>
    /// Gets the full address as a formatted string
    /// </summary>
    /// <returns>The formatted address</returns>
    public string GetFullAddress()
    {
        var parts = new List<string> { Street, City };
        
        if (!string.IsNullOrWhiteSpace(State))
            parts.Add(State);
            
        parts.Add(PostalCode);
        parts.Add(Country);
        
        return string.Join(", ", parts);
    }
    
    /// <summary>
    /// Validates the address
    /// </summary>
    /// <returns>True if valid, false otherwise</returns>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Street) &&
               !string.IsNullOrWhiteSpace(City) &&
               !string.IsNullOrWhiteSpace(PostalCode) &&
               !string.IsNullOrWhiteSpace(Country);
    }
    
    /// <summary>
    /// Returns a string representation of the address
    /// </summary>
    /// <returns>The address string</returns>
    public override string ToString()
    {
        return GetFullAddress();
    }
}
