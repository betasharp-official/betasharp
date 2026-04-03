using System.Net.Http;
using System.Threading.Tasks;

namespace BetaSharp.Launcher.Features.Home.GitHub;

internal sealed class GitHubClient(IHttpClientFactory clientFactory)
{
    public async Task<ReleasesResponse[]> GetReleasesAsync(string owner, string repository)
    {
        var client = clientFactory.CreateClient(nameof(GitHubClient));

        // Use named client instead of this?
        client.DefaultRequestHeaders.Add("User-Agent", nameof(BetaSharp));

        return await client.GetAsync($"https://api.github.com/repos/{owner}/{repository}/releases", GitHubSerializerContext.Default.ReleasesResponseArray);
    }
}
