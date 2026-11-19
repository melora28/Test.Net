using Microsoft.AspNetCore.Http.HttpResults;
using RoomHub.Api.Common.Cqrs;
using RoomHub.Api.Common.Http;
using RoomHub.Api.Domain.Common;
using RoomHub.Api.Domain.Rooms;

namespace RoomHub.Api.Features.Rooms.CreateRoom;

public sealed record CreateRoomCommand(string? Name, int Capacity) : ICommand<CreateRoomResponse>;
public sealed record CreateRoomResponse(Guid Id, string Name, int Capacity);

public sealed class CreateRoomHandler(IRoomRepository rooms, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateRoomCommand, CreateRoomResponse>
{
    public async Task<Result<CreateRoomResponse>> HandleAsync(CreateRoomCommand command, CancellationToken ct = default)
    {
        var roomResult = Room.Create(command.Name, command.Capacity);
        if (roomResult.IsFailure)
            return roomResult.Error;

        var room = roomResult.Value;
        rooms.Add(room);
        await unitOfWork.SaveChangesAsync(ct);

        return new CreateRoomResponse(room.Id, room.Name, room.Capacity);
    }
}

public static class CreateRoomEndpoint
{
    public sealed record Request(string? Name, int Capacity);

    public static void Map(RouteGroupBuilder group) =>
        group.MapPost("/", Handle).WithName("CreateRoom");

    private static async Task<Results<Created<CreateRoomResponse>, ProblemHttpResult>> Handle(
        Request request,
        ICommandHandler<CreateRoomCommand, CreateRoomResponse> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new CreateRoomCommand(request.Name, request.Capacity), ct);
        if (result.IsFailure)
            return result.Error.ToProblem();

        return TypedResults.Created($"/api/rooms/{result.Value.Id}", result.Value);
    }
}