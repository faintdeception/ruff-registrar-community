using AutoMapper;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Data.Repositories;
using StudentRegistrar.Models;

namespace StudentRegistrar.Api.Services;

public class CourseServiceV2 : ICourseServiceV2
{
    private readonly ICourseRepository _courseRepository;
    private readonly ICourseInstructorRepository _courseInstructorRepository;
    private readonly IAccountHolderRepository _accountHolderRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly IMapper _mapper;

    public CourseServiceV2(
        ICourseRepository courseRepository, 
        ICourseInstructorRepository courseInstructorRepository,
        IAccountHolderRepository accountHolderRepository,
        IRoomRepository roomRepository,
        IMapper mapper)
    {
        _courseRepository = courseRepository;
        _courseInstructorRepository = courseInstructorRepository;
        _accountHolderRepository = accountHolderRepository;
        _roomRepository = roomRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<CourseDto>> GetAllCoursesAsync()
    {
        var courses = await _courseRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<CourseDto>>(courses);
    }

    public async Task<IEnumerable<CourseDto>> GetCoursesBySemesterAsync(Guid semesterId)
    {
        var courses = await _courseRepository.GetBySemesterAsync(semesterId);
        return _mapper.Map<IEnumerable<CourseDto>>(courses);
    }

    public async Task<CourseDto?> GetCourseByIdAsync(Guid id)
    {
        var course = await _courseRepository.GetByIdAsync(id);
        return course != null ? _mapper.Map<CourseDto>(course) : null;
    }

    public async Task<CourseDto> CreateCourseAsync(CreateCourseDto createDto)
    {
        var course = _mapper.Map<Course>(createDto);
        var createdCourse = await _courseRepository.CreateAsync(course);
        return _mapper.Map<CourseDto>(createdCourse);
    }

    public async Task<CourseDto?> UpdateCourseAsync(Guid id, UpdateCourseDto updateDto)
    {
        var existingCourse = await _courseRepository.GetByIdAsync(id);
        if (existingCourse == null)
            return null;

        _mapper.Map(updateDto, existingCourse);
        var updatedCourse = await _courseRepository.UpdateAsync(existingCourse);
        return _mapper.Map<CourseDto>(updatedCourse);
    }

    public async Task<bool> DeleteCourseAsync(Guid id)
    {
        return await _courseRepository.DeleteAsync(id);
    }

    // Instructor management methods
    public async Task<IEnumerable<CourseInstructorDto>> GetCourseInstructorsAsync(Guid courseId)
    {
        var instructors = await _courseInstructorRepository.GetByCourseIdAsync(courseId);
        return _mapper.Map<IEnumerable<CourseInstructorDto>>(instructors);
    }

    public async Task<CourseInstructorDto> AddInstructorAsync(CreateCourseInstructorDto createDto)
    {
        var instructor = _mapper.Map<CourseInstructor>(createDto);
        
        // If AccountHolderId is provided, populate name and email from the account holder
        if (createDto.AccountHolderId.HasValue)
        {
            var accountHolder = await _accountHolderRepository.GetByIdAsync(createDto.AccountHolderId.Value);
            if (accountHolder != null)
            {
                instructor.FirstName = accountHolder.FirstName;
                instructor.LastName = accountHolder.LastName;
                instructor.Email = accountHolder.EmailAddress;
            }
        }

        var createdInstructor = await _courseInstructorRepository.CreateAsync(instructor);
        return _mapper.Map<CourseInstructorDto>(createdInstructor);
    }

    public async Task<CourseInstructorDto?> UpdateInstructorAsync(Guid instructorId, UpdateCourseInstructorDto updateDto)
    {
        var existingInstructor = await _courseInstructorRepository.GetByIdAsync(instructorId);
        if (existingInstructor == null)
            return null;

        _mapper.Map(updateDto, existingInstructor);
        var updatedInstructor = await _courseInstructorRepository.UpdateAsync(existingInstructor);
        return _mapper.Map<CourseInstructorDto>(updatedInstructor);
    }

    public async Task<bool> RemoveInstructorAsync(Guid instructorId)
    {
        return await _courseInstructorRepository.DeleteAsync(instructorId);
    }

    public async Task<IEnumerable<AccountHolderDto>> GetAvailableMembersAsync()
    {
        var accountHolders = await _accountHolderRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<AccountHolderDto>>(accountHolders);
    }
}
