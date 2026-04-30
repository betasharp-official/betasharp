using BetaSharp.Blocks.Entities;
using BetaSharp.Client.Entities.FX;
using BetaSharp.Client.Input;
using BetaSharp.Client.Network;
using BetaSharp.Client.Rendering.Particles;
using BetaSharp.Client.UI.Screens.InGame;
using BetaSharp.Client.UI.Screens.InGame.Containers;
using BetaSharp.Entities;
using BetaSharp.Inventorys;
using BetaSharp.NBT;
using BetaSharp.Network.Packets.Play;
using BetaSharp.Stats;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Client.Entities;

public class ClientPlayerEntity : EntityPlayer
{
    private bool _isFlying;
    private byte _lastJump;
    protected BetaSharp Game;
    public MovementInput movementInput;

    public ClientPlayerEntity(BetaSharp game, IWorldContext world, Session session, int dimensionId) : base(world)
    {
        Game = game;
        DimensionId = dimensionId;
        Name = session.username;
    }

    public override EntityType Type => EntityRegistry.Player;

    protected new float AirSpeed => GameMode.DisallowFlying || !_isFlying ? 0.02f : AirFlySpeedMult * 0.02f;

    protected override void TickLiving()
    {
        base.TickLiving();
        if (GameMode is { CanWalk: false, DisallowFlying: true })
        {
            SidewaysSpeed = 0;
            ForwardSpeed = 0;
        }
        else if (!GameMode.DisallowFlying)
        {
            SidewaysSpeed = movementInput.moveStrafe * AirFlySpeedMult;
            ForwardSpeed = movementInput.moveForward * AirFlySpeedMult;
        }
        else
        {
            SidewaysSpeed = movementInput.moveStrafe;
            ForwardSpeed = movementInput.moveForward;
        }

        if (Jumping != movementInput.jump)
        {
            Jumping = movementInput.jump;
            if (Jumping)
            {
                // double jump
                if (!GameMode.DisallowFlying && _lastJump <= 2)
                {
                    _isFlying = !_isFlying;
                }

                _lastJump = 0;
            }
        }
        else
        {
            _lastJump = (byte)((_lastJump + 1) & 127);
        }
    }

    protected override void TickMovement()
    {
        if (!Game.StatFileWriter.HasAchievementUnlocked(global::BetaSharp.Achievements.OpenInventory))
        {
            Game.HUD.AchievementToast.QueueInfo(global::BetaSharp.Achievements.OpenInventory);
        }

        LastScreenDistortion = ChangeDimensionCooldown;
        if (InTeleportationState)
        {
            if (!World.IsRemote && Vehicle != null)
            {
                SetVehicle(null);
            }

            if (Game.CurrentScreen != null)
            {
                Game.Navigate(null);
            }

            if (ChangeDimensionCooldown == 0.0F)
            {
                Game.SoundManager.PlaySoundFX("portal.trigger", 1.0F, Random.NextFloat() * 0.4F + 0.8F);
            }

            ChangeDimensionCooldown += 0.0125F;
            if (ChangeDimensionCooldown >= 1.0F)
            {
                ChangeDimensionCooldown = 1.0F;
            }

            InTeleportationState = false;
        }
        else
        {
            if (ChangeDimensionCooldown > 0.0F)
            {
                ChangeDimensionCooldown -= 0.05F;
            }

            if (ChangeDimensionCooldown < 0.0F)
            {
                ChangeDimensionCooldown = 0.0F;
            }
        }

        if (PortalCooldown > 0)
        {
            --PortalCooldown;
        }

        movementInput.updatePlayerMoveState(this);

        if (!GameMode.DisallowFlying && (_isFlying || !GameMode.CanWalk))
        {
            _isFlying &= !OnGround;

            if (!movementInput.sneak)
            {
                if (movementInput.jump)
                {
                    // flying up
                    VelocityY += 0.1;
                }
                else
                {
                    // hold height, but smoothly
                    VelocityY = VelocityY < -0.15 ? VelocityY + 0.15 : Math.Max(0, VelocityY);
                }
            }
            else if (movementInput.jump)
            {
                // shift + space = hold height
                VelocityY = 0;
            }
            else
            {
                // limit flying decent speed
                VelocityY = Math.Max(VelocityY, -0.5);
            }
        }

        if (movementInput.sneak && CameraOffset < 0.2F)
        {
            CameraOffset = 0.2F;
        }

        PushOutOfBlocks(X - Width * 0.35D, BoundingBox.MinY + 0.5D, Z + Width * 0.35D);
        PushOutOfBlocks(X - Width * 0.35D, BoundingBox.MinY + 0.5D, Z - Width * 0.35D);
        PushOutOfBlocks(X + Width * 0.35D, BoundingBox.MinY + 0.5D, Z - Width * 0.35D);
        PushOutOfBlocks(X + Width * 0.35D, BoundingBox.MinY + 0.5D, Z + Width * 0.35D);
        base.TickMovement();
    }

    public void resetPlayerKeyState() => movementInput.resetKeyState();

    public void handleKeyPress(int scanCode, bool isPressed) => movementInput.checkKeyForMovementInput(scanCode, isPressed);

    protected override void WriteNbt(NBTTagCompound nbt)
    {
        base.WriteNbt(nbt);
        nbt.SetInteger("Score", Score);
    }

    protected override void ReadNbt(NBTTagCompound nbt)
    {
        base.ReadNbt(nbt);
        Score = nbt.GetInteger("Score");
    }

    public override void closeHandledScreen()
    {
        base.closeHandledScreen();
        Game.Navigate(null);
    }

