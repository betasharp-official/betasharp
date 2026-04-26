using System.Collections.Concurrent;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BetaSharp.Client.Rendering.Core.Textures;

public sealed class SkinManager : IDisposable
{
    private const string SkinCacheDirectoryName = "SkinCache";
    private const int SkinCacheSize = 128;
    private const int SkinCacheValidForDays = 14;

    private const string CapeCacheDirectoryName = "CapeCache";
    private const string CapeTextureKeySuffix = "#cape";

    private readonly ConcurrentDictionary<string, Image<Rgba32>> _downloadedImages = new();
    private readonly ConcurrentDictionary<string, bool> _downloading = new();
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger = Log.Instance.For<SkinManager>();
    private readonly ConcurrentDictionary<string, TextureHandle> _textureHandles = new();
    private readonly TextureManager _textureManager;

    private int _cacheSize = 0;

    public SkinManager(TextureManager textureManager)
    {
        _textureManager = textureManager;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        _httpClient.DefaultRequestHeaders.Add("User-Agent", nameof(BetaSharp));

        InvalidateCache();
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

    private void InvalidateCache()
    {
        string path = Path.Combine(Path.GetTempPath(), "BetaSharp", SkinCacheDirectoryName);
        if (!Directory.Exists(path)) return;

        DateTime cacheAgeLimit = DateTime.Now.AddDays(-SkinCacheValidForDays);
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
                _logger.LogWarning(ex, "Failed to delete old cached skin for {name}", Path.GetFileNameWithoutExtension(file));
            }
        }

