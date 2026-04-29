using BetaSharp.Blocks;
using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Items;

internal class ItemBucket : Item
{
    private const float PartialTick = 1.0F;
    private readonly int _isFull;

    public ItemBucket(int id, int isFull) : base(id)
    {
        MaxCount = 1;
        _isFull = isFull;
    }

    public override ItemStack Use(ItemStack itemStack, IWorldContext world, EntityPlayer entityPlayer)
    {
        float pitch = entityPlayer.PrevPitch + (entityPlayer.Pitch - entityPlayer.PrevPitch) * PartialTick;
        float yaw = entityPlayer.PrevYaw + (entityPlayer.Yaw - entityPlayer.PrevYaw) * PartialTick;
        double x = entityPlayer.PrevX + (entityPlayer.X - entityPlayer.PrevX) * PartialTick;
        double y = entityPlayer.PrevY + (entityPlayer.Y - entityPlayer.PrevY) * PartialTick + 1.62D - entityPlayer.StandingEyeHeight;
        double z = entityPlayer.PrevZ + (entityPlayer.Z - entityPlayer.PrevZ) * PartialTick;
        Vec3D rayStart = new(x, y, z);
        float cosYaw = MathHelper.Cos(-yaw * ((float)Math.PI / 180.0F) - (float)Math.PI);
        float sinYaw = MathHelper.Sin(-yaw * ((float)Math.PI / 180.0F) - (float)Math.PI);
        float cosPitch = -MathHelper.Cos(-pitch * ((float)Math.PI / 180.0F));
        float sinPitch = MathHelper.Sin(-pitch * ((float)Math.PI / 180.0F));
        float dirX = sinYaw * cosPitch;
        float dirZ = cosYaw * cosPitch;
        double reachDistance = 5.0D;
        Vec3D rayEnd = rayStart + new Vec3D(dirX * reachDistance, sinPitch * reachDistance, dirZ * reachDistance);
        HitResult hitResult = world.Reader.Raycast(rayStart, rayEnd, _isFull == 0);
        if (hitResult.Type == HitResultType.MISS)
        {
            return itemStack;
        }

        if (hitResult.Type == HitResultType.TILE)
        {
            int hitX = hitResult.BlockX;
            int hitY = hitResult.BlockY;
            int hitZ = hitResult.BlockZ;
            if (!world.CanInteract(entityPlayer, hitX, hitY, hitZ))
            {
                return itemStack;
            }

            if (_isFull == 0)
            {
                if (world.Reader.GetMaterial(hitX, hitY, hitZ) == Material.Water && world.Reader.GetBlockMeta(hitX, hitY, hitZ) == 0)
                {
                    world.Writer.SetBlock(hitX, hitY, hitZ, 0);
                    return new ItemStack(WaterBucket);
                }

                if (world.Reader.GetMaterial(hitX, hitY, hitZ) == Material.Lava && world.Reader.GetBlockMeta(hitX, hitY, hitZ) == 0)
                {
                    world.Writer.SetBlock(hitX, hitY, hitZ, 0);
                    return new ItemStack(LavaBucket);
                }
            }
            else
            {
                if (_isFull < 0)
                {
                    return new ItemStack(Bucket);
                }

                if (hitResult.Side == 0)
                {
                    --hitY;
                }

                if (hitResult.Side == 1)
                {
                    ++hitY;
                }

                if (hitResult.Side == 2)
                {
                    --hitZ;
                }

                if (hitResult.Side == 3)
                {
                    ++hitZ;
                }

                if (hitResult.Side == 4)
                {
                    --hitX;
                }

                if (hitResult.Side == 5)
                {
                    ++hitX;
                }

                if (world.Reader.IsAir(hitX, hitY, hitZ) || !world.Reader.GetMaterial(hitX, hitY, hitZ).IsSolid)
                {
                    if (world.Dimension.EvaporatesWater && _isFull == Block.FlowingWater.id)
                    {
                        world.Broadcaster.PlaySoundAtPos(x + 0.5D, y + 0.5D, z + 0.5D, "random.fizz", 0.5F, 2.6F + (world.Random.NextFloat() - world.Random.NextFloat()) * 0.8F);

                        for (int particleIndex = 0; particleIndex < 8; ++particleIndex)
                        {
                            world.Broadcaster.AddParticle("largesmoke", hitX + Random.Shared.NextDouble(), hitY + Random.Shared.NextDouble(), hitZ + Random.Shared.NextDouble(), 0.0D, 0.0D, 0.0D);
                        }
                    }
                    else
                    {
                        world.Writer.SetBlock(hitX, hitY, hitZ, _isFull, 0);
                    }

                    return new ItemStack(Bucket);
                }
            }
        }

        if (_isFull == 0 && hitResult.Entity is EntityCow)
        {
            return new ItemStack(MilkBucket);
        }

        return itemStack;
    }
}
