using RoomHub.Api.Domain.Common;
using RoomHub.Api.Domain.Rooms;

namespace RoomHub.Api.Domain.Bookings;

public enum BookingStatus
{
    Active,
    Cancelled
}

public static class BookingErrors
{
    public static readonly Error BookedByRequired = Error.Validation("Booking.BookedByRequired", "BookedBy is required.");
    public static readonly Error InvalidRange = Error.Validation("Booking.InvalidRange", "The end must be after the start.");
    public static readonly Error DurationTooLong = Error.Validation("Booking.DurationTooLong", "Booking duration exceeds the maximum allowed for this room.");
    public static readonly Error InvalidAttendees = Error.Validation("Booking.InvalidAttendees", "Attendees must be positive.");
    public static readonly Error ExceedsCapacity = Error.Validation("Booking.ExceedsCapacity", "Attendees exceed room capacity.");
    public static readonly Error StartInPast = Error.Validation("Booking.StartInPast", "Booking cannot start in the past.");
    public static readonly Error NotFound = Error.NotFound("Booking.NotFound", "The booking does not exist.");
    public static readonly Error RoomOccupied = Error.Conflict("Booking.RoomOccupied", "The room is occupied in this interval.");
    public static readonly Error AlreadyCancelled = Error.Conflict("Booking.AlreadyCancelled", "The booking is already cancelled.");
    public static readonly Error DailyLimitExceeded = Error.Conflict("Booking.DailyLimitExceeded", "A participant cannot have more than 2 active bookings per day.");
    public static readonly Error TooLateToCancel = Error.Conflict("Booking.TooLateToCancel", "A booking can only be cancelled at least 60 minutes before it starts.");
}

public sealed class Booking
{
    private Booking() { }

    public Guid Id { get; private set; }
    public Guid RoomId { get; private set; }
    public string BookedBy { get; private set; } = string.Empty;
    public DateRange Range { get; private set; } = null!;
    public int Attendees { get; private set; }
    public BookingStatus Status { get; private set; }

    public static Result<Booking> Create(Room room, string? bookedBy, DateRange range, int attendees, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(bookedBy))
            return BookingErrors.BookedByRequired;

        if (attendees <= 0)
            return BookingErrors.InvalidAttendees;

        if (attendees > room.Capacity)
            return BookingErrors.ExceedsCapacity;

        if (range.StartUtc < nowUtc)
            return BookingErrors.StartInPast;

        var maxDuration = room.Capacity > 10 ? TimeSpan.FromHours(2) : TimeSpan.FromHours(4);
        if (range.Duration > maxDuration)
            return BookingErrors.DurationTooLong;

        return new Booking
        {
            Id = Guid.NewGuid(),
            RoomId = room.Id,
            BookedBy = bookedBy.Trim(),
            Range = range,
            Attendees = attendees,
            Status = BookingStatus.Active
        };
    }

    public Result<Unit> Cancel(DateTime nowUtc)
    {
        if (Status == BookingStatus.Cancelled)
            return BookingErrors.AlreadyCancelled;

        if (Range.StartUtc - nowUtc < TimeSpan.FromMinutes(60))
            return BookingErrors.TooLateToCancel;

        Status = BookingStatus.Cancelled;
        return Unit.Value;
    }
}

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Booking>> GetActiveForRoomAsync(Guid roomId, CancellationToken ct = default);
    Task<int> CountActiveForParticipantOnDayAsync(string bookedBy, DateTime dayStartUtc, CancellationToken ct = default);
    void Add(Booking booking);
}