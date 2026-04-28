using Microsoft.EntityFrameworkCore;
using StudentRegistrar.Models;

namespace StudentRegistrar.Data.Repositories;

public class EducatorRepository : IEducatorRepository
{
    private readonly StudentRegistrarDbContext _context;

    public EducatorRepository(StudentRegistrarDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Educator>> GetAllAsync()
    {
        return await _context.Educators.ToListAsync();
    }

    public async Task<Educator?> GetByIdAsync(Guid id)
    {
        return await _context.Educators.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<IEnumerable<Educator>> GetActiveAsync()
    {
        return await _context.Educators.Where(e => e.IsActive).ToListAsync();
    }

    public async Task<Educator> CreateAsync(Educator educator)
    {
        _context.Educators.Add(educator);
        await _context.SaveChangesAsync();
        return educator;
    }

    public async Task<Educator> UpdateAsync(Educator educator)
    {
        _context.Educators.Update(educator);
        await _context.SaveChangesAsync();
        return educator;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var educator = await _context.Educators.FindAsync(id);
        if (educator == null)
            return false;

        _context.Educators.Remove(educator);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeactivateAsync(Guid id)
    {
        var educator = await _context.Educators.FindAsync(id);
        if (educator == null)
            return false;

        educator.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ActivateAsync(Guid id)
    {
        var educator = await _context.Educators.FindAsync(id);
        if (educator == null)
            return false;

        educator.IsActive = true;
        await _context.SaveChangesAsync();
        return true;
    }
}
