namespace RoomBooking.Api.Exceptions;

public sealed class NotFoundException(string message) : Exception(message);
