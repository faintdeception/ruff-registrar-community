using AutoMapper;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Data.Repositories;
using StudentRegistrar.Models;

namespace StudentRegistrar.Api.Services;

public class CourseInstructorService : ICourseInstructorService
{
    private readonly ICourseInstructorRepository _courseInstructorRepository;
    private readonly IMapper _mapper;

    public CourseInstructorService(ICourseInstructorRepository courseInstructorRepository, IMapper mapper)
    {
        _courseInstructorRepository = courseInstructorRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<CourseInstructorDto>> GetAllCourseInstructorsAsync()
    {
        var courseInstructors = await _courseInstructorRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<CourseInstructorDto>>(courseInstructors);
    }

    public async Task<CourseInstructorDto?> GetCourseInstructorByIdAsync(Guid id)
    {
        var courseInstructor = await _courseInstructorRepository.GetByIdAsync(id);
        return courseInstructor != null ? _mapper.Map<CourseInstructorDto>(courseInstructor) : null;
    }

    public async Task<IEnumerable<CourseInstructorDto>> GetCourseInstructorsByCourseIdAsync(Guid courseId)
    {
        var courseInstructors = await _courseInstructorRepository.GetByCourseIdAsync(courseId);
        return _mapper.Map<IEnumerable<CourseInstructorDto>>(courseInstructors);
    }

    public async Task<CourseInstructorDto> CreateCourseInstructorAsync(CreateCourseInstructorDto createDto)
    {
        var courseInstructor = _mapper.Map<CourseInstructor>(createDto);
        var createdCourseInstructor = await _courseInstructorRepository.CreateAsync(courseInstructor);
        return _mapper.Map<CourseInstructorDto>(createdCourseInstructor);
    }

    public async Task<CourseInstructorDto?> UpdateCourseInstructorAsync(Guid id, UpdateCourseInstructorDto updateDto)
    {
        var existingCourseInstructor = await _courseInstructorRepository.GetByIdAsync(id);
        if (existingCourseInstructor == null)
            return null;

        _mapper.Map(updateDto, existingCourseInstructor);
        var updatedCourseInstructor = await _courseInstructorRepository.UpdateAsync(existingCourseInstructor);
        return _mapper.Map<CourseInstructorDto>(updatedCourseInstructor);
    }

    public async Task<bool> DeleteCourseInstructorAsync(Guid id)
    {
        return await _courseInstructorRepository.DeleteAsync(id);
    }
}
