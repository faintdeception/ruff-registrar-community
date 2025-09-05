using StudentRegistrar.Models;
using StudentRegistrar.Api.DTOs;

namespace StudentRegistrar.Api.Services;

public interface IRoomService
{
    Task<IEnumerable<RoomDto>> GetAllRoomsAsync();
    Task<RoomDto?> GetRoomByIdAsync(Guid id);
    Task<IEnumerable<RoomDto>> GetRoomsByTypeAsync(RoomType roomType);
    Task<RoomDto> CreateRoomAsync(CreateRoomDto createDto);
    Task<RoomDto?> UpdateRoomAsync(Guid id, UpdateRoomDto updateDto);
    Task<bool> DeleteRoomAsync(Guid id);
    Task<bool> IsRoomInUseAsync(Guid roomId);
}
