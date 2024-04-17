using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;
using PlatformPlatform.SharedKernel.DomainCore.DomainEvents;

namespace PlatformPlatform.SharedKernel.ApplicationCore.Behaviors;

/// <summary>
///     This method publishes any domain events were generated during the execution of a command (and added to aggregates).
///     It's crucial to understand that domain event handlers are not supposed to produce any external side effects outside
///     the current transaction/UnitOfWork. For instance, they can be utilized to create, update, or delete aggregates,
///     or they could be used to update read models. However, they should not be used to invoke other services (e.g., send
/// </summary>
public sealed class PublishDomainEventsPipelineBehavior<TRequest, TResponse>(
    IDomainEventCollector domainEventCollector,
    IPublisher mediator
) : IPipelineBehavior<TRequest, TResponse> where TRequest : ICommand where TResponse : ResultBase
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();
        
        while (true)
        {
            var aggregatesWithDomainEvents = domainEventCollector.GetAggregatesWithDomainEvents();
            
            if (aggregatesWithDomainEvents.Length == 0) break;
            
            foreach (var aggregate in aggregatesWithDomainEvents)
            {
                var domainEvents = aggregate.GetAndClearDomainEvents();
                foreach (var domainEvent in domainEvents)
                {
                    // Publish the domain event using MediatR. Registered event handlers will be invoked immediately
                    await mediator.Publish(domainEvent, cancellationToken);
                }
            }
        }
        
        return response;
    }
}
