using Microsoft.AspNetCore.Mvc;
using RoomBooking.Api.Dtos;
using RoomBooking.Api.Services;

namespace RoomBooking.Api.Controllers;

[ApiController]
[Route("api/reservations")]
public sealed class ReservationsController(IReservationService reservationService) : ControllerBase
{
    /// <summary>Returns an active reservation by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<ReservationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservationDto>> GetReservation(int id, CancellationToken ct)
    {
        ReservationDto reservation = await reservationService.GetReservationAsync(id, ct);
        return Ok(reservation);
    }

    /// <summary>Cancels (soft-deletes) an existing reservation.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelReservation(int id, CancellationToken ct)
    {
        await reservationService.CancelReservationAsync(id, ct);
        return NoContent();
    }
}
