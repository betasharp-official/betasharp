using BetaSharp.Blocks;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public abstract class EntityFlying : EntityLiving
{
    public EntityFlying(IWorldContext world) : base(world)
    {
    }

    protected override void OnLanding(float fallDistance)
    {
    }

    public override void travel(float strafe, float forward)
    {
        if (IsInWater())
        {
            MoveNonSolid(strafe, forward, 0.02F);
            Move(VelocityX, VelocityY, VelocityZ);
            VelocityX *= (double)0.8F;
            VelocityY *= (double)0.8F;
            VelocityZ *= (double)0.8F;
        }
        else if (IsTouchingLava())
        {
            MoveNonSolid(strafe, forward, 0.02F);
            Move(VelocityX, VelocityY, VelocityZ);
            VelocityX *= 0.5D;
            VelocityY *= 0.5D;
            VelocityZ *= 0.5D;
        }
        else
        {
            float friction = 0.91F;
            if (OnGround)
            {
                friction = 546.0F * 0.1F * 0.1F * 0.1F;
                int groundBlockId = World.Reader.GetBlockId(MathHelper.Floor(X), MathHelper.Floor(BoundingBox.MinY) - 1, MathHelper.Floor(Z));
                if (groundBlockId > 0)
                {
                    friction = Block.Blocks[groundBlockId].Slipperiness * 0.91F;
                }
            }

            float accelerationFactor = 0.16277136F / (friction * friction * friction);
            MoveNonSolid(strafe, forward, OnGround ? 0.1F * accelerationFactor : 0.02F);
            friction = 0.91F;
            if (OnGround)
            {
                friction = 546.0F * 0.1F * 0.1F * 0.1F;
                int groundBlockId = World.Reader.GetBlockId(MathHelper.Floor(X), MathHelper.Floor(BoundingBox.MinY) - 1, MathHelper.Floor(Z));
                if (groundBlockId > 0)
                {
                    friction = Block.Blocks[groundBlockId].Slipperiness * 0.91F;
                }
            }

            Move(VelocityX, VelocityY, VelocityZ);
            VelocityX *= (double)friction;
            VelocityY *= (double)friction;
            VelocityZ *= (double)friction;
        }

        LastWalkAnimationSpeed = WalkAnimationSpeed;
        double dx = X - PrevX;
        double dy = Z - PrevZ;
        float distanceMoved = MathHelper.Sqrt(dx * dx + dy * dy) * 4.0F;
        if (distanceMoved > 1.0F)
        {
            distanceMoved = 1.0F;
        }

        WalkAnimationSpeed += (distanceMoved - WalkAnimationSpeed) * 0.4F;
        AnimationPhase += WalkAnimationSpeed;
    }

    public override bool isOnLadder()
    {
        return false;
    }
}
