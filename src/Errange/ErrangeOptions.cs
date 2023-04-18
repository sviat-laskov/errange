using System.Net;
using Errange.Exceptions;
using Microsoft.Extensions.Hosting;

namespace Errange;

public class ErrangeOptions
{
    private static readonly ProblemPolicy<Exception> BaseExceptionProblemPolicy = new ProblemPolicy<Exception>()
        .WithHttpStatusCode(HttpStatusCode.InternalServerError)
        .WithDetail("Internal server error.");
    private static readonly ProblemPolicy<InvalidModelStateException> InvalidModelStateExceptionProblemPolicy = new ProblemPolicy<InvalidModelStateException>()
        .WithHttpStatusCode(HttpStatusCode.BadRequest)
        .WithTitle("One or more validation errors occurred.")
        .WithDataItems((exception, _, _) => exception.ModelState,
            (keyToModelStateEntry, _, _, _) => keyToModelStateEntry.Key,
            (keyToModelStateEntry, _, _, _) => keyToModelStateEntry.Value.RawValue,
            (keyToModelStateEntry, _, _, _) => keyToModelStateEntry.Value.Errors.Select(error => error.ErrorMessage));

    internal static Func<IHostEnvironment, bool> DefaultExceptionDetailsAdditionPredicate = env => env.IsDevelopment();

    private readonly IDictionary<Type, ProblemPolicy> _exceptionTypeToErrorFactories = new Dictionary<Type, ProblemPolicy>();

    internal Func<IHostEnvironment, bool> ExceptionInfoInclusionPredicate = DefaultExceptionDetailsAdditionPredicate;

    internal ErrangeOptions()
    {
        _exceptionTypeToErrorFactories.Add(BaseExceptionProblemPolicy.ExceptionType, BaseExceptionProblemPolicy);
        _exceptionTypeToErrorFactories.Add(InvalidModelStateExceptionProblemPolicy.ExceptionType, InvalidModelStateExceptionProblemPolicy);
    }

    /// <summary>
    ///     Adds policy to map exception to error.
    /// </summary>
    /// <typeparam name="TException">
    ///     Exception, that should be mapped. If it's child is thrown, but policy for it is not
    ///     present - policy for this exception is used.
    /// </typeparam>
    /// <returns></returns>
    public ErrangeOptions WithPolicy<TException>(Action<ProblemPolicy<TException>> errorPolicyConfigure) where TException : Exception
    {
        var errorPolicy = new ProblemPolicy<TException>();
        errorPolicyConfigure(errorPolicy);
        _exceptionTypeToErrorFactories[errorPolicy.ExceptionType] = errorPolicy;

        return this;
    }

    internal ProblemPolicy GetPolicyForNearestExceptionType(Type exceptionType)
    {
        while (true) // Latest exception type will be Exception.
        {
            if (_exceptionTypeToErrorFactories.TryGetValue(exceptionType, out ProblemPolicy? errorPolicy)) return errorPolicy;
            exceptionType = exceptionType.BaseType!;
        }
    }

    public ErrangeOptions AddExceptionDetailsWhen(Func<IHostEnvironment, bool> predicate)
    {
        ExceptionInfoInclusionPredicate = predicate;
        return this;
    }
}