using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;

namespace BetaSharp.Blocks;

internal class BlockPumpkin : Block
{
    private readonly bool lit;

    public BlockPumpkin(int id, int textureId, bool lit) : base(id, Material.Pumpkin)
    {
        this.textureId = textureId;
        setTickRandomly(true);
        this.lit = lit;
    }

    public override int getTexture(int side, int meta)
    {
        if (side == 1)
        {
            return textureId;
        }

        if (side == 0)
        {
            return textureId;
        }

        int faceTexture = textureId + 1 + 16;
        if (lit)
        {
            ++faceTexture;
        }

        return meta == 2 && side == 2 ? faceTexture :
            meta == 3 && side == 5 ? faceTexture :
            meta == 0 && side == 3 ? faceTexture :
            meta == 1 && side == 4 ? faceTexture : textureId + 16;
    }

    public override int getTexture(int side) => side == 1 ? textureId : side == 0 ? textureId : side == 3 ? textureId + 1 + 16 : textureId + 16;

    public override bool canPlaceAt(CanPlaceAtContext evt)
    {
        int blockId = evt.World.Reader.GetBlockId(evt.X, evt.Y, evt.Z);
        return (blockId == 0 || Blocks[blockId].material.IsReplaceable) && evt.World.Reader.ShouldSuffocate(evt.X, evt.Y - 1, evt.Z);
    }

    public override void onPlaced(OnPlacedEvent @event)
    {
        if (@event.Placer == null) return;
        int direction = MathHelper.Floor(@event.Placer.yaw * 4.0F / 360.0F + 2.5D) & 3;
        @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, direction);
    }
}
