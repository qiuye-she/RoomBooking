using RoomBooking.Api.Data;
using RoomBooking.Api.Dtos;
using RoomBooking.Api.Entities;
using RoomBooking.Api.Exceptions;
using RoomBooking.Api.Services;
using Xunit;

namespace RoomBooking.Tests;

public sealed class ReservationServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private readonly AppDbContext _context;
    private readonly ReservationService _sut;
    private readonly Room _room;

    public ReservationServiceTests()
    {
        _context = _database.Context;
        _room = new Room { Name = "Test Room", Capacity = 10 };
        _context.Rooms.Add(_room);
        _context.SaveChanges();

        _sut = new ReservationService(_context, new RoomLockProvider());
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task CreateReservationAsync_WithNoExistingReservations_Succeeds()
    {
        ReservationDto result = await Book(10, 0, 11, 0, "Planning");

        Assert.Equal("Planning", result.Title);
    }

    [Theory]
    [InlineData(10, 30, 11, 30)] // overlaps the tail of an existing 10:00-11:00 reservation
    [InlineData(10, 15, 10, 45)] // fully inside an existing 10:00-11:00 reservation
    public async Task CreateReservationAsync_OverlappingExistingReservation_ThrowsConflict(
        int startHour, int startMinute, int endHour, int endMinute)
    {
        await Book(10, 0, 11, 0, "Existing");

        await Assert.ThrowsAsync<ReservationConflictException>(() =>
            Book(startHour, startMinute, endHour, endMinute, "New"));
    }

    [Fact]
    public async Task CreateReservationAsync_BackToBackReservations_Succeeds()
    {
        await Book(10, 0, 11, 0, "First");

        ReservationDto result = await Book(11, 0, 12, 0, "Second");

        Assert.Equal("Second", result.Title);
    }

    [Fact]
    public async Task CreateReservationAsync_WhenEndBeforeStart_ThrowsValidation()
    {
        await Assert.ThrowsAsync<DomainValidationException>(() => Book(11, 0, 10, 0, "Invalid"));
    }

    [Fact]
    public async Task CreateReservationAsync_WhenRoomDoesNotExist_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.CreateReservationAsync(_room.Id + 999, Request(10, 0, 11, 0, "X"), CancellationToken.None));
    }

    [Fact]
    public async Task CancelReservationAsync_FreesUpTheSlotForANewReservation()
    {
        ReservationDto created = await Book(10, 0, 11, 0, "ToCancel");

        await _sut.CancelReservationAsync(created.Id, CancellationToken.None);

        ReservationDto result = await Book(10, 0, 11, 0, "Replacement");
        Assert.Equal("Replacement", result.Title);
    }

    [Fact]
    public async Task CancelReservationAsync_WhenAlreadyCancelled_ThrowsInvalidState()
    {
        ReservationDto created = await Book(10, 0, 11, 0, "ToCancel");
        await _sut.CancelReservationAsync(created.Id, CancellationToken.None);

        await Assert.ThrowsAsync<InvalidStateException>(() =>
            _sut.CancelReservationAsync(created.Id, CancellationToken.None));
    }

    [Fact]
    public async Task GetReservationAsync_WhenCancelled_ThrowsNotFound()
    {
        ReservationDto created = await Book(10, 0, 11, 0, "ToCancel");
        await _sut.CancelReservationAsync(created.Id, CancellationToken.None);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.GetReservationAsync(created.Id, CancellationToken.None));
    }

    [Fact]
    public async Task GetReservationsAsync_WithRange_ReturnsOnlyOverlappingReservations()
    {
        await Book(9, 0, 10, 0, "Before");
        await Book(10, 0, 11, 0, "Inside");
        await Book(11, 0, 12, 0, "After");

        IReadOnlyList<ReservationDto> result = await _sut.GetReservationsAsync(
            _room.Id, Utc(10, 0), Utc(11, 0), CancellationToken.None);

        // Reservations that merely touch the range boundaries must not be included.
        Assert.Equal("Inside", Assert.Single(result).Title);
    }

    [Fact]
    public async Task GetReservationsAsync_WithOnlyStart_ReturnsEverythingFromThatPoint()
    {
        await Book(9, 0, 10, 0, "Before");
        await Book(10, 0, 11, 0, "Inside");
        await Book(11, 0, 12, 0, "After");

        IReadOnlyList<ReservationDto> result = await _sut.GetReservationsAsync(
            _room.Id, Utc(10, 30), null, CancellationToken.None);

        Assert.Equal(["Inside", "After"], result.Select(r => r.Title));
    }

    [Fact]
    public async Task GetReservationsAsync_ReturnsStoredTimesAsUtcInstants()
    {
        CreateReservationRequest request = new CreateReservationRequest
        {
            Start = new DateTimeOffset(2025, 1, 1, 10, 0, 0, TimeSpan.FromHours(2)),
            End = new DateTimeOffset(2025, 1, 1, 11, 0, 0, TimeSpan.FromHours(2)),
            Title = "Offset input"
        };
        await _sut.CreateReservationAsync(_room.Id, request, CancellationToken.None);

        ReservationDto stored = Assert.Single(
            await _sut.GetReservationsAsync(_room.Id, null, null, CancellationToken.None));

        // Same instant, expressed in UTC. Note: this only guards the SpecifyKind fix on
        // machines whose local time zone is not UTC.
        Assert.Equal(request.Start, stored.Start);
        Assert.Equal(TimeSpan.Zero, stored.Start.Offset);
    }

    private Task<ReservationDto> Book(int startHour, int startMinute, int endHour, int endMinute, string title) =>
        _sut.CreateReservationAsync(
            _room.Id, Request(startHour, startMinute, endHour, endMinute, title), CancellationToken.None);

    private static DateTimeOffset Utc(int hour, int minute) =>
        new(2025, 1, 1, hour, minute, 0, TimeSpan.Zero);

    private static CreateReservationRequest Request(int startHour, int startMinute, int endHour, int endMinute, string title) =>
        new()
        {
            Start = new DateTimeOffset(2025, 1, 1, startHour, startMinute, 0, TimeSpan.Zero),
            End = new DateTimeOffset(2025, 1, 1, endHour, endMinute, 0, TimeSpan.Zero),
            Title = title
        };
}
