using StudentRegistrar.Models;
using System.Text.Json;
using Xunit;

namespace StudentRegistrar.Models.Tests;

public class AccountHolderTests
{
    [Fact]
    public void AccountHolder_Should_HaveDefaultValues()
    {
        // Act
        var accountHolder = new AccountHolder();
        var now = DateTime.UtcNow;

        // Assert
        Assert.NotEqual(Guid.Empty, accountHolder.Id);
        Assert.Equal(string.Empty, accountHolder.FirstName);
        Assert.Equal(string.Empty, accountHolder.LastName);
        Assert.Equal(string.Empty, accountHolder.EmailAddress);
        Assert.Equal(string.Empty, accountHolder.KeycloakUserId);
        Assert.Equal(0, accountHolder.MembershipDuesOwed);
        Assert.Equal(0, accountHolder.MembershipDuesReceived);
        Assert.Equal("{}", accountHolder.AddressJson);
        Assert.Equal("{}", accountHolder.EmergencyContactJson);
        Assert.InRange(accountHolder.MemberSince, now - TimeSpan.FromSeconds(1), now + TimeSpan.FromSeconds(1));
        Assert.InRange(accountHolder.CreatedAt, now - TimeSpan.FromSeconds(1), now + TimeSpan.FromSeconds(1));
        Assert.InRange(accountHolder.UpdatedAt, now - TimeSpan.FromSeconds(1), now + TimeSpan.FromSeconds(1));
        Assert.NotNull(accountHolder.Students);
        Assert.Empty(accountHolder.Students);
        Assert.NotNull(accountHolder.Payments);
        Assert.Empty(accountHolder.Payments);
    }

    [Fact]
    public void FullName_Should_CombineFirstAndLastName()
    {
        // Arrange
        var accountHolder = new AccountHolder
        {
            FirstName = "John",
            LastName = "Doe"
        };

        // Act & Assert
        Assert.Equal("John Doe", accountHolder.FullName);
    }

    [Fact]
    public void MembershipDuesBalance_Should_CalculateCorrectly()
    {
        // Arrange
        var accountHolder = new AccountHolder
        {
            MembershipDuesOwed = 100.50m,
            MembershipDuesReceived = 75.25m
        };

        // Act & Assert
        Assert.Equal(25.25m, accountHolder.MembershipDuesBalance);
    }

    [Fact]
    public void GetAddress_Should_ReturnValidAddress_WhenJsonIsValid()
    {
        // Arrange
        var accountHolder = new AccountHolder();
        var address = new Address
        {
            Street = "123 Main St",
            City = "Springfield",
            State = "IL",
            PostalCode = "62701",
            Country = "US"
        };
        accountHolder.SetAddress(address);

        // Act
        var retrievedAddress = accountHolder.GetAddress();

        // Assert
        Assert.NotNull(retrievedAddress);
        Assert.Equal("123 Main St", retrievedAddress.Street);
        Assert.Equal("Springfield", retrievedAddress.City);
        Assert.Equal("IL", retrievedAddress.State);
        Assert.Equal("62701", retrievedAddress.PostalCode);
        Assert.Equal("US", retrievedAddress.Country);
    }

    [Fact]
    public void GetAddress_Should_ReturnEmptyAddress_WhenJsonIsInvalid()
    {
        // Arrange
        var accountHolder = new AccountHolder
        {
            AddressJson = "invalid json"
        };

        // Act
        var address = accountHolder.GetAddress();

        // Assert
        Assert.NotNull(address);
        Assert.Equal(string.Empty, address.Street);
        Assert.Equal(string.Empty, address.City);
        Assert.Equal(string.Empty, address.State);
        Assert.Equal(string.Empty, address.PostalCode);
        Assert.Equal("US", address.Country);
    }

    [Fact]
    public void SetAddress_Should_SerializeCorrectly()
    {
        // Arrange
        var accountHolder = new AccountHolder();
        var address = new Address
        {
            Street = "456 Oak Ave",
            City = "Chicago",
            State = "IL",
            PostalCode = "60601"
        };

        // Act
        accountHolder.SetAddress(address);

        // Assert
        Assert.NotEqual("{}", accountHolder.AddressJson);
        
        // Verify we can deserialize it back
        var deserializedAddress = JsonSerializer.Deserialize<Address>(accountHolder.AddressJson);
        Assert.NotNull(deserializedAddress);
        Assert.Equal("456 Oak Ave", deserializedAddress!.Street);
        Assert.Equal("Chicago", deserializedAddress.City);
    }

    [Fact]
    public void GetEmergencyContact_Should_ReturnValidContact_WhenJsonIsValid()
    {
        // Arrange
        var accountHolder = new AccountHolder();
        var emergencyContact = new EmergencyContact
        {
            FirstName = "Jane",
            LastName = "Smith",
            MobilePhone = "555-0123",
            Email = "jane.smith@example.com",
            Address = new Address { Street = "789 Pine St", City = "Springfield" }
        };
        accountHolder.SetEmergencyContact(emergencyContact);

        // Act
        var retrievedContact = accountHolder.GetEmergencyContact();

        // Assert
        Assert.NotNull(retrievedContact);
        Assert.Equal("Jane", retrievedContact.FirstName);
        Assert.Equal("Smith", retrievedContact.LastName);
        Assert.Equal("555-0123", retrievedContact.MobilePhone);
        Assert.Equal("jane.smith@example.com", retrievedContact.Email);
        Assert.Equal("Jane Smith", retrievedContact.FullName);
        Assert.NotNull(retrievedContact.Address);
        Assert.Equal("789 Pine St", retrievedContact.Address.Street);
    }

    [Fact]
    public void GetEmergencyContact_Should_ReturnEmptyContact_WhenJsonIsInvalid()
    {
        // Arrange
        var accountHolder = new AccountHolder
        {
            EmergencyContactJson = "invalid json"
        };

        // Act
        var contact = accountHolder.GetEmergencyContact();

        // Assert
        Assert.NotNull(contact);
        Assert.Equal(string.Empty, contact.FirstName);
        Assert.Equal(string.Empty, contact.LastName);
        Assert.Equal(" ", contact.FullName);
        Assert.NotNull(contact.Address);
    }

    [Theory]
    [InlineData("", "", " ")]
    [InlineData("John", "", "John ")]
    [InlineData("", "Doe", " Doe")]
    [InlineData("John", "Doe", "John Doe")]
    public void FullName_Should_HandleVariousNameCombinations(string firstName, string lastName, string expected)
    {
        // Arrange
        var accountHolder = new AccountHolder
        {
            FirstName = firstName,
            LastName = lastName
        };

        // Act & Assert
        Assert.Equal(expected, accountHolder.FullName);
    }

    [Fact]
    public void Address_DefaultCountry_Should_BeUS()
    {
        // Arrange & Act
        var address = new Address();

        // Assert
        Assert.Equal("US", address.Country);
    }

    [Fact]
    public void EmergencyContact_FullName_Should_CombineNames()
    {
        // Arrange
        var contact = new EmergencyContact
        {
            FirstName = "Emergency",
            LastName = "Contact"
        };

        // Act & Assert
        Assert.Equal("Emergency Contact", contact.FullName);
    }

    [Fact]
    public void EmergencyContact_Should_HaveDefaultAddress()
    {
        // Arrange & Act
        var contact = new EmergencyContact();

        // Assert
        Assert.NotNull(contact.Address);
        Assert.Equal("US", contact.Address.Country);
    }
}
