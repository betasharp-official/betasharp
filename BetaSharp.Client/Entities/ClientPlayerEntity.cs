using BetaSharp.Blocks.Entities;
using BetaSharp.Client.Entities.FX;
using BetaSharp.Client.Guis;
using BetaSharp.Client.Rendering.Particles;
using BetaSharp.Client.Input;
using BetaSharp.Entities;
using BetaSharp.Inventorys;
using BetaSharp.NBT;
using BetaSharp.Stats;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Client.Entities;

public class ClientPlayerEntity : EntityPlayer
{
    public override EntityType Type => EntityRegistry.Player;
    public MovementInput movementInput;
    protected BetaSharp Game;
    private readonly MouseFilter field_21903_bJ = new();
    private readonly MouseFilter field_21904_bK = new();
    private readonly MouseFilter field_21902_bL = new();

    public ClientPlayerEntity(BetaSharp game, IWorldContext world, Session session, int dimensionId) : base(world)
    {
        this.Game = game;
        base.dimensionId = dimensionId;
        name = session.username;
    }

    public override void move(double x, double y, double z)
    {
        base.move(x, y, z);
    }

    public override void tickLiving()
    {
        base.tickLiving();
        sidewaysSpeed = movementInput.moveStrafe;
        forwardSpeed = movementInput.moveForward;
        jumping = movementInput.jump;
    }

    public override void tickMovement()
    {
        if (!Game.statFileWriter.HasAchievementUnlocked(global::BetaSharp.Achievements.OpenInventory))
        {
            Game.guiAchievement.QueueAchievementInformation(global::BetaSharp.Achievements.OpenInventory);
        }

        lastScreenDistortion = changeDimensionCooldown;
        if (inTeleportationState)
        {
            if (!world.IsRemote && vehicle != null)
            {
                setVehicle((Entity)null);
            }

            if (Game.currentScreen != null)
            {
                Game.displayGuiScreen((GuiScreen)null);
            }

            if (changeDimensionCooldown == 0.0F)
            {
                Game.sndManager.PlaySoundFX("portal.trigger", 1.0F, random.NextFloat() * 0.4F + 0.8F);
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
        Game.displayGuiScreen(null);
    }

    public override void openEditSignScreen(BlockEntitySign sign)
    {
        Game.displayGuiScreen(new GuiEditSign(sign));
    }

    public override void openChestScreen(IInventory inventory)
    {
        Game.displayGuiScreen(new GuiChest(base.inventory, inventory));
    }

    public override void openCraftingScreen(int x, int y, int z)
    {
        Game.displayGuiScreen(new GuiCrafting(inventory, world, x, y, z));
    }

    public override void openFurnaceScreen(BlockEntityFurnace furnace)
    {
        Game.displayGuiScreen(new GuiFurnace(inventory, furnace));
    }

    public override void openDispenserScreen(BlockEntityDispenser dispenser)
    {
        Game.displayGuiScreen(new GuiDispenser(inventory, dispenser));
    }

    public override void sendPickup(Entity entity, int count)
    {
        Game.particleManager.AddSpecialParticle(new LegacyParticleAdapter(new EntityPickupFX(Game.world, entity, this, -0.5F)));
    }

    public int getPlayerArmorValue()
    {
        return inventory.getTotalArmorValue();
    }

    public virtual void sendChatMessage(string message)
    {
        Game.ingameGUI.AddChatMessage($"<{name}> {message}");
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
        Game.respawn(false, 0);
    }

    public override void spawn()
    {
    }

    public override void sendMessage(string message)
    {
        Game.ingameGUI.AddChatMessageTranslate(message);
    }

    public override void increaseStat(StatBase stat, int value)
    {
        if (stat != null)
        {
            if (stat.IsAchievement())
            {
                Achievement achievement = (Achievement)stat;
                bool parentUnlocked = achievement.parent == null || Game.statFileWriter.HasAchievementUnlocked(achievement.parent);
                bool alreadyUnlocked = Game.statFileWriter.HasAchievementUnlocked(achievement);

                if (parentUnlocked)
                {
                    if (!alreadyUnlocked)
                    {
                        Game.guiAchievement.QueueTakenAchievement(achievement);
                    }

                    Game.statFileWriter.ReadStat(stat, value);
                }
            }
            else
            {
                Game.statFileWriter.ReadStat(stat, value);
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
}
