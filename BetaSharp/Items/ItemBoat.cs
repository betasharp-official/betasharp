using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Items;

internal class ItemBoat : Item
{

    public ItemBoat(int id) : base(id)
    {
        maxCount = 1;
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
        double rayLength = 5.0D;
        Vec3D rayEnd = rayStart + new Vec3D((double)dirX * rayLength, (double)sinPitch * rayLength, (double)dirZ * rayLength);
        HitResult hitResult = world.Reader.Raycast(rayStart, rayEnd, true);
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
                if (!world.IsRemote)
                {
                    if (world.Reader.GetBlockId(hitX, hitY, hitZ) == Block.Snow.ID)
                    {
                        --hitY;
                    }

                    world.SpawnEntity(new EntityBoat(world, (double)((float)hitX + 0.5F), (double)((float)hitY + 1.0F), (double)((float)hitZ + 0.5F)));
                }

                itemStack.ConsumeItem(entityPlayer);
            }

            return itemStack;
        }
    }
}
