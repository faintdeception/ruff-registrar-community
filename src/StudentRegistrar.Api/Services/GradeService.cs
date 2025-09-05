using AutoMapper;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Data.Repositories;
using StudentRegistrar.Models;

namespace StudentRegistrar.Api.Services;

public class GradeService : IGradeService
{
    private readonly IGradeRepository _gradeRepository;
    private readonly IMapper _mapper;

    public GradeService(
        IGradeRepository gradeRepository,
        IMapper mapper)
    {
        _gradeRepository = gradeRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<GradeRecordDto>> GetAllGradesAsync()
    {
        var grades = await _gradeRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<GradeRecordDto>>(grades);
    }

    public async Task<GradeRecordDto?> GetGradeByIdAsync(int id)
    {
        var grade = await _gradeRepository.GetByIdAsync(id);
        return grade == null ? null : _mapper.Map<GradeRecordDto>(grade);
    }

    public async Task<IEnumerable<GradeRecordDto>> GetGradesByStudentAsync(Guid studentId)
    {
        var grades = await _gradeRepository.GetByStudentIdAsync(studentId);
        return _mapper.Map<IEnumerable<GradeRecordDto>>(grades);
    }

    public async Task<IEnumerable<GradeRecordDto>> GetGradesByCourseAsync(Guid courseId)
    {
        var grades = await _gradeRepository.GetByCourseIdAsync(courseId);
        return _mapper.Map<IEnumerable<GradeRecordDto>>(grades);
    }

    public async Task<GradeRecordDto> CreateGradeAsync(CreateGradeRecordDto createGradeDto)
    {
        var grade = _mapper.Map<GradeRecord>(createGradeDto);
        var createdGrade = await _gradeRepository.CreateAsync(grade);
        return _mapper.Map<GradeRecordDto>(createdGrade);
    }

    public async Task<GradeRecordDto?> UpdateGradeAsync(int id, CreateGradeRecordDto updateGradeDto)
    {
        var existingGrade = await _gradeRepository.GetByIdAsync(id);
        if (existingGrade == null)
            return null;

        _mapper.Map(updateGradeDto, existingGrade);
        var updatedGrade = await _gradeRepository.UpdateAsync(existingGrade);
        return _mapper.Map<GradeRecordDto>(updatedGrade);
    }

    public async Task<bool> DeleteGradeAsync(int id)
    {
        return await _gradeRepository.DeleteAsync(id);
    }
}
