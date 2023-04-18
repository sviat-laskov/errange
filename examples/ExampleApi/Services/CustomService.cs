using ExampleApi.Services.Interfaces;

namespace ExampleApi.Services;

public class CustomService : ICustomService
{
    public string Data => nameof(Data);
}