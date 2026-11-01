using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomBooking.Api.Entities;

namespace RoomBooking.Api.Data.Configurations;

public sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.Property(r => r.Title).IsRequired().HasMaxLength(200);

        builder.HasOne(r => r.Room)
            .WithMany(room => room.Reservations)
            .HasForeignKey(r => r.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        // Backs both "reservations for a room in a date range" and the overlap check
        // performed on creation. Filtered to active reservations, since cancelled ones
        // never participate in either query.
        builder.HasIndex(r => new { r.RoomId, r.Start, r.End })
            .HasFilter("CancelledAt IS NULL")
            .HasDatabaseName("IX_Reservations_RoomId_Start_End_Active");
    }
}
