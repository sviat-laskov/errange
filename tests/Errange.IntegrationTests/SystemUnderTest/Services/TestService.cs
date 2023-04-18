namespace Errange.IntegrationTests.SystemUnderTest.Services;

public class TestService
{
    public string? Data { get; }

    public TestService(string? data = null) => Data = data;

    public string Throw() => throw new Exception();
}