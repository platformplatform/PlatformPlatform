namespace PlatformPlatform.SharedKernel.Domain;

public interface IBulkRemoveRepository<in T> where T : IAggregateRoot
{
    void RemoveRange(T[] aggregates);
}
