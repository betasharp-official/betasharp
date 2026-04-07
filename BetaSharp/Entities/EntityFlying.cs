using BetaSharp.Blocks;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public abstract class EntityFlying : EntityLiving
{
    public EntityFlying(IWorldContext world) : base(world)
    {
    }

    protected override void onLanding(float fallDistance)
    {
    }

    public override void travel(float strafe, float forward)
    {
        if (isInWater())
        {
            moveNonSolid(strafe, forward, 0.02F);
            move(velocityX, velocityY, velocityZ);
            velocityX *= (double)0.8F;
            velocityY *= (double)0.8F;
            velocityZ *= (double)0.8F;
        }
        else if (isTouchingLava())
        {
            moveNonSolid(strafe, forward, 0.02F);
            move(velocityX, velocityY, velocityZ);
            velocityX *= 0.5D;
            velocityY *= 0.5D;
            velocityZ *= 0.5D;
        }
        else
        {
            float friction = 0.91F;
            if (onGround)
            {
                friction = 546.0F * 0.1F * 0.1F * 0.1F;
                int groundBlockId = world.Reader.GetBlockId(MathHelper.Floor(x), MathHelper.Floor(boundingBox.MinY) - 1, MathHelper.Floor(z));
                if (groundBlockId > 0)
                {
                    friction = Block.Blocks[groundBlockId].Slipperiness * 0.91F;
                }
            }

            float accelerationFactor = 0.16277136F / (friction * friction * friction);
            moveNonSolid(strafe, forward, onGround ? 0.1F * accelerationFactor : 0.02F);
            friction = 0.91F;
            if (onGround)
            {
                friction = 546.0F * 0.1F * 0.1F * 0.1F;
                int groundBlockId = world.Reader.GetBlockId(MathHelper.Floor(x), MathHelper.Floor(boundingBox.MinY) - 1, MathHelper.Floor(z));
                if (groundBlockId > 0)
                {
                    friction = Block.Blocks[groundBlockId].Slipperiness * 0.91F;
                }
            }

            move(velocityX, velocityY, velocityZ);
            velocityX *= (double)friction;
            velocityY *= (double)friction;
            velocityZ *= (double)friction;
        }

        lastWalkAnimationSpeed = walkAnimationSpeed;
        double dx = x - prevX;
        double dy = z - prevZ;
        float distanceMoved = MathHelper.Sqrt(dx * dx + dy * dy) * 4.0F;
        if (distanceMoved > 1.0F)
        {
            distanceMoved = 1.0F;
        }

        walkAnimationSpeed += (distanceMoved - walkAnimationSpeed) * 0.4F;
        animationPhase += walkAnimationSpeed;
    }

    public override bool isOnLadder()
    {
        return false;
    }
}
