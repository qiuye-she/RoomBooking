using Microsoft.EntityFrameworkCore;
using RoomBooking.Api.Data;
using RoomBooking.Api.Dtos;
using RoomBooking.Api.Entities;
using RoomBooking.Api.Exceptions;

namespace RoomBooking.Api.Services;

public sealed class RoomService(AppDbContext context) : IRoomService
{
    public async Task<IReadOnlyList<RoomDto>> GetRoomsAsync(
        DateTimeOffset? start,
        DateTimeOffset? end,
        int? minCapacity,
        CancellationToken ct)
    {
        if (start.HasValue != end.HasValue)
        {
            throw new DomainValidationException("Start and End must both be provided, or both omitted.");
        }

        if (start is not null && end <= start)
        {
            throw new DomainValidationException("End must be after Start.");
        }

        IQueryable<Room> query = context.Rooms.AsNoTracking().AsQueryable();

        if (minCapacity is not null)
        {
            query = query.Where(room => room.Capacity >= minCapacity.Value);
        }

        if (start is not null && end is not null)
        {
            DateTime startUtc = start.Value.UtcDateTime;
            DateTime endUtc = end.Value.UtcDateTime;

            query = query.Where(room => !room.Reservations.Any(r =>
                r.CancelledAt == null && r.Start < endUtc && r.End > startUtc));
        }

        return await query
            .OrderBy(room => room.Name)
            .Select(room => new RoomDto(room.Id, room.Name, room.Capacity))
            .ToListAsync(ct);
    }
}
