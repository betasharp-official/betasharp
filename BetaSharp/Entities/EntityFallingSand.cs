using BetaSharp.Blocks;
using BetaSharp.NBT;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntityFallingSand : Entity
{
    public override EntityType Type => EntityRegistry.FallingSand;
    public int blockId;
    public int fallTime;

    public EntityFallingSand(IWorldContext world) : base(world)
    {
    }

    public EntityFallingSand(IWorldContext world, double x, double y, double z, int blockId) : base(world)
    {
        this.blockId = blockId;
        PreventEntitySpawning = true;
        SetBoundingBoxSpacing(0.98F, 0.98F);
        StandingEyeHeight = Height / 2.0F;
        SetPosition(x, y, z);
        VelocityX = 0.0D;
        VelocityY = 0.0D;
        VelocityZ = 0.0D;
        PrevX = x;
        PrevY = y;
        PrevZ = z;
    }

    protected override bool BypassesSteppingEffects()
    {
        return false;
    }


    public override bool IsCollidable()
    {
        return !Dead;
    }

    public override void Tick()
    {
        if (blockId == 0)
        {
            MarkDead();
        }
        else
        {
            PrevX = X;
            PrevY = Y;
            PrevZ = Z;
            ++fallTime;
            VelocityY -= (double)0.04F;
            Move(VelocityX, VelocityY, VelocityZ);
            VelocityX *= (double)0.98F;
            VelocityY *= (double)0.98F;
            VelocityZ *= (double)0.98F;
            int floorX = MathHelper.Floor(X);
            int floorY = MathHelper.Floor(Y);
            int floorZ = MathHelper.Floor(Z);
            if (World.Reader.GetBlockId(floorX, floorY, floorZ) == blockId)
            {
                World.Writer.SetBlock(floorX, floorY, floorZ, 0);
            }

            if (OnGround)
            {
                VelocityX *= (double)0.7F;
                VelocityZ *= (double)0.7F;
                VelocityY *= -0.5D;
                MarkDead();
                if ((!Block.Blocks[blockId].canPlaceAt(new CanPlaceAtContext(World, 0, floorX, floorY, floorZ)) || BlockSand.canFallThrough(new OnTickEvent(World, floorX, floorY - 1, floorZ, 0, blockId)) || !World.Writer.SetBlock(floorX, floorY, floorZ, blockId)) && !World.IsRemote)
                {
                    DropItem(blockId, 1);
                }
            }
            else if (fallTime > 100 && !World.IsRemote)
            {
                DropItem(blockId, 1);
                MarkDead();
            }

        }
    }

    public override void WriteNbt(NBTTagCompound nbt)
    {
        nbt.SetByte("Tile", (sbyte)blockId);
    }

    public override void ReadNbt(NBTTagCompound nbt)
    {
        blockId = nbt.GetByte("Tile") & 255;
    }

    public override float GetShadowRadius()
    {
        return 0.0F;
    }

    public IWorldContext getWorld()
    {
        return World;
    }
}
