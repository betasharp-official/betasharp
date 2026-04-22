using BetaSharp.Entities;
using BetaSharp.Network.Packets;
using BetaSharp.Network.Packets.S2CPlay;

namespace BetaSharp.Registries;

/// <summary>
/// Orchestrates the full reload-sync pipeline.
/// </summary>
public static class RegistryReloadPipeline
{
    /// <summary>
    /// Bundles all registry sync packets and listener migration packets into atomic
    /// <see cref="BundleS2CPacket"/>s and sends one to each connected player.
    /// </summary>
    public static void SyncToPlayers(
        RegistryAccess registries,
        IReadOnlyList<IRegistryReloadListener> listeners,
        IEnumerable<ServerPlayerEntity> players)
    {
        Dictionary<ServerPlayerEntity, BundleS2CPacket> bundles = players.ToDictionary(
            p => p,
            _ => Packet.Get<BundleS2CPacket>(PacketId.BundleS2C));

        List<RegistryDataS2CPacket> syncPackets = [.. registries.BuildSyncPackets()];

        try
        {
            // Pack registry data into every player's bundle
            foreach (RegistryDataS2CPacket rp in syncPackets)
            {
                foreach (BundleS2CPacket bundle in bundles.Values)
                {
                    Interlocked.Increment(ref rp.UseCount);
                    bundle.Packets.Add(rp);
                }
            }

            // Collect per-player migration packets from each listener
            foreach ((ServerPlayerEntity player, BundleS2CPacket bundle) in bundles)
            {
                foreach (IRegistryReloadListener listener in listeners)
                {
                    Packet[] packets = listener.GetSyncPackets(registries, player);

                    foreach (Packet packet in packets)
                    {
                        bundle.Packets.Add(packet);
                    }
                }

                bundle.Packets.Add(Packet.Get<FinishConfigurationS2CPacket>(PacketId.FinishConfigurationS2C));
                player.NetworkHandler.SendPacket(bundle);
            }
        }
        finally
        {
            foreach (RegistryDataS2CPacket rp in syncPackets)
            {
                rp.Return();
            }
        }
    }
}
