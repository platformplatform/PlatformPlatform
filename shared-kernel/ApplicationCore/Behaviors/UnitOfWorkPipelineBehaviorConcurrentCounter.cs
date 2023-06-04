namespace PlatformPlatform.SharedKernel.ApplicationCore.Behaviors;

/// <summary>
///     The UnitOfWorkPipelineBehaviorConcurrentCounter class is a concurrent counter used to count the number of
///     concurrent requests that are being handled by the UnitOfWorkPipelineBehavior. It is used by only commit changes
///     to the database when all requests have been handled. This is to ensure that all changes to all aggregates and
///     entities are committed to the database only after all command and domain events are successfully handled.
/// </summary>
public sealed class UnitOfWorkPipelineBehaviorConcurrentCounter
{
    private int _concurrentCount;

    public void Increment()
    {
        _concurrentCount++;
    }

    public void Decrement()
    {
        _concurrentCount--;
    }

    public bool IsZero()
    {
        return _concurrentCount == 0;
    }
}