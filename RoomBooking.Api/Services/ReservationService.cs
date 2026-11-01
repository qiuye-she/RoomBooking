using Microsoft.EntityFrameworkCore;
using RoomBooking.Api.Data;
using RoomBooking.Api.Dtos;
using RoomBooking.Api.Entities;
using RoomBooking.Api.Exceptions;

namespace RoomBooking.Api.Services;

public sealed class ReservationService(AppDbContext context, IRoomLockProvider lockProvider)
    : IReservationService
{
    public async Task<IReadOnlyList<ReservationDto>> GetReservationsAsync(
        int roomId,
        DateTimeOffset? start,
        DateTimeOffset? end,
        CancellationToken ct)
    {
        await EnsureRoomExistsAsync(roomId, ct);

        IQueryable<Reservation> query = context.Reservations
            .AsNoTracking()
            .Where(r => r.RoomId == roomId && r.CancelledAt == null);

        if (start is not null)
        {
            DateTime startUtc = start.Value.UtcDateTime;
            query = query.Where(r => r.End > startUtc);
        }

        if (end is not null)
        {
            DateTime endUtc = end.Value.UtcDateTime;
            query = query.Where(r => r.Start < endUtc);
        }

        List<Reservation> reservations = await query.OrderBy(r => r.Start).ToListAsync(ct);
        return [.. reservations.Select(ReservationDto.FromEntity)];
    }

    public async Task<ReservationDto> GetReservationAsync(int reservationId, CancellationToken ct)
    {
        Reservation? reservation = await context.Reservations
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == reservationId && r.CancelledAt == null, ct)
            ?? throw new NotFoundException($"Reservation {reservationId} was not found.");

        return ReservationDto.FromEntity(reservation);
    }

    public async Task<ReservationDto> CreateReservationAsync(
        int roomId,
        CreateReservationRequest request,
        CancellationToken ct)
    {
        if (request.End <= request.Start)
        {
            throw new DomainValidationException("End must be after Start.");
        }

        await EnsureRoomExistsAsync(roomId, ct);

        DateTime startUtc = request.Start.UtcDateTime;
        DateTime endUtc = request.End.UtcDateTime;

        SemaphoreSlim gate = lockProvider.GetLock(roomId);
        await gate.WaitAsync(ct);
        try
        {
            Reservation? conflicting = await context.Reservations
                .AsNoTracking()
                .Where(r => r.RoomId == roomId && r.CancelledAt == null)
                .FirstOrDefaultAsync(r => r.Start < endUtc && r.End > startUtc, ct);

            if (conflicting is not null)
            {
                throw new ReservationConflictException(ReservationDto.FromEntity(conflicting));
            }

            Reservation reservation = new Reservation
            {
                RoomId = roomId,
                Start = startUtc,
                End = endUtc,
                Title = request.Title.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            context.Reservations.Add(reservation);
            await context.SaveChangesAsync(ct);

            return ReservationDto.FromEntity(reservation);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task CancelReservationAsync(int reservationId, CancellationToken ct)
    {
        Reservation? reservation = await context.Reservations.FirstOrDefaultAsync(r => r.Id == reservationId, ct)
            ?? throw new NotFoundException($"Reservation {reservationId} was not found.");

        if (reservation.CancelledAt is not null)
        {
            throw new InvalidStateException("This reservation has already been cancelled.");
        }

        reservation.CancelledAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
    }

    private async Task EnsureRoomExistsAsync(int roomId, CancellationToken ct)
    {
        bool exists = await context.Rooms.AnyAsync(r => r.Id == roomId, ct);
        if (!exists)
        {
            throw new NotFoundException($"Room {roomId} was not found.");
        }
    }
}
