using System.Net.Sockets;
using BetaSharp.Entities;

namespace BetaSharp.Network.Packets.Play;

public class EntityAnimationPacket() : PacketBaseEntity(PacketId.EntityAnimation)
{
    public int animationId;

    public static EntityAnimationPacket Get(Entity ent, EntityAnimation id) => Get(ent, (int)id);
    public static EntityAnimationPacket Get(Entity ent, int animationId)
    {
        var p = Get<EntityAnimationPacket>(PacketId.EntityAnimation);
        p.EntityId = ent.id;
        p.animationId = animationId;
        return p;
    }

    public override void Read(NetworkStream stream)
    {
        base.Read(stream);
        animationId = (sbyte)stream.ReadByte();
    }

    public override void Write(NetworkStream stream)
    {
        base.Write(stream);
        stream.WriteByte((byte)animationId);
    }

    public override void Apply(NetHandler handler)
    {
        handler.onEntityAnimation(this);
    }

    public override int Size()
    {
        return PacketBaseEntitySize + 1;
    }

    public enum EntityAnimation : byte
    {
        SwingHand = 1,
        Hurt = 2,
        WakeUp = 3,
        Spawn = 4
    }
}
