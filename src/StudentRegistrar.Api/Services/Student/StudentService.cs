using AutoMapper;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Data.Repositories;
using StudentRegistrar.Models;

namespace StudentRegistrar.Api.Services;

public class StudentService : IStudentService
{
    private readonly IStudentRepository _studentRepository;
    private readonly IAccountHolderRepository _accountHolderRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IMapper _mapper;

    public StudentService(
        IStudentRepository studentRepository,
        IAccountHolderRepository accountHolderRepository,
        IEnrollmentRepository enrollmentRepository,
        IMapper mapper)
    {
        _studentRepository = studentRepository;
        _accountHolderRepository = accountHolderRepository;
        _enrollmentRepository = enrollmentRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<StudentDto>> GetAllStudentsAsync()
    {
        var students = await _studentRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<StudentDto>>(students);
    }

    public async Task<StudentDto?> GetStudentByIdAsync(Guid id)
    {
        var student = await _studentRepository.GetByIdAsync(id);
        return student != null ? _mapper.Map<StudentDto>(student) : null;
    }

    public async Task<StudentDto> CreateStudentAsync(CreateStudentDto createStudentDto)
    {
        var student = _mapper.Map<Student>(createStudentDto);
        var createdStudent = await _studentRepository.CreateAsync(student);
        return _mapper.Map<StudentDto>(createdStudent);
    }

    public async Task<StudentDto?> UpdateStudentAsync(Guid id, UpdateStudentDto updateStudentDto)
    {
        var existingStudent = await _studentRepository.GetByIdAsync(id);
        if (existingStudent == null)
            return null;

        _mapper.Map(updateStudentDto, existingStudent);
        var updatedStudent = await _studentRepository.UpdateAsync(existingStudent);
        return _mapper.Map<StudentDto>(updatedStudent);
    }

    public async Task<bool> DeleteStudentAsync(Guid id)
    {
        return await _studentRepository.DeleteAsync(id);
    }

    public async Task<IEnumerable<StudentDto>> GetStudentsByAccountHolderAsync(Guid accountHolderId)
    {
        var students = await _studentRepository.GetByAccountHolderAsync(accountHolderId);
        return _mapper.Map<IEnumerable<StudentDto>>(students);
    }
}
