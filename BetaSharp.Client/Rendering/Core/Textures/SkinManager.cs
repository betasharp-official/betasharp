using System.Collections.Concurrent;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BetaSharp.Client.Rendering.Core.Textures;

public sealed class SkinManager : ISkinManager
{
    private readonly ILogger _logger = Log.Instance.For<SkinManager>();
    private readonly ITextureManager _textureManager;
    private readonly HttpClient _httpClient;

    private readonly ConcurrentDictionary<string, Image<Rgba32>> _downloadedImages = new();
    private readonly ConcurrentDictionary<string, TextureHandle> _textureHandles = new();
    private readonly ConcurrentDictionary<string, bool> _downloading = new();

    private const string SkinCacheDirectoryName = "SkinCache";
    private const int CacheValidForDays = 14;

    public SkinManager(ITextureManager textureManager)
    {
        _textureManager = textureManager;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        _httpClient.DefaultRequestHeaders.Add("User-Agent", nameof(BetaSharp));

        InvalidateCache();
    }

    private void InvalidateCache()
    {
        string path = Path.Combine(Path.GetTempPath(), "BetaSharp", SkinCacheDirectoryName);
        if (!Directory.Exists(path)) return;

        DateTime cacheAgeLimit = DateTime.Now.AddDays(-CacheValidForDays);
        foreach (string file in Directory.GetFiles(path))
        {
            if (File.GetCreationTime(file) >= cacheAgeLimit) continue;

            try
            {
                File.Delete(file);
                _logger.LogInformation("Deleted old cached skin for {name}", Path.GetFileNameWithoutExtension(file));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete old cached skin for {name}",
                    Path.GetFileNameWithoutExtension(file));
            }
        }
    }

    public void RequestDownload(string? username)
    {
        RequestDownload(username, cache: false);
    }

    public void RequestDownload(string? username, bool cache)
    {
        if (string.IsNullOrWhiteSpace(username) || _textureHandles.ContainsKey(username)
                                                || _downloadedImages.ContainsKey(username)
                                                || !_downloading.TryAdd(username, true))
        {
            return;
        }

        _ = DownloadTask(username, cache);
    }

    private async Task DownloadTask(string username, bool cache = false)
    {
        try
        {
            if (await TryLoadSkinFromCache(username))
            {
                _logger.LogInformation("Skin loaded from cache for {Name}", username);
                return;
            }

            _logger.LogInformation("Downloading skin for {Url}", username);

            string? id = await GetProfileIdFromName(username);
            if (id == null) throw new ProfileException();

            string? value = await GetProfilePropertiesFromId(id);
            ArgumentException.ThrowIfNullOrWhiteSpace(value);

            var node = JsonNode.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(value)));
            string? texture = node?["textures"]?["SKIN"]?["url"]?.GetValue<string>();

            ArgumentException.ThrowIfNullOrWhiteSpace(texture);

            await using var textureStream = await _httpClient.GetStreamAsync(texture);

            var image = await Image.LoadAsync<Rgba32>(textureStream);

            if (image is { Height: 64, Width: 64 })
            {
                image.Mutate(ctx => ctx.Crop(64, 32));
            }

            _downloadedImages[username] = image;

            _logger.LogInformation("Skin downloaded successfully for {Name}: ({W}x{H})", username, image.Width,
                image.Height);

            if (cache)
            {
                await SaveSkinToCache(username, image);
                _logger.LogInformation("Skin saved in cache for {Name}", username);
            }
        }
        catch (ProfileException)
        {
            _logger.LogWarning("Failed to download skin for {Name}{br}Profile not found.", username,
                Environment.NewLine);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to download skin for {Name}", username);
        }
        finally
        {
            _downloading.TryRemove(username, out _);
        }
    }


    private class ProfileException : Exception;

    private async Task<bool> TryLoadSkinFromCache(string username)
    {
        string skinCachePath = Path.Combine(Path.GetTempPath(), "BetaSharp", SkinCacheDirectoryName, username + ".png");
        if (File.Exists(skinCachePath))
        {
            Image<Rgba32> cachedImage = await Image.LoadAsync<Rgba32>(skinCachePath);
            _downloadedImages[username] = cachedImage;

            return true;
        }

        return false;
    }

    private async Task SaveSkinToCache(string username, Image image)
    {
        string path = Path.Combine(Path.GetTempPath(), "BetaSharp", SkinCacheDirectoryName);
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        path = Path.Combine(path, username + ".png");

        await using var cacheStream = new FileStream(path, FileMode.Create, FileAccess.Write);
        await image.SaveAsPngAsync(cacheStream);
        cacheStream.Close();
    }

    private async Task<string?> GetProfileIdFromName(string username)
    {
        var profileResponse =
            await _httpClient.GetAsync($"https://api.mojang.com/minecraft/profile/lookup/name/{username}");
        await using var profileStream = await profileResponse.Content.ReadAsStreamAsync();
        var profileNode = await JsonNode.ParseAsync(profileStream);

        return profileNode?["id"]?.GetValue<string>();
    }

    private async Task<string?> GetProfilePropertiesFromId(string id)
    {
        var skinResponse =
            await _httpClient.GetAsync($"https://sessionserver.mojang.com/session/minecraft/profile/{id}");
        await using var skinStream = await skinResponse.Content.ReadAsStreamAsync();
        var skinNode = await JsonNode.ParseAsync(skinStream);

        return skinNode?["properties"]?[0]?["value"]?.GetValue<string>();
    }

    public TextureHandle? GetTextureHandle(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        if (_textureHandles.TryGetValue(url, out TextureHandle? handle))
        {
            return handle;
        }

        if (!_downloadedImages.TryRemove(url, out Image<Rgba32>? image))
        {
            return null;
        }

        handle = _textureManager.Load(image);

        _textureHandles[url] = handle;
        _logger.LogInformation("Skin texture created for {Url}", url);

        return handle;
    }

    public void Release(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        if (_downloadedImages.TryRemove(url, out Image<Rgba32>? image))
        {
            image.Dispose();
        }

        if (_textureHandles.TryRemove(url, out TextureHandle? handle))
        {
            _textureManager.Delete(handle);
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();

        foreach (Image<Rgba32> image in _downloadedImages.Values)
        {
            image.Dispose();
        }

        _downloadedImages.Clear();

        foreach (TextureHandle handle in _textureHandles.Values)
        {
            _textureManager.Delete(handle);
        }

        _textureHandles.Clear();
    }
}
