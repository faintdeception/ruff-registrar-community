using Microsoft.EntityFrameworkCore;
using StudentRegistrar.Models;

namespace StudentRegistrar.Data.Repositories;

public class GradeRepository : IGradeRepository
{
    private readonly StudentRegistrarDbContext _context;

    public GradeRepository(StudentRegistrarDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<GradeRecord>> GetAllAsync()
    {
        return await _context.GradeRecords
            .Include(g => g.Student)
            .Include(g => g.Course)
            .ToListAsync();
    }

    public async Task<GradeRecord?> GetByIdAsync(Guid id)
    {
        return await _context.GradeRecords
            .Include(g => g.Student)
            .Include(g => g.Course)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<IEnumerable<GradeRecord>> GetByStudentIdAsync(Guid studentId)
    {
        return await _context.GradeRecords
            .Include(g => g.Student)
            .Include(g => g.Course)
            .Where(g => g.StudentId == studentId)
            .ToListAsync();
    }

    public async Task<IEnumerable<GradeRecord>> GetByCourseIdAsync(Guid courseId)
    {
        return await _context.GradeRecords
            .Include(g => g.Student)
            .Include(g => g.Course)
            .Where(g => g.CourseId == courseId)
            .ToListAsync();
    }

    public async Task<GradeRecord> CreateAsync(GradeRecord grade)
    {
        _context.GradeRecords.Add(grade);
        await _context.SaveChangesAsync();
        return grade;
    }

    public async Task<GradeRecord> UpdateAsync(GradeRecord grade)
    {
        _context.GradeRecords.Update(grade);
        await _context.SaveChangesAsync();
        return grade;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var grade = await _context.GradeRecords.FindAsync(id);
        if (grade == null)
            return false;

        _context.GradeRecords.Remove(grade);
        await _context.SaveChangesAsync();
        return true;
    }
}
