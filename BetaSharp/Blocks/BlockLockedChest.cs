using BetaSharp.Blocks.Materials;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockLockedChest : Block
{
    public BlockLockedChest(int id) : base(id, Material.Wood) => TextureId = 26;

    public override int GetTextureId(IBlockReader iBlockReader, int x, int y, int z, Side side)
    {
        if (side is Side.Up or Side.Down)
        {
            return TextureId - 1;
        }

        int var6 = iBlockReader.GetBlockId(x, y, z - 1);
        int var7 = iBlockReader.GetBlockId(x, y, z + 1);
        int var8 = iBlockReader.GetBlockId(x - 1, y, z);
        int var9 = iBlockReader.GetBlockId(x + 1, y, z);
        Side var10 = Side.South;
        if (BlocksOpaque[var6] && !BlocksOpaque[var7])
        {
            var10 = Side.South;
        }

        if (BlocksOpaque[var7] && !BlocksOpaque[var6])
        {
            var10 = Side.North;
        }

        if (BlocksOpaque[var8] && !BlocksOpaque[var9])
        {
            var10 = Side.East;
        }

        if (BlocksOpaque[var9] && !BlocksOpaque[var8])
        {
            var10 = Side.West;
        }

        return side == var10 ? TextureId + 1 : TextureId;
    }

    public override int GetTexture(Side side) => side switch
    {
        Side.Up or Side.Down => TextureId - 1,
        Side.South => TextureId + 1,
        _ => TextureId
    };

    public override bool CanPlaceAt(CanPlaceAtContext context) => true;

    public override void OnTick(OnTickEvent @event) => @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
}
