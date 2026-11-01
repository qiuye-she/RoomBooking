using RoomBooking.Api.Dtos;

namespace RoomBooking.Api.Exceptions;

public sealed class ReservationConflictException(ReservationDto conflictingReservation)
    : Exception("The requested time range overlaps with an existing reservation.")
{
    public ReservationDto ConflictingReservation { get; } = conflictingReservation;
}
