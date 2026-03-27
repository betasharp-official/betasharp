using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace BetaSharp.Launcher.Features.Authentication;

internal sealed class AuthenticationService(ILogger<AuthenticationService> logger)
{
    private readonly IPublicClientApplication _application = PublicClientApplicationBuilder
        .Create("C36A9FB6-4F2A-41FF-90BD-AE7CC92031EB") // Probably not the best idea to use Prism's ID?
        .WithAuthority("https://login.microsoftonline.com/consumers")
        .WithRedirectUri("http://localhost")
        .Build();

    private readonly SystemWebViewOptions _webViewOptions = new()
    {
        BrowserRedirectSuccess = new Uri("https://betasharp.net/successful-login")
    };

    private readonly string[] _scopes = ["XboxLive.signin offline_access"];

    public async Task InitializeAsync()
    {
        logger.LogInformation("Initializing authentication service");

        string path = Path.Combine(App.Folder, "betasharp.launcher.cache");

        var properties = new StorageCreationPropertiesBuilder(Path.GetFileName(path), Path.GetDirectoryName(path))
            .WithLinuxKeyring(
                "betasharp.launcher",
                MsalCacheHelper.LinuxKeyRingDefaultCollection,
                "MSAL cache for BetaSharp's launcher",
                new KeyValuePair<string, string>("Version", "1"),
                new KeyValuePair<string, string>("Application", "BetaSharp.Launcher"))
            .WithMacKeyChain("betasharp.launcher", "betasharp")
            .Build();

        var helper = await MsalCacheHelper.CreateAsync(properties);
        helper.RegisterCache(_application.UserTokenCache);

        logger.LogInformation("Finished initializing authentication service");
    }

    public async Task<string> AuthenticateWebAsync()
    {
        try
        {
            return await AuthenticateSilentAsync();
        }
        catch (MsalUiRequiredException)
        {
            var result = await _application
                .AcquireTokenInteractive(_scopes)
                .WithUseEmbeddedWebView(false)
                .WithSystemWebViewOptions(_webViewOptions)
                .ExecuteAsync();

            logger.LogInformation("Finished authentication via Web");

            return result.AccessToken;
        }
    }

    public async Task<string> AuthenticateCodeAsync(Func<DeviceCodeResult, Task> callback)
    {
        try
        {
            return await AuthenticateSilentAsync();
        }
        catch (MsalUiRequiredException)
        {
            var result = await _application
                .AcquireTokenWithDeviceCode(_scopes, callback)
                .ExecuteAsync();

            logger.LogInformation("Finished authentication via code flow");

            return result.AccessToken;
        }
    }

    private async Task<string> AuthenticateSilentAsync()
    {
        var accounts = await _application.GetAccountsAsync();

        var result = await _application
            .AcquireTokenSilent(_scopes, accounts.FirstOrDefault())
            .ExecuteAsync();

        logger.LogInformation("Finished authentication silently");

        return result.AccessToken;
    }
}
