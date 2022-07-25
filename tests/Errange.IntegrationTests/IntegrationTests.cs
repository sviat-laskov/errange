using System.Net;
using Errange.Extensions;
using Errange.IntegrationTests.SystemUnderTest;
using Errange.IntegrationTests.SystemUnderTest.Exceptions;
using Errange.IntegrationTests.SystemUnderTest.Factories;
using Errange.IntegrationTests.SystemUnderTest.Validators;
using Errange.IntegrationTests.SystemUnderTest.ViewModels;
using Errange.ViewModels;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Errange.IntegrationTests;

public class IntegrationTests
{
    [TestCase(
        HttpStatusCode.BadRequest,
        "CUS001",
        "Happens when you want this.",
        "Custom error happened.",
        "defaultParameterValue",
        "Constant", "This message happened, cause you configured it.")]
    public async Task ErrorShouldMatchConfiguration(
        HttpStatusCode httpStatusCode,
        string errorCode,
        string errorDescription,
        string exceptionMessage,
        string requestParameterValue,
        string constantValue, string dataItemConstantMessage)
    {
        const string dataItemFromExceptionKey = "fromException",
                     dataItemFromRequestKey = "fromRequest",
                     requestParameterKey = "parameter",
                     dataItemConstantKey = "constant";

        // Arrange
        TestServer server = new TestWebApiFactory(
                builder => builder.MapGet(TestWebApiFactory.DefaultEndpoint, _ => throw new CustomException(exceptionMessage)),
                options => options
                    .AddPolicy<CustomException>(policy => policy
                        .WithHttpStatusCode(httpStatusCode)
                        .WithCode(errorCode)
                        .WithDetail(errorDescription)
                        .WithDataItem((_, _, _) => dataItemFromExceptionKey, (exception, _, _) => exception.Message)
                        .WithDataItem((_, _, _) => dataItemFromRequestKey, (_, context, _) => context.Request.Query[requestParameterKey].SingleOrDefault()).When((value, _, _, _) => value == requestParameterValue)
                        .WithDataItem((_, _, _) => dataItemConstantKey, (_, _, _) => constantValue, dataItemConstantMessage)))
            .Server;
        HttpClient client = server.CreateClient();

        // Act
        HttpResponseMessage response = await client.GetAsync(QueryHelpers.AddQueryString(TestWebApiFactory.DefaultEndpoint, requestParameterKey, requestParameterValue));

        // Assert
        response.StatusCode.Should().Be(httpStatusCode);
        var errangeProblemDetails = await response.Content.ReadAsAsync<ErrangeProblemDetails>(MediaTypeFormatterCollectionFactory.WithProblemJsonMediaTypeFormatter);

        errangeProblemDetails.Code.Should().Be(errorCode);
        errangeProblemDetails.Detail.Should().Be(errorDescription);
        errangeProblemDetails.Code.Should().Be(errorCode);
        errangeProblemDetails.Data.Should().ContainKey(dataItemFromExceptionKey).WhoseValue
            .Should().Match<ProblemDataItemVM>(dataItem => dataItem.Value as string == exceptionMessage)
            .And.Match<ProblemDataItemVM>(dataItem => !dataItem.Messages.Any());
        errangeProblemDetails.Data.Should().ContainKey(dataItemFromRequestKey).WhoseValue
            .Should().Match<ProblemDataItemVM>(dataItem => dataItem.Value as string == requestParameterValue)
            .And.Match<ProblemDataItemVM>(dataItem => !dataItem.Messages.Any());
        errangeProblemDetails.Data.Should().ContainKey(dataItemConstantKey).WhoseValue
            .Should().Match<ProblemDataItemVM>(dataItem => dataItem.Value as string == constantValue)
            .And.Match<ProblemDataItemVM>(dataItem => dataItem.Messages.Single() == dataItemConstantMessage);
    }

    [TestCase]
    public async Task ErrorShouldBeReturnedWhenModelStateIsInvalid()
    {
        // Arrange
        var testVM = new TestVM();
        TestServer server = new TestWebApiFactory(
                app => app.MapPost(TestWebApiFactory.DefaultEndpoint, ([FromBody] TestVM testVM, [FromServices] IValidator<TestVM> validator) =>
                {
                    ValidationResult? validationResult = validator.Validate(testVM);

                    return validationResult.IsValid
                        ? Results.Ok()
                        : throw new ValidationException(validationResult.Errors);
                }),
                options => options.AddPolicy<ValidationException>(policy => policy
                    .WithHttpStatusCode(HttpStatusCode.BadRequest)
                    .WithDataItemForEach(
                        exception => exception.Errors.GroupBy(validationFailure => validationFailure.PropertyName, validationFailure => validationFailure),
                        (validationFailures, _, _, _) => validationFailures.Key,
                        (validationFailures, _, _, _) => validationFailures.First().AttemptedValue,
                        (validationFailures, _, _, _) => validationFailures.Select(validationFailure => validationFailure.ErrorMessage))),
                services => services.AddScoped<IValidator<TestVM>, TestVmValidator>())
            .Server;
        HttpClient client = server.CreateClient();

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync(TestWebApiFactory.DefaultEndpoint, testVM);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errangeProblemDetails = await response.Content.ReadAsAsync<ErrangeProblemDetails>(MediaTypeFormatterCollectionFactory.WithProblemJsonMediaTypeFormatter);
        errangeProblemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
        errangeProblemDetails.Data.Should().ContainKey(nameof(TestVM.RequiredProperty)).WhoseValue
            .Should().Match<ProblemDataItemVM>(dataItem => dataItem.Value as string == testVM.RequiredProperty)
            .And.Match<ProblemDataItemVM>(dataItem => dataItem.Messages.Count == 1);
        errangeProblemDetails.Data.Should().ContainKey(nameof(TestVM.RequiredPropertyWithRangeLimit)).WhoseValue
            .Should().Match<ProblemDataItemVM>(dataItem => (long)dataItem.Value! == testVM.RequiredPropertyWithRangeLimit)
            .And.Match<ProblemDataItemVM>(dataItem => dataItem.Messages.Count == 2);
    }
}