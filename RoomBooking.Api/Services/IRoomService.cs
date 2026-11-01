using RoomBooking.Api.Dtos;

namespace RoomBooking.Api.Services;

public interface IRoomService
{
    Task<IReadOnlyList<RoomDto>> GetRoomsAsync(
        DateTimeOffset? start,
        DateTimeOffset? end,
        int? minCapacity,
        CancellationToken ct);
}
