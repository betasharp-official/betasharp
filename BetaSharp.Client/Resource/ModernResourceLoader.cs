using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Client.Resource;

public class ModernAssetDownloader : IResourceLoader, IDisposable
{
    private const string ASSET_INDEX_URL = "https://piston-meta.mojang.com/v1/packages/fb03309bb7711f3f9b5d44eea061e27222edc2d2/26.json";
    private const string ASSET_BASE_URL = "https://resources.download.minecraft.net/";
    private const string OUTPUT_FOLDER = "custom";

    private readonly ILogger<ModernAssetDownloader> _logger = Log.Instance.For<ModernAssetDownloader>();
    private readonly HttpClient _httpClient;
    private readonly string _resourcesDirectory;
    private readonly BetaSharp _game;
    private bool _cancelled;
    private readonly IEnumerable<string> _wantedAssets;
    private static readonly Dictionary<string, string> ExtensionToFolder = new()
    {
        { ".ogg", "music" }
    };

    public ModernAssetDownloader(BetaSharp game, string baseDirectory, IEnumerable<string> wantedAssets)
    {
        _wantedAssets = wantedAssets;
        _game = game;
        _resourcesDirectory = System.IO.Path.Combine(baseDirectory, "resources");
        _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
    }

    public async Task LoadAsync() => await DownloadAssetsAsync(_wantedAssets);

    public async Task DownloadAssetsAsync(IEnumerable<string> wantedAssets)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(ASSET_INDEX_URL);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();
            Dictionary<string, AssetEntry> index = ParseAssetIndex(json);

            foreach (string assetName in wantedAssets)
            {
                if (_cancelled) return;

                if (!index.TryGetValue(assetName, out AssetEntry? entry))
                {
                    continue;
                }

                string fileName = System.IO.Path.GetFileName(assetName);
                string extension = System.IO.Path.GetExtension(assetName).ToLowerInvariant();

                if (!ExtensionToFolder.TryGetValue(extension, out string? subFolder))
                {
                    _logger.LogError($"No folder mapping for extension {extension}, skipping {fileName}");
                    continue;
                }

                string outputKey = System.IO.Path.Combine(OUTPUT_FOLDER, subFolder, fileName).Replace('\\', '/');
                var localFile = new FileInfo(System.IO.Path.Combine(_resourcesDirectory, OUTPUT_FOLDER, subFolder, fileName));

                if (localFile.Exists && localFile.Length == entry.Size)
                {
                    _game.installResource(outputKey, localFile);
                    continue;
                }

                localFile.Directory?.Create();

                string hash = entry.Hash;
                string url = ASSET_BASE_URL + hash[..2] + "/" + hash;

                await DownloadFile(url, localFile.FullName);

                if (!_cancelled)
                {
                    _game.installResource(outputKey, localFile);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error downloading modern assets: {ex}");
        }
    }

    private static Dictionary<string, AssetEntry> ParseAssetIndex(string json)
    {
        var result = new Dictionary<string, AssetEntry>();
        using JsonDocument doc = JsonDocument.Parse(json);

        JsonElement objects = doc.RootElement.GetProperty("objects");

        foreach (JsonProperty prop in objects.EnumerateObject())
        {
            string hash = prop.Value.GetProperty("hash").GetString()!;
            long size = prop.Value.GetProperty("size").GetInt64();
            result[prop.Name] = new AssetEntry { Hash = hash, Size = size };
        }

        return result;
    }

    private async Task DownloadFile(string url, string destinationPath)
    {
        HttpResponseMessage response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        using Stream stream = await response.Content.ReadAsStreamAsync();
        using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
        byte[] buffer = new byte[4096];
        int bytesRead;

        while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
        {
            if (_cancelled) return;
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
        }
    }

    public void Cancel() => _cancelled = true;

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _httpClient?.Dispose();
    }

    private class AssetEntry
    {
        public string Hash { get; set; } = "";
        public long Size { get; set; }
    }
}