    public override void openEditSignScreen(BlockEntitySign sign)
    {
        Action? sendUpdate = null;
        if (this is EntityClientPlayerMP mp && (Game.World?.IsRemote ?? false))
        {
            sendUpdate = () => mp.sendQueue.AddToSendQueue(UpdateSignPacket.Get(sign.X, sign.Y, sign.Z, sign.Texts));
        }

        Game.Navigate(new SignEditScreen(Game.UIContext, sign, sendUpdate));
    }

    public override void openChestScreen(IInventory inventory) => Game.Navigate(new ChestScreen(Game.UIContext, this, Game.PlayerController, Inventory, inventory));

    public override void openCraftingScreen(int x, int y, int z) => Game.Navigate(new CraftingScreen(Game.UIContext, this, Game.PlayerController, Inventory, World, x, y, z));

    public override void openFurnaceScreen(BlockEntityFurnace furnace) => Game.Navigate(new FurnaceScreen(Game.UIContext, this, Game.PlayerController, Inventory, furnace));

    public override void openDispenserScreen(BlockEntityDispenser dispenser) => Game.Navigate(new DispenserScreen(Game.UIContext, this, Game.PlayerController, Inventory, dispenser));

    public override void sendPickup(Entity entity, int count) => Game.ParticleManager.AddSpecialParticle(new LegacyParticleAdapter(new EntityPickupFX(Game.World, entity, this, -0.5F)));

    public int getPlayerArmorValue() => Inventory.GetTotalArmorValue();

    public override void SendChatMessage(string message) => Game.HUD.AddChatMessage($"<{Name}> {message}");

    public override bool IsSneaking() => movementInput.sneak && !Sleeping;

    public virtual void setHealth(int newHealth)
    {
        int damageAmount = Health - newHealth;
        if (damageAmount <= 0)
        {
            Health = newHealth;
            if (damageAmount < 0)
            {
                Hearts = MaxHealth / 2;
            }
        }
        else
        {
            if (!GameMode.CanReceiveDamage)
            {
                return;
            }

            DamageForDisplay = damageAmount;
            LastHealth = Health;
            Hearts = MaxHealth;
            ApplyDamage(damageAmount);
        }
    }

    public override void Respawn() => Game.Respawn(false, 0);

    public override void Spawn()
    {
    }

    public override void SendMessage(string message) => Game.HUD.AddChatMessage(message);

    public override void IncreaseStat(StatBase stat, int value)
    {
        if (stat != null)
        {
            if (stat.IsAchievement())
            {
                Achievement achievement = (Achievement)stat;
                bool parentUnlocked = achievement.parent == null || Game.StatFileWriter.HasAchievementUnlocked(achievement.parent);
                bool alreadyUnlocked = Game.StatFileWriter.HasAchievementUnlocked(achievement);

                if (parentUnlocked)
                {
                    if (!alreadyUnlocked)
                    {
                        Game.HUD.AchievementToast.QueueAchievement(achievement);
                    }

                    Game.StatFileWriter.ReadStat(stat, value);
                }
            }
            else
            {
                Game.StatFileWriter.ReadStat(stat, value);
            }
        }
    }

    private bool isBlockTranslucent(int x, int y, int z) => World.Reader.ShouldSuffocate(x, y, z);

    protected override bool PushOutOfBlocks(double posX, double posY, double posZ)
    {
        int floorX = MathHelper.Floor(posX);
        int floorY = MathHelper.Floor(posY);
        int floorZ = MathHelper.Floor(posZ);
        double fracX = posX - floorX;
        double fracZ = posZ - floorZ;
        if (isBlockTranslucent(floorX, floorY, floorZ) || isBlockTranslucent(floorX, floorY + 1, floorZ))
        {
            bool canPushWest = !isBlockTranslucent(floorX - 1, floorY, floorZ) && !isBlockTranslucent(floorX - 1, floorY + 1, floorZ);
            bool canPushEast = !isBlockTranslucent(floorX + 1, floorY, floorZ) && !isBlockTranslucent(floorX + 1, floorY + 1, floorZ);
            bool canPushNorth = !isBlockTranslucent(floorX, floorY, floorZ - 1) && !isBlockTranslucent(floorX, floorY + 1, floorZ - 1);
            bool canPushSouth = !isBlockTranslucent(floorX, floorY, floorZ + 1) && !isBlockTranslucent(floorX, floorY + 1, floorZ + 1);
            int pushDirection = -1;
            double closestEdgeDistance = 9999.0D;
            if (canPushWest && fracX < closestEdgeDistance)
            {
                closestEdgeDistance = fracX;
                pushDirection = 0;
            }

            if (canPushEast && 1.0D - fracX < closestEdgeDistance)
            {
                closestEdgeDistance = 1.0D - fracX;
                pushDirection = 1;
            }

            if (canPushNorth && fracZ < closestEdgeDistance)
            {
                closestEdgeDistance = fracZ;
                pushDirection = 4;
            }

            if (canPushSouth && 1.0D - fracZ < closestEdgeDistance)
            {
                closestEdgeDistance = 1.0D - fracZ;
                pushDirection = 5;
            }

            float pushStrength = 0.1F;
            if (pushDirection == 0)
            {
                VelocityX = -pushStrength;
            }

            if (pushDirection == 1)
            {
                VelocityX = pushStrength;
            }

            if (pushDirection == 4)
            {
                VelocityZ = -pushStrength;
            }

            if (pushDirection == 5)
            {
                VelocityZ = pushStrength;
            }
        }

        return false;
    }

    public override void MarkDead()
    {
        _isFlying = false;
        base.MarkDead();
    }
}
