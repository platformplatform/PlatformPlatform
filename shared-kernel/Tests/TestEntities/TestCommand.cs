using MediatR;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;

namespace PlatformPlatform.SharedKernel.Tests.TestEntities;

public record TestCommand : ICommand, IRequest<Result<TestAggregate>>;