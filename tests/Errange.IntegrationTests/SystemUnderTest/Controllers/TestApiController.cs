using Microsoft.AspNetCore.Mvc;

namespace Errange.IntegrationTests.SystemUnderTest.Controllers;

[Route("controller-with-model-state-invalid-filter")]
[ApiController]
public class TestApiController : ControllerBase
{
    [HttpGet]
    public IActionResult Get(string requiredParameter) => Ok();
}

[Route("[controller]")]
[ApiController]
public class ErrorController : ControllerBase
{
    [HttpGet("custom")]
    public void ThrowCustomException(string parameter = "defaultParameterValue") => throw new Exception();

    [HttpGet("validation")]
    public void ThrowValidationException(string requiredParameter = null) { }

    [HttpGet("base")]
    public void ThrowException() => throw new Exception();

    //[HttpGet("base")]
    //public IActionResult ThrowException1()
    //{
    //    if (!ModelState.IsValid) return BadRequest(ModelState);
    //    return Ok();
    //}
}