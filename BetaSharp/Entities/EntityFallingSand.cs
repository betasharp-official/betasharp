using BetaSharp.Blocks;
using BetaSharp.NBT;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public sealed class EntityFallingSand : Entity
{
    private int _fallTime;
    public int BlockId;

    public EntityFallingSand(IWorldContext world) : base(world)
    {
    }

    public EntityFallingSand(IWorldContext world, double x, double y, double z, int blockId) : base(world)
    {
        BlockId = blockId;
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

    public override EntityType Type => EntityRegistry.FallingSand;

    public override bool HasCollision => !Dead;

    protected override bool BypassesSteppingEffects() => false;

    public override void Tick()
    {
        if (BlockId == 0)
        {
            MarkDead();
            return;
        }

        PrevX = X;
        PrevY = Y;
        PrevZ = Z;
        ++_fallTime;
        VelocityY -= 0.04F;
        Move(VelocityX, VelocityY, VelocityZ);
        VelocityX *= 0.98F;
        VelocityY *= 0.98F;
        VelocityZ *= 0.98F;
        int floorX = MathHelper.Floor(X);
        int floorY = MathHelper.Floor(Y);
        int floorZ = MathHelper.Floor(Z);
        if (World.Reader.GetBlockId(floorX, floorY, floorZ) == BlockId)
        {
            World.Writer.SetBlock(floorX, floorY, floorZ, 0);
        }

        if (OnGround)
        {
            VelocityX *= 0.7F;
            VelocityZ *= 0.7F;
            VelocityY *= -0.5D;
            MarkDead();
            if ((!Block.Blocks[BlockId].canPlaceAt(new CanPlaceAtContext(World, 0, floorX, floorY, floorZ)) || BlockSand.canFallThrough(new OnTickEvent(World, floorX, floorY - 1, floorZ, 0, BlockId)) ||
                 !World.Writer.SetBlock(floorX, floorY, floorZ, BlockId)) && !World.IsRemote)
            {
                DropItem(BlockId, 1);
            }
        }
        else if (_fallTime > 100 && !World.IsRemote)
        {
            DropItem(BlockId, 1);
            MarkDead();
        }
    }

    protected override void WriteNbt(NBTTagCompound nbt) => nbt.SetByte("Tile", (sbyte)BlockId);

    protected override void ReadNbt(NBTTagCompound nbt) => BlockId = nbt.GetByte("Tile") & 255;

    public override float GetShadowRadius() => 0.0F;
}
