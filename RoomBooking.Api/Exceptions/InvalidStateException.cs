namespace RoomBooking.Api.Exceptions;

public sealed class InvalidStateException(string message) : Exception(message);
