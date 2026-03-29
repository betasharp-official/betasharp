using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

public class BlockPistonExtension : Block
{
    private readonly int _pistonHeadSprite = -1;

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
        Side towardPiston = SideExtensions.OppositeFace(GetFacing(blockMeta));
        x += PistonConstants.HeadOffsetX(towardPiston);
        y += PistonConstants.HeadOffsetY(towardPiston);
        z += PistonConstants.HeadOffsetZ(towardPiston);
        int blockId = @event.World.Reader.GetBlockId(x, y, z);
        if (blockId != Piston.Id && blockId != StickyPiston.Id)
        {
            return;
        }

        blockMeta = @event.World.Reader.GetBlockMeta(x, y, z);
        if (!BlockPistonBase.IsExtended(blockMeta))
        {
            return;
        }

        Blocks[blockId]?.DropStacks(new OnDropEvent(@event.World, x, y, z, blockMeta));
        @event.World.Writer.SetBlock(x, y, z, 0);
    }

    public override int GetTexture(Side side, int meta)
    {
        Side facing = GetFacing(meta);
        return side == facing ? _pistonHeadSprite >= 0 ? _pistonHeadSprite : (meta & 8) != 0 ? TextureId - 1 : TextureId : side == SideExtensions.OppositeFace(facing) ? 107 : 108;
    }

    public override BlockRendererType GetRenderType() => BlockRendererType.PistonExtension;

    public override bool IsOpaque => false;

    public override bool IsFullCube => false;

    public override bool CanPlaceAt(CanPlaceAtContext context) => false;

    public override int DroppedItemCount => 0;

    public override void AddIntersectingBoundingBox(IBlockReader reader, EntityManager entities, int x, int y, int z, Box box, List<Box> boxes)
    {
        switch (GetFacing(reader.GetBlockMeta(x, y, z)))
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
        int blockId = @event.World.Reader.GetBlockId(@event.X - PistonConstants.HeadOffsetX(facing), @event.Y - PistonConstants.HeadOffsetY(facing), @event.Z - PistonConstants.HeadOffsetZ(facing));
        if (blockId != Piston.Id && blockId != StickyPiston.Id)
        {
            @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
        }
        else
        {
            Blocks[blockId]!.NeighborUpdate(new OnTickEvent(@event.World,
                @event.X - PistonConstants.HeadOffsetX(facing),
                @event.Y - PistonConstants.HeadOffsetY(facing),
                @event.Z - PistonConstants.HeadOffsetZ(facing),
                @event.World.Reader.GetBlockMeta(@event.X - PistonConstants.HeadOffsetX(facing),
                    @event.Y - PistonConstants.HeadOffsetY(facing),
                    @event.Z - PistonConstants.HeadOffsetZ(facing)),
                Id
            ));
        }
    }

    public static Side GetFacing(int meta) => (meta & 7).ToSide();
}
