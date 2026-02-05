using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRegistrar.Models;

namespace StudentRegistrar.Data;

/// <summary>
/// Interface for providing the current tenant ID to the DbContext.
/// This allows the DbContext to be constructed without a direct dependency
/// on the API layer's ITenantContext.
/// </summary>
public interface ITenantProvider
{
    /// <summary>
    /// Gets the current tenant ID for query filtering.
    /// Returns null if no tenant context is available (e.g., portal routes).
    /// </summary>
    Guid? CurrentTenantId { get; }
    
    /// <summary>
    /// Whether tenant filtering should be applied.
    /// False in self-hosted mode or when accessing tenant-agnostic data.
    /// </summary>
    bool ShouldApplyTenantFilter { get; }
}

/// <summary>
/// Default tenant provider that applies no filtering.
/// Used for migrations and self-hosted mode.
/// </summary>
public class DefaultTenantProvider : ITenantProvider
{
    public Guid? CurrentTenantId => null;
    public bool ShouldApplyTenantFilter => false;
}

public class StudentRegistrarDbContext : DbContext
{
    private readonly ITenantProvider _tenantProvider;
    
    /// <summary>
    /// Constructor for EF Core migrations and design-time tools.
    /// This simpler constructor is used by migration tools that don't have access to DI.
    /// It delegates to the primary constructor with a DefaultTenantProvider.
    /// Note: This is internal to prevent accidental use in application code where
    /// the DI-based constructor should be used instead.
    /// </summary>
    internal StudentRegistrarDbContext(DbContextOptions<StudentRegistrarDbContext> options) 
        : this(options, new DefaultTenantProvider())
    {
    }
    
