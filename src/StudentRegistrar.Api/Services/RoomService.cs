using AutoMapper;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Data.Repositories;
using StudentRegistrar.Models;

namespace StudentRegistrar.Api.Services;

public class RoomService : IRoomService
{
    private readonly IRoomRepository _roomRepository;
    private readonly IMapper _mapper;

    public RoomService(IRoomRepository roomRepository, IMapper mapper)
    {
        _roomRepository = roomRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<RoomDto>> GetAllRoomsAsync()
    {
        var rooms = await _roomRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<RoomDto>>(rooms);
    }

    public async Task<RoomDto?> GetRoomByIdAsync(Guid id)
    {
        var room = await _roomRepository.GetByIdAsync(id);
        return room == null ? null : _mapper.Map<RoomDto>(room);
    }

    public async Task<IEnumerable<RoomDto>> GetRoomsByTypeAsync(RoomType roomType)
    {
        var rooms = await _roomRepository.GetByTypeAsync(roomType);
        return _mapper.Map<IEnumerable<RoomDto>>(rooms);
    }

    public async Task<RoomDto> CreateRoomAsync(CreateRoomDto createDto)
    {
        // Check if room name already exists
        var existingRoom = await _roomRepository.GetByNameAsync(createDto.Name);
        if (existingRoom != null)
        {
            throw new InvalidOperationException($"A room with the name '{createDto.Name}' already exists.");
        }

        var room = _mapper.Map<Room>(createDto);
        var createdRoom = await _roomRepository.CreateAsync(room);
        return _mapper.Map<RoomDto>(createdRoom);
    }

    public async Task<RoomDto?> UpdateRoomAsync(Guid id, UpdateRoomDto updateDto)
    {
        var existingRoom = await _roomRepository.GetByIdAsync(id);
        if (existingRoom == null)
            return null;

        // Check if name is being changed and if it conflicts with another room
        if (existingRoom.Name != updateDto.Name)
        {
            var nameExists = await _roomRepository.NameExistsAsync(updateDto.Name, id);
            if (nameExists)
            {
                throw new InvalidOperationException($"A room with the name '{updateDto.Name}' already exists.");
            }
        }

        _mapper.Map(updateDto, existingRoom);
        var updatedRoom = await _roomRepository.UpdateAsync(existingRoom);
        return _mapper.Map<RoomDto>(updatedRoom);
    }

    public async Task<bool> DeleteRoomAsync(Guid id)
    {
        // Check if room is in use by any courses
        var isInUse = await _roomRepository.IsRoomInUseAsync(id);
        if (isInUse)
        {
            throw new InvalidOperationException("Cannot delete room because it is currently assigned to one or more courses.");
        }

        return await _roomRepository.DeleteAsync(id);
    }

    public async Task<bool> IsRoomInUseAsync(Guid roomId)
    {
        return await _roomRepository.IsRoomInUseAsync(roomId);
    }
}
