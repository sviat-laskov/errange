using System.Net.Http.Formatting;
using System.Net.Http.Headers;

namespace Errange.IntegrationTests.SystemUnderTest.Factories;

public static class MediaTypeFormatterCollectionFactory
{
    public static MediaTypeFormatterCollection WithProblemJsonMediaTypeFormatter { get; }

    static MediaTypeFormatterCollectionFactory()
    {
        var jsonMediaTypeFormatter = new JsonMediaTypeFormatter();
        jsonMediaTypeFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/problem+json"));
        WithProblemJsonMediaTypeFormatter = new MediaTypeFormatterCollection { jsonMediaTypeFormatter };
    }
}