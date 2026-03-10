using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace BetaSharp.Launcher.Features.Home;

internal sealed class ClientService(IHttpClientFactory clientFactory)
{
    private const string Base = "https://launcher.mojang.com/v1/objects/43db9b498cb67058d2e12d394e6507722e71bb45/client.jar";

    private readonly byte[] _expectedHash = [175, 31, 160, 75, 128, 6, 211, 239, 120, 199, 226, 79, 141, 228, 170, 86, 244, 57, 167, 77, 127, 49, 72, 39, 82, 144, 98, 213, 186, 182, 219, 76];

    public async Task DownloadAsync(string directory)
    {
        using var sha256 = SHA256.Create();

        await using var file = File.Open(Path.Combine(directory, "b1.7.3.jar"), FileMode.OpenOrCreate);

        byte[] hash = await sha256.ComputeHashAsync(file);

        if (hash.SequenceEqual(_expectedHash))
        {
            return;
        }

        var client = clientFactory.CreateClient();

        await using var stream = await client.GetStreamAsync(Base);
        await stream.CopyToAsync(file);
    }
}
