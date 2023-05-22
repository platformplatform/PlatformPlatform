using MediatR;
using PlatformPlatform.Foundation.DomainModeling.Cqrs;
using PlatformPlatform.Foundation.DomainModeling.Persistence;

namespace PlatformPlatform.Foundation.DomainModeling.Behaviors;

/// <summary>
///     The UnitOfWorkPipelineBehavior class is a MediatR pipeline behavior that encapsulates the unit of work pattern.
///     It is called after the handling of a Command and after handling all Domain Events. The pipeline ensures that all
///     changes to all aggregates and entities are committed to the database only after the command and domain events
///     are successfully handled. If an exception occurs the UnitOfWork.Commit will never be called, and all changes
///     will be lost.
/// </summary>
public sealed class UnitOfWorkPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IUnitOfWork _unitOfWork;

    public UnitOfWorkPipelineBehavior(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next();

        if (response is ICommandResult {IsSuccess: true})
        {
            await _unitOfWork.CommitAsync(cancellationToken);
        }

        return response;
    }
}