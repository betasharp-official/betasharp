using BetaSharp.Entities;
using BetaSharp.Network.Packets;
using BetaSharp.Network.Packets.S2CPlay;
using BetaSharp.Registries;
using BetaSharp.Registries.Data;
using BetaSharp.Server;
using Microsoft.Extensions.Logging;

namespace BetaSharp;

internal sealed class DefaultGameModeListener(BetaSharpServer server) : IRegistryReloadListener
{
    private static readonly ILogger<DefaultGameModeListener> s_logger = Log.Instance.For<DefaultGameModeListener>();

    public void OnRegistriesRebuilt(RegistryAccess registryAccess)
    {
        Holder<GameMode>? resolved = ResolveDefaultGameMode(
            registryAccess.GetOrThrow(RegistryKeys.GameModes),
            server.config.GetDefaultGamemode("survival"));

        if (resolved == null)
        {
            s_logger.LogError("No game modes are registered.");
        }
        else
        {
            server.DefaultGameMode = resolved;
        }
    }

    public Packet[] GetSyncPackets(RegistryAccess registries, ServerPlayerEntity player)
    {
        DataAssetLoader<GameMode> gameModes = registries.GetOrThrow(RegistryKeys.GameModes).AsAssetLoader();

        if (gameModes.TryGetHolder(player.GameMode.Name, out Holder<GameMode>? updated))
        {
            player.GameModeHolder = updated;
        }
        else
        {
            player.GameModeHolder = server.DefaultGameMode;
        }

        return [PlayerGameModeUpdateS2CPacket.Get(player.GameMode)];
    }

    /// <summary>
    /// Resolves which game mode should be the server default.
    /// Tries <paramref name="configuredName"/> first, then "survival", then "default",
    /// then the first registered entry. Returns <c>null</c> if no game modes exist.
    /// </summary>
    internal static Holder<GameMode>? ResolveDefaultGameMode(
        IReadableRegistry<GameMode> registry, string configuredName)
    {
        DataAssetLoader<GameMode> loader = registry.AsAssetLoader();

        if (!string.IsNullOrEmpty(configuredName) && loader.TryGetHolder(configuredName, out Holder<GameMode>? named))
            return named;

        if (loader.TryGetHolder("survival", out Holder<GameMode>? survival))
            return survival;

        if (loader.TryGetHolder("default", out Holder<GameMode>? defaultMode))
            return defaultMode;

        ResourceLocation? firstKey = registry.Keys.FirstOrDefault();
        return firstKey != null ? registry.Get(firstKey) : null;
    }
}
