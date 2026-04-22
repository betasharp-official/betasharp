using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Client.Network;

public class OtherPlayerEntity : EntityPlayer
{
    public override EntityType Type => EntityRegistry.Player;
    private int lerpSteps;
    private double lerpX;
    private double lerpY;
    private double lerpZ;
    private double lerpYaw;
    private double lerpPitch;

    public OtherPlayerEntity(World world, string name) : base(world)
    {
        base.name = name;
        StandingEyeHeight = 0.0F;
        StepHeight = 0.0F;
        NoClip = true;
        sleepOffsetY = 0.25F;
        RenderDistanceWeight = 10.0D;
    }

    protected override void resetEyeHeight()
    {
        StandingEyeHeight = 0.0F;
    }

    public override bool Damage(Entity ent, int amount)
    {
        return true;
    }

    public override void SetPositionAndAnglesAvoidEntities(double lerpX, double lerpY, double lerpZ, float lerpYaw, float lerpPitch, int lerpSteps)
    {
        this.lerpX = lerpX;
        this.lerpY = lerpY;
        this.lerpZ = lerpZ;
        this.lerpYaw = lerpYaw;
        this.lerpPitch = lerpPitch;
        this.lerpSteps = lerpSteps;
    }

    public override void Tick()
    {
        sleepOffsetY = 0.0F;
        base.Tick();
        LastWalkAnimationSpeed = WalkAnimationSpeed;
        double dx = X - PrevX;
        double dz = Z - PrevZ;
        float horizontalDistance = MathHelper.Sqrt(dx * dx + dz * dz) * 4.0F;
        if (horizontalDistance > 1.0F)
        {
            horizontalDistance = 1.0F;
        }

        WalkAnimationSpeed += (horizontalDistance - WalkAnimationSpeed) * 0.4F;
        AnimationPhase += WalkAnimationSpeed;
    }

    public override float GetShadowRadius()
    {
        return 0.0F;
    }

    public override void tickMovement()
    {
        base.tickLiving();
        if (lerpSteps > 0)
        {
            double newX = X + (lerpX - X) / lerpSteps;
            double newY = Y + (lerpY - Y) / lerpSteps;
            double newZ = Z + (lerpZ - Z) / lerpSteps;

            double dYaw;
            for (dYaw = lerpYaw - Yaw; dYaw < -180.0D; dYaw += 360.0D)
            {
            }

            while (dYaw >= 180.0D)
            {
                dYaw -= 360.0D;
            }

            Yaw = (float)(Yaw + dYaw / lerpSteps);
            Pitch = (float)(Pitch + (lerpPitch - Pitch) / lerpSteps);
            --lerpSteps;
            SetPosition(newX, newY, newZ);
            SetRotation(Yaw, Pitch);
        }

        prevStepBobbingAmount = stepBobbingAmount;
        float horizontalSpeed = MathHelper.Sqrt(VelocityX * VelocityX + VelocityZ * VelocityZ);
        float tiltAmount = (float)Math.Atan(-VelocityY * (double)0.2F) * 15.0F;
        if (horizontalSpeed > 0.1F)
        {
            horizontalSpeed = 0.1F;
        }

        if (!OnGround || Health <= 0)
        {
            horizontalSpeed = 0.0F;
        }

        if (OnGround || Health <= 0)
        {
            tiltAmount = 0.0F;
        }

        stepBobbingAmount += (horizontalSpeed - stepBobbingAmount) * 0.4F;
        Tilt += (tiltAmount - Tilt) * 0.8F;
    }

    public override void SetEquipmentStack(int slotIndex, int itemId, int damage)
    {
        ItemStack itemStack = null;
        if (itemId >= 0)
        {
            itemStack = new ItemStack(itemId, 1, damage);
        }

        if (slotIndex == 0)
        {
            inventory.Main[inventory.SelectedSlot] = itemStack;
        }
        else
        {
            inventory.Armor[slotIndex - 1] = itemStack;
        }

    }

    public override void spawn()
    {
    }
}
