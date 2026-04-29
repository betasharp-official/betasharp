using BetaSharp.Entities;
using BetaSharp.Network.Packets;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Items;

public class NetworkSyncedItem(int id) : Item(id)
{
    public override bool IsNetworkSynced() => true;

    public virtual Packet? getUpdatePacket(ItemStack stack, IWorldContext world, EntityPlayer player) => null;
}
