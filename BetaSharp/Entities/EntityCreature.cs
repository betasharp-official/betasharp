using BetaSharp.PathFinding;
using BetaSharp.Profiling;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public abstract class EntityCreature : EntityLiving
{
    private PathEntity pathToEntity;
    protected Entity playerToAttack;
    protected bool hasAttacked;

    public EntityCreature(IWorldContext world) : base(world)
    {
    }

    protected virtual bool isMovementCeased()
    {
        return false;
    }

    public override void tickLiving()
    {
        hasAttacked = isMovementCeased();
        float range = 16.0F;
        if (playerToAttack == null)
        {
            playerToAttack = findPlayerToAttack();
            if (playerToAttack != null)
            {
                pathToEntity = world.Pathing.findPath(this, playerToAttack, range);
            }
        }
        else if (!playerToAttack.isAlive())
        {
            playerToAttack = null;
        }
        else
        {
            float distance = playerToAttack.getDistance(this);
            if (canSee(playerToAttack))
            {
                attackEntity(playerToAttack, distance);
            }
            else
            {
                attackBlockedEntity(playerToAttack, distance);
            }
        }

        if (hasAttacked || playerToAttack == null || pathToEntity != null && random.NextInt(20) != 0)
        {
            if (!hasAttacked && (pathToEntity == null && random.NextInt(80) == 0 || random.NextInt(80) == 0))
            {
                findRandomWanderTarget();
            }
        }
        else
        {
            pathToEntity = world.Pathing.findPath(this, playerToAttack, range);
        }

        int floorY = MathHelper.Floor(boundingBox.MinY + 0.5D);
        bool isInWater = base.isInWater();
        bool isTouchingLava = base.isTouchingLava();
        pitch = 0.0F;
        if (pathToEntity != null && random.NextInt(100) != 0)
        {
            Vec3D? pos = pathToEntity.GetPosition(this);
            double distance = (double)(width * 2.0F);

            while (pos != null && pos.Value.squareDistanceTo(new Vec3D(x, pos.Value.y, z)) < distance * distance)
            {
                pathToEntity.IncrementPathIndex();
                if (pathToEntity.IsFinished)
                {
                    pos = null;
                    pathToEntity = null;
                }
                else
                {
                    pos = pathToEntity.GetPosition(this);
                }
            }

            jumping = false;
            if (pos != null)
            {
                double dx = pos.Value.x - x;
                double dz = pos.Value.z - z;
                double verticalOffset = pos.Value.y - (double)floorY;
                float targetYaw = (float)(System.Math.Atan2(dz, dx) * 180.0D / (double)((float)System.Math.PI)) - 90.0F;
                float yawDelta = targetYaw - yaw;

                for (forwardSpeed = movementSpeed; yawDelta < -180.0F; yawDelta += 360.0F)
                {
                }

                while (yawDelta >= 180.0F)
                {
                    yawDelta -= 360.0F;
                }

                if (yawDelta > 30.0F)
                {
                    yawDelta = 30.0F;
                }

                if (yawDelta < -30.0F)
                {
                    yawDelta = -30.0F;
                }

                yaw += yawDelta;
                if (hasAttacked && playerToAttack != null)
                {
                    double targetDeltaX = playerToAttack.x - x;
                    double targetDeltaZ = playerToAttack.z - z;
                    float previousYaw = yaw;
                    yaw = (float)(System.Math.Atan2(targetDeltaZ, targetDeltaX) * 180.0D / (double)((float)System.Math.PI)) - 90.0F;
                    yawDelta = (previousYaw - yaw + 90.0F) * (float)System.Math.PI / 180.0F;
                    sidewaysSpeed = -MathHelper.Sin(yawDelta) * forwardSpeed * 1.0F;
                    forwardSpeed = MathHelper.Cos(yawDelta) * forwardSpeed * 1.0F;
                }

                if (verticalOffset > 0.0D)
                {
                    jumping = true;
                }
            }

            if (playerToAttack != null)
            {
                faceEntity(playerToAttack, 30.0F, 30.0F);
            }

            if (horizontalCollison && !hasPath())
            {
                jumping = true;
            }

            if (random.NextFloat() < 0.8F && (isInWater || isTouchingLava))
            {
                jumping = true;
            }

        }
        else
        {
            base.tickLiving();
            pathToEntity = null;
        }
    }

    protected void findRandomWanderTarget()
    {
        bool foundWanderTarget = false;
        int bestX = -1;
        int bestY = -1;
        int bestZ = -1;
        float bestCost = -99999.0F;

        for (int _ = 0; _ < 10; ++_)
        {
            int floorX = MathHelper.Floor(x + (double)random.NextInt(13) - 6.0D);
            int floorY = MathHelper.Floor(y + (double)random.NextInt(7) - 3.0D);
            int floorZ = MathHelper.Floor(z + (double)random.NextInt(13) - 6.0D);
            float cost = getBlockPathWeight(floorX, floorY, floorZ);
            if (cost > bestCost)
            {
                bestCost = cost;
                bestX = floorX;
                bestY = floorY;
                bestZ = floorZ;
                foundWanderTarget = true;
            }
        }

        if (foundWanderTarget)
        {
            pathToEntity = world.Pathing.findPath(this, bestX, bestY, bestZ, 10.0F);
        }
    }

    protected virtual void attackEntity(Entity entity, float distance)
    {
    }

    protected virtual void attackBlockedEntity(Entity entity, float distance)
    {
    }

    protected virtual float getBlockPathWeight(int x, int y, int z)
    {
        return 0.0F;
    }

    protected virtual Entity findPlayerToAttack()
    {
        return null;
    }

    public override bool canSpawn()
    {
        int floorX = MathHelper.Floor(x);
        int floorY = MathHelper.Floor(boundingBox.MinY);
        int floorZ = MathHelper.Floor(z);
        return base.canSpawn() && getBlockPathWeight(floorX, floorY, floorZ) >= 0.0F;
    }

    public bool hasPath()
    {
        return pathToEntity != null;
    }

    internal void setPathToEntity(PathEntity pathToEntity)
    {
        this.pathToEntity = pathToEntity;
    }

    public Entity getTarget()
    {
        return playerToAttack;
    }

    public void setTarget(Entity playerToAttack)
    {
        this.playerToAttack = playerToAttack;
    }
}
