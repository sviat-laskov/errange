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
    public static IServiceCollection AddErrange(this IServiceCollection services, Action<ErrangeOptions> optionsConfigure)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (optionsConfigure == null) throw new ArgumentNullException(nameof(optionsConfigure));

        var errangeOptions = new ErrangeOptions();
        optionsConfigure(errangeOptions);

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