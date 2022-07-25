namespace Errange.IntegrationTests.SystemUnderTest.Exceptions;

public class CustomException : Exception
{
    public CustomException(string message) : base(message) { }
}