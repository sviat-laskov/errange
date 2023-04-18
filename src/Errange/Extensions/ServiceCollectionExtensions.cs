using Errange.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;

namespace Errange.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Adds policies, needed for exception to error mapping. Default policies for <see cref="Exception" /> and invalid
    ///     <see cref="ModelStateDictionary" /> are included.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public static IServiceCollection AddErrange(this IServiceCollection services, Action<ErrangeOptions>? optionsConfigure = null)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        var errangeOptions = new ErrangeOptions();
        optionsConfigure?.Invoke(errangeOptions);

        services.ConfigureInvalidModelStateResponseFactory();

        return services.AddSingleton(errangeOptions);
    }

    /// <summary>
    ///     Changes default <see cref="ApiBehaviorOptions.InvalidModelStateResponseFactory" /> to direct response
    ///     generation through <see cref="ExceptionHandlerMiddleware" />
    /// </summary>
    internal static IServiceCollection ConfigureInvalidModelStateResponseFactory(this IServiceCollection services) => services
        .PostConfigure<ApiBehaviorOptions>(options => options.InvalidModelStateResponseFactory = context => throw new InvalidModelStateException(context.ModelState));
}