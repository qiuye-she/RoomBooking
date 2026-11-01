using Microsoft.AspNetCore.Mvc;
using RoomBooking.Api.Dtos;
using RoomBooking.Api.Exceptions;

namespace RoomBooking.Api.Middleware;


public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (NotFoundException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status404NotFound, "Not Found", ex.Message);
        }
        catch (DomainValidationException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "Validation Error", ex.Message);
        }
        catch (InvalidStateException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status409Conflict, "Conflict", ex.Message);
        }
        catch (ReservationConflictException ex)
        {
            await WriteProblemAsync(
                context,
                StatusCodes.Status409Conflict,
                "Reservation Conflict",
                ex.Message,
                new Dictionary<string, object?> { ["conflictingReservation"] = ex.ConflictingReservation });
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Client disconnected; there is nobody to respond to and nothing to log as an error.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteProblemAsync(
                context, StatusCodes.Status500InternalServerError, "Internal Server Error", "An unexpected error occurred.");
        }
    }

    private static Task WriteProblemAsync(
        HttpContext context,
        int statusCode,
        string title,
        string detail,
        IDictionary<string, object?>? extensions = null)
    {
        return Results.Problem(
                detail: detail,
                instance: context.Request.Path,
                statusCode: statusCode,
                title: title,
                extensions: extensions)
            .ExecuteAsync(context);
    }
}
