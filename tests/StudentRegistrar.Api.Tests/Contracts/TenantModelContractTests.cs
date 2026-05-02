using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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

    [Fact]
    public async Task TenantScopedQueries_Should_ReturnOnlyCurrentTenantRows_InSaaSMode()
    {
        var databaseName = $"TenantIsolationTests-{Guid.NewGuid()}";
        var databaseRoot = new InMemoryDatabaseRoot();
        var tenantAId = Guid.NewGuid();
        var tenantBId = Guid.NewGuid();

        await using (var seedContext = CreateDbContext(databaseName, databaseRoot, new TestTenantProvider(null, shouldApplyTenantFilter: false)))
        {
            seedContext.Rooms.AddRange(
                new Room { TenantId = tenantAId, Name = "Tenant A Room", Capacity = 20 },
                new Room { TenantId = tenantBId, Name = "Tenant B Room", Capacity = 30 });

            await seedContext.SaveChangesAsync();
        }

        await using (var tenantAContext = CreateDbContext(databaseName, databaseRoot, new TestTenantProvider(tenantAId, shouldApplyTenantFilter: true)))
        {
            var tenantARooms = await tenantAContext.Rooms.Select(room => room.Name).ToArrayAsync();

            tenantARooms.Should().ContainSingle().Which.Should().Be("Tenant A Room");
        }

        await using (var tenantBContext = CreateDbContext(databaseName, databaseRoot, new TestTenantProvider(tenantBId, shouldApplyTenantFilter: true)))
        {
            var tenantBRooms = await tenantBContext.Rooms.Select(room => room.Name).ToArrayAsync();

            tenantBRooms.Should().ContainSingle().Which.Should().Be("Tenant B Room");
        }

        await using (var missingTenantContext = CreateDbContext(databaseName, databaseRoot, new TestTenantProvider(null, shouldApplyTenantFilter: true)))
        {
            var visibleRooms = await missingTenantContext.Rooms.ToArrayAsync();

            visibleRooms.Should().BeEmpty("SaaS requests without tenant context must not see tenant-scoped data");
        }

        await using (var selfHostedContext = CreateDbContext(databaseName, databaseRoot, new TestTenantProvider(null, shouldApplyTenantFilter: false)))
        {
            var visibleRooms = await selfHostedContext.Rooms.Select(room => room.Name).ToArrayAsync();

            visibleRooms.Should().BeEquivalentTo("Tenant A Room", "Tenant B Room");
        }
    }

    private static StudentRegistrarDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<StudentRegistrarDbContext>()
            .UseInMemoryDatabase($"TenantModelContractTests-{Guid.NewGuid()}")
            .Options;

        return new StudentRegistrarDbContext(options);
    }

    private static StudentRegistrarDbContext CreateDbContext(
        string databaseName,
        InMemoryDatabaseRoot databaseRoot,
        ITenantProvider tenantProvider)
    {
        var options = new DbContextOptionsBuilder<StudentRegistrarDbContext>()
            .UseInMemoryDatabase(databaseName, databaseRoot)
            .Options;

        return new StudentRegistrarDbContext(options, tenantProvider);
    }

    private sealed class TestTenantProvider : ITenantProvider
    {
        public TestTenantProvider(Guid? currentTenantId, bool shouldApplyTenantFilter)
        {
            CurrentTenantId = currentTenantId;
            ShouldApplyTenantFilter = shouldApplyTenantFilter;
        }

        public Guid? CurrentTenantId { get; }
        public bool ShouldApplyTenantFilter { get; }
    }
}
