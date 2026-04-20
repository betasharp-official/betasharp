using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;

namespace BetaSharp.Blocks;

internal class BlockPumpkin : Block
{
    private readonly bool _lit;

    public BlockPumpkin(int id, int textureId, bool lit) : base(id, Material.Pumpkin)
    {
        TextureId = textureId;
        SetTickRandomly(true);
        _lit = lit;
    }

    public override int GetTexture(Side side, int meta)
    {
        if (side is Side.Up or Side.Down)
        {
            return TextureId;
        }

        int faceTexture = TextureId + 1 + 16;
        if (_lit)
        {
            ++faceTexture;
        }

        return meta == 2 && side == Side.North ? faceTexture :
            meta == 3 && side == Side.East ? faceTexture :
            meta == 0 && side == Side.South ? faceTexture :
            meta == 1 && side == Side.West ? faceTexture : TextureId + 16;
    }

    public override int GetTexture(Side side) => side switch
    {
        Side.Up or Side.Down => TextureId,
        Side.South => TextureId + 1 + 16,
        _ => TextureId + 16
    };

    public override bool CanPlaceAt(CanPlaceAtContext evt)
    {
        int blockId = evt.World.Reader.GetBlockId(evt.X, evt.Y, evt.Z);
        return (blockId == 0 || Blocks[blockId].Material.IsReplaceable) && evt.World.Reader.ShouldSuffocate(evt.X, evt.Y - 1, evt.Z);
    }

    public override void OnPlaced(OnPlacedEvent @event)
    {
        if (@event.Placer == null) return;
        int direction = MathHelper.Floor(@event.Placer.Yaw * 4.0F / 360.0F + 2.5D) & 3;
        @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, direction);
    }
}
