namespace PlatformPlatform.SharedKernel.EntityFramework;

/// <summary>
///     Tracks entities that should be permanently deleted, bypassing the soft delete interceptor.
///     Implemented by <see cref="SharedKernelDbContext{TContext}" /> to coordinate between
///     repositories and the <see cref="SoftDeleteInterceptor" />.
/// </summary>
internal interface IPurgeTracker
{
    void MarkForPurge(object entity);

    bool IsMarkedForPurge(object entity);
}
