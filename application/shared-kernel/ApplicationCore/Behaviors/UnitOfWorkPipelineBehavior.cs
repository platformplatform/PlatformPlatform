using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;
using PlatformPlatform.SharedKernel.DomainCore.Persistence;

namespace PlatformPlatform.SharedKernel.ApplicationCore.Behaviors;

/// <summary>
///     The UnitOfWorkPipelineBehavior class is a MediatR pipeline behavior that encapsulates the unit of work pattern.
///     It is called after the handling of a Command and after handling all Domain Events. The pipeline ensures that all
///     changes to all aggregates and entities are committed to the database only after the command and domain events
///     are successfully handled. If an exception occurs the UnitOfWork.Commit will never be called, and all changes
///     will be lost.
/// </summary>
public sealed class UnitOfWorkPipelineBehavior<TRequest, TResponse>(
    IUnitOfWork unitOfWork,
    ConcurrentCommandCounter concurrentCommandCounter
) : IPipelineBehavior<TRequest, TResponse> where TRequest : ICommand where TResponse : ResultBase
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        concurrentCommandCounter.Increment();
        var response = await next();

        if (response is ResultBase { IsSuccess: true })
        {
            concurrentCommandCounter.Decrement();
            if (concurrentCommandCounter.IsZero())
            {
                await unitOfWork.CommitAsync(cancellationToken);
            }
        }

        return response;
    }
}