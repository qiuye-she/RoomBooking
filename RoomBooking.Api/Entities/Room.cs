namespace RoomBooking.Api.Entities;

public class Room
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
