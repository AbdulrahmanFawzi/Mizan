using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Mizan.Application.Abstractions;
using Mizan.Domain.Common;

namespace Mizan.Application.EventHandlers;

public sealed class DomainEventDispatcher(IServiceProvider serviceProvider)
{
    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
        var handle = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!;

        foreach (var handler in serviceProvider.GetServices(handlerType))
        {
            // DoNotWrapExceptions: a DomainException from a handler stays a DomainException.
            var task = (Task)handle.Invoke(handler, BindingFlags.DoNotWrapExceptions, null, [domainEvent, cancellationToken], null)!;
            await task;
        }
    }
}
