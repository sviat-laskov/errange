using System.Text.Json;
using Errange.ViewModels;
using FluentAssertions;

namespace Errange.IntegrationTests.SystemUnderTest.Extensions;

public static class ErrangeProblemDetailsExtensions
{
    public static ProblemDataItem GetDataItemVm(this ErrangeProblemDetails errangeProblemDetails, string key) => errangeProblemDetails.Data
        .Should().ContainKey(key).WhoseValue
        .Should().BeAssignableTo<ProblemDataItem>().Which;

    public static ProblemDataItem<TValue> GetDataItemVm<TValue>(this ErrangeProblemDetails errangeProblemDetails, string key)
    {
        ProblemDataItem problemDataItem = errangeProblemDetails.GetDataItemVm(key);

        return new ProblemDataItem<TValue>
        {
            //Key = problemDataItem.Key,
            Value = problemDataItem.Value.As<JsonElement>().Deserialize<TValue>(),
            Messages = problemDataItem.Messages
        };
    }
}