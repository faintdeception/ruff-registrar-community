using AutoMapper;
using Moq;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services;
using StudentRegistrar.Data.Repositories;
using StudentRegistrar.Models;
using Xunit;

namespace StudentRegistrar.Api.Tests.Services;

public class RoomServiceTests
{
    private readonly Mock<IRoomRepository> _mockRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly RoomService _service;

    public RoomServiceTests()
    {
        _mockRepository = new Mock<IRoomRepository>();
        _mockMapper = new Mock<IMapper>();
        _service = new RoomService(_mockRepository.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task GetAllRoomsAsync_Should_ReturnMappedRooms()
    {
        // Arrange
        var rooms = new List<Room>
        {
            new() { Id = Guid.NewGuid(), Name = "Room A", Capacity = 25, RoomType = RoomType.Classroom },
            new() { Id = Guid.NewGuid(), Name = "Room B", Capacity = 30, RoomType = RoomType.Lab }
        };

        var expectedDtos = new List<RoomDto>
        {
            new() { Id = rooms[0].Id, Name = "Room A", Capacity = 25, RoomType = RoomType.Classroom, CourseCount = 0 },
            new() { Id = rooms[1].Id, Name = "Room B", Capacity = 30, RoomType = RoomType.Lab, CourseCount = 0 }
        };

        _mockRepository
            .Setup(r => r.GetAllAsync())
            .Returns(Task.FromResult<IEnumerable<Room>>(rooms));

        _mockMapper
            .Setup(m => m.Map<IEnumerable<RoomDto>>(rooms))
            .Returns(expectedDtos);

        // Act
        var result = await _service.GetAllRoomsAsync();

        // Assert
        Assert.Same(expectedDtos, result);
    }

    [Fact]
    public async Task GetRoomByIdAsync_Should_ReturnMappedRoom_WhenRoomExists()
    {
        // Arrange
        var roomId = Guid.NewGuid();
        var room = new Room { Id = roomId, Name = "Test Room", Capacity = 25, RoomType = RoomType.Classroom };
        var expectedDto = new RoomDto { Id = roomId, Name = "Test Room", Capacity = 25, RoomType = RoomType.Classroom, CourseCount = 0 };

        _mockRepository
            .Setup(r => r.GetByIdAsync(roomId))
            .Returns(Task.FromResult<Room?>(room));

        _mockMapper
            .Setup(m => m.Map<RoomDto>(room))
            .Returns(expectedDto);

        // Act
        var result = await _service.GetRoomByIdAsync(roomId);

        // Assert
        Assert.Same(expectedDto, result);
    }

    [Fact]
    public async Task GetRoomByIdAsync_Should_ReturnNull_WhenRoomDoesNotExist()
    {
        // Arrange
        var roomId = Guid.NewGuid();
        _mockRepository
            .Setup(r => r.GetByIdAsync(roomId))
            .Returns(Task.FromResult<Room?>(null));

        // Act
        var result = await _service.GetRoomByIdAsync(roomId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetRoomsByTypeAsync_Should_ReturnFilteredMappedRooms()
    {
        // Arrange
        var roomType = RoomType.Lab;
        var rooms = new List<Room>
        {
            new() { Id = Guid.NewGuid(), Name = "Lab A", Capacity = 20, RoomType = RoomType.Lab },
            new() { Id = Guid.NewGuid(), Name = "Lab B", Capacity = 25, RoomType = RoomType.Lab }
        };

        var expectedDtos = new List<RoomDto>
        {
            new() { Id = rooms[0].Id, Name = "Lab A", Capacity = 20, RoomType = RoomType.Lab, CourseCount = 0 },
            new() { Id = rooms[1].Id, Name = "Lab B", Capacity = 25, RoomType = RoomType.Lab, CourseCount = 0 }
        };

        _mockRepository
            .Setup(r => r.GetByTypeAsync(roomType))
            .Returns(Task.FromResult<IEnumerable<Room>>(rooms));

        _mockMapper
            .Setup(m => m.Map<IEnumerable<RoomDto>>(rooms))
            .Returns(expectedDtos);

        // Act
        var result = await _service.GetRoomsByTypeAsync(roomType);

        // Assert
        Assert.Same(expectedDtos, result);
    }

    [Fact]
    public async Task CreateRoomAsync_Should_CreateAndReturnMappedRoom_WhenNameIsUnique()
    {
        // Arrange
        var createDto = new CreateRoomDto
        {
            Name = "New Room",
            Capacity = 25,
            RoomType = RoomType.Classroom,
            Notes = "Test notes"
        };

        var mappedRoom = new Room
        {
            Name = createDto.Name,
            Capacity = createDto.Capacity,
            RoomType = createDto.RoomType,
            Notes = createDto.Notes
        };

        var createdRoom = new Room
        {
            Id = Guid.NewGuid(),
            Name = createDto.Name,
            Capacity = createDto.Capacity,
            RoomType = createDto.RoomType,
            Notes = createDto.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var expectedDto = new RoomDto
        {
            Id = createdRoom.Id,
            Name = createdRoom.Name,
            Capacity = createdRoom.Capacity,
            RoomType = createdRoom.RoomType,
            Notes = createdRoom.Notes,
            CourseCount = 0,
            CreatedAt = createdRoom.CreatedAt,
            UpdatedAt = createdRoom.UpdatedAt
        };

        _mockRepository
            .Setup(r => r.GetByNameAsync(createDto.Name))
            .Returns(Task.FromResult<Room?>(null));

        _mockMapper
            .Setup(m => m.Map<Room>(createDto))
            .Returns(mappedRoom);

        _mockRepository
            .Setup(r => r.CreateAsync(mappedRoom))
            .Returns(Task.FromResult(createdRoom));

        _mockMapper
            .Setup(m => m.Map<RoomDto>(createdRoom))
            .Returns(expectedDto);

        // Act
        var result = await _service.CreateRoomAsync(createDto);

        // Assert
        Assert.Same(expectedDto, result);
    }

    [Fact]
    public async Task CreateRoomAsync_Should_ThrowInvalidOperationException_WhenNameAlreadyExists()
    {
        // Arrange
        var createDto = new CreateRoomDto
        {
            Name = "Existing Room",
            Capacity = 25,
            RoomType = RoomType.Classroom
        };

        var existingRoom = new Room { Id = Guid.NewGuid(), Name = createDto.Name };

        _mockRepository
            .Setup(r => r.GetByNameAsync(createDto.Name))
            .Returns(Task.FromResult<Room?>(existingRoom));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.CreateRoomAsync(createDto));
        
        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task UpdateRoomAsync_Should_UpdateAndReturnMappedRoom_WhenRoomExists()
    {
        // Arrange
        var roomId = Guid.NewGuid();
        var updateDto = new UpdateRoomDto
        {
            Name = "Updated Room",
            Capacity = 30,
            RoomType = RoomType.Lab,
            Notes = "Updated notes"
        };

        var existingRoom = new Room
        {
            Id = roomId,
            Name = "Old Room",
            Capacity = 25,
            RoomType = RoomType.Classroom,
            Notes = "Old notes"
        };

        var updatedRoom = new Room
        {
            Id = roomId,
            Name = updateDto.Name,
            Capacity = updateDto.Capacity,
            RoomType = updateDto.RoomType,
            Notes = updateDto.Notes,
            UpdatedAt = DateTime.UtcNow
        };

        var expectedDto = new RoomDto
        {
            Id = roomId,
            Name = updateDto.Name,
            Capacity = updateDto.Capacity,
            RoomType = updateDto.RoomType,
            Notes = updateDto.Notes,
            CourseCount = 0,
            UpdatedAt = updatedRoom.UpdatedAt
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(roomId))
            .Returns(Task.FromResult<Room?>(existingRoom));

        _mockRepository
            .Setup(r => r.NameExistsAsync(updateDto.Name, roomId))
            .Returns(Task.FromResult(false));

        _mockMapper
            .Setup(m => m.Map(updateDto, existingRoom))
            .Returns(existingRoom);

        _mockRepository
            .Setup(r => r.UpdateAsync(existingRoom))
            .Returns(Task.FromResult(updatedRoom));

        _mockMapper
            .Setup(m => m.Map<RoomDto>(updatedRoom))
            .Returns(expectedDto);

        // Act
        var result = await _service.UpdateRoomAsync(roomId, updateDto);

        // Assert
        Assert.Same(expectedDto, result);
    }

    [Fact]
    public async Task UpdateRoomAsync_Should_ReturnNull_WhenRoomDoesNotExist()
    {
        // Arrange
        var roomId = Guid.NewGuid();
        var updateDto = new UpdateRoomDto
        {
            Name = "Non-existent Room",
            Capacity = 25,
            RoomType = RoomType.Classroom
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(roomId))
            .Returns(Task.FromResult<Room?>(null));

        // Act
        var result = await _service.UpdateRoomAsync(roomId, updateDto);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateRoomAsync_Should_ThrowInvalidOperationException_WhenNameConflicts()
    {
        // Arrange
        var roomId = Guid.NewGuid();
        var updateDto = new UpdateRoomDto
        {
            Name = "Conflicting Name",
            Capacity = 25,
            RoomType = RoomType.Classroom
        };

        var existingRoom = new Room
        {
            Id = roomId,
            Name = "Old Name",
            Capacity = 25,
            RoomType = RoomType.Classroom
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(roomId))
            .Returns(Task.FromResult<Room?>(existingRoom));

        _mockRepository
            .Setup(r => r.NameExistsAsync(updateDto.Name, roomId))
            .Returns(Task.FromResult(true));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.UpdateRoomAsync(roomId, updateDto));
        
        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task DeleteRoomAsync_Should_ReturnTrue_WhenRoomIsNotInUse()
    {
        // Arrange
        var roomId = Guid.NewGuid();

        _mockRepository
            .Setup(r => r.IsRoomInUseAsync(roomId))
            .Returns(Task.FromResult(false));

        _mockRepository
            .Setup(r => r.DeleteAsync(roomId))
            .Returns(Task.FromResult(true));

        // Act
        var result = await _service.DeleteRoomAsync(roomId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteRoomAsync_Should_ThrowInvalidOperationException_WhenRoomIsInUse()
    {
        // Arrange
        var roomId = Guid.NewGuid();

        _mockRepository
            .Setup(r => r.IsRoomInUseAsync(roomId))
            .Returns(Task.FromResult(true));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.DeleteRoomAsync(roomId));
        
        Assert.Contains("currently assigned to one or more courses", exception.Message);
    }

    [Fact]
    public async Task IsRoomInUseAsync_Should_ReturnRepositoryResult()
    {
        // Arrange
        var roomId = Guid.NewGuid();
        var expectedResult = true;

        _mockRepository
            .Setup(r => r.IsRoomInUseAsync(roomId))
            .Returns(Task.FromResult(expectedResult));

        // Act
        var result = await _service.IsRoomInUseAsync(roomId);

        // Assert
        Assert.Equal(expectedResult, result);
    }
}
