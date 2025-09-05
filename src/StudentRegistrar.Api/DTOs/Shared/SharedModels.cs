namespace StudentRegistrar.Api.DTOs;

public class AddressInfo
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}

public class EmergencyContactInfo
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? HomePhone { get; set; }
    public string? MobilePhone { get; set; }
    public string Email { get; set; } = string.Empty;
}

public class StudentInfoDetails
{
    public List<string> SpecialConditions { get; set; } = new();
    public List<string> Allergies { get; set; } = new();
    public List<string> Medications { get; set; } = new();
    public string? PreferredName { get; set; }
    public string? ParentNotes { get; set; }
}
