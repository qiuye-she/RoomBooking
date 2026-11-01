using System.Collections.Concurrent;

namespace RoomBooking.Api.Services;

// SQLite is effectively single-writer and this API runs as a single instance, so an
// in-process per-room lock is enough to make "check for overlap, then insert" atomic,
// closing the race condition a bare check-then-insert would have under concurrent
// requests for the same room. Registered as a singleton.
//
// This would not hold up across multiple API instances; at that point the guarantee
// needs to move into the database itself (e.g. an exclusion constraint).
public sealed class RoomLockProvider : IRoomLockProvider
{
    private readonly ConcurrentDictionary<int, SemaphoreSlim> _locks = new();

    public SemaphoreSlim GetLock(int roomId) =>
        _locks.GetOrAdd(roomId, _ => new SemaphoreSlim(1, 1));
}
