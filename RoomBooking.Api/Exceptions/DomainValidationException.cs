namespace RoomBooking.Api.Exceptions;

public sealed class DomainValidationException(string message) : Exception(message);
