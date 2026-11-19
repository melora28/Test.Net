using Microsoft.EntityFrameworkCore;
using RoomHub.Api.Domain.Bookings;
using RoomHub.Api.Domain.Rooms;

namespace RoomHub.Api.Infrastructure.Persistence.Repositories;

public sealed class RoomRepository(RoomHubDbContext db) : IRoomRepository
{
    public Task<Room?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Rooms.FirstOrDefaultAsync(r => r.Id == id, ct);

    public void Add(Room room) => db.Rooms.Add(room);
}

public sealed class BookingRepository(RoomHubDbContext db) : IBookingRepository
{
    public Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Bookings.FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<IReadOnlyList<Booking>> GetActiveForRoomAsync(Guid roomId, CancellationToken ct = default) =>
        await db.Bookings
            .AsNoTracking()
            .Where(b => b.RoomId == roomId && b.Status == BookingStatus.Active)
            .ToListAsync(ct);

    public async Task<int> CountActiveForParticipantOnDayAsync(
        string bookedBy, DateTime dayStartUtc, CancellationToken ct = default)
    {
        var dayStart = DateTime.SpecifyKind(dayStartUtc.Date, DateTimeKind.Utc);
        var dayEnd = dayStart.AddDays(1);
        var normalizedBookedBy = bookedBy.Trim().ToLower();

        return await db.Bookings
            .AsNoTracking()
            .CountAsync(b =>
                b.Status == BookingStatus.Active &&
                b.BookedBy.ToLower() == normalizedBookedBy &&
                b.Range.StartUtc >= dayStart &&
                b.Range.StartUtc < dayEnd, ct);
    }

    public void Add(Booking booking) => db.Bookings.Add(booking);
}