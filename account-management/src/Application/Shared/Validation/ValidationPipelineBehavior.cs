using FluentValidation;
using MediatR;

namespace PlatformPlatform.AccountManagement.Application.Shared.Validation;

/// <summary>
///     The ValidationPipelineBehavior class is a MediatR pipeline behavior that validates the request using
///     FluentValidation. If the request is not valid, the pipeline will be short-circuited and the request will not be
///     handled. If the request is valid, the next pipeline behavior will be called.
/// </summary>
public sealed class ValidationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse> where TResponse : ICommandResult
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationPipelineBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        // Run all validators in parallel and await the results
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken))
        );

        // Aggregate the results from all validators into a distinct list of validation errors
        var validationErrors = validationResults
            .SelectMany(vr => vr.Errors)
            .Where(vf => vf is not null)
            .Select(vf => new PropertyError(vf.PropertyName, vf.ErrorMessage))
            .Distinct()
            .ToArray();

        if (validationErrors.Any())
        {
            return CreateValidationResult<TResponse>(validationErrors);
        }

        return await next();
    }

    /// <summary>
    ///     Uses reflection to create a new instance of the specified Result type, passing the validation errors to the
    ///     constructor.
    /// </summary>
    private static TResult CreateValidationResult<TResult>(PropertyError[] validationErrors)
        where TResult : ICommandResult
    {
        return (TResult) typeof(CommandResult<>)
            .GetGenericTypeDefinition()
            .MakeGenericType(typeof(TResult).GenericTypeArguments[0])
            .GetMethod(nameof(CommandResult<object>.Failure))!
            .Invoke(null, new object?[] {validationErrors})!;
    }
}