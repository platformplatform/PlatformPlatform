using SharedKernel.Cqrs;

namespace SharedKernel.Tests.TestEntities;

public record TestCommand : ICommand, IRequest<Result<TestAggregate>>;
