using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StudentRegistrar.Data;
using StudentRegistrar.Models;
using Xunit;

namespace StudentRegistrar.Api.Tests.Contracts;

public class TenantModelContractTests
{
    [Fact]
    public void TenantEntities_Should_Have_Required_TenantId_QueryFilter_And_Guid_Primary_Key()
    {
        using var dbContext = CreateDbContext();
        var tenantEntityTypes = dbContext.Model.GetEntityTypes()
            .Where(entityType => typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            .ToArray();

        tenantEntityTypes.Should().NotBeEmpty();

        foreach (var entityType in tenantEntityTypes)
        {
            var tenantId = entityType.FindProperty(nameof(ITenantEntity.TenantId));
            tenantId.Should().NotBeNull($"{entityType.ClrType.Name} must be tenant scoped");
            tenantId!.ClrType.Should().Be<Guid>($"{entityType.ClrType.Name}.TenantId must match ITenantEntity");
            tenantId.IsNullable.Should().BeFalse($"{entityType.ClrType.Name}.TenantId must be required");

            entityType.GetDeclaredQueryFilters().Should().NotBeEmpty($"{entityType.ClrType.Name} must have a tenant query filter");

            var primaryKey = entityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull($"{entityType.ClrType.Name} must have a primary key");
            primaryKey!.Properties.Should().ContainSingle($"{entityType.ClrType.Name} should use a single primary key");
            primaryKey.Properties[0].ClrType.Should().Be<Guid>($"{entityType.ClrType.Name} should use Guid IDs before the clean EF baseline");
        }
    }

    [Fact]
    public void Tenant_Should_Not_Have_A_Tenant_Query_Filter()
    {
        using var dbContext = CreateDbContext();
        var tenantEntityType = dbContext.Model.FindEntityType(typeof(Tenant));

        tenantEntityType.Should().NotBeNull();
        tenantEntityType!.GetDeclaredQueryFilters().Should().BeEmpty("Tenant is the source of tenant resolution and is not tenant scoped");
        tenantEntityType.FindPrimaryKey()!.Properties.Should().ContainSingle();
        tenantEntityType.FindPrimaryKey()!.Properties[0].ClrType.Should().Be<Guid>();
    }

    [Fact]
    public void Entity_Model_Should_Not_Contain_Shadow_Foreign_Keys()
    {
        using var dbContext = CreateDbContext();
        var shadowForeignKeys = dbContext.Model.GetEntityTypes()
            .SelectMany(entityType => entityType.GetForeignKeys())
            .SelectMany(foreignKey => foreignKey.Properties)
            .Where(property => property.IsShadowProperty())
            .Select(property => $"{property.DeclaringType.DisplayName()}.{property.Name}")
            .ToArray();

        shadowForeignKeys.Should().BeEmpty("foreign keys should be explicit model properties in the clean EF baseline");
    }

    private static StudentRegistrarDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<StudentRegistrarDbContext>()
            .UseInMemoryDatabase($"TenantModelContractTests-{Guid.NewGuid()}")
            .Options;

        return new StudentRegistrarDbContext(options);
    }
}
