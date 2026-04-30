using BetaSharp.Blocks.Materials;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntitySquid : EntityWaterMob
{
    private float _animationSpeed;
    public float PrevTentaclePhase;
    public float PrevTentacleSpread;
    public float PrevTiltAngle;
    private float _randomMotionSpeed;
    private float _randomMotionVecX;
    private float _randomMotionVecY;
    private float _randomMotionVecZ;
    private float _squidRotation;
    private float _swimPhase;
    public float TentaclePhase;
    public float TentacleSpread;
    public float TiltAngle;

    public EntitySquid(IWorldContext world) : base(world)
    {
        Texture = "/mob/squid.png";
        SetBoundingBoxSpacing(0.95F, 0.95F);
        _animationSpeed = 1.0F / (Random.NextFloat() + 1.0F) * 0.2F;
    }

    protected sealed override void SetBoundingBoxSpacing(float widthOffset, float heightOffset)
    {
        base.SetBoundingBoxSpacing(widthOffset, heightOffset);
    }

    public override EntityType Type => EntityRegistry.Squid;

    protected override bool IsInWater => World.Reader.UpdateMovementInFluid(BoundingBox.Expand(0.0D, -0.6F, 0.0D), Material.Water, this);

    protected override string? LivingSound => null;

    protected override string? HurtSound => null;

    protected override string? DeathSound => null;

    protected override float SoundVolume => 0.4F;

    protected override int DropItemId => 0;

    protected override void DropFewItems()
    {
        int dropCount = Random.NextInt(3) + 1;

        for (int _ = 0; _ < dropCount; ++_)
        {
            DropItem(new ItemStack(Item.Dye, 1, 0), 0.0F);
        }
    }

    public override bool Interact(EntityPlayer player) => false;

    protected override void TickMovement()
    {
        base.TickMovement();
        PrevTiltAngle = TiltAngle;
        PrevTentaclePhase = TentaclePhase;
        PrevTentacleSpread = TentacleSpread;
        _swimPhase += _animationSpeed;
        if (_swimPhase > (float)Math.PI * 2.0F)
        {
            _swimPhase -= (float)Math.PI * 2.0F;
            if (Random.NextInt(10) == 0)
            {
                _animationSpeed = 1.0F / (Random.NextFloat() + 1.0F) * 0.2F;
            }
        }

        if (IsInWater)
        {
            float phaseProgress;
            if (_swimPhase < (float)Math.PI)
            {
                phaseProgress = _swimPhase / (float)Math.PI;
                TentacleSpread = MathHelper.Sin(phaseProgress * phaseProgress * (float)Math.PI) * (float)Math.PI * 0.25F;
                if (phaseProgress > 0.75D)
                {
                    _randomMotionSpeed = 1.0F;
                    _squidRotation = 1.0F;
                }
                else
                {
                    _squidRotation *= 0.8F;
                }
            }
            else
            {
                TentacleSpread = 0.0F;
                _randomMotionSpeed *= 0.9F;
                _squidRotation *= 0.99F;
            }

            if (!InterpolateOnly)
            {
                VelocityX = _randomMotionVecX * _randomMotionSpeed;
                VelocityY = _randomMotionVecY * _randomMotionSpeed;
                VelocityZ = _randomMotionVecZ * _randomMotionSpeed;
            }

            phaseProgress = MathHelper.Sqrt(VelocityX * VelocityX + VelocityZ * VelocityZ);
            BodyYaw += (-(float)Math.Atan2(VelocityX, VelocityZ) * 180.0F / (float)Math.PI - BodyYaw) * 0.1F;
            Yaw = BodyYaw;
            TentaclePhase += (float)Math.PI * _squidRotation * 1.5F;
            TiltAngle += (-(float)Math.Atan2(phaseProgress, VelocityY) * 180.0F / (float)Math.PI - TiltAngle) * 0.1F;
        }
        else
        {
            TentacleSpread = MathHelper.Abs(MathHelper.Sin(_swimPhase)) * (float)Math.PI * 0.25F;
            if (!InterpolateOnly)
            {
                VelocityX = 0.0D;
                VelocityY -= 0.08D;
                VelocityY *= 0.98F;
                VelocityZ = 0.0D;
            }

            TiltAngle = (float)(TiltAngle + (-90.0F - TiltAngle) * 0.02D);
        }
    }

    protected override void Travel(float strafe, float forward) => Move(VelocityX, VelocityY, VelocityZ);

    protected override void TickLiving()
    {
        if (Random.NextInt(50) == 0 || !InWater || (_randomMotionVecX == 0.0F && _randomMotionVecY == 0.0F && _randomMotionVecZ == 0.0F))
        {
            float randomAngle = Random.NextFloat() * (float)Math.PI * 2.0F;
            _randomMotionVecX = MathHelper.Cos(randomAngle) * 0.2F;
            _randomMotionVecY = -0.1F + Random.NextFloat() * 0.2F;
            _randomMotionVecZ = MathHelper.Sin(randomAngle) * 0.2F;
        }

        func_27021_X();
    }
}
