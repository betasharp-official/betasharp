using System.Net;
using System.Xml;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Client.Resource;

public class BetaResourceDownloader : IResourceLoader, IDisposable
{
    // Mojang's old Beta/Classic resource bucket. HTTPS works more reliably on modern networks.
    private const string RESOURCE_URL = "https://s3.amazonaws.com/MinecraftResources/";
    private const string BETACRAFT_PROXY_HOST = "betacraft.uk";
    private const int BETACRAFT_PROXY_PORT = 11705;

    private readonly ILogger<BetaResourceDownloader> _logger = Log.Instance.For<BetaResourceDownloader>();
    private readonly HttpClient _directHttpClient;
    private readonly HttpClient _proxyHttpClient;
    private readonly string _resourcesDirectory;
    private readonly BetaSharp _game;
    private bool _cancelled;

    public BetaResourceDownloader(BetaSharp game, string baseDirectory)
    {
        _game = game;
        _resourcesDirectory = System.IO.Path.Combine(baseDirectory, "resources");
        Directory.CreateDirectory(_resourcesDirectory);

        _directHttpClient = new HttpClient(new HttpClientHandler
        {
            UseProxy = false,
            AutomaticDecompression = DecompressionMethods.All
        })
        { Timeout = TimeSpan.FromMinutes(10) };

        _proxyHttpClient = new HttpClient(new HttpClientHandler
        {
            Proxy = new WebProxy(BETACRAFT_PROXY_HOST, BETACRAFT_PROXY_PORT),
            UseProxy = true,
            AutomaticDecompression = DecompressionMethods.All
        })
        { Timeout = TimeSpan.FromMinutes(10) };
    }

    private bool DoManifestStuff(string manifestFilePath)
    {
        if (File.Exists(manifestFilePath))
        {
            string[] lines = File.ReadAllLines(manifestFilePath);
            int loaded = 0;

            foreach (string line in lines)
            {
                string localFile = System.IO.Path.Combine(_resourcesDirectory, line);
                if (File.Exists(localFile))
                {
                    loaded++;
                    _game.installResource(line, new FileInfo(localFile));
                }
            }

            if (lines.Length == loaded)
            {
                _logger.LogInformation($"{loaded} resources");
                return true;
            }
            else
            {
                _logger.LogError($"resource count mismatch, expected {lines.Length}, loaded {loaded}");
            }
        }

        return false;
    }

    public async Task LoadAsync()
    {
        string manifestFilePath = System.IO.Path.Combine(_resourcesDirectory, "resourceManifest.txt");

        if (DoManifestStuff(manifestFilePath))
        {
            return;
        }

        try
        {
            _logger.LogInformation("Fetching resource list...");

            string xmlContent = await GetStringWithFallbackAsync(RESOURCE_URL);

            List<ResourceEntry> resources = ParseResourceXml(xmlContent);

            List<string> resourceFileNames = [];

            foreach (ResourceEntry resource in resources)
            {
                resourceFileNames.Add(resource.Key);
            }

            File.WriteAllLines(manifestFilePath, resourceFileNames);

            _logger.LogInformation($"Found {resources.Count} resources to download");

            for (int pass = 0; pass < 2; pass++)
            {
                foreach (ResourceEntry resource in resources)
                {
                    if (_cancelled) return;

                    await LoadFromUrl(resource.Key, resource.Size, pass);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error downloading resources: {ex.Message}");
        }
    }

    private static List<ResourceEntry> ParseResourceXml(string xmlContent)
    {
        var resources = new List<ResourceEntry>();
        var doc = new XmlDocument();
        doc.LoadXml(xmlContent);

        XmlNodeList contents = doc.GetElementsByTagName("Contents");

        foreach (XmlNode node in contents)
        {
            if (node.NodeType == XmlNodeType.Element)
            {
                var element = (XmlElement)node;

                XmlNode? keyNode = element.GetElementsByTagName("Key")[0];
                XmlNode? sizeNode = element.GetElementsByTagName("Size")[0];

                string key = keyNode!.InnerText;
                long size = long.Parse(sizeNode!.InnerText);

                if (size > 0)
                {
                    resources.Add(new ResourceEntry { Key = key, Size = size });
                }
            }
        }

        return resources;
    }

    private async Task LoadFromUrl(string path, long size, int pass)
    {
        try
        {
            int slashIndex = path.IndexOf('/');
            if (slashIndex < 0) return;

            string category = path.Substring(0, slashIndex);

            bool isSoundFile = category.StartsWith("sound", StringComparison.OrdinalIgnoreCase);

            if (isSoundFile && pass != 0) return;
            if (!isSoundFile && pass != 1) return;

            var localFile = new FileInfo(System.IO.Path.Combine(_resourcesDirectory, path));

            if (localFile.Exists && localFile.Length == size)
            {
                _game.installResource(path, new FileInfo(localFile.FullName));
                return;
            }

            localFile.Directory?.Create();

            // Escape each segment to avoid breaking URLs that contain spaces or special chars.
            string fullUrl = CombineUrl(RESOURCE_URL, path);

            await DownloadFile(fullUrl, localFile.FullName);

            if (!_cancelled)
            {
                _game.installResource(path, new FileInfo(localFile.FullName));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to download {path}: {ex.Message}");
        }
    }

    private async Task DownloadFile(string url, string destinationPath)
    {
        using HttpResponseMessage response = await GetResponseWithFallbackAsync(url, HttpCompletionOption.ResponseHeadersRead);

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

    public void Cancel()
    {
        _cancelled = true;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _directHttpClient?.Dispose();
        _proxyHttpClient?.Dispose();
    }

    private async Task<string> GetStringWithFallbackAsync(string url)
    {
        using HttpResponseMessage response = await GetResponseWithFallbackAsync(url, HttpCompletionOption.ResponseContentRead);
        return await response.Content.ReadAsStringAsync();
    }

    private async Task<HttpResponseMessage> GetResponseWithFallbackAsync(string url, HttpCompletionOption option)
    {
        // Prefer betacraft proxy by default (most reliable for legacy resources).
        try
        {
            HttpResponseMessage proxied = await _proxyHttpClient.GetAsync(url, option);
            proxied.EnsureSuccessStatusCode();
            return proxied;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Proxy fetch failed, trying direct: {ex.Message}");
        }

        HttpResponseMessage direct = await _directHttpClient.GetAsync(url, option);
        direct.EnsureSuccessStatusCode();
        return direct;
    }

    private static string CombineUrl(string baseUrl, string relativePath)
    {
        // baseUrl is expected to end with '/'
        string trimmed = relativePath.TrimStart('/');
        string[] parts = trimmed.Split('/', StringSplitOptions.RemoveEmptyEntries);
        string escaped = string.Join("/", parts.Select(Uri.EscapeDataString));
        return baseUrl + escaped;
    }

    private class ResourceEntry
    {
        public string Key { get; set; }
        public long Size { get; set; }
    }
}
