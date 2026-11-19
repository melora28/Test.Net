using Microsoft.AspNetCore.Http.HttpResults;
using RoomHub.Api.Common.Cqrs;
using RoomHub.Api.Common.Http;
using RoomHub.Api.Domain.Bookings;
using RoomHub.Api.Domain.Common;
using RoomHub.Api.Domain.Rooms;

namespace RoomHub.Api.Features.Bookings.CreateBooking;

public sealed record CreateBookingCommand(
    Guid RoomId, string? BookedBy, DateTime StartUtc, DateTime EndUtc, int Attendees)
    : ICommand<CreateBookingResponse>;

public sealed record CreateBookingResponse(Guid Id, Guid RoomId, BookingStatus Status);

public sealed class CreateBookingHandler(
    IRoomRepository rooms,
    IBookingRepository bookings,
    IUnitOfWork unitOfWork,
    TimeProvider time)
    : ICommandHandler<CreateBookingCommand, CreateBookingResponse>
{
    public async Task<Result<CreateBookingResponse>> HandleAsync(CreateBookingCommand command, CancellationToken ct = default)
    {
        var room = await rooms.GetByIdAsync(command.RoomId, ct);
        if (room is null)
            return RoomErrors.NotFound;

        var rangeResult = DateRange.Create(command.StartUtc, command.EndUtc);
        if (rangeResult.IsFailure)
            return rangeResult.Error;

        var bookingResult = Booking.Create(
            room, command.BookedBy, rangeResult.Value, command.Attendees, time.GetUtcNow().UtcDateTime);
        if (bookingResult.IsFailure)
            return bookingResult.Error;

        var booking = bookingResult.Value;

        var activeBookings = await bookings.GetActiveForRoomAsync(room.Id, ct);
        if (activeBookings.Any(b => b.Range.Overlaps(booking.Range)))
            return BookingErrors.RoomOccupied;

        var activeCountToday = await bookings.CountActiveForParticipantOnDayAsync(
            booking.BookedBy, booking.Range.StartUtc, ct);
        if (activeCountToday >= 2)
            return BookingErrors.DailyLimitExceeded;

        bookings.Add(booking);
        await unitOfWork.SaveChangesAsync(ct);

        return new CreateBookingResponse(booking.Id, booking.RoomId, booking.Status);
    }
}

public static class CreateBookingEndpoint
{
    public sealed record Request(string? BookedBy, DateTime StartUtc, DateTime EndUtc, int Attendees);

    public static void Map(RouteGroupBuilder group) =>
        group.MapPost("/{roomId:guid}/bookings", Handle).WithName("CreateBooking");

    private static async Task<Results<Created<CreateBookingResponse>, ProblemHttpResult>> Handle(
        Guid roomId,
        Request request,
        ICommandHandler<CreateBookingCommand, CreateBookingResponse> handler,
        CancellationToken ct)
    {
        var command = new CreateBookingCommand(
            roomId, request.BookedBy, request.StartUtc, request.EndUtc, request.Attendees);

        var result = await handler.HandleAsync(command, ct);
        if (result.IsFailure)
            return result.Error.ToProblem();

        return TypedResults.Created($"/api/bookings/{result.Value.Id}", result.Value);
    }
}