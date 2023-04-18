using System.Net;
using System.Text.Json.Serialization;
using Errange.Extensions;
using ExampleApi.Exceptions;
using ExampleApi.Services;
using ExampleApi.Services.Interfaces;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddScoped<ICustomService, CustomService>()
    .AddEndpointsApiExplorer()
    .AddSwaggerGen()
    .AddRouting(options => options.LowercaseUrls = true)
    .AddErrange(options => options
        .WithPolicy<CustomException>(policy => policy
            .WithHttpStatusCode(HttpStatusCode.BadRequest)
            .WithCustomProblemCode("CUS001")
            .WithDetail("Happens when you want this.")
            .WithDataItem("fromException").WithValue(exception => new
            {
                exception.Message,
                Parameter = exception.Data["parameter"]
            }).WithMessages("This is item from exception.")
            .ProblemPolicy.WithDataItem("fromRequest").WithValue(context => context.Request.Query["parameter"].SingleOrDefault()).WithMessages("This is item from http context request.")
            .ProblemPolicy.WithDataItem("fromServices").WithValue<ICustomService, string>(customService => customService.Data).WithMessages("This is item from service.")
            .ProblemPolicy.WithDataItem("constant").WithValue("Constant").WithMessages("This is constant value.")
        ))
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault);

WebApplication app = builder.Build();

app.UseErrange();

app
    .UseSwagger()
    .UseSwaggerUI();

app.MapControllers();

app.Run();