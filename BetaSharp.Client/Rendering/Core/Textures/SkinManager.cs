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
    private readonly ILogger _logger = Log.Instance.For<SkinManager>();
    private readonly TextureManager _textureManager;
    private readonly HttpClient _httpClient;

    private readonly ConcurrentDictionary<string, Image<Rgba32>> _downloadedImages = new();
    private readonly ConcurrentDictionary<string, TextureHandle> _textureHandles = new();
    private readonly ConcurrentDictionary<string, bool> _downloading = new();

    public SkinManager(TextureManager textureManager)
    {
        _textureManager = textureManager;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        _httpClient.DefaultRequestHeaders.Add("User-Agent", nameof(BetaSharp));
    }

    public void RequestDownload(string? username)
    {
        if (string.IsNullOrWhiteSpace(username) || _textureHandles.ContainsKey(username)
                                                || _downloadedImages.ContainsKey(username)
                                                || !_downloading.TryAdd(username, true))
        {
            return;
        }

        Task.Run(async () =>
        {
            try
            {
                _logger.LogInformation("Downloading skin for {Url}", username);

                var profileResponse = await _httpClient.GetAsync($"https://api.mojang.com/minecraft/profile/lookup/name/{username}");
                await using var profileStream = await profileResponse.Content.ReadAsStreamAsync();
                var profileNode = await JsonNode.ParseAsync(profileStream);

                string? id = profileNode?["id"]?.GetValue<string>();

                ArgumentException.ThrowIfNullOrWhiteSpace(id);

                var skinResponse = await _httpClient.GetAsync($"https://sessionserver.mojang.com/session/minecraft/profile/{id}");
                await using var skinStream = await skinResponse.Content.ReadAsStreamAsync();
                var skinNode = await JsonNode.ParseAsync(skinStream);

                string? value = skinNode?["properties"]?[0]?["value"]?.GetValue<string>();

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

                _logger.LogInformation("Skin downloaded successfully for {Name}: ({W}x{H})", username, image.Width, image.Height);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to download skin for {Name}", username);
            }
            finally
            {
                _downloading.TryRemove(username, out _);
            }
        });
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
