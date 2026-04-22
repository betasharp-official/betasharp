using BetaSharp.Entities;
using BetaSharp.Network.Packets;
using BetaSharp.Server;

namespace BetaSharp.Registries;

/// <summary>
/// Implemented by any system that holds cached data derived from registry entries and
/// needs to refresh that data when datapacks are reloaded via <c>/reload</c>.
/// </summary>
/// <remarks>
/// Register implementations with <see cref="BetaSharpServer.RegisterReloadListener"/>.
/// </remarks>
public interface IRegistryReloadListener
{
    /// <summary>
    /// Called once per reload, after the new <paramref name="registryAccess"/> is built but
    /// before any packets are sent to players.
    /// </summary>
    void OnRegistriesRebuilt(RegistryAccess registryAccess);

    /// <summary>
    /// Returns migration packets for the given player that should be included in their
    /// atomic sync bundle during a reload. Returns <c>null</c> if no packets are needed.
    /// </summary>
    Packet[] GetSyncPackets(RegistryAccess registries, ServerPlayerEntity player) => [];
}
