using PlatformPlatform.SharedKernel.Application.Cqrs;

namespace PlatformPlatform.SharedKernel.Tests.TestEntities;

public record TestCommand : ICommand, IRequest<Result<TestAggregate>>;