    /// <summary>
    /// Primary constructor for runtime use with dependency injection.
    /// This constructor receives ITenantProvider from DI and is marked with
    /// [ActivatorUtilitiesConstructor] to ensure DI uses this constructor.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public StudentRegistrarDbContext(
        DbContextOptions<StudentRegistrarDbContext> options,
        ITenantProvider tenantProvider) : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    // Tenant management (not filtered by tenant)
    public DbSet<Tenant> Tenants { get; set; }
    
    // Current entities (filtered by tenant in SaaS mode)
    public DbSet<Student> Students { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<Enrollment> Enrollments { get; set; }
    public DbSet<GradeRecord> GradeRecords { get; set; }
    public DbSet<AcademicYear> AcademicYears { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<AccountHolder> AccountHolders { get; set; }
    public DbSet<Semester> Semesters { get; set; }
    public DbSet<CourseInstructor> CourseInstructors { get; set; }
    public DbSet<Educator> Educators { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Room> Rooms { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Tenant (not filtered - this is the source of tenant data)
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Subdomain).IsRequired().HasMaxLength(63);
            entity.Property(e => e.SubscriptionTier).IsRequired();
            entity.Property(e => e.SubscriptionStatus).IsRequired();
            entity.Property(e => e.StripeCustomerId).HasMaxLength(255);
            entity.Property(e => e.StripeSubscriptionId).HasMaxLength(255);
            entity.Property(e => e.LogoBase64).HasMaxLength(700000);
            entity.Property(e => e.LogoMimeType).HasMaxLength(50);
            entity.Property(e => e.ThemeConfigJson).HasColumnType("jsonb");
            entity.Property(e => e.KeycloakRealm).IsRequired().HasMaxLength(100);
            entity.Property(e => e.AdminEmail).IsRequired().HasMaxLength(255);
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasIndex(e => e.Subdomain).IsUnique();
            entity.HasIndex(e => e.KeycloakRealm).IsUnique();
        });

        // Apply global query filters for tenant isolation
        // These filters are automatically applied to all queries in SaaS mode
        ConfigureTenantFilters(modelBuilder);

        // Configure Current Entities
        
        // Configure AccountHolder
        modelBuilder.Entity<AccountHolder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EmailAddress).IsRequired().HasMaxLength(255);
            entity.Property(e => e.HomePhone).HasMaxLength(20);
            entity.Property(e => e.MobilePhone).HasMaxLength(20);
            entity.Property(e => e.AddressJson).HasColumnType("jsonb");
            entity.Property(e => e.EmergencyContactJson).HasColumnType("jsonb");
            entity.Property(e => e.MembershipDuesOwed).HasPrecision(10, 2);
            entity.Property(e => e.MembershipDuesReceived).HasPrecision(10, 2);
            entity.Property(e => e.KeycloakUserId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasIndex(e => new { e.TenantId, e.EmailAddress }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.KeycloakUserId }).IsUnique();
            entity.HasIndex(e => e.TenantId);
        });

        // Configure Semester
        modelBuilder.Entity<Semester>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.StartDate).IsRequired();
            entity.Property(e => e.EndDate).IsRequired();
            entity.Property(e => e.RegistrationStartDate).IsRequired();
            entity.Property(e => e.RegistrationEndDate).IsRequired();
            entity.Property(e => e.PeriodConfigJson).HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();
            entity.HasIndex(e => e.TenantId);
        });

        // Configure Student
        modelBuilder.Entity<Student>(entity =>
        {
            entity.ToTable("Students");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Grade).HasMaxLength(20);
            entity.Property(e => e.StudentInfoJson).HasColumnType("jsonb");
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasOne(e => e.AccountHolder)
                .WithMany(a => a.Students)
                .HasForeignKey(e => e.AccountHolderId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasIndex(e => e.TenantId);
        });

        // Configure Course
        modelBuilder.Entity<Course>(entity =>
        {
            entity.ToTable("Courses");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.MaxCapacity).IsRequired();
            entity.Property(e => e.Fee).HasPrecision(10, 2);
            entity.Property(e => e.PeriodCode).HasMaxLength(50);
            entity.Property(e => e.CourseConfigJson).HasColumnType("jsonb");
            entity.Property(e => e.AgeGroup).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasOne(e => e.Semester)
                .WithMany(s => s.Courses)
                .HasForeignKey(e => e.SemesterId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Room)
                .WithMany(r => r.Courses)
                .HasForeignKey(e => e.RoomId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasIndex(e => e.TenantId);
        });

        // Configure Room
        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Capacity).IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.RoomType).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasIndex(e => new { e.TenantId, e.Name }).IsUnique();
            entity.HasIndex(e => e.TenantId);
        });

        // Configure CourseInstructor
        modelBuilder.Entity<CourseInstructor>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.StripeAccountId).HasMaxLength(255);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.InstructorInfoJson).HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasOne(e => e.Course)
                .WithMany(c => c.CourseInstructors)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasIndex(e => e.TenantId);
        });

        // Configure Enrollment
        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.ToTable("Enrollments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.EnrollmentType).IsRequired();
            entity.Property(e => e.EnrollmentDate).IsRequired();
            entity.Property(e => e.FeeAmount).HasPrecision(10, 2);
            entity.Property(e => e.AmountPaid).HasPrecision(10, 2);
            entity.Property(e => e.PaymentStatus).IsRequired();
            entity.Property(e => e.EnrollmentInfoJson).HasColumnType("jsonb");
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasOne(e => e.Student)
                .WithMany(s => s.Enrollments)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Course)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Semester)
                .WithMany(s => s.Enrollments)
                .HasForeignKey(e => e.SemesterId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.StudentId, e.CourseId, e.SemesterId }).IsUnique();
            entity.HasIndex(e => e.TenantId);
        });

        // Configure Payment
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Amount).HasPrecision(10, 2);
            entity.Property(e => e.PaymentDate).IsRequired();
            entity.Property(e => e.PaymentMethod).IsRequired();
            entity.Property(e => e.PaymentType).IsRequired();
            entity.Property(e => e.TransactionId).HasMaxLength(255);
            entity.Property(e => e.PaymentInfoJson).HasColumnType("jsonb");
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).IsRequired();

            entity.HasOne(e => e.AccountHolder)
                .WithMany(a => a.Payments)
                .HasForeignKey(e => e.AccountHolderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Enrollment)
                .WithMany(en => en.Payments)
                .HasForeignKey(e => e.EnrollmentId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasIndex(e => e.TenantId);
        });

        // Configure GradeRecord
        modelBuilder.Entity<GradeRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.LetterGrade).HasMaxLength(10);
            entity.Property(e => e.NumericGrade).HasPrecision(5, 2);
            entity.Property(e => e.GradePoints).HasPrecision(3, 2);
            entity.Property(e => e.Comments).HasMaxLength(500);
            entity.Property(e => e.GradeDate).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasOne(g => g.Student)
                .WithMany()
                .HasForeignKey(g => g.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(g => g.Course)
                .WithMany()
                .HasForeignKey(g => g.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasIndex(e => e.TenantId);
        });

        // Configure AcademicYear
        modelBuilder.Entity<AcademicYear>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(20);
            entity.Property(e => e.StartDate).IsRequired();
            entity.Property(e => e.EndDate).IsRequired();
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasIndex(e => new { e.TenantId, e.Name }).IsUnique();
            entity.HasIndex(e => e.TenantId);
        });

        // Configure Educator
        modelBuilder.Entity<Educator>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.EducatorInfoJson).HasColumnType("jsonb");
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.IsPrimary).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasOne(e => e.Course)
                .WithMany()
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasIndex(e => e.TenantId);
        });

        // Configure User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(320);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.KeycloakId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Role).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);

            entity.HasIndex(e => new { e.TenantId, e.Email }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.KeycloakId }).IsUnique();
            entity.HasIndex(e => e.TenantId);
        });

        // Configure UserProfile
        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.State).HasMaxLength(50);
            entity.Property(e => e.ZipCode).HasMaxLength(10);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.Bio).HasMaxLength(1000);
            entity.Property(e => e.ProfilePictureUrl).HasMaxLength(500);

            entity.HasOne(p => p.User)
                .WithOne()
                .HasForeignKey<UserProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasIndex(e => e.TenantId);
        });
    }
    
    /// <summary>
    /// Configures global query filters for tenant isolation.
    /// In SaaS mode, all queries are automatically filtered by TenantId.
    /// </summary>
    private void ConfigureTenantFilters(ModelBuilder modelBuilder)
    {
        // Only apply filters if tenant filtering is enabled
        // The filter checks ShouldApplyTenantFilter at query time. When tenant filtering
        // is enabled but CurrentTenantId is null, the guarded comparison below evaluates
        // to false for all rows (no tenant context => no tenant-scoped data returned).
        // This is intentional for SaaS mode when no tenant context is available.
        modelBuilder.Entity<AccountHolder>()
            .HasQueryFilter(e =>
                !_tenantProvider.ShouldApplyTenantFilter ||
                (_tenantProvider.CurrentTenantId.HasValue &&
                 e.TenantId == _tenantProvider.CurrentTenantId.Value));
        modelBuilder.Entity<Student>()
            .HasQueryFilter(e =>
                !_tenantProvider.ShouldApplyTenantFilter ||
                (_tenantProvider.CurrentTenantId.HasValue &&
                 e.TenantId == _tenantProvider.CurrentTenantId.Value));
        modelBuilder.Entity<Course>()
            .HasQueryFilter(e =>
                !_tenantProvider.ShouldApplyTenantFilter ||
                (_tenantProvider.CurrentTenantId.HasValue &&
                 e.TenantId == _tenantProvider.CurrentTenantId.Value));
        modelBuilder.Entity<Semester>()
            .HasQueryFilter(e =>
                !_tenantProvider.ShouldApplyTenantFilter ||
                (_tenantProvider.CurrentTenantId.HasValue &&
                 e.TenantId == _tenantProvider.CurrentTenantId.Value));
        modelBuilder.Entity<Enrollment>()
            .HasQueryFilter(e =>
                !_tenantProvider.ShouldApplyTenantFilter ||
                (_tenantProvider.CurrentTenantId.HasValue &&
                 e.TenantId == _tenantProvider.CurrentTenantId.Value));
        modelBuilder.Entity<Payment>()
            .HasQueryFilter(e =>
                !_tenantProvider.ShouldApplyTenantFilter ||
                (_tenantProvider.CurrentTenantId.HasValue &&
                 e.TenantId == _tenantProvider.CurrentTenantId.Value));
        modelBuilder.Entity<CourseInstructor>()
            .HasQueryFilter(e =>
                !_tenantProvider.ShouldApplyTenantFilter ||
                (_tenantProvider.CurrentTenantId.HasValue &&
                 e.TenantId == _tenantProvider.CurrentTenantId.Value));
        modelBuilder.Entity<Educator>()
            .HasQueryFilter(e =>
                !_tenantProvider.ShouldApplyTenantFilter ||
                (_tenantProvider.CurrentTenantId.HasValue &&
                 e.TenantId == _tenantProvider.CurrentTenantId.Value));
        modelBuilder.Entity<Room>()
            .HasQueryFilter(e =>
                !_tenantProvider.ShouldApplyTenantFilter ||
                (_tenantProvider.CurrentTenantId.HasValue &&
                 e.TenantId == _tenantProvider.CurrentTenantId.Value));
        modelBuilder.Entity<GradeRecord>()
            .HasQueryFilter(e =>
                !_tenantProvider.ShouldApplyTenantFilter ||
                (_tenantProvider.CurrentTenantId.HasValue &&
                 e.TenantId == _tenantProvider.CurrentTenantId.Value));
        modelBuilder.Entity<AcademicYear>()
            .HasQueryFilter(e =>
                !_tenantProvider.ShouldApplyTenantFilter ||
                (_tenantProvider.CurrentTenantId.HasValue &&
                 e.TenantId == _tenantProvider.CurrentTenantId.Value));
        modelBuilder.Entity<User>()
            .HasQueryFilter(e =>
                !_tenantProvider.ShouldApplyTenantFilter ||
                (_tenantProvider.CurrentTenantId.HasValue &&
                 e.TenantId == _tenantProvider.CurrentTenantId.Value));
        modelBuilder.Entity<UserProfile>()
            .HasQueryFilter(e =>
                !_tenantProvider.ShouldApplyTenantFilter ||
                (_tenantProvider.CurrentTenantId.HasValue &&
                 e.TenantId == _tenantProvider.CurrentTenantId.Value));
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is GradeRecord || e.Entity is AcademicYear || e.Entity is User ||
                       e.Entity is AccountHolder || e.Entity is Semester || e.Entity is Student ||
                       e.Entity is Course || e.Entity is Enrollment || e.Entity is CourseInstructor ||
                       e.Entity is Educator || e.Entity is Payment || e.Entity is Room)
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property("CreatedAt").CurrentValue = DateTime.UtcNow;
            }
            entry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
        }
    }
}
