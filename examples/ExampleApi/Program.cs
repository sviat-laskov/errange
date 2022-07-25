using System.Net;
using System.Text.Json.Serialization;
using Errange.Extensions;
using ExampleApi.Exceptions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddEndpointsApiExplorer()
    .AddSwaggerGen()
    .AddRouting(options => options.LowercaseUrls = true)
    .AddErrange(options => options
        .AddPolicy<CustomException>(policy => policy
            .WithHttpStatusCode(HttpStatusCode.BadRequest)
            .WithCode("CUS001")
            .WithDetail("Happens when you want this.")
            .WithDataItem((_, _, _) => "fromException", (exception, _, _) => exception.Message)
            .WithDataItem((_, _, _) => "fromRequest", (_, context, _) => context.Request.Query["parameter"].SingleOrDefault()).When((value, _, _, _) => value == "defaultParameterValue")
            .WithDataItem((_, _, _) => "constant", (_, _, _) => "Constant", "This message happened, cause you configured it.")))
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault);

WebApplication app = builder.Build();

app.UseErrange();

app
    .UseSwagger()
    .UseSwaggerUI();

app.MapControllers();

app.Run();