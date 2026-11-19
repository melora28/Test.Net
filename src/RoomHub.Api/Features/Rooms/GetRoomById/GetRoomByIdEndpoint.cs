using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using RoomHub.Api.Common.Http;
using RoomHub.Api.Domain.Rooms;
using RoomHub.Api.Infrastructure.Persistence;

namespace RoomHub.Api.Features.Rooms.GetRoomById;

public static class GetRoomByIdEndpoint
{
    public sealed record Response(Guid Id, string Name, int Capacity);

    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/{id:guid}", Handle).WithName("GetRoomById");

    private static async Task<Results<Ok<Response>, ProblemHttpResult>> Handle(
        Guid id,
        RoomHubDbContext db,
        CancellationToken ct)
    {
        var room = await db.Rooms
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new Response(r.Id, r.Name, r.Capacity))
            .FirstOrDefaultAsync(ct);

        return room is null
            ? RoomErrors.NotFound.ToProblem()
            : TypedResults.Ok(room);
    }
}