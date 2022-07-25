using System.Text.Json;

namespace Errange.IntegrationTests.SystemUnderTest.Extensions;

public static class HttpContentExtensions
{
    public static async Task<T> ReadAsAsync<T>(this HttpContent content, JsonSerializerOptions? options = null)
    {
        await using Stream contentStream = await content.ReadAsStreamAsync();
        return (await JsonSerializer.DeserializeAsync<T>(contentStream, options))!;
    }
}