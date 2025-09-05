using AutoMapper;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Data.Repositories;
using StudentRegistrar.Models;

namespace StudentRegistrar.Api.Services;

public class SemesterService : ISemesterService
{
    private readonly ISemesterRepository _semesterRepository;
    private readonly IMapper _mapper;

    public SemesterService(ISemesterRepository semesterRepository, IMapper mapper)
    {
        _semesterRepository = semesterRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<SemesterDto>> GetAllSemestersAsync()
    {
        var semesters = await _semesterRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<SemesterDto>>(semesters);
    }

    public async Task<SemesterDto?> GetSemesterByIdAsync(Guid id)
    {
        var semester = await _semesterRepository.GetByIdAsync(id);
        return semester != null ? _mapper.Map<SemesterDto>(semester) : null;
    }

    public async Task<SemesterDto?> GetActiveSemesterAsync()
    {
        var semester = await _semesterRepository.GetActiveAsync();
        return semester != null ? _mapper.Map<SemesterDto>(semester) : null;
    }

    public async Task<SemesterDto> CreateSemesterAsync(CreateSemesterDto createDto)
    {
        var semester = _mapper.Map<Semester>(createDto);
        var createdSemester = await _semesterRepository.CreateAsync(semester);
        return _mapper.Map<SemesterDto>(createdSemester);
    }

    public async Task<SemesterDto?> UpdateSemesterAsync(Guid id, UpdateSemesterDto updateDto)
    {
        var existingSemester = await _semesterRepository.GetByIdAsync(id);
        if (existingSemester == null)
            return null;

        _mapper.Map(updateDto, existingSemester);
        var updatedSemester = await _semesterRepository.UpdateAsync(existingSemester);
        return _mapper.Map<SemesterDto>(updatedSemester);
    }

    public async Task<bool> DeleteSemesterAsync(Guid id)
    {
        return await _semesterRepository.DeleteAsync(id);
    }
}
