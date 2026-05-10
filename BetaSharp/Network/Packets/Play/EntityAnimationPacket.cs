using BetaSharp.Entities;

namespace BetaSharp.Network.Packets.Play;

public class EntityAnimationPacket() : Packet(PacketId.EntityAnimation), IPacketEntity
{
    public enum EntityAnimation : byte
    {
        SwingHand = 1,
        Hurt = 2,
        WakeUp = 3,
        Spawn = 4
    }

    public byte AnimationId { get; private set; }
    public int EntityId { get; private set; }

    public static EntityAnimationPacket Get(Entity ent, EntityAnimation id) => Get(ent, (byte)id);

    public static EntityAnimationPacket Get(Entity ent, byte animationId)
    {
        EntityAnimationPacket p = Get<EntityAnimationPacket>(PacketId.EntityAnimation);
        p.EntityId = ent.ID;
        p.AnimationId = animationId;
        return p;
    }

    public override void Read(Stream stream)
    {
        EntityId = stream.ReadInt();
        AnimationId = (byte)stream.ReadByte();
    }

    public override void Write(Stream stream)
    {
        stream.WriteInt(EntityId);
        stream.WriteByte(AnimationId);
    }

    public override void Apply(NetHandler handler) => handler.onEntityAnimation(this);

    public override int Size() => IPacketEntity.PacketBaseEntitySize + 1;
}
