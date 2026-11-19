using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using RoomHub.Api.Common.Http;
using RoomHub.Api.Domain.Bookings;
using RoomHub.Api.Infrastructure.Persistence;

namespace RoomHub.Api.Features.Bookings.GetBookingById;

public static class GetBookingByIdEndpoint
{
    public sealed record Response(
        Guid Id, Guid RoomId, string BookedBy, DateTime StartUtc, DateTime EndUtc, int Attendees, BookingStatus Status);

    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/{id:guid}", Handle).WithName("GetBookingById");

    private static async Task<Results<Ok<Response>, ProblemHttpResult>> Handle(
        Guid id,
        RoomHubDbContext db,
        CancellationToken ct)
    {
        var booking = await db.Bookings
            .AsNoTracking()
            .Where(b => b.Id == id)
            .Select(b => new Response(
                b.Id, b.RoomId, b.BookedBy, b.Range.StartUtc, b.Range.EndUtc, b.Attendees, b.Status))
            .FirstOrDefaultAsync(ct);

        return booking is null
            ? BookingErrors.NotFound.ToProblem()
            : TypedResults.Ok(booking);
    }
}