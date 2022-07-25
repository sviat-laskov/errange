using System.Diagnostics;
using Errange.Extensions;
using Errange.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Errange;

public class ExceptionHandlerMiddleware
{
    private const string ExceptionInfoKey = "exceptionInfo", TraceIdentifierKey = "traceId";
    private static readonly Type BaseExceptionType = typeof(Exception);
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;
    private readonly RequestDelegate _next;

    public ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleException(context, exception);
        }
    }

    public async Task HandleException(HttpContext httpContext, Exception exception)
    {
        Type exceptionType = exception.GetType();
        _logger.LogTrace("Handling '{ExceptionTypeFullName}' exception started.", exceptionType.FullName);

        PrepareHttpContextForExceptionHandling(httpContext, exception);
        PathString originalPath = httpContext.Request.Path;

        var errangeOptions = httpContext.RequestServices.GetRequiredService<ErrangeOptions>();
        ErrorPolicy errorPolicy = errangeOptions.GetPolicyForNearestExceptionType(exceptionType);

        var problemDetails = new ErrangeProblemDetails();

        try
        {
            _logger.LogTrace("Generating error from '{ExceptionTypeFullName}' exception via error policy for '{ErrorPolicyExceptionTypeFullName}' exception.", exceptionType.FullName, errorPolicy.ExceptionType.FullName);

            problemDetails = errorPolicy.GenerateError(exception, httpContext, httpContext.RequestServices);
        }
        catch (Exception exceptionFromErrorPolicy)
        {
            _logger.LogError(exceptionFromErrorPolicy, "Failed generating error from '{ExceptionTypeFullName}' exception via error policy for '{ErrorPolicyExceptionTypeFullName}' exception.", exceptionType.FullName, errorPolicy.ExceptionType.FullName);

            problemDetails = errangeOptions
                .GetPolicyForNearestExceptionType(BaseExceptionType)
                .GenerateError(exception, httpContext, httpContext.RequestServices);
        }
        finally
        {
            _logger.LogTrace("Writing error from '{ExceptionTypeFullName}' exception to response.", exceptionType.FullName);

            if (errangeOptions.ExceptionDetailsAdditionPredicate(httpContext.RequestServices.GetRequiredService<IHostEnvironment>()))
                problemDetails.Extensions[ExceptionInfoKey] = new ExceptionInfoVM
                {
                    Message = exception.Message,
                    StackTrace = exception.StackTrace?.Trim()
                };
            problemDetails.Extensions[TraceIdentifierKey] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

            await httpContext.WriteObjectResult(problemDetails.Status!.Value, problemDetails);

            httpContext.Request.Path = originalPath;
        }
    }

    private void PrepareHttpContextForExceptionHandling(HttpContext httpContext, Exception exception)
    {
        if (httpContext.Response.HasStarted)
        {
            _logger.LogDebug("Can't handle exception, because response has already been started.");
            throw exception;
        }

        httpContext
            .ClearHttpContext()
            .SetExceptionHandlerFeatures(exception);
    }
}