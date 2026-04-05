using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

public class BlockPistonExtension : Block
{
    private int _pistonHeadSprite = -1;

    public BlockPistonExtension(int id, int textureId) : base(id, textureId, Material.Piston)
    {
        setSoundGroup(SoundStoneFootstep);
        setHardness(0.5F);
    }

    public override void onBreak(OnBreakEvent @event)
    {
        base.onBreak(@event);
        int x = @event.X;
        int y = @event.Y;
        int z = @event.Z;
        int blockMeta = @event.World.Reader.GetBlockMeta(x, y, z);
        int var6 = PistonConstants.OppositeFace[getFacing(blockMeta)];

        x += PistonConstants.HeadOffsetX[var6];
        y += PistonConstants.HeadOffsetY[var6];
        z += PistonConstants.HeadOffsetZ[var6];

        int blockId = @event.World.Reader.GetBlockId(x, y, z);
        if (blockId != Piston.id && blockId != StickyPiston.id) return;

        int meta = @event.World.Reader.GetBlockMeta(x, y, z);
        if (!BlockPistonBase.IsExtended(meta)) return;

        Blocks[blockId].dropStacks(new OnDropEvent(@event.World, x, y, z, meta));
        @event.World.Writer.SetBlock(x, y, z, 0);
    }

    public override int getTexture(int side, int meta)
    {
        int facing = getFacing(meta);
        return side == facing ? _pistonHeadSprite >= 0 ? _pistonHeadSprite : (meta & 8) != 0 ? textureId - 1 : textureId : side == PistonConstants.OppositeFace[facing] ? 107 : 108;
    }

    public override BlockRendererType getRenderType() => BlockRendererType.PistonExtension;

    public override bool isOpaque() => false;

    public override bool isFullCube() => false;

    public override bool canPlaceAt(CanPlaceAtContext context) => false;

    public override int getDroppedItemCount() => 0;

    public override void addIntersectingBoundingBox(IBlockReader reader, EntityManager entities, int x, int y, int z, Box box, List<Box> boxes)
    {
        int var7 = reader.GetBlockMeta(x, y, z);
        switch (getFacing(var7))
        {
            case 0:
                setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 0.25F, 1.0F);
                base.addIntersectingBoundingBox(reader, entities, x, y, z, box, boxes);
                setBoundingBox(6.0F / 16.0F, 0.25F, 6.0F / 16.0F, 10.0F / 16.0F, 1.0F, 10.0F / 16.0F);
                base.addIntersectingBoundingBox(reader, entities, x, y, z, box, boxes);
                break;
            case 1:
                setBoundingBox(0.0F, 12.0F / 16.0F, 0.0F, 1.0F, 1.0F, 1.0F);
                base.addIntersectingBoundingBox(reader, entities, x, y, z, box, boxes);
                setBoundingBox(6.0F / 16.0F, 0.0F, 6.0F / 16.0F, 10.0F / 16.0F, 12.0F / 16.0F, 10.0F / 16.0F);
                base.addIntersectingBoundingBox(reader, entities, x, y, z, box, boxes);
                break;
            case 2:
                setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 0.25F);
                base.addIntersectingBoundingBox(reader, entities, x, y, z, box, boxes);
                setBoundingBox(0.25F, 6.0F / 16.0F, 0.25F, 12.0F / 16.0F, 10.0F / 16.0F, 1.0F);
                base.addIntersectingBoundingBox(reader, entities, x, y, z, box, boxes);
                break;
            case 3:
                setBoundingBox(0.0F, 0.0F, 12.0F / 16.0F, 1.0F, 1.0F, 1.0F);
                base.addIntersectingBoundingBox(reader, entities, x, y, z, box, boxes);
                setBoundingBox(0.25F, 6.0F / 16.0F, 0.0F, 12.0F / 16.0F, 10.0F / 16.0F, 12.0F / 16.0F);
                base.addIntersectingBoundingBox(reader, entities, x, y, z, box, boxes);
                break;
            case 4:
                setBoundingBox(0.0F, 0.0F, 0.0F, 0.25F, 1.0F, 1.0F);
                base.addIntersectingBoundingBox(reader, entities, x, y, z, box, boxes);
                setBoundingBox(6.0F / 16.0F, 0.25F, 0.25F, 10.0F / 16.0F, 12.0F / 16.0F, 1.0F);
                base.addIntersectingBoundingBox(reader, entities, x, y, z, box, boxes);
                break;
            case 5:
                setBoundingBox(12.0F / 16.0F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
                base.addIntersectingBoundingBox(reader, entities, x, y, z, box, boxes);
                setBoundingBox(0.0F, 6.0F / 16.0F, 0.25F, 12.0F / 16.0F, 10.0F / 16.0F, 12.0F / 16.0F);
                base.addIntersectingBoundingBox(reader, entities, x, y, z, box, boxes);
                break;
        }

        setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
    }

    public override void updateBoundingBox(IBlockReader blockReader, EntityManager? entities, int x, int y, int z)
    {
        int var5 = blockReader.GetBlockMeta(x, y, z);
        switch (getFacing(var5))
        {
            case 0:
                setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 0.25F, 1.0F);
                break;
            case 1:
                setBoundingBox(0.0F, 12.0F / 16.0F, 0.0F, 1.0F, 1.0F, 1.0F);
                break;
            case 2:
                setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 0.25F);
                break;
            case 3:
                setBoundingBox(0.0F, 0.0F, 12.0F / 16.0F, 1.0F, 1.0F, 1.0F);
                break;
            case 4:
                setBoundingBox(0.0F, 0.0F, 0.0F, 0.25F, 1.0F, 1.0F);
                break;
            case 5:
                setBoundingBox(12.0F / 16.0F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
                break;
        }
    }

    public override void neighborUpdate(OnTickEvent @event)
    {
        int facing = getFacing(@event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z));
        int blockId = @event.World.Reader.GetBlockId(@event.X - PistonConstants.HeadOffsetX[facing], @event.Y - PistonConstants.HeadOffsetY[facing], @event.Z - PistonConstants.HeadOffsetZ[facing]);
        if (blockId != Piston.id && blockId != StickyPiston.id)
        {
            @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
        }
        else
        {
            Blocks[blockId].neighborUpdate(new OnTickEvent(@event.World, @event.X - PistonConstants.HeadOffsetX[facing], @event.Y - PistonConstants.HeadOffsetY[facing], @event.Z - PistonConstants.HeadOffsetZ[facing],
                @event.World.Reader.GetBlockMeta(@event.X - PistonConstants.HeadOffsetX[facing], @event.Y - PistonConstants.HeadOffsetY[facing], @event.Z - PistonConstants.HeadOffsetZ[facing]), id));
        }
    }

    public static int getFacing(int meta) => meta & 7;
}
