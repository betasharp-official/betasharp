using System.Net;
using System.Xml;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Client.Resource;

public class BetaResourceDownloader : IResourceLoader, IDisposable
{
    private const string RESOURCE_URL = "http://s3.amazonaws.com/MinecraftResources/";
    private const string BETACRAFT_PROXY_HOST = "betacraft.uk";
    private const int BETACRAFT_PROXY_PORT = 11705;

    private readonly ILogger<BetaResourceDownloader> _logger = Log.Instance.For<BetaResourceDownloader>();
    private readonly HttpClient _httpClient;
    private readonly string _resourcesDirectory;
    private readonly BetaSharp _game;
    private bool _cancelled;

    public BetaResourceDownloader(BetaSharp game, string baseDirectory)
    {
        _game = game;
        _resourcesDirectory = System.IO.Path.Combine(baseDirectory, "resources");
        Directory.CreateDirectory(_resourcesDirectory);

        var handler = new HttpClientHandler
        {
            Proxy = new WebProxy(BETACRAFT_PROXY_HOST, BETACRAFT_PROXY_PORT),
            UseProxy = true
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMinutes(10)
        };
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

            HttpResponseMessage response = await _httpClient.GetAsync(RESOURCE_URL);
            response.EnsureSuccessStatusCode();

            string xmlContent = await response.Content.ReadAsStringAsync();

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

            bool isSoundFile = category == "sound" || category == "newsound";

            if (isSoundFile && pass != 0) return;
            if (!isSoundFile && pass != 1) return;

            var localFile = new FileInfo(System.IO.Path.Combine(_resourcesDirectory, path));

            if (localFile.Exists && localFile.Length == size)
            {
                _game.installResource(path, new FileInfo(localFile.FullName));
                return;
            }

            localFile.Directory?.Create();

            string urlPath = path.Replace(" ", "%20");
            string fullUrl = RESOURCE_URL + urlPath;

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

    public void Cancel()
    {
        _cancelled = true;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _httpClient?.Dispose();
    }

    private class ResourceEntry
    {
        public string Key { get; set; }
        public long Size { get; set; }
    }
}
