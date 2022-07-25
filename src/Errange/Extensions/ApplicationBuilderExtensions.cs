using Microsoft.AspNetCore.Builder;

namespace Errange.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseErrange(this IApplicationBuilder app) => app.UseMiddleware<ExceptionHandlerMiddleware>();
}