using Microsoft.AspNetCore.Http.HttpResults;
using RoomHub.Api.Common.Cqrs;
using RoomHub.Api.Common.Http;
using RoomHub.Api.Domain.Bookings;
using RoomHub.Api.Domain.Common;

namespace RoomHub.Api.Features.Bookings.CancelBooking;

public sealed record CancelBookingCommand(Guid BookingId) : ICommand<Unit>;

public sealed class CancelBookingHandler(
    IBookingRepository bookings,
    IUnitOfWork unitOfWork,
    TimeProvider time)
    : ICommandHandler<CancelBookingCommand, Unit>
{
    public async Task<Result<Unit>> HandleAsync(CancelBookingCommand command, CancellationToken ct = default)
    {
        var booking = await bookings.GetByIdAsync(command.BookingId, ct);
        if (booking is null)
            return BookingErrors.NotFound;

        var cancelResult = booking.Cancel(time.GetUtcNow().UtcDateTime);
        if (cancelResult.IsFailure)
            return cancelResult.Error;

        await unitOfWork.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

public static class CancelBookingEndpoint
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapPost("/{id:guid}/cancel", Handle).WithName("CancelBooking");

    private static async Task<Results<NoContent, ProblemHttpResult>> Handle(
        Guid id,
        ICommandHandler<CancelBookingCommand, Unit> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new CancelBookingCommand(id), ct);
        return result.IsFailure ? result.Error.ToProblem() : TypedResults.NoContent();
    }
}