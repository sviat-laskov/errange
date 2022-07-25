using Errange.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Errange.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddErrange(this IServiceCollection services, Action<ErrangeOptions> optionsConfigure)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (optionsConfigure == null) throw new ArgumentNullException(nameof(optionsConfigure));

        var errangeOptions = ErrangeOptions.Instance;
        optionsConfigure(errangeOptions);

        services.ConfigureOrOverrideApiBehaviorOptionsWithIInvalidModelStateResponseFactory();

        return services.AddSingleton(errangeOptions);
    }

    internal static IServiceCollection ConfigureOrOverrideApiBehaviorOptionsWithIInvalidModelStateResponseFactory(this IServiceCollection services) => services
        .PostConfigure<ApiBehaviorOptions>(options => options.InvalidModelStateResponseFactory = context => throw new InvalidModelStateException(context.ModelState));

    //new BadRequestObjectResult(new ErrangeProblemDetails
    //{
    //    Status = StatusCodes.Status400BadRequest,
    //    Title = "One or more validation errors occurred.",
    //    Data = context.ModelState.ToDictionary(
    //        keyAndModelStateEntry => keyAndModelStateEntry.Key,
    //        keyAndModelStateEntry => new ProblemDataItemVM
    //        {
    //            Value = keyAndModelStateEntry.Value!.RawValue,
    //            Messages = keyAndModelStateEntry.Value!.Errors
    //                .Select(modelError => modelError.ErrorMessage)
    //                .ToHashSet()
    //        })
    //}));
}