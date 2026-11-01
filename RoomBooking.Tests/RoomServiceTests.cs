using RoomBooking.Api.Data;
using RoomBooking.Api.Dtos;
using RoomBooking.Api.Entities;
using RoomBooking.Api.Exceptions;
using RoomBooking.Api.Services;
using Xunit;

namespace RoomBooking.Tests;

public sealed class RoomServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private readonly AppDbContext _context;
    private readonly RoomService _sut;

    public RoomServiceTests()
    {
        _context = _database.Context;
        _sut = new RoomService(_context);
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task GetRoomsAsync_WithoutFilters_ReturnsAllRooms()
    {
        _context.Rooms.AddRange(new Room { Name = "A", Capacity = 4 }, new Room { Name = "B", Capacity = 10 });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        IReadOnlyList<RoomDto> rooms = await _sut.GetRoomsAsync(null, null, null, CancellationToken.None);

        Assert.Equal(2, rooms.Count);
    }

    [Fact]
    public async Task GetRoomsAsync_WithMinCapacity_FiltersOutSmallerRooms()
    {
        _context.Rooms.AddRange(new Room { Name = "Small", Capacity = 4 }, new Room { Name = "Big", Capacity = 50 });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        IReadOnlyList<RoomDto> rooms = await _sut.GetRoomsAsync(null, null, 10, CancellationToken.None);

        RoomDto room = Assert.Single(rooms);
        Assert.Equal("Big", room.Name);
    }

    [Fact]
    public async Task GetRoomsAsync_ExcludesOnlyRoomsWithOverlappingReservation()
    {
        Room room = new Room { Name = "Busy Room", Capacity = 4 };
        _context.Rooms.Add(room);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        _context.Reservations.Add(new Reservation
        {
            RoomId = room.Id,
            Start = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            End = new DateTime(2025, 1, 1, 11, 0, 0, DateTimeKind.Utc),
            Title = "Busy",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        IReadOnlyList<RoomDto> duringMeeting = await _sut.GetRoomsAsync(
            new DateTimeOffset(2025, 1, 1, 10, 30, 0, TimeSpan.Zero),
            new DateTimeOffset(2025, 1, 1, 10, 45, 0, TimeSpan.Zero),
            null,
            CancellationToken.None);

        IReadOnlyList<RoomDto> afterMeeting = await _sut.GetRoomsAsync(
            new DateTimeOffset(2025, 1, 1, 11, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero),
            null,
            CancellationToken.None);

        Assert.Empty(duringMeeting);
        Assert.Single(afterMeeting);
    }

    [Fact]
    public async Task GetRoomsAsync_OnlyOneTimeBoundaryProvided_ThrowsValidation()
    {
        await Assert.ThrowsAsync<DomainValidationException>(() =>
            _sut.GetRoomsAsync(DateTimeOffset.UtcNow, null, null, CancellationToken.None));
    }
}
