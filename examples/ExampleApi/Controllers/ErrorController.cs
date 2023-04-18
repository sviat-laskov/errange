using System.ComponentModel.DataAnnotations;
using ExampleApi.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace ExampleApi.Controllers;

[Route("[controller]")]
[ApiController]
public class ErrorController : ControllerBase
{
    [HttpGet("custom")]
    public void ThrowCustomException(string parameter = "defaultParameterValue")
    {
        var customException = new CustomException();
        customException.Data.Add(nameof(parameter), parameter);

        throw customException;
    }

    [HttpGet("validation/range")]
    public void ThrowValidationExceptionForNullableParameter([Range(minimum: 1, maximum: 2)] int requiredParameter) { }

    [HttpGet("validation/not-nullable")]
    public void ThrowValidationExceptionForNotNullableParameter(string requiredParameter) { }

    [HttpGet("base")]
    public void ThrowException() => throw new Exception();
}