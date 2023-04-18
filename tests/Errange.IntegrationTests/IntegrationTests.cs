using System.Net;
using Errange.IntegrationTests.SystemUnderTest;
using Errange.IntegrationTests.SystemUnderTest.Exceptions;
using Errange.IntegrationTests.SystemUnderTest.Extensions;
using Errange.IntegrationTests.SystemUnderTest.Services;
using Errange.IntegrationTests.SystemUnderTest.Validators;
using Errange.IntegrationTests.SystemUnderTest.ViewModels;
using Errange.ViewModels;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        string constantValue,
        string dataItemConstantMessage)
    {
        const string dataItemFromExceptionKey = "fromException",
                     dataItemFromRequestKey = "fromRequest",
                     requestParameterKey = "parameter",
                     dataItemConstantKey = "constant";

        // Arrange
        HttpClient client = new TestWebApiFactory(
                builder => builder.MapGet(TestWebApiFactory.DefaultEndpoint, _ => throw new CustomException(exceptionMessage)),
                options => options
                    .WithPolicy<CustomException>(policy => policy
                        .WithHttpStatusCode(httpStatusCode)
                        .WithCustomProblemCode(errorCode)
                        .WithDetail(errorDescription)
                        .WithDataItem(dataItemFromExceptionKey).WithValue(exception => exception.Message)
                        .ProblemPolicy.WithDataItem(dataItemFromRequestKey).WithValue(httpContext => httpContext.Request.Query[requestParameterKey].SingleOrDefault()).When(value => value == requestParameterValue)
                        .ProblemPolicy.WithDataItem(dataItemConstantKey).WithValue(constantValue).WithMessages(dataItemConstantMessage)))
            .CreateClient();

        // Act
        HttpResponseMessage response = await client.GetAsync(QueryHelpers.AddQueryString(TestWebApiFactory.DefaultEndpoint, requestParameterKey, requestParameterValue));

        // Assert
        ErrangeProblemDetails errangeProblemDetails = await response.MapToErrangeProblemDetailsIfStatusCodeMatches();

        errangeProblemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
        errangeProblemDetails.ProblemCode.Should().Be(errorCode);
        errangeProblemDetails.Detail.Should().Be(errorDescription);

        ProblemDataItem problemDataItemFromExceptionKey = errangeProblemDetails.GetDataItemVm<string>(dataItemFromExceptionKey);
        problemDataItemFromExceptionKey.Value.Should().Be(exceptionMessage);
        problemDataItemFromExceptionKey.Messages.Should().BeEmpty();

        ProblemDataItem problemDataItemFromRequestKey = errangeProblemDetails.GetDataItemVm<string>(dataItemFromRequestKey);
        problemDataItemFromRequestKey.Value.Should().Be(requestParameterValue);
        problemDataItemFromRequestKey.Messages.Should().BeEmpty();

        ProblemDataItem problemDataItemFromConstantKey = errangeProblemDetails.GetDataItemVm<string>(dataItemConstantKey);
        problemDataItemFromConstantKey.Value.Should().Be(constantValue);
        problemDataItemFromConstantKey.Messages.Should().ContainSingle(dataItemConstantMessage);
    }

    [TestCase]
    public async Task ErrorShouldBeReturnedWhenFluentValidationExceptionIsThrown()
    {
        // Arrange
        var testVM = new TestVM();
        HttpClient client = new TestWebApiFactory(
                app => app.MapPost(TestWebApiFactory.DefaultEndpoint, ([FromBody] TestVM testVM, [FromServices] IValidator<TestVM> validator) => { throw new ValidationException(validator.Validate(testVM).Errors); }),
                options => options.WithPolicy<ValidationException>(policy => policy
                    .WithHttpStatusCode(HttpStatusCode.BadRequest)
                    .WithDataItems(
                        (exception, _, _) => exception.Errors.GroupBy(validationFailure => validationFailure.PropertyName, validationFailure => validationFailure),
                        (validationFailures, _, _, _) => validationFailures.Key,
                        (validationFailures, _, _, _) => validationFailures.First().AttemptedValue,
                        (validationFailures, _, _, _) => validationFailures.Select(validationFailure => validationFailure.ErrorMessage))),
                services => services.AddScoped<IValidator<TestVM>, TestVmValidator>())
            .CreateClient();

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync(TestWebApiFactory.DefaultEndpoint, testVM);

        // Assert
        ErrangeProblemDetails errangeProblemDetails = await response.MapToErrangeProblemDetailsIfStatusCodeMatches();
        errangeProblemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);

        ProblemDataItem requiredPropertyProblemDataItem = errangeProblemDetails.GetDataItemVm(nameof(TestVM.RequiredProperty));
        requiredPropertyProblemDataItem.Value.Should().Be(testVM.RequiredProperty);
        requiredPropertyProblemDataItem.Messages.Should().HaveCount(expected: 1);

        ProblemDataItem requiredPropertyWithRangeLimitProblemDataItem = errangeProblemDetails.GetDataItemVm<long>(nameof(TestVM.RequiredPropertyWithRangeLimit));
        requiredPropertyWithRangeLimitProblemDataItem.Value.Should().Be(testVM.RequiredPropertyWithRangeLimit);
        requiredPropertyWithRangeLimitProblemDataItem.Messages.Should().HaveCount(expected: 2);
    }

    [TestCase]
    public async Task ErrorForParentExceptionShouldBeReturnedWhenDerivedExceptionIsThrown()
    {
        // Arrange
        HttpClient client = new TestWebApiFactory(
                builder => builder.MapGet(TestWebApiFactory.DefaultEndpoint, _ => throw new CustomDerivedException()),
                options => options.WithPolicy<CustomException>(policy => policy
                    .WithHttpStatusCode(HttpStatusCode.BadRequest)))
            .CreateClient();

        // Act
        HttpResponseMessage response = await client.GetAsync(TestWebApiFactory.DefaultEndpoint);

        // Assert
        ErrangeProblemDetails errangeProblemDetails = await response.MapToErrangeProblemDetailsIfStatusCodeMatches();
        errangeProblemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
    }

    [TestCase]
    public async Task ExceptionInfoShouldBeReturnedWhenEnvIsDevelopment()
    {
        // Arrange
        string exceptionMessage = "This is custom exception.";
        HttpClient client = new TestWebApiFactory(
                builder => builder.MapGet(TestWebApiFactory.DefaultEndpoint, _ => throw new CustomException(exceptionMessage)),
                options => options.WithPolicy<CustomException>(policy => policy
                    .WithHttpStatusCode(HttpStatusCode.BadRequest)
                    .WithDataItem("message").WithValue(exception => exception.Message).WithMessages("This is exception message.")),
                environmentName: Environments.Development)
            .CreateClient();

        // Act
        HttpResponseMessage response = await client.GetAsync(TestWebApiFactory.DefaultEndpoint);

        // Assert
        ErrangeProblemDetails errangeProblemDetails = await response.MapToErrangeProblemDetailsIfStatusCodeMatches();

        errangeProblemDetails.ExceptionInfo.Should().NotBeNull();
        errangeProblemDetails.ExceptionInfo!.Message.Should().Be(exceptionMessage);

        ProblemDataItem<string> messageProblemDataItem = errangeProblemDetails.GetDataItemVm<string>("message");
        messageProblemDataItem.Value.Should().Be(exceptionMessage);
        messageProblemDataItem.Messages.Should().HaveCount(expected: 1);
    }

    [Test]
    public async Task ExceptionInfoShouldNotBeReturnedWhenEnvIsNotDevelopment()
    {
        // Arrange
        HttpClient client = new TestWebApiFactory(
                builder => builder.MapGet(TestWebApiFactory.DefaultEndpoint, _ => throw new CustomException()),
                options => options.WithPolicy<CustomException>(policy => policy
                    .WithHttpStatusCode(HttpStatusCode.BadRequest)
                    .WithDataItem("message").WithValue(exception => exception.Message).WithMessages("This is exception message.")),
                environmentName: Environments.Production)
            .CreateClient();

        // Act
        HttpResponseMessage response = await client.GetAsync(TestWebApiFactory.DefaultEndpoint);

        // Assert
        ErrangeProblemDetails errangeProblemDetails = await response.MapToErrangeProblemDetailsIfStatusCodeMatches();

        errangeProblemDetails.ExceptionInfo.Should().BeNull();
    }

    [Test]
    public async Task DataItemShouldBeAddedWhenServiceForValueGenerationIsScoped()
    {
        // Arrange
        string testData = "Test data";
        HttpClient client = new TestWebApiFactory(
                builder => builder.MapGet(TestWebApiFactory.DefaultEndpoint, _ => throw new CustomException()),
                options => options.WithPolicy<CustomException>(policy => policy
                    .WithHttpStatusCode(HttpStatusCode.BadRequest)
                    .WithDataItem("testKey").WithValue<TestService, string>(testService => testService.Data)),
                services => services.AddScoped(_ => new TestService(testData)))
            .CreateClient();

        // Act
        HttpResponseMessage response = await client.GetAsync(TestWebApiFactory.DefaultEndpoint);

        // Assert
        ErrangeProblemDetails errangeProblemDetails = await response.MapToErrangeProblemDetailsIfStatusCodeMatches();

        ProblemDataItem<string> messageProblemDataItem = errangeProblemDetails.GetDataItemVm<string>("testKey");
        messageProblemDataItem.Value.Should().Be(testData);
    }

    [Test]
    public async Task DataItemShouldNotBeAddedWhenServiceForValueGenerationIsNotAvailable()
    {
        // Arrange
        HttpClient client = new TestWebApiFactory(
                builder => builder.MapGet(TestWebApiFactory.DefaultEndpoint, _ => throw new CustomException()),
                options => options.WithPolicy<CustomException>(policy => policy
                    .WithHttpStatusCode(HttpStatusCode.BadRequest)
                    .WithDataItem("testKey").WithValue<TestService, string>(testService => testService.Throw())))
            .CreateClient();

        // Act
        HttpResponseMessage response = await client.GetAsync(TestWebApiFactory.DefaultEndpoint);

        // Assert
        ErrangeProblemDetails errangeProblemDetails = await response.MapToErrangeProblemDetailsIfStatusCodeMatches();

        errangeProblemDetails.Data.Should().BeEmpty();
    }

    [Test]
    public async Task DataItemShouldNotBeAddedWhenServiceForValueGenerationThrows()
    {
        // Arrange
        HttpClient client = new TestWebApiFactory(
                builder => builder.MapGet(TestWebApiFactory.DefaultEndpoint, _ => throw new CustomException()),
                options => options.WithPolicy<CustomException>(policy => policy
                    .WithHttpStatusCode(HttpStatusCode.BadRequest)
                    .WithDataItem("testKey").WithValue<TestService, string>(testService => testService.Throw())),
                services => services.AddScoped<TestService>())
            .CreateClient();

        // Act
        HttpResponseMessage response = await client.GetAsync(TestWebApiFactory.DefaultEndpoint);

        // Assert
        ErrangeProblemDetails errangeProblemDetails = await response.MapToErrangeProblemDetailsIfStatusCodeMatches();

        errangeProblemDetails.Data.Should().BeEmpty();
    }
}