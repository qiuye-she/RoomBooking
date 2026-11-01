namespace RoomBooking.Api.Entities;

public class Reservation
{
    public int Id { get; set; }

    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;

    // Always written and compared as UTC. SQLite does not preserve DateTimeKind across a
    // round trip, so values read back from the database must be re-tagged as Utc before
    // being turned into a DateTimeOffset for API responses — see ReservationDto.FromEntity.
    public DateTime Start { get; set; }
    public DateTime End { get; set; }

    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Null means active. Cancelling is a soft delete to preserve history and to keep the
    // overlap/listing queries simple (a single "CancelledAt IS NULL" filter).
    public DateTime? CancelledAt { get; set; }
}
