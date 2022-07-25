using System.ComponentModel.DataAnnotations;
using ExampleApi.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace ExampleApi.Controllers;

[Route("[controller]")]
[ApiController]
public class ErrorController : ControllerBase
{
    [HttpGet("custom")]
    public void ThrowCustomException(string parameter = "defaultParameterValue") => throw new CustomException();

    [HttpGet("validation")]
    public void ThrowValidationException([Required] [Range(minimum: 1, maximum: 2)] int? requiredParameter = null) { }

    [HttpGet("base")]
    public void ThrowException() => throw new Exception();

    //[HttpGet("base")]
    //public IActionResult ThrowException1()
    //{
    //    if (!ModelState.IsValid) return BadRequest(ModelState);
    //    return Ok();
    //}
}