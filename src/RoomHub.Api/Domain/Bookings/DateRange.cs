using RoomHub.Api.Domain.Common;

namespace RoomHub.Api.Domain.Bookings;

public sealed record DateRange
{
    private DateRange() { }

    public DateTime StartUtc { get; private set; }
    public DateTime EndUtc { get; private set; }
    public TimeSpan Duration => EndUtc - StartUtc;

    public static Result<DateRange> Create(DateTime start, DateTime end)
    {
        var startUtc = start.Kind == DateTimeKind.Utc ? start : start.ToUniversalTime();
        var endUtc = end.Kind == DateTimeKind.Utc ? end : end.ToUniversalTime();

        if (endUtc <= startUtc)
            return BookingErrors.InvalidRange;

        return new DateRange
        {
            StartUtc = startUtc,
            EndUtc = endUtc
        };
    }

    public bool Overlaps(DateRange other) =>
        StartUtc < other.EndUtc && other.StartUtc < EndUtc;
}