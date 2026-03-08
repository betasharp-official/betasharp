using System;
using System.Linq;
using System.Threading.Tasks;
using BetaSharp.Launcher.Features.Mojang;
using BetaSharp.Launcher.Features.Xbox;

namespace BetaSharp.Launcher.Features.Sessions;

internal sealed class SessionService(XboxClient xboxClient, MojangClient mojangClient)
{
    public async Task<Session?> TryCreateAsync(string token)
    {
        var user = await xboxClient.GetUserAsync(token);
        var xbox = await xboxClient.GetTokenAsync(user.Token);

        var mojang = await mojangClient.GetTokenAsync(xbox.Value, user.DisplayClaims.Xui[0].Uhs);
        var entitlements = await mojangClient.GetEntitlementsAsync(mojang.Value);

        if (!entitlements.Items.Any(item => item.Name is "product_minecraft" or "game_minecraft"))
        {
            return null;
        }

        var profile = await mojangClient.GetProfileAsync(mojang.Value);

        return new Session
        {
            Name = profile.Name,
            Face = $"https://mc-heads.net/avatar/{profile.Name}/8.png",
            Token = mojang.Value,
            Expiration = DateTimeOffset.UtcNow.Add(TimeSpan.FromSeconds(mojang.Expiration)),
        };
    }
}
