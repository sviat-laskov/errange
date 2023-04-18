using System.Net;
using System.Text.Json;
using Errange.ViewModels;
using FluentAssertions;

namespace Errange.IntegrationTests.SystemUnderTest.Extensions;

public static class HttpResponseMessageExtensions
{
    public static async Task<ErrangeProblemDetails> MapToErrangeProblemDetailsIfStatusCodeMatches(this HttpResponseMessage httpResponseMessage, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest)
    {
        httpResponseMessage.StatusCode.Should().Be(httpStatusCode);
        string errangeProblemDetailsJson = await httpResponseMessage.Content.ReadAsStringAsync();
        var errangeProblemDetails = JsonSerializer.Deserialize<ErrangeProblemDetails>(errangeProblemDetailsJson, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        return errangeProblemDetails;
    }
}