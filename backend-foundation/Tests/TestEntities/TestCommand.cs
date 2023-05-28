using MediatR;
using PlatformPlatform.Foundation.DomainModeling.Cqrs;

namespace PlatformPlatform.Foundation.Tests.TestEntities;

public record TestCommand : ICommand, IRequest<Result<TestAggregate>>;