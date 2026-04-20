using BetaSharp.Client.Entities;
using BetaSharp.Entities;
using BetaSharp.Network.Packets.C2SPlay;
using BetaSharp.Network.Packets.Play;
using BetaSharp.Network.Packets.S2CPlay;
using BetaSharp.Stats;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

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

    public override bool Damage(Entity ent, int amount)
    {
        return false;
    }

    public override void heal(int amount)
    {
    }

    public override void Tick()
    {
        if (World.Reader.IsPosLoaded(MathHelper.Floor(X), 64, MathHelper.Floor(Z)))
        {
            base.Tick();
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

        bool isSneaking = base.IsSneaking();
        if (isSneaking != wasSneaking)
        {
            if (isSneaking)
            {
                sendQueue.AddToSendQueue(ClientCommandC2SPacket.Get(this, 1));
            }
            else
            {
                sendQueue.AddToSendQueue(ClientCommandC2SPacket.Get(this, 2));
            }

            wasSneaking = isSneaking;
        }

        double dx = X - oldPosX;
        double dMinY = BoundingBox.MinY - lastSentMinY;
        double dy = Y - oldPosY;
        double dz = Z - oldPosZ;
        double dYaw = (double)(Yaw - oldRotationYaw);
        double yPitch = (double)(Pitch - oldRotationPitch);
        bool positionChanged = dMinY != 0.0D || dy != 0.0D || dx != 0.0D || dz != 0.0D;
        bool rotationChanged = dYaw != 0.0D || yPitch != 0.0D;
        if (Vehicle != null)
        {
            if (rotationChanged)
            {
                sendQueue.AddToSendQueue(PlayerMovePositionAndOnGroundPacket.Get(VelocityX, -999.0D, -999.0D, VelocityZ, OnGround));
            }
            else
            {
                sendQueue.AddToSendQueue(PlayerMoveFullPacket.Get(VelocityX, -999.0D, -999.0D, VelocityZ, Yaw, Pitch, OnGround));
            }

            positionChanged = false;
        }
        else if (positionChanged && rotationChanged)
        {
            sendQueue.AddToSendQueue(PlayerMoveFullPacket.Get(X, BoundingBox.MinY, Y, Z, Yaw, Pitch, OnGround));
        }
        else if (positionChanged)
        {
            sendQueue.AddToSendQueue(PlayerMovePositionAndOnGroundPacket.Get(X, BoundingBox.MinY, Y, Z, OnGround));
        }
        else if (rotationChanged)
        {
            sendQueue.AddToSendQueue(PlayerMoveLookAndOnGroundPacket.Get(Yaw, Pitch, OnGround));
        }
        else if (lastOnGround != OnGround)
        {
            sendQueue.AddToSendQueue(PlayerMovePacket.Get(OnGround));
        }

        lastOnGround = OnGround;
        if (positionChanged)
        {
            oldPosX = X;
            lastSentMinY = BoundingBox.MinY;
            oldPosY = Y;
            oldPosZ = Z;
        }

        if (rotationChanged)
        {
            oldRotationYaw = Yaw;
            oldRotationPitch = Pitch;
        }

    }

    public override void DropSelectedItem()
    {
        if (!Game.Player.GameMode.CanDrop) return;

        var selected = getHand();
        if (selected != null && selected.Count > 0)
        {
            increaseStat(Stats.Stats.DropStat, 1);
        }
        sendQueue.AddToSendQueue(PlayerActionC2SPacket.Get(4, 0, 0, 0, 0));
    }

    private void sendInventoryChanged()
    {
    }

    protected override void spawnItem(EntityItem ent)
    {
    }

    public override void sendChatMessage(string message)
    {
        sendQueue.AddToSendQueue(ChatMessagePacket.Get(message));
    }

    public override void swingHand()
    {
        base.swingHand();
        sendQueue.AddToSendQueue(EntityAnimationPacket.Get(this, EntityAnimationPacket.EntityAnimation.SwingHand));
    }

    public override void respawn()
    {
        sendInventoryChanged();
        sendQueue.AddToSendQueue(PlayerRespawnPacket.Get((sbyte)dimensionId));
    }

    protected override void applyDamage(int amount)
    {
        Health -= amount;
    }

    public override void closeHandledScreen()
    {
        sendQueue.AddToSendQueue(CloseScreenS2CPacket.Get(currentScreenHandler.SyncId));
        inventory.SetCursorStack(null);
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
            Health = amount;
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

    public void IncreaseRemoteStat(StatBase stat, int amount)
    {
        if (stat != null && !stat.LocalOnly)
        {
            base.increaseStat(stat, amount);
        }
    }
}
