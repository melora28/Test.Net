using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RoomHub.Api.Domain.Bookings;
using RoomHub.Api.Domain.Rooms;

namespace RoomHub.Api.Infrastructure.Persistence.Configurations;

public sealed class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("Rooms");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.Name).IsRequired().HasMaxLength(200);
        builder.Property(r => r.Capacity).IsRequired();
    }
}

public sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        var utcConverter = new ValueConverter<DateTime, DateTime>(
            v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        builder.ToTable("Bookings");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();
        builder.Property(b => b.BookedBy).IsRequired().HasMaxLength(200);
        builder.Property(b => b.Status).IsRequired().HasConversion<string>().HasMaxLength(20);

        builder.ComplexProperty(b => b.Range, range =>
        {
            range.Property(r => r.StartUtc).HasColumnName("StartUtc").HasConversion(utcConverter);
            range.Property(r => r.EndUtc).HasColumnName("EndUtc").HasConversion(utcConverter);
        });

        builder.HasOne<Room>()
            .WithMany()
            .HasForeignKey(b => b.RoomId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}