using System.ComponentModel.DataAnnotations;

namespace StudentRegistrar.Api.DTOs;

public class CreateAccountHolderDto
{
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string EmailAddress { get; set; } = string.Empty;
    
    [StringLength(20)]
    public string? HomePhone { get; set; }
    
    [StringLength(20)]
    public string? MobilePhone { get; set; }
    
    public AddressInfo? AddressJson { get; set; }
    public EmergencyContactInfo? EmergencyContactJson { get; set; }
}

public class UpdateAccountHolderDto
{
    [StringLength(100)]
    public string? FirstName { get; set; }
    
    [StringLength(100)]
    public string? LastName { get; set; }
    
    [EmailAddress]
    [StringLength(255)]
    public string? EmailAddress { get; set; }
    
    [StringLength(20)]
    public string? HomePhone { get; set; }
    
    [StringLength(20)]
    public string? MobilePhone { get; set; }
    
    public AddressInfo? AddressJson { get; set; }
    public EmergencyContactInfo? EmergencyContactJson { get; set; }
}

public class CreateAccountHolderResponse
{
    public AccountHolderDto AccountHolder { get; set; } = null!;
    public UserCredentials? Credentials { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class UserCredentials
{
    public string Username { get; set; } = string.Empty;
    public string TemporaryPassword { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; } = true;
}
