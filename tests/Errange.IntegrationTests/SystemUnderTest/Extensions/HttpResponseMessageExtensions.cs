using System.Net;
using System.Net.Http.Json;
using Errange.ViewModels;
using FluentAssertions;

namespace Errange.IntegrationTests.SystemUnderTest.Extensions;

public static class HttpResponseMessageExtensions
{
    public static async Task<ErrangeProblemDetails> MapToErrangeProblemDetailsIfStatusCodeMatches(this HttpResponseMessage httpResponseMessage, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest)
    {
        httpResponseMessage.StatusCode.Should().Be(httpStatusCode);
        var errangeProblemDetails = await httpResponseMessage.Content.ReadFromJsonAsync<ErrangeProblemDetails>();

        return errangeProblemDetails!;
    }
}