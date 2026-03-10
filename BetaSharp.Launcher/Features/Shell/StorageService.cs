using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Launcher.Features.Shell;

internal sealed class StorageService(ILogger<StorageService> logger)
{
    public async Task<T?> GetAsync<T>(JsonTypeInfo<T> typeInfo) where T : class
    {
        string path = $"{Path.Combine(App.Folder, typeInfo.Type.Name.ToLowerInvariant())}.json";

        try
        {
            await using var stream = File.Open(path, FileMode.OpenOrCreate);
            return await JsonSerializer.DeserializeAsync(stream, typeInfo);
        }
        catch (JsonException)
        {
            logger.LogWarning("Failed to read {Path}", path);
            return null;
        }
    }

    public async Task SetAsync<T>(T instance, JsonTypeInfo<T> typeInfo) where T : class
    {
        await using var stream = File.OpenWrite($"{Path.Combine(App.Folder, typeInfo.Type.Name.ToLowerInvariant())}.json");
        await JsonSerializer.SerializeAsync(stream, instance, typeInfo);
    }

    public void Delete(string name)
    {
        File.Delete($"{Path.Combine(App.Folder, name.ToLowerInvariant())}.json");
    }
}
