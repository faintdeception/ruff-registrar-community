using Microsoft.EntityFrameworkCore;
using StudentRegistrar.Models;

namespace StudentRegistrar.Data.Repositories;

public class AccountHolderRepository : IAccountHolderRepository
{
    private readonly StudentRegistrarDbContext _context;

    public AccountHolderRepository(StudentRegistrarDbContext context)
    {
        _context = context;
    }

    public async Task<AccountHolder?> GetByIdAsync(Guid id)
    {
        return await _context.AccountHolders
            .Include(a => a.Students)
                .ThenInclude(s => s.Enrollments)
                    .ThenInclude(e => e.Course)
                        .ThenInclude(c => c.Semester)
            .Include(a => a.Payments)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<AccountHolder?> GetByKeycloakUserIdAsync(string keycloakUserId)
    {
        return await _context.AccountHolders
            .Include(a => a.Students)
                .ThenInclude(s => s.Enrollments)
                    .ThenInclude(e => e.Course)
                        .ThenInclude(c => c.Semester)
            .Include(a => a.Payments)
            .FirstOrDefaultAsync(a => a.KeycloakUserId == keycloakUserId);
    }

    public async Task<AccountHolder?> GetByEmailAsync(string email)
    {
        return await _context.AccountHolders
            .Include(a => a.Students)
                .ThenInclude(s => s.Enrollments)
                    .ThenInclude(e => e.Course)
                        .ThenInclude(c => c.Semester)
            .Include(a => a.Payments)
            .FirstOrDefaultAsync(a => a.EmailAddress == email);
    }

    public async Task<IEnumerable<AccountHolder>> GetAllAsync()
    {
        return await _context.AccountHolders
            .Include(a => a.Students)
            .OrderBy(a => a.LastName)
            .ThenBy(a => a.FirstName)
            .ToListAsync();
    }

    public async Task<AccountHolder> CreateAsync(AccountHolder accountHolder)
    {
        accountHolder.CreatedAt = DateTime.UtcNow;
        accountHolder.UpdatedAt = DateTime.UtcNow;
        
        _context.AccountHolders.Add(accountHolder);
        await _context.SaveChangesAsync();
        
        return await GetByIdAsync(accountHolder.Id) ?? accountHolder;
    }

    public async Task<AccountHolder> UpdateAsync(AccountHolder accountHolder)
    {
        accountHolder.UpdatedAt = DateTime.UtcNow;
        
        _context.AccountHolders.Update(accountHolder);
        await _context.SaveChangesAsync();
        
        return await GetByIdAsync(accountHolder.Id) ?? accountHolder;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var accountHolder = await _context.AccountHolders.FindAsync(id);
        if (accountHolder == null)
            return false;

        _context.AccountHolders.Remove(accountHolder);
        await _context.SaveChangesAsync();
        
        return true;
    }

    public async Task<IEnumerable<AccountHolder>> GetAccountHoldersWithStudentsAsync(Guid? semesterId = null)
    {
        var query = _context.AccountHolders
            .Include(a => a.Students)
                .ThenInclude(s => s.Enrollments)
                    .ThenInclude(e => e.Course)
            .Where(a => a.Students.Any());

        if (semesterId.HasValue)
        {
            query = query.Where(a => a.Students.Any(s => s.Enrollments.Any(e => e.SemesterId == semesterId.Value)));
        }

        return await query
            .OrderBy(a => a.LastName)
            .ThenBy(a => a.FirstName)
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.AccountHolders.AnyAsync(a => a.Id == id);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _context.AccountHolders.AnyAsync(a => a.EmailAddress == email);
    }
}
