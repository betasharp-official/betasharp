using BetaSharp.Blocks.Entities;
using BetaSharp.Client.Input;
using BetaSharp.Client.Network;
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
    public override EntityType Type => EntityRegistry.Player;
    public MovementInput movementInput;
    protected BetaSharp Game;
    private byte _lastJump;
    private bool _isFlying;

    public ClientPlayerEntity(BetaSharp game, IWorldContext world, Session session, int dimensionId) : base(world)
    {
        Game = game;
        base.dimensionId = dimensionId;
        name = session.username;
    }

    public override void tickLiving()
    {
        base.tickLiving();
        if (GameMode is { CanWalk: false, DisallowFlying: true })
        {
            sidewaysSpeed = 0;
            forwardSpeed = 0;
        }
        else if (!GameMode.DisallowFlying)
        {
            sidewaysSpeed = movementInput.moveStrafe * AirFlySpeedMult;
            forwardSpeed = movementInput.moveForward * AirFlySpeedMult;
        }
        else
        {
            sidewaysSpeed = movementInput.moveStrafe;
            forwardSpeed = movementInput.moveForward;
        }

        if (jumping != movementInput.jump)
        {
            jumping = movementInput.jump;
            if (jumping)
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

    public override void tickMovement()
    {
        if (!Game.StatFileWriter.HasAchievementUnlocked(global::BetaSharp.Achievements.OpenInventory))
        {
            Game.HUD.AchievementToast.QueueInfo(global::BetaSharp.Achievements.OpenInventory);
        }

        lastScreenDistortion = changeDimensionCooldown;
        if (inTeleportationState)
        {
            if (!world.IsRemote && vehicle != null)
            {
                setVehicle((Entity)null);
            }

            if (Game.CurrentScreen != null)
            {
                Game.Navigate(null);
            }

            if (changeDimensionCooldown == 0.0F)
            {
                Game.SoundManager.PlaySoundFX("portal.trigger", 1.0F, random.NextFloat() * 0.4F + 0.8F);
            }

            changeDimensionCooldown += 0.0125F;
            if (changeDimensionCooldown >= 1.0F)
            {
                changeDimensionCooldown = 1.0F;
            }

            inTeleportationState = false;
        }
        else
        {
            if (changeDimensionCooldown > 0.0F)
            {
                changeDimensionCooldown -= 0.05F;
            }

            if (changeDimensionCooldown < 0.0F)
            {
                changeDimensionCooldown = 0.0F;
            }
        }

        if (portalCooldown > 0)
        {
            --portalCooldown;
        }

        movementInput.updatePlayerMoveState(this);

        if (!GameMode.DisallowFlying && (_isFlying || !GameMode.CanWalk))
        {
            _isFlying &= !onGround;

            if (!movementInput.sneak)
            {
                if (movementInput.jump)
                {
                    // flying up
                    velocityY += 0.1;
                }
                else
                {
                    // hold height, but smoothly
                    velocityY = velocityY < -0.15 ? velocityY + 0.15 : Math.Max(0, velocityY);
                }

            }
            else if (movementInput.jump)
            {
                // shift + space = hold height
                velocityY = 0;
            }
            else
            {
                // limit flying decent speed
                velocityY = Math.Max(velocityY, -0.5);
            }

        }

        if (movementInput.sneak && cameraOffset < 0.2F)
        {
            cameraOffset = 0.2F;
        }

        pushOutOfBlocks(x - (double)width * 0.35D, boundingBox.MinY + 0.5D, z + (double)width * 0.35D);
        pushOutOfBlocks(x - (double)width * 0.35D, boundingBox.MinY + 0.5D, z - (double)width * 0.35D);
        pushOutOfBlocks(x + (double)width * 0.35D, boundingBox.MinY + 0.5D, z - (double)width * 0.35D);
        pushOutOfBlocks(x + (double)width * 0.35D, boundingBox.MinY + 0.5D, z + (double)width * 0.35D);
        base.tickMovement();
    }

    public void resetPlayerKeyState()
    {
        movementInput.resetKeyState();
    }

    public void handleKeyPress(int key, bool isPressed)
    {
        movementInput.checkKeyForMovementInput(key, isPressed);
    }

    public override void writeNbt(NBTTagCompound nbt)
    {
        base.writeNbt(nbt);
        nbt.SetInteger("Score", score);
    }

    public override void readNbt(NBTTagCompound nbt)
    {
        base.readNbt(nbt);
        score = nbt.GetInteger("Score");
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

    public override void openChestScreen(IInventory inventory)
    {
        Game.Navigate(new ChestScreen(Game.UIContext, this, Game.PlayerController, base.inventory, inventory));
    }

    public override void openCraftingScreen(int x, int y, int z)
    {
        Game.Navigate(new CraftingScreen(Game.UIContext, this, Game.PlayerController, inventory, (IWorldContext)world, x, y, z));
    }

    public override void openFurnaceScreen(BlockEntityFurnace furnace)
    {
        Game.Navigate(new FurnaceScreen(Game.UIContext, this, Game.PlayerController, inventory, furnace));
    }

    public override void openDispenserScreen(BlockEntityDispenser dispenser)
    {
        Game.Navigate(new DispenserScreen(Game.UIContext, this, Game.PlayerController, inventory, dispenser));
    }

    public override void sendPickup(Entity entity, int count)
    {
        Game.ParticleManager.AddPickupParticle(entity, this, -0.5F, Game.EntityRenderDispatcher);
    }

    public int getPlayerArmorValue()
    {
        return inventory.GetTotalArmorValue();
    }

    public override void sendChatMessage(string message)
    {
        Game.HUD.AddChatMessage($"<{name}> {message}");
    }

    public override bool isSneaking()
    {
        return movementInput.sneak && !sleeping;
    }

    public virtual void setHealth(int newHealth)
    {
        int damageAmount = health - newHealth;
        if (damageAmount <= 0)
        {
            health = newHealth;
            if (damageAmount < 0)
            {
                hearts = maxHealth / 2;
            }
        }
        else
        {
            if (!GameMode.CanReceiveDamage) return;
            damageForDisplay = damageAmount;
            lastHealth = health;
            hearts = maxHealth;
            applyDamage(damageAmount);
        }
    }

    public override void respawn()
    {
        Game.Respawn(false, 0);
    }

    public override void spawn()
    {
    }

    public override void sendMessage(string message)
    {
        Game.HUD.AddChatMessage(message);
    }

    public override void increaseStat(StatBase stat, int value)
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

    private bool isBlockTranslucent(int x, int y, int z)
    {
        return world.Reader.ShouldSuffocate(x, y, z);
    }

    protected override bool pushOutOfBlocks(double posX, double posY, double posZ)
    {
        int floorX = MathHelper.Floor(posX);
        int floorY = MathHelper.Floor(posY);
        int floorZ = MathHelper.Floor(posZ);
        double fracX = posX - (double)floorX;
        double fracZ = posZ - (double)floorZ;
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
                velocityX = (double)(-pushStrength);
            }

            if (pushDirection == 1)
            {
                velocityX = (double)pushStrength;
            }

            if (pushDirection == 4)
            {
                velocityZ = (double)(-pushStrength);
            }

            if (pushDirection == 5)
            {
                velocityZ = (double)pushStrength;
            }
        }

        return false;
    }

    public override void markDead()
    {
        _isFlying = false;
        base.markDead();
    }

    protected override float AirSpeed() => GameMode.DisallowFlying || !_isFlying ? 0.02f : AirFlySpeedMult * 0.02f;
}
