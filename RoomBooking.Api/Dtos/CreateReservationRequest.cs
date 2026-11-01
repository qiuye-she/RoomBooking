using System.ComponentModel.DataAnnotations;

namespace RoomBooking.Api.Dtos;

public sealed class CreateReservationRequest
{
    public required DateTimeOffset Start { get; init; }

    public required DateTimeOffset End { get; init; }

    [Required, StringLength(200, MinimumLength = 1)]
    public required string Title { get; init; }
}
