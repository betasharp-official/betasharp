using BetaSharp.Blocks;
using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Items;

internal class ItemBucket : Item
{

    private int isFull;

    public ItemBucket(int id, int isFull) : base(id)
    {
        maxCount = 1;
        this.isFull = isFull;
    }

    public override ItemStack use(ItemStack itemStack, IWorldContext world, EntityPlayer entityPlayer)
    {
        float partialTick = 1.0F;
        float pitch = entityPlayer.PrevPitch + (entityPlayer.Pitch - entityPlayer.PrevPitch) * partialTick;
        float yaw = entityPlayer.PrevYaw + (entityPlayer.Yaw - entityPlayer.PrevYaw) * partialTick;
        double x = entityPlayer.PrevX + (entityPlayer.X - entityPlayer.PrevX) * (double)partialTick;
        double y = entityPlayer.PrevY + (entityPlayer.Y - entityPlayer.PrevY) * (double)partialTick + 1.62D - (double)entityPlayer.StandingEyeHeight;
        double z = entityPlayer.PrevZ + (entityPlayer.Z - entityPlayer.PrevZ) * (double)partialTick;
        Vec3D rayStart = new Vec3D(x, y, z);
        float cosYaw = MathHelper.Cos(-yaw * ((float)Math.PI / 180.0F) - (float)Math.PI);
        float sinYaw = MathHelper.Sin(-yaw * ((float)Math.PI / 180.0F) - (float)Math.PI);
        float cosPitch = -MathHelper.Cos(-pitch * ((float)Math.PI / 180.0F));
        float sinPitch = MathHelper.Sin(-pitch * ((float)Math.PI / 180.0F));
        float dirX = sinYaw * cosPitch;
        float dirZ = cosYaw * cosPitch;
        double reachDistance = 5.0D;
        Vec3D rayEnd = rayStart + new Vec3D((double)dirX * reachDistance, (double)sinPitch * reachDistance, (double)dirZ * reachDistance);
        HitResult hitResult = world.Reader.Raycast(rayStart, rayEnd, isFull == 0);
        if (hitResult.Type == HitResultType.MISS)
        {
            return itemStack;
        }
        else
        {
            if (hitResult.Type == HitResultType.TILE)
            {
                int hitX = hitResult.BlockX;
                int hitY = hitResult.BlockY;
                int hitZ = hitResult.BlockZ;
                if (!world.CanInteract(entityPlayer, hitX, hitY, hitZ))
                {
                    return itemStack;
                }

                if (isFull == 0)
                {
                    if (world.Reader.GetMaterial(hitX, hitY, hitZ) == Material.Water && world.Reader.GetBlockMeta(hitX, hitY, hitZ) == 0)
                    {
                        world.Writer.SetBlock(hitX, hitY, hitZ, 0);
                        return new ItemStack(Item.WaterBucket);
                    }

                    if (world.Reader.GetMaterial(hitX, hitY, hitZ) == Material.Lava && world.Reader.GetBlockMeta(hitX, hitY, hitZ) == 0)
                    {
                        world.Writer.SetBlock(hitX, hitY, hitZ, 0);
                        return new ItemStack(Item.LavaBucket);
                    }
                }
                else
                {
                    if (isFull < 0)
                    {
                        return new ItemStack(Item.Bucket);
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
                        if (world.Dimension.EvaporatesWater && isFull == Block.FlowingWater.id)
                        {
                            world.Broadcaster.PlaySoundAtPos(x + 0.5D, y + 0.5D, z + 0.5D, "random.fizz", 0.5F, 2.6F + (world.Random.NextFloat() - world.Random.NextFloat()) * 0.8F);

                            for (int particleIndex = 0; particleIndex < 8; ++particleIndex)
                            {
                                world.Broadcaster.AddParticle("largesmoke", hitX + Random.Shared.NextDouble(), hitY + Random.Shared.NextDouble(), hitZ + Random.Shared.NextDouble(), 0.0D, 0.0D, 0.0D);
                            }
                        }
                        else
                        {
                            world.Writer.SetBlock(hitX, hitY, hitZ, isFull, 0);
                        }

                        return new ItemStack(Item.Bucket);
                    }
                }
            }
            else if (isFull == 0 && hitResult.Entity is EntityCow)
            {
                return new ItemStack(Item.MilkBucket);
            }

            return itemStack;
        }
    }
}
