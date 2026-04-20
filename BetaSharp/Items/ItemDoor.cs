using BetaSharp.Blocks;
using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Items;

internal class ItemDoor : Item
{

    private Material doorMaterial;

    public ItemDoor(int id, Material material) : base(id)
    {
        doorMaterial = material;
        maxCount = 1;
    }

    public override bool useOnBlock(ItemStack itemStack, EntityPlayer entityPlayer, IWorldContext world, int x, int y, int z, int side)
    {
        if (side != 1) return false;
        y++;

        int blockId = doorMaterial == Material.Wood ? Block.Door.ID : Block.IronDoor.ID;
        if (!Block.Blocks[blockId].CanPlaceAt(new CanPlaceAtContext(world, 0, x, y, z))) return false;

        int facing = MathHelper.Floor((entityPlayer.Yaw + 180.0f) * 4.0f / 360.0f - 0.5f) & 3;
        int offsetX = 0;
        int offsetZ = 0;
        if (facing == 0) offsetZ = 1;
        if (facing == 1) offsetX = -1;
        if (facing == 2) offsetZ = -1;
        if (facing == 3) offsetX = 1;

        int leftSolid = (world.Reader.ShouldSuffocate(x - offsetX, y, z - offsetZ) ? 1 : 0) +
                        (world.Reader.ShouldSuffocate(x - offsetX, y + 1, z - offsetZ) ? 1 : 0);
        int rightSolid = (world.Reader.ShouldSuffocate(x + offsetX, y, z + offsetZ) ? 1 : 0) +
                         (world.Reader.ShouldSuffocate(x + offsetX, y + 1, z + offsetZ) ? 1 : 0);
        bool leftHasDoor = world.Reader.GetBlockId(x - offsetX, y, z - offsetZ) == blockId ||
                           world.Reader.GetBlockId(x - offsetX, y + 1, z - offsetZ) == blockId;
        bool rightHasDoor = world.Reader.GetBlockId(x + offsetX, y, z + offsetZ) == blockId ||
                            world.Reader.GetBlockId(x + offsetX, y + 1, z + offsetZ) == blockId;
        bool mirror = (leftHasDoor && !rightHasDoor) || (rightSolid > leftSolid);

        if (mirror)
        {
            facing = (facing - 1) & 3;
            facing += 4;
        }

        world.Writer.SetBlock(x, y, z, blockId, facing);
        world.Writer.SetBlock(x, y + 1, z, blockId, facing + 8);
        world.Broadcaster.NotifyNeighbors(x, y, z, blockId);
        world.Broadcaster.NotifyNeighbors(x, y + 1, z, blockId);
        itemStack.ConsumeItem(entityPlayer);
        return true;
    }
}
