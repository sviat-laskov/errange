using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace Errange.Extensions;

public static class HttpContextExtensions
{
    private static readonly ActionDescriptor EmptyActionDescriptor = new();
    private static readonly RouteData EmptyRouteData = new();

    public static Task WriteObjectResult(this HttpContext httpContext, int httpStatusCode, object objectResultValue)
    {
        httpContext.Response.OnStarting(state =>
        {
            ((HttpContext)state).ClearResponseCacheHeaders();
            return Task.CompletedTask;
        }, httpContext);

        var actionObjectResultExecutor = httpContext.RequestServices.GetRequiredService<IActionResultExecutor<ObjectResult>>();
        var actionContext = new ActionContext(httpContext, httpContext.GetRouteData() ?? EmptyRouteData, EmptyActionDescriptor);
        var objectResult = new ObjectResult(objectResultValue) { StatusCode = httpStatusCode };

        return actionObjectResultExecutor.ExecuteAsync(actionContext, objectResult);
    }

    public static HttpContext SetExceptionHandlerFeatures(this HttpContext httpContext, Exception exception)
    {
        IExceptionHandlerPathFeature exceptionHandlerFeature = new ExceptionHandlerFeature
        {
            Error = exception,
            Path = httpContext.Request.Path
        };
        SetExceptionHandlerFeatures(exceptionHandlerFeature, httpContext.Features);

        return httpContext;
    }

    public static HttpContext ClearHttpContext(this HttpContext httpContext)
    {
        // An endpoint may have already been set. Since we're going to re-invoke the middleware pipeline we need to reset
        // the endpoint and route values to ensure things are re-calculated.
        httpContext.Features.Get<IRouteValuesFeature>()?.RouteValues?.Clear();
        httpContext.Response.Clear();

        return httpContext;
    }

    public static HttpContext ClearResponseCacheHeaders(this HttpContext httpContext)
    {
        ClearCacheHeaders(httpContext.Response.Headers);

        return httpContext;
    }

    private static void ClearCacheHeaders(IHeaderDictionary headers)
    {
        headers[HeaderNames.CacheControl] = "no-cache,no-store";
        headers[HeaderNames.Pragma] = "no-cache";
        headers[HeaderNames.Expires] = "-1";
        headers.Remove(HeaderNames.ETag);
    }

    private static void SetExceptionHandlerFeatures(IExceptionHandlerPathFeature exceptionHandlerPathFeature, IFeatureCollection features)
    {
        features.Set(exceptionHandlerPathFeature);
        features.Set<IExceptionHandlerFeature>(exceptionHandlerPathFeature);
    }
}