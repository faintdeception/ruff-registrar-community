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

        Assert.NotEmpty(tenantEntityTypes);

        foreach (var entityType in tenantEntityTypes)
        {
            var tenantId = entityType.FindProperty(nameof(ITenantEntity.TenantId));
            Assert.True(tenantId is not null, $"{entityType.ClrType.Name} must be tenant scoped");
            Assert.Equal(typeof(Guid), tenantId!.ClrType);
            Assert.False(tenantId.IsNullable);

            Assert.NotEmpty(entityType.GetDeclaredQueryFilters());

            var primaryKey = entityType.FindPrimaryKey();
            Assert.True(primaryKey is not null, $"{entityType.ClrType.Name} must have a primary key");
            var primaryKeyProperty = Assert.Single(primaryKey!.Properties);
            Assert.Equal(typeof(Guid), primaryKeyProperty.ClrType);
        }
    }

    [Fact]
    public void Tenant_Should_Not_Have_A_Tenant_Query_Filter()
    {
        using var dbContext = CreateDbContext();
        var tenantEntityType = dbContext.Model.FindEntityType(typeof(Tenant));

        Assert.NotNull(tenantEntityType);
        Assert.Empty(tenantEntityType!.GetDeclaredQueryFilters());
        var primaryKeyProperty = Assert.Single(tenantEntityType.FindPrimaryKey()!.Properties);
        Assert.Equal(typeof(Guid), primaryKeyProperty.ClrType);
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

        Assert.Empty(shadowForeignKeys);
    }

    private static StudentRegistrarDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<StudentRegistrarDbContext>()
            .UseInMemoryDatabase($"TenantModelContractTests-{Guid.NewGuid()}")
            .Options;

        return new StudentRegistrarDbContext(options);
    }
}
