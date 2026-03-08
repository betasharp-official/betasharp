using BetaSharp.Client.Entities;
using BetaSharp.Entities;
using BetaSharp.Network.Packets.C2SPlay;
using BetaSharp.Network.Packets.Play;
using BetaSharp.Network.Packets.S2CPlay;
using BetaSharp.Stats;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;

namespace BetaSharp.Client.Network;

public class EntityClientPlayerMP : ClientPlayerEntity
{
    public ClientNetworkHandler sendQueue;
    private int inventorySyncTickCounter;
    private bool hasReceivedInitialHealth;
    private double oldPosX;
    private double lastSentMinY;
    private double oldPosY;
    private double oldPosZ;
    private float oldRotationYaw;
    private float oldRotationPitch;
    private bool lastOnGround;
    private bool wasSneaking;

    public EntityClientPlayerMP(BetaSharp game, World world, Session session, ClientNetworkHandler clientNetworkHandler) : base(game, world, session, 0)
    {
        sendQueue = clientNetworkHandler;
    }

    public override bool damage(Entity ent, int amount)
    {
        return false;
    }

    public override void heal(int amount)
    {
    }

    public override void tick()
    {
        if (world.isPosLoaded(MathHelper.Floor(x), 64, MathHelper.Floor(z)))
        {
            base.tick();
            func_4056_N();
        }
    }

    public void func_4056_N()
    {
        if (inventorySyncTickCounter++ == 20)
        {
            sendInventoryChanged();
            inventorySyncTickCounter = 0;
        }

        bool isSneaking = base.isSneaking();
        if (isSneaking != wasSneaking)
        {
            if (isSneaking)
            {
                sendQueue.addToSendQueue(ClientCommandC2SPacket.Get(this, 1));
            }
            else
            {
                sendQueue.addToSendQueue(ClientCommandC2SPacket.Get(this, 2));
            }

            wasSneaking = isSneaking;
        }

        double dx = x - oldPosX;
        double dMinY = boundingBox.MinY - lastSentMinY;
        double dy = y - oldPosY;
        double dz = z - oldPosZ;
        double dYaw = (double)(yaw - oldRotationYaw);
        double yPitch = (double)(pitch - oldRotationPitch);
        bool positionChanged = dMinY != 0.0D || dy != 0.0D || dx != 0.0D || dz != 0.0D;
        bool rotationChanged = dYaw != 0.0D || yPitch != 0.0D;
        if (vehicle != null)
        {
            if (rotationChanged)
            {
                sendQueue.addToSendQueue(PlayerMovePositionAndOnGroundPacket.Get(velocityX, -999.0D, -999.0D, velocityZ, onGround));
            }
            else
            {
                sendQueue.addToSendQueue(PlayerMoveFullPacket.Get(velocityX, -999.0D, -999.0D, velocityZ, yaw, pitch, onGround));
            }

            positionChanged = false;
        }
        else if (positionChanged && rotationChanged)
        {
            sendQueue.addToSendQueue(PlayerMoveFullPacket.Get(x, boundingBox.MinY, y, z, yaw, pitch, onGround));
        }
        else if (positionChanged)
        {
            sendQueue.addToSendQueue(PlayerMovePositionAndOnGroundPacket.Get(x, boundingBox.MinY, y, z, onGround));
        }
        else if (rotationChanged)
        {
            sendQueue.addToSendQueue(PlayerMoveLookAndOnGroundPacket.Get(yaw, pitch, onGround));
        }
        else if (lastOnGround != onGround)
        {
            sendQueue.addToSendQueue(PlayerMovePacket.Get(onGround));
        }

        lastOnGround = onGround;
        if (positionChanged)
        {
            oldPosX = x;
            lastSentMinY = boundingBox.MinY;
            oldPosY = y;
            oldPosZ = z;
        }

        if (rotationChanged)
        {
            oldRotationYaw = yaw;
            oldRotationPitch = pitch;
        }

    }

    public override void dropSelectedItem()
    {
        var selected = getHand();
        if (selected != null && selected.count > 0)
        {
            increaseStat(Stats.Stats.DropStat, 1);
        }
        sendQueue.addToSendQueue(PlayerActionC2SPacket.Get(4, 0, 0, 0, 0));
    }

    private void sendInventoryChanged()
    {
    }

    protected override void spawnItem(EntityItem ent)
    {
    }

    public override void sendChatMessage(string message)
    {
        sendQueue.addToSendQueue(ChatMessagePacket.Get(message));
    }

    public override void swingHand()
    {
        base.swingHand();
        sendQueue.addToSendQueue(EntityAnimationPacket.Get(this, EntityAnimationPacket.EntityAnimation.SwingHand));
    }

    public override void respawn()
    {
        sendInventoryChanged();
        sendQueue.addToSendQueue(PlayerRespawnPacket.Get((sbyte)dimensionId));
    }

    protected override void applyDamage(int amount)
    {
        health -= amount;
    }

    public override void closeHandledScreen()
    {
        sendQueue.addToSendQueue(CloseScreenS2CPacket.Get(currentScreenHandler.SyncId));
        inventory.setItemStack(null);
        base.closeHandledScreen();
    }

    public override void setHealth(int amount)
    {
        if (hasReceivedInitialHealth)
        {
            base.setHealth(amount);
        }
        else
        {
            health = amount;
            hasReceivedInitialHealth = true;
        }

    }

    public override void increaseStat(StatBase stat, int amount)
    {
        if (stat != null)
        {
            if (stat.LocalOnly)
            {
                base.increaseStat(stat, amount);
            }

        }
    }

    public void func_27027_b(StatBase stat, int amount)
    {
        if (stat != null && !stat.LocalOnly)
        {
            base.increaseStat(stat, amount);
        }
    }
}
