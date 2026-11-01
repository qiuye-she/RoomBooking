using Microsoft.AspNetCore.Mvc;
using RoomBooking.Api.Dtos;
using RoomBooking.Api.Services;

namespace RoomBooking.Api.Controllers;

[ApiController]
[Route("api/rooms")]
public sealed class RoomsController(IRoomService roomService, IReservationService reservationService)
    : ControllerBase
{
    /// <summary>
    /// Returns rooms. If start/end are provided (both required together), only rooms with
    /// no active reservation overlapping that range are returned.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<RoomDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<RoomDto>>> GetRooms(
        [FromQuery] DateTimeOffset? start,
        [FromQuery] DateTimeOffset? end,
        [FromQuery] int? minCapacity,
        CancellationToken ct)
    {
        IReadOnlyList<RoomDto> rooms = await roomService.GetRoomsAsync(start, end, minCapacity, ct);
        return Ok(rooms);
    }

    /// <summary>Returns reservations for a room, optionally limited to a date/time range.</summary>
    [HttpGet("{roomId:int}/reservations")]
    [ProducesResponseType<IReadOnlyList<ReservationDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ReservationDto>>> GetReservations(
        int roomId,
        [FromQuery] DateTimeOffset? start,
        [FromQuery] DateTimeOffset? end,
        CancellationToken ct)
    {
        IReadOnlyList<ReservationDto> reservations = await reservationService.GetReservationsAsync(roomId, start, end, ct);
        return Ok(reservations);
    }

    /// <summary>Creates a reservation for the room. Returns 409 if it overlaps an existing one.</summary>
    [HttpPost("{roomId:int}/reservations")]
    [ProducesResponseType<ReservationDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservationDto>> CreateReservation(
        int roomId,
        [FromBody] CreateReservationRequest request,
        CancellationToken ct)
    {
        ReservationDto reservation = await reservationService.CreateReservationAsync(roomId, request, ct);

        return CreatedAtAction(
            nameof(ReservationsController.GetReservation),
            "Reservations",
            new { id = reservation.Id },
            reservation);
    }
}
