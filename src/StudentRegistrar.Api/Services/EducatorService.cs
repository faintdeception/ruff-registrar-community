using AutoMapper;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Data.Repositories;
using StudentRegistrar.Models;

namespace StudentRegistrar.Api.Services;

public class EducatorService : IEducatorService
{
    private readonly IEducatorRepository _educatorRepository;
    private readonly IMapper _mapper;

    public EducatorService(
        IEducatorRepository educatorRepository,
        IMapper mapper)
    {
        _educatorRepository = educatorRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<EducatorDto>> GetAllEducatorsAsync()
    {
        var educators = await _educatorRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<EducatorDto>>(educators);
    }

    public async Task<EducatorDto?> GetEducatorByIdAsync(Guid id)
    {
        var educator = await _educatorRepository.GetByIdAsync(id);
        return educator == null ? null : _mapper.Map<EducatorDto>(educator);
    }

    public async Task<IEnumerable<EducatorDto>> GetEducatorsByCourseIdAsync(Guid courseId)
    {
        var educators = await _educatorRepository.GetByCourseIdAsync(courseId);
        return _mapper.Map<IEnumerable<EducatorDto>>(educators);
    }

    public async Task<IEnumerable<EducatorDto>> GetUnassignedEducatorsAsync()
    {
        var educators = await _educatorRepository.GetUnassignedAsync();
        return _mapper.Map<IEnumerable<EducatorDto>>(educators);
    }

    public async Task<EducatorDto> CreateEducatorAsync(CreateEducatorDto createDto)
    {
        var educator = _mapper.Map<Educator>(createDto);
        educator.IsActive = true; // Set as active by default
        var createdEducator = await _educatorRepository.CreateAsync(educator);
        return _mapper.Map<EducatorDto>(createdEducator);
    }

    public async Task<EducatorDto?> UpdateEducatorAsync(Guid id, UpdateEducatorDto updateDto)
    {
        var existingEducator = await _educatorRepository.GetByIdAsync(id);
        if (existingEducator == null)
            return null;

        _mapper.Map(updateDto, existingEducator);
        var updatedEducator = await _educatorRepository.UpdateAsync(existingEducator);
        return _mapper.Map<EducatorDto>(updatedEducator);
    }

    public async Task<bool> DeleteEducatorAsync(Guid id)
    {
        return await _educatorRepository.DeleteAsync(id);
    }

    public async Task<bool> DeactivateEducatorAsync(Guid id)
    {
        return await _educatorRepository.DeactivateAsync(id);
    }

    public async Task<bool> ActivateEducatorAsync(Guid id)
    {
        return await _educatorRepository.ActivateAsync(id);
    }
}
