using System.Reflection;
using RoomHub.Api.Domain.Common;

namespace RoomHub.Api.Common.Cqrs;

public interface ICommand<TResult>;

public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken ct = default);
}

public static class HandlerRegistrationExtensions
{
    public static IServiceCollection AddHandlers(this IServiceCollection services, Assembly assembly)
    {
        var handlers = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>))
                .Select(i => new { Interface = i, Implementation = t }));

        foreach (var h in handlers)
            services.AddScoped(h.Interface, h.Implementation);

        return services;
    }
}