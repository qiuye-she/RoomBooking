using Microsoft.EntityFrameworkCore;
using RoomBooking.Api.Entities;

namespace RoomBooking.Api.Data;

public static class SeedData
{
    public static async Task EnsureSeededAsync(AppDbContext context)
    {
        if (await context.Rooms.AnyAsync())
        {
            return;
        }

        context.Rooms.AddRange(
            new Room { Name = "Small Room", Capacity = 4 },
            new Room { Name = "Conference Room", Capacity = 10 },
            new Room { Name = "Auditorium", Capacity = 50 });

        await context.SaveChangesAsync();
    }
}
