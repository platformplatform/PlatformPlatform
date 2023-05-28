using MediatR;
using PlatformPlatform.SharedKernel.DomainModeling.Cqrs;

namespace PlatformPlatform.SharedKernel.Tests.TestEntities;

public record TestCommand : ICommand, IRequest<Result<TestAggregate>>;