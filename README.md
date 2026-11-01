# Room Booking API

REST API for managing conference room reservations.
Built with ASP.NET Core (.NET 10), EF Core and SQLite.

## Run

Prerequisite: .NET 10 SDK.

```bash
dotnet dev-certs https --trust    # one-time: trust the local HTTPS dev certificate
dotnet run --project RoomBooking.Api
```

- API reference (Scalar): https://localhost:10001/scalar/v1
- `http://localhost:10000` is also available and redirects to HTTPS.
- `roombooking.db` is created on first start and seeded with three rooms
  (Small Room: 4, Conference Room: 10, Auditorium: 50).

## Test

```bash
dotnet test
```

The root `global.json` selects the Microsoft Testing Platform runner required by xunit.v3 on the .NET 10 SDK.

## Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/rooms?start&end&minCapacity` | Lists rooms. If `start` and `end` are given (both required together), only rooms with no active reservation in that range are returned. |
| GET | `/api/rooms/{roomId}/reservations?start&end` | Lists active reservations of a room, optionally limited to a date/time range. |
| POST | `/api/rooms/{roomId}/reservations` | Creates a reservation and returns `201 Created` with a `Location` header. Returns `409 Conflict` (with the conflicting reservation) on overlap. Back-to-back bookings are allowed. |
| GET | `/api/reservations/{id}` | Returns an active reservation (target of the `Location` header). |
| DELETE | `/api/reservations/{id}` | Cancels a reservation (soft delete). |

Request body for POST:

```json
{ "start": "2025-01-01T10:00:00+00:00", "end": "2025-01-01T11:00:00+00:00", "title": "Sprint planning" }
```

Notes:
- Timestamps are ISO-8601 with an offset on the wire and are stored in UTC.
- In query strings, encode `+` in offsets as `%2B` (or use `Z`).
- Errors are returned as RFC 7807 problem details (400, 404, 409, 500).

## Design highlights

- Controllers -> services -> `DbContext`; no generic repository on top of EF Core.
- Overlap rule: `existing.Start < new.End AND existing.End > new.Start`.
- Composite filtered index on `(RoomId, Start, End)` for active reservations.
- Per-room in-process lock makes "check overlap, then insert" atomic (single instance only).
- Cancellation is a soft delete.

## Known limitations

- **Multi-instance safety**: the per-room lock works in a single process only. Next step: PostgreSQL exclusion constraint on `(room_id, tstzrange(start, end))`.
- **Authentication/authorization**: not implemented. Next step: JWT Bearer against the company IdP, a `CreatedBy` column and policy-based authorization.
- **Pagination**: list endpoints return all matches. Next step: `page`/`pageSize` with a hard upper bound.
- **Migrations**: `EnsureCreated` is used for simplicity. Next step: EF Core migrations.
- **Concurrency test**: only the lock is implemented; no parallel integration test yet.