using System.Text.Json;
using System.Text.Json.Serialization;
using Errange.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Errange.IntegrationTests.SystemUnderTest;

public class TestWebApiFactory : WebApplicationFactory<TestWebApiFactory>
{
    public const string DefaultEndpoint = "/endpoint";
    private readonly Action<IServiceCollection>? _additionalServicesConfigure;
    private readonly Action<IEndpointRouteBuilder>? _endpointRouteBuilderConfigure;
    private readonly Action<ErrangeOptions>? _errangeOptionsConfigure;

    public TestWebApiFactory(
        Action<IEndpointRouteBuilder>? endpointRouteBuilderConfigure = null,
        Action<ErrangeOptions>? errangeOptionsConfigure = null,
        Action<IServiceCollection>? additionalServicesConfigure = null)
    {
        _endpointRouteBuilderConfigure = endpointRouteBuilderConfigure;
        _errangeOptionsConfigure = errangeOptionsConfigure;
        _additionalServicesConfigure = additionalServicesConfigure;
    }

    protected override IHost CreateHost(IHostBuilder builder) => base.CreateHost(builder.UseContentRoot(Directory.GetCurrentDirectory()));

    protected override IHostBuilder CreateHostBuilder() => Host
        .CreateDefaultBuilder()
        .ConfigureServices(services =>
        {
            services
                .AddErrange(options => _errangeOptionsConfigure?.Invoke(options))
                .AddEndpointsApiExplorer()
                .AddRouting(options => options.LowercaseUrls = true)
                .AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                });
            _additionalServicesConfigure?.Invoke(services);
        });

    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.Configure(app => app
        .UseErrange()
        .UseRouting()
        .UseEndpoints(endpointRouteBuilder => _endpointRouteBuilderConfigure?.Invoke(endpointRouteBuilder)));
}