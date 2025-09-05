using System.ComponentModel.DataAnnotations;

namespace StudentRegistrar.Api.DTOs;

public class AccountHolderDto
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string? HomePhone { get; set; }
    public string? MobilePhone { get; set; }
    public AddressInfo AddressJson { get; set; } = new();
    public EmergencyContactInfo EmergencyContactJson { get; set; } = new();
    public decimal MembershipDuesOwed { get; set; }
    public decimal MembershipDuesReceived { get; set; }
    public DateTime MemberSince { get; set; }
    public DateTime? LastLogin { get; set; }
    public DateTime LastEdit { get; set; }
    public List<StudentDetailDto> Students { get; set; } = new();
    public List<PaymentDto> Payments { get; set; } = new();
}
