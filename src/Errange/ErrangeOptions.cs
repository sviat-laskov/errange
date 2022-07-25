using System.Net;
using Errange.Exceptions;
using Microsoft.Extensions.Hosting;

namespace Errange;

//todo lock when all is configured
public class ErrangeOptions
{
    private static readonly ErrorPolicy<Exception> BaseExceptionErrorPolicy = new ErrorPolicy<Exception>()
        .WithHttpStatusCode(HttpStatusCode.InternalServerError)
        .WithDetail("Internal server error.");
    private static readonly ErrorPolicy<InvalidModelStateException> InvalidModelStateExceptionErrorPolicy = new ErrorPolicy<InvalidModelStateException>()
        .WithHttpStatusCode(HttpStatusCode.BadRequest)
        .WithTitle("One or more validation errors occurred.")
        .WithDataItemForEach(exception => exception.ModelState,
            (keyToModelStateEntry, _, _, _) => keyToModelStateEntry.Key,
            (keyToModelStateEntry, _, _, _) => keyToModelStateEntry.Value.RawValue,
            (keyToModelStateEntry, _, _, _) => keyToModelStateEntry.Value.Errors.Select(error => error.ErrorMessage));

    private readonly IDictionary<Type, ErrorPolicy> _exceptionTypeToErrorFactories = new Dictionary<Type, ErrorPolicy>();

    public Func<IHostEnvironment, bool> ExceptionDetailsAdditionPredicate = env => env.IsDevelopment();

    public static ErrangeOptions Instance => new();

    public ErrangeOptions()
    {
        _exceptionTypeToErrorFactories.Add(BaseExceptionErrorPolicy.ExceptionType, BaseExceptionErrorPolicy);
        _exceptionTypeToErrorFactories.Add(InvalidModelStateExceptionErrorPolicy.ExceptionType, InvalidModelStateExceptionErrorPolicy);
    }

    public ErrangeOptions AddPolicy<TException>(Action<ErrorPolicy<TException>> errorPolicyConfigure) where TException : Exception
    {
        var errorPolicy = new ErrorPolicy<TException>();
        errorPolicyConfigure(errorPolicy);
        return _exceptionTypeToErrorFactories.TryAdd(errorPolicy.ExceptionType, errorPolicy)
            ? this
            : throw new ArgumentException("Policy for this exception was already configured.", nameof(TException));
    }

    //public ErrangeOptions AddJsonSerializerOptionsFactory(Func<IServiceProvider, JsonSerializerOptions> jsonSerializerOptionsFactory)
    //{
    //    JsonSerializerOptionsFactory = jsonSerializerOptionsFactory;
    //    return this;
    //}

    //public ErrangeOptions AddWithBaseExceptions<TException>(Action<HttpContext, IServiceProvider, ErrorPolicy> errorFactory) where TException : Exception
    //{
    //    AddPolicy<TException>((_, httpContext, serviceProvider, error) => errorFactory(httpContext, serviceProvider, error)); // Will throw if TException is Exception, cause it's registered at constructor.

    //    var baseExceptionErrorPolicy = new ErrorPolicy
    //    {
    //        ExceptionType = typeof(TException).BaseType!,
    //        ErrorFactory = (_, httpContext, serviceProvider, error) => errorFactory(httpContext, serviceProvider, error)
    //    };
    //    while (_exceptionTypeToErrorFactories.TryAdd(baseExceptionErrorPolicy.ExceptionType, baseExceptionErrorPolicy)) // Last will be registered via config or Exception.
    //        baseExceptionErrorPolicy = new ErrorPolicy
    //        {
    //            ExceptionType = baseExceptionErrorPolicy.ExceptionType.BaseType!,
    //            ErrorFactory = baseExceptionErrorPolicy.ErrorFactory
    //        };

    //    return this;
    //}

    //public ErrangeOptions AddWithBaseExceptions<TException>(Action<HttpContext, ErrorPolicy> errorFactory) where TException : Exception => AddWithBaseExceptions<TException>((httpContext, _, error) => errorFactory(httpContext, error));

    //public ErrangeOptions AddWithBaseExceptions<TException>(Action<IServiceProvider, ErrorPolicy> errorFactory) where TException : Exception => AddWithBaseExceptions<TException>((_, serviceProvider, error) => errorFactory(serviceProvider, error));

    //public ErrangeOptions AddWithBaseExceptions<TException>(Action<ErrorPolicy> errorFactory) where TException : Exception => AddWithBaseExceptions<TException>((_, _, error) => errorFactory(error));

    public ErrorPolicy GetPolicyForNearestExceptionType(Type exceptionType)
    {
        while (true) // Latest exception type will be Exception.
        {
            if (_exceptionTypeToErrorFactories.TryGetValue(exceptionType, out ErrorPolicy? errorPolicy)) return errorPolicy;
            exceptionType = exceptionType.BaseType!;
        }
    }

    public ErrangeOptions AddExceptionDetails(Func<IHostEnvironment, bool> predicate)
    {
        ExceptionDetailsAdditionPredicate = predicate;
        return this;
    }
}