using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace StudentRegistrar.Data;

/// <summary>
/// Design-time factory used by EF Core tooling (dotnet ef migrations) to create a
/// DbContext instance without requiring a running application or real database.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<StudentRegistrarDbContext>
{
    public StudentRegistrarDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<StudentRegistrarDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=studentregistrar_design;Username=postgres;Password=postgres");

        return new StudentRegistrarDbContext(optionsBuilder.Options, new DefaultTenantProvider());
    }
}
