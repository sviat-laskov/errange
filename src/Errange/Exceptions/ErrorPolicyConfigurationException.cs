namespace Errange.Exceptions;

public class ErrorPolicyConfigurationException : ArgumentException
{
    public ErrorPolicyConfigurationException(string message, string paramName) : base(message, paramName) { }
}