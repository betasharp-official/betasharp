using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace BetaSharp.Launcher.Features;

internal static class HttpClientExtensions
{
    extension(HttpClient client)
    {
        public async Task<T> GetAsync<T>(string url, JsonTypeInfo<T> typeInfo)
        {
            var response = await client.GetAsync(url);

            await using var stream = await response.Content.ReadAsStreamAsync();

            var instance = JsonSerializer.Deserialize(stream, typeInfo);

            ArgumentNullException.ThrowIfNull(instance);

            return instance;
        }

        public async Task<TResponse> PostAsync<TResponse, TRequest>(string url, TRequest request, JsonTypeInfo<TRequest> requestInfo, JsonTypeInfo<TResponse> responseInfo)
        {
            var response = await client.PostAsync(url, JsonContent.Create(request, requestInfo));

            await using var stream = await response.Content.ReadAsStreamAsync();

            var instance = JsonSerializer.Deserialize(stream, responseInfo);

            ArgumentNullException.ThrowIfNull(instance);

            return instance;
        }
    }
}
