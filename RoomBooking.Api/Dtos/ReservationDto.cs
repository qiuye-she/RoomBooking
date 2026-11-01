using RoomBooking.Api.Entities;

namespace RoomBooking.Api.Dtos;

public sealed record ReservationDto(
    int Id,
    int RoomId,
    DateTimeOffset Start,
    DateTimeOffset End,
    string Title,
    DateTimeOffset CreatedAt)
{
    public static ReservationDto FromEntity(Reservation reservation) => new(
        reservation.Id,
        reservation.RoomId,
        AsUtcOffset(reservation.Start),
        AsUtcOffset(reservation.End),
        reservation.Title,
        AsUtcOffset(reservation.CreatedAt));

    // SQLite does not round-trip DateTimeKind, so values coming back from the database
    // are Unspecified and must be explicitly re-tagged as Utc before conversion —
    // otherwise DateTimeOffset would assume the server's local offset.
    private static DateTimeOffset AsUtcOffset(DateTime utcValue) =>
        new(DateTime.SpecifyKind(utcValue, DateTimeKind.Utc));
}
