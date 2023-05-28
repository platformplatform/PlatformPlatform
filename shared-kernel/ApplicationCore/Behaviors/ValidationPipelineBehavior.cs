using System.Net;
using FluentValidation;
using MediatR;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;
using PlatformPlatform.SharedKernel.ApplicationCore.Validation;

namespace PlatformPlatform.SharedKernel.ApplicationCore.Behaviors;

/// <summary>
///     The ValidationPipelineBehavior class is a MediatR pipeline behavior that validates the request using
///     FluentValidation. If the request is not valid, the pipeline will be short-circuited and the request will not be
///     handled. If the request is valid, the next pipeline behavior will be called.
/// </summary>
public sealed class ValidationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand where TResponse : IResult
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
        var validationResults =
            await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        // Aggregate the results from all validators into a distinct list of attribute errors
        var attributeErrors = validationResults
            .SelectMany(result => result.Errors)
            .Where(failure => failure != null)
            .Select(failure => new AttributeError(failure.PropertyName.Split('.').First(), failure.ErrorMessage))
            .ToArray();

        if (attributeErrors.Any())
        {
            return CreateValidationResult<TResponse>(attributeErrors);
        }

        return await next();
    }

    /// <summary>
    ///     Uses reflection to create a new instance of the specified Result type, passing the attributeErrors to the
    ///     constructor.
    /// </summary>
    // ReSharper disable once SuggestBaseTypeForParameter
    private static TResult CreateValidationResult<TResult>(AttributeError[] attributeErrors)
        where TResult : IResult
    {
        return (TResult) Activator.CreateInstance(typeof(TResult), null, attributeErrors, HttpStatusCode.BadRequest)!;
    }
}