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

    private static Side GetFrontFace(int meta) => meta switch
    {
        0 => Side.South,
        1 => Side.West,
        2 => Side.North,
        3 => Side.East,
        _ => Side.South
    };

    public override int GetTexture(Side side, int meta)
    {
        if (side is Side.Up or Side.Down) return TextureId;

        int faceTexture = TextureId + 1 + 16;
        if (_lit) ++faceTexture;
        return side == GetFrontFace(meta) ? faceTexture : TextureId + 16;
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
        return (blockId == 0 || Blocks[blockId]!.Material.IsReplaceable) && evt.World.Reader.ShouldSuffocate(evt.X, evt.Y - 1, evt.Z);
    }

    public override void OnPlaced(OnPlacedEvent @event)
    {
        if (@event.Placer == null) return;

        int direction = MathHelper.Floor(@event.Placer.yaw * 4.0F / 360.0F + 2.5D) & 3;
        @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, direction);
    }
}
