using BetaSharp.Blocks.Materials;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockLockedChest : Block
{
    public BlockLockedChest(int id) : base(id, Material.Wood) => textureId = 26;

    public override int getTextureId(IBlockReader iBlockReader, int x, int y, int z, int side)
    {
        if (side == 1)
        {
            return textureId - 1;
        }

        if (side == 0)
        {
            return textureId - 1;
        }

        int var6 = iBlockReader.GetBlockId(x, y, z - 1);
        int var7 = iBlockReader.GetBlockId(x, y, z + 1);
        int var8 = iBlockReader.GetBlockId(x - 1, y, z);
        int var9 = iBlockReader.GetBlockId(x + 1, y, z);
        sbyte var10 = 3;
        if (BlocksOpaque[var6] && !BlocksOpaque[var7])
        {
            var10 = 3;
        }

        if (BlocksOpaque[var7] && !BlocksOpaque[var6])
        {
            var10 = 2;
        }

        if (BlocksOpaque[var8] && !BlocksOpaque[var9])
        {
            var10 = 5;
        }

        if (BlocksOpaque[var9] && !BlocksOpaque[var8])
        {
            var10 = 4;
        }

        return side == var10 ? textureId + 1 : textureId;
    }

    public override int getTexture(int side) => side == 1 ? textureId - 1 : side == 0 ? textureId - 1 : side == 3 ? textureId + 1 : textureId;

    public override bool canPlaceAt(CanPlaceAtContext context) => true;

    public override void onTick(OnTickEvent @event) => @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
}
