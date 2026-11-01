using RoomBooking.Api.Dtos;

namespace RoomBooking.Api.Services;

public interface IReservationService
{
    Task<IReadOnlyList<ReservationDto>> GetReservationsAsync(
        int roomId,
        DateTimeOffset? start,
        DateTimeOffset? end,
        CancellationToken ct);

    Task<ReservationDto> GetReservationAsync(int reservationId, CancellationToken ct);

    Task<ReservationDto> CreateReservationAsync(
        int roomId,
        CreateReservationRequest request,
        CancellationToken ct);

    Task CancelReservationAsync(int reservationId, CancellationToken ct);
}
