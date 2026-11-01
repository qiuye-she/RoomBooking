namespace RoomBooking.Api.Services;

public interface IRoomLockProvider
{
    SemaphoreSlim GetLock(int roomId);
}