        _cacheSize = Directory.GetFiles(path, "*.png").Length;
    }

    public void RequestDownload(string? username, bool cache = true)
    {
        if (string.IsNullOrWhiteSpace(username) || _textureHandles.ContainsKey(username)
                                                || _downloadedImages.ContainsKey(username)
                                                || !_downloading.TryAdd(username, true))
        {
            return;
        }

        _ = DownloadTask(username, cache);
    }

    private async Task DownloadTask(string username, bool cache)
    {
        try
        {
            bool skinLoadedFromCache = await TryLoadTextureFromCache(username, username + ".png");
            bool capeLoadedFromCache = await TryLoadCapeId(username);

            if (skinLoadedFromCache && capeLoadedFromCache)
            {
                _logger.LogInformation("Skin and cape loaded from cache for {Name}", username);
                return;
            }

            if (skinLoadedFromCache)
            {
                _logger.LogInformation("Skin loaded from cache for {Name}", username);
                _logger.LogInformation("Downloading cape for {Name}", username);
            }
            else if (capeLoadedFromCache)
            {
                _logger.LogInformation("Cape loaded from cache for {Name}", username);
                _logger.LogInformation("Downloading skin for {Name}", username);
            }
            else
            {
                _logger.LogInformation("Downloading skin and cape for {Name}", username);
            }


            string? id = await GetProfileIdFromName(username);
            if (id == null) throw new ProfileException();

            string? value = await GetProfilePropertiesFromId(id);
            ArgumentException.ThrowIfNullOrWhiteSpace(value);

            JsonNode? node = JsonNode.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(value)));
            string? texture = node?["textures"]?["SKIN"]?["url"]?.GetValue<string>();
            string? capeTexture = node?["textures"]?["CAPE"]?["url"]?.GetValue<string>();

            ArgumentException.ThrowIfNullOrWhiteSpace(texture);

            bool isCacheFull = _cacheSize >= SkinCacheSize;

            if (!skinLoadedFromCache)
            {
                await using Stream textureStream = await _httpClient.GetStreamAsync(texture);

                Image<Rgba32> image = await Image.LoadAsync<Rgba32>(textureStream);

                if (image is { Height: 64, Width: 64 })
                {
                    image.Mutate(ctx => ctx.Crop(64, 32));
                }

                _downloadedImages[username] = image;

                _logger.LogInformation("Skin downloaded successfully for {Name}: ({W}x{H})", username, image.Width, image.Height);

                if (cache && !isCacheFull)
                {
                    await SaveTextureToCache(username + ".png", image, SkinCacheDirectoryName);
                    _cacheSize++;
                    _logger.LogInformation("Skin saved in cache for {Name}", username);

                    if (!string.IsNullOrWhiteSpace(capeTexture))
                    {
                        await SaveTxtCache(username + ".txt", GetIdFromUrl(capeTexture), SkinCacheDirectoryName);
                    }
                    else
                    {
                        // save empty file so we know not to try to get cape next time.
                        await SaveTxtCache(username + ".txt", "", SkinCacheDirectoryName);
                    }

                    _logger.LogInformation("Cape id for {Name} saved in cache", username);
                }
            }

            if (!capeLoadedFromCache)
            {
                if (!string.IsNullOrWhiteSpace(capeTexture))
                {
                    string capeId = GetIdFromUrl(capeTexture);
                    await DownloadCapeFromUrl(capeTexture, capeId, username, cache);
                    if (cache && !isCacheFull) await SaveTxtCache(username + ".txt", capeId, SkinCacheDirectoryName);
                }
                else if (cache && !isCacheFull)
                {
                    await SaveTxtCache(username + ".txt", "", SkinCacheDirectoryName);
                    _logger.LogInformation("Cape id for {Name} saved in cache", username);
                }
            }
        }
        catch (ProfileException)
        {
            _logger.LogWarning("Failed to download skin for {Name}{br}Profile not found.", username, Environment.NewLine);
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

    private static string GetCapeTextureKey(string username) => username + CapeTextureKeySuffix;

    private async Task<bool> TryLoadTextureFromCache(string textureKey, string cacheFileName, string dir = SkinCacheDirectoryName)
    {
        string skinCachePath = Path.Combine(Path.GetTempPath(), "BetaSharp", dir, cacheFileName);
        if (!File.Exists(skinCachePath)) return false;

        Image<Rgba32> cachedImage = await Image.LoadAsync<Rgba32>(skinCachePath);
        _downloadedImages[textureKey] = cachedImage;
        return true;
    }

    private async Task<bool> TryLoadCapeId(string username)
    {
        string skinCachePath = Path.Combine(Path.GetTempPath(), "BetaSharp", SkinCacheDirectoryName, username + ".txt");
        if (!File.Exists(skinCachePath)) return false;

        string id = Encoding.ASCII.GetString(await File.ReadAllBytesAsync(skinCachePath));

        // player have no cape equipped.
        if (string.IsNullOrEmpty(id)) return true;

        if (!await TryLoadTextureFromCache(id, id + ".png", CapeCacheDirectoryName))
        {
            await DownloadCapeFromId(id);
        }

        _downloadedImages[GetCapeTextureKey(username)] = _downloadedImages[id];

        return true;
    }

    private Task DownloadCapeFromId(string id) =>
        DownloadCapeFromUrl("http://textures.minecraft.net/texture/" + id, id);

    private string GetIdFromUrl(string str) => str.Substring(str.LastIndexOf('/') + 1);

    private async Task DownloadCapeFromUrl(string url, string id, string? username = null, bool cache = true)
    {
        await using Stream capeTextureStream = await _httpClient.GetStreamAsync(url);

        Image<Rgba32> capeImage = await Image.LoadAsync<Rgba32>(capeTextureStream);
        _downloadedImages[id] = capeImage;

        if (!string.IsNullOrEmpty(username))
        {
            _downloadedImages[GetCapeTextureKey(username)] = capeImage;
            _logger.LogInformation("Cape downloaded successfully for {Name}: ({W}x{H})", username, capeImage.Width, capeImage.Height);
        }
        else
        {
            _logger.LogInformation("Cape downloaded successfully {ID}: ({W}x{H})", id, capeImage.Width, capeImage.Height);
        }


        if (cache)
        {
            await SaveTextureToCache(id + ".png", capeImage, CapeCacheDirectoryName);

            if (!string.IsNullOrEmpty(username))
                _logger.LogInformation("Cape saved in cache for {Name}", username);
            else
                _logger.LogInformation("Cape saved in cache for id {ID}", id);
        }
    }

    private async Task SaveTextureToCache(string cacheFileName, Image image, string directoryName)
    {
        string path = Path.Combine(Path.GetTempPath(), "BetaSharp", directoryName);
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        path = Path.Combine(path, cacheFileName);

        await using FileStream cacheStream = new(path, FileMode.Create, FileAccess.Write);
        await image.SaveAsPngAsync(cacheStream);
        cacheStream.Close();
    }

    private async Task SaveTxtCache(string filename, string text, string directoryName)
    {
        string path = Path.Combine(Path.GetTempPath(), "BetaSharp", directoryName);
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        path = Path.Combine(path, filename);

        await using FileStream cacheStream = new(path, FileMode.Create, FileAccess.Write);
        await cacheStream.WriteAsync(Encoding.ASCII.GetBytes(text));
        cacheStream.Close();
    }

    private async Task<string?> GetProfileIdFromName(string username)
    {
        HttpResponseMessage profileResponse = await _httpClient.GetAsync($"https://api.mojang.com/minecraft/profile/lookup/name/{username}");
        await using Stream profileStream = await profileResponse.Content.ReadAsStreamAsync();
        JsonNode? profileNode = await JsonNode.ParseAsync(profileStream);

        return profileNode?["id"]?.GetValue<string>();
    }

    private async Task<string?> GetProfilePropertiesFromId(string id)
    {
        HttpResponseMessage skinResponse = await _httpClient.GetAsync($"https://sessionserver.mojang.com/session/minecraft/profile/{id}");
        await using Stream skinStream = await skinResponse.Content.ReadAsStreamAsync();
        JsonNode? skinNode = await JsonNode.ParseAsync(skinStream);

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
        if (string.IsNullOrWhiteSpace(url)) return;
        if (_downloadedImages.TryRemove(url, out Image<Rgba32>? image)) image.Dispose();
        if (_textureHandles.TryRemove(url, out TextureHandle? handle)) _textureManager.Delete(handle);
    }

    private class ProfileException : Exception;
}
