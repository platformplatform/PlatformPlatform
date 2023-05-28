using MediatR;

namespace PlatformPlatform.SharedKernel.DomainCore.DomainEvents;

/// <summary>
///     The DomainEvent interface represents a domain event that occurred in the domain. The DomainEvent implements the
///     <see cref="INotification" /> interface from MediatR. To configure an event handler, you need to create a class
///     that implements the <see cref="INotificationHandler{TNotification}" /> interface. This should be done in the
///     Application Layer. Any event that occurs in the domain can be handled by one or more domain event handlers.
///     Domain events are happening in the context of an aggregate. Events are published by the
///     PublishDomainEventsPipelineBehavior in the Application Layer, before the UnitOfWork is committed and the parent
///     aggregate that raises the event is saved to the database. That means that the EventHandler also runs before
///     changes are saved to the database. It's crucial to understand that domain event handlers are not supposed to
///     produce any external side effects outside the current transaction/UnitOfWork. For instance, they can be utilized
///     to create, update, or delete aggregates, or they could be used to update read models. However, they should not
///     be used to invoke other services (e.g., send emails) that are not part of the same database transaction. For
///     such tasks, use Integration Events instead.
/// </summary>
public interface IDomainEvent : INotification
{
}