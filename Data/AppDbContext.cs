using Microsoft.EntityFrameworkCore;

namespace BookingStudyRoom.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<StaffUser> StaffUsers => Set<StaffUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Room>()
            .HasIndex(x => x.Number)
            .IsUnique();

        modelBuilder.Entity<StaffUser>()
            .HasIndex(x => x.Username)
            .IsUnique();

        modelBuilder.Entity<Booking>()
            .HasIndex(x => new { x.RoomId, x.StartAt, x.EndAt });

        modelBuilder.Entity<Booking>()
            .HasIndex(x => x.Rut);

        modelBuilder.Entity<Booking>()
            .HasOne(x => x.Room)
            .WithMany(x => x.Bookings)
            .HasForeignKey(x => x.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Booking>()
            .HasOne(x => x.CreatedByStaffUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedByStaffUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
