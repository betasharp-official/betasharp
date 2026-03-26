using BetaSharp.Entities;
using BetaSharp.Network.Packets;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Items;

public class NetworkSyncedItem : Item
{
    public NetworkSyncedItem(int id) : base(id)
    {
    }

    public override bool isNetworkSynced()
    {
        return true;
    }

    public virtual Packet? getUpdatePacket(ItemStack stack, IWorldContext world, EntityPlayer player)
    {
        return null;
    }
}
