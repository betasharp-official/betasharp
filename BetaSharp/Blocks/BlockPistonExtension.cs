using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

public class BlockPistonExtension : Block
{
    private const int PistonHeadSprite = -1;

    public BlockPistonExtension(int id, int textureId) : base(id, textureId, Material.Piston)
    {
        SetSoundGroup(SoundStoneFootstep);
        SetHardness(0.5F);
    }

    public override void OnBreak(OnBreakEvent @event)
    {
        base.OnBreak(@event);
        (int x, int y, int z) = (@event.X, @event.Y, @event.Z);
        int blockMeta = @event.World.Reader.GetBlockMeta(x, y, z);
        Side oppositeFace = GetFacing(blockMeta).OppositeFace();

        x += PistonConstants.HeadOffsetX[oppositeFace.ToInt()];
        y += PistonConstants.HeadOffsetY[oppositeFace.ToInt()];
        z += PistonConstants.HeadOffsetZ[oppositeFace.ToInt()];

        int blockId = @event.World.Reader.GetBlockId(x, y, z);
        if (blockId != Piston.ID && blockId != StickyPiston.ID)
        {
            return;
        }

        int meta = @event.World.Reader.GetBlockMeta(x, y, z);
        if (!BlockPistonBase.IsExtended(meta))
        {
            return;
        }

        Blocks[blockId].DropStacks(new OnDropEvent(@event.World, x, y, z, meta));
        @event.World.Writer.SetBlock(x, y, z, 0);
    }

    public override int GetTexture(Side side, int meta)
    {
        Side facing = GetFacing(meta);
        if (side == facing)
        {
            return PistonHeadSprite >= 0 ? PistonHeadSprite : (meta & 8) != 0 ? TextureId - 1 : TextureId;
        }

        return side == facing.OppositeFace() ? 107 : 108;
    }

    public override BlockRendererType GetRenderType() => BlockRendererType.PistonExtension;

    public override bool IsOpaque() => false;

    public override bool IsFullCube() => false;

    public override bool CanPlaceAt(CanPlaceAtContext context) => false;

    public override int GetDroppedItemCount() => 0;

    public override void AddIntersectingBoundingBox(IBlockReader reader, EntityManager entities, int x, int y, int z, Box box, List<Box> boxes)
    {
        int facing = reader.GetBlockMeta(x, y, z);
        switch (GetFacing(facing))
        {
            case Side.Down:
                SetBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 0.25F, 1.0F);
                base.AddIntersectingBoundingBox(reader, entities, x, y, z, box, boxes);
                SetBoundingBox(6.0F / 16.0F, 0.25F, 6.0F / 16.0F, 10.0F / 16.0F, 1.0F, 10.0F / 16.0F);
                base.AddIntersectingBoundingBox(reader, entities, x, y, z, box, boxes);
                break;
            case Side.Up:
                SetBoundingBox(0.0F, 12.0F / 16.0F, 0.0F, 1.0F, 1.0F, 1.0F);
                base.AddIntersectingBoundingBox(reader, entities, x, y, z, box, boxes);
                SetBoundingBox(6.0F / 16.0F, 0.0F, 6.0F / 16.0F, 10.0F / 16.0F, 12.0F / 16.0F, 10.0F / 16.0F);
                base.AddIntersectingBoundingBox(reader, entities, x, y, z, box, boxes);
                break;
            case Side.North:
                SetBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 0.25F);
                base.AddIntersectingBoundingBox(reader, entities, x, y, z, box, boxes);
                SetBoundingBox(0.25F, 6.0F / 16.0F, 0.25F, 12.0F / 16.0F, 10.0F / 16.0F, 1.0F);
                base.AddIntersectingBoundingBox(reader, entities, x, y, z, box, boxes);
                break;
            case Side.South:
                SetBoundingBox(0.0F, 0.0F, 12.0F / 16.0F, 1.0F, 1.0F, 1.0F);
                base.AddIntersectingBoundingBox(reader, entities, x, y, z, box, boxes);
                SetBoundingBox(0.25F, 6.0F / 16.0F, 0.0F, 12.0F / 16.0F, 10.0F / 16.0F, 12.0F / 16.0F);
                base.AddIntersectingBoundingBox(reader, entities, x, y, z, box, boxes);
                break;
            case Side.West:
                SetBoundingBox(0.0F, 0.0F, 0.0F, 0.25F, 1.0F, 1.0F);
                base.AddIntersectingBoundingBox(reader, entities, x, y, z, box, boxes);
                SetBoundingBox(6.0F / 16.0F, 0.25F, 0.25F, 10.0F / 16.0F, 12.0F / 16.0F, 1.0F);
                base.AddIntersectingBoundingBox(reader, entities, x, y, z, box, boxes);
                break;
            case Side.East:
                SetBoundingBox(12.0F / 16.0F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
                base.AddIntersectingBoundingBox(reader, entities, x, y, z, box, boxes);
                SetBoundingBox(0.0F, 6.0F / 16.0F, 0.25F, 12.0F / 16.0F, 10.0F / 16.0F, 12.0F / 16.0F);
                base.AddIntersectingBoundingBox(reader, entities, x, y, z, box, boxes);
                break;
        }

        SetBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
    }

    public override void UpdateBoundingBox(IBlockReader blockReader, EntityManager? entities, int x, int y, int z)
    {
        int meta = blockReader.GetBlockMeta(x, y, z);
        switch (GetFacing(meta))
        {
            case Side.Down:
                SetBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 0.25F, 1.0F);
                break;
            case Side.Up:
                SetBoundingBox(0.0F, 12.0F / 16.0F, 0.0F, 1.0F, 1.0F, 1.0F);
                break;
            case Side.North:
                SetBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 0.25F);
                break;
            case Side.South:
                SetBoundingBox(0.0F, 0.0F, 12.0F / 16.0F, 1.0F, 1.0F, 1.0F);
                break;
            case Side.West:
                SetBoundingBox(0.0F, 0.0F, 0.0F, 0.25F, 1.0F, 1.0F);
                break;
            case Side.East:
                SetBoundingBox(12.0F / 16.0F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
                break;
        }
    }

    public override void NeighborUpdate(OnTickEvent @event)
    {
        int facing = GetFacing(@event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z)).ToInt();
        int blockId = @event.World.Reader.GetBlockId(@event.X - PistonConstants.HeadOffsetX[facing], @event.Y - PistonConstants.HeadOffsetY[facing], @event.Z - PistonConstants.HeadOffsetZ[facing]);
        if (blockId != Piston.ID && blockId != StickyPiston.ID)
        {
            @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
        }
        else
        {
            Blocks[blockId].NeighborUpdate(new OnTickEvent(@event.World, @event.X - PistonConstants.HeadOffsetX[facing], @event.Y - PistonConstants.HeadOffsetY[facing], @event.Z - PistonConstants.HeadOffsetZ[facing],
                @event.World.Reader.GetBlockMeta(@event.X - PistonConstants.HeadOffsetX[facing], @event.Y - PistonConstants.HeadOffsetY[facing], @event.Z - PistonConstants.HeadOffsetZ[facing]), ID));
        }
    }

    public static Side GetFacing(int meta) => (meta & 7).ToSide();
}
