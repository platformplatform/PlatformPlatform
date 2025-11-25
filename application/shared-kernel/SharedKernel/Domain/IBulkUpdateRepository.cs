namespace PlatformPlatform.SharedKernel.Domain;

public interface IBulkUpdateRepository<in T> where T : IAggregateRoot
{
    void UpdateRange(T[] aggregates);
}
