using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace StudentRegistrar.Models;

public class AccountHolder : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// The tenant (organization) this account holder belongs to.
    /// </summary>
    [Required]
    public Guid TenantId { get; set; }
    
    // Primary Contact
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    [Column(TypeName = "jsonb")]
    public string AddressJson { get; set; } = "{}";
    
    [Phone]
    [MaxLength(20)]
    public string? HomePhone { get; set; }
    
    [Phone]
    [MaxLength(20)]
    public string? MobilePhone { get; set; }
    
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string EmailAddress { get; set; } = string.Empty;
    
    // Membership Financial Info
    [Column(TypeName = "decimal(10,2)")]
    public decimal MembershipDuesOwed { get; set; } = 0;
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal MembershipDuesReceived { get; set; } = 0;
    
    [NotMapped]
    public decimal MembershipDuesBalance => MembershipDuesOwed - MembershipDuesReceived;
    
    // Emergency Contact (JSON for flexibility)
    [Column(TypeName = "jsonb")]
    public string EmergencyContactJson { get; set; } = "{}";
    
    // Misc Info
    public DateTime MemberSince { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }
    public DateTime LastEdit { get; set; } = DateTime.UtcNow;
    
    // Keycloak Integration
    [Required]
    [MaxLength(255)]
    public string KeycloakUserId { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation Properties
    public virtual ICollection<Student> Students { get; set; } = new List<Student>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public virtual ICollection<CourseInstructor> CourseInstructors { get; set; } = new List<CourseInstructor>();
    
    // Computed Properties
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";
    
    // Helper methods for JSON fields
    public Address GetAddress()
    {
        try
        {
            return JsonSerializer.Deserialize<Address>(AddressJson) ?? new Address();
        }
        catch
        {
            return new Address();
        }
    }
    
    public void SetAddress(Address address)
    {
        AddressJson = JsonSerializer.Serialize(address);
    }
    
    public EmergencyContact GetEmergencyContact()
    {
        try
        {
            return JsonSerializer.Deserialize<EmergencyContact>(EmergencyContactJson) ?? new EmergencyContact();
        }
        catch
        {
            return new EmergencyContact();
        }
    }
    
    public void SetEmergencyContact(EmergencyContact contact)
    {
        EmergencyContactJson = JsonSerializer.Serialize(contact);
    }
}

// Supporting value objects
public class Address
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = "US";
}

public class EmergencyContact
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public Address Address { get; set; } = new();
    public string? HomePhone { get; set; }
    public string? MobilePhone { get; set; }
    public string? Email { get; set; }
    
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";
}
