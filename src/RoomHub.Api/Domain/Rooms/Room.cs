using RoomHub.Api.Domain.Common;

namespace RoomHub.Api.Domain.Rooms;

public static class RoomErrors
{
    public static readonly Error NameRequired = Error.Validation("Room.NameRequired", "Room name is required.");
    public static readonly Error InvalidCapacity = Error.Validation("Room.InvalidCapacity", "Capacity must be positive.");
    public static readonly Error NotFound = Error.NotFound("Room.NotFound", "The room does not exist.");
}

public sealed class Room
{
    private Room() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Capacity { get; private set; }

    public static Result<Room> Create(string? name, int capacity)
    {
        if (string.IsNullOrWhiteSpace(name))
            return RoomErrors.NameRequired;

        if (capacity <= 0)
            return RoomErrors.InvalidCapacity;

        return new Room
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Capacity = capacity
        };
    }
}

public interface IRoomRepository
{
    Task<Room?> GetByIdAsync(Guid id, CancellationToken ct = default);
    void Add(Room room);
}