using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;
using PlatformPlatform.SharedKernel.DomainCore.DomainEvents;

namespace PlatformPlatform.SharedKernel.ApplicationCore.Behaviors;

/// <summary>
///     This method publishes any domain events that were generated during the execution of a command (and added to
///     aggregates). It's crucial to understand that domain event handlers are not supposed to produce any external side
///     effects outside the current transaction/UnitOfWork. For instance, they can be utilized to create, update, or
///     delete aggregates, or they could be used to update read models. However, they should not be used to invoke other
///     services (e.g., send emails) that are not part of the same database transaction. For such tasks, use Integration
///     Events instead.
/// </summary>
public sealed class PublishDomainEventsPipelineBehavior<TRequest, TResponse>(
    IDomainEventCollector domainEventCollector,
    IPublisher mediator
) : IPipelineBehavior<TRequest, TResponse> where TRequest : ICommand where TResponse : ResultBase
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        var response = await next();

        var domainEvents = new Queue<IDomainEvent>();

        EnqueueAndClearDomainEvents(domainEvents);

        while (domainEvents.Count > 0)
        {
            var domainEvent = domainEvents.Dequeue();

            // Publish the domain event to the MediatR pipeline. Registered event handlers will be invoked immediately
            await mediator.Publish(domainEvent, cancellationToken);

            if (domainEvents.Count == 0)
            {
                // Add any new domain events that were generated during the execution of the event handlers
                EnqueueAndClearDomainEvents(domainEvents);
            }
        }

        return response;
    }

    /// <summary>
    ///     Adds any new domain events to the processing queue and clears them from the originating aggregates.
    /// </summary>
    private void EnqueueAndClearDomainEvents(Queue<IDomainEvent> domainEvents)
    {
        foreach (var aggregate in domainEventCollector.GetAggregatesWithDomainEvents())
        {
            foreach (var domainEvent in aggregate.DomainEvents)
            {
                domainEvents.Enqueue(domainEvent);
            }

            aggregate.ClearDomainEvents();
        }
    }
}