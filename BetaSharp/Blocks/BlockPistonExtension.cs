using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

public class BlockPistonExtension : Block
{
    private int pistonHeadSprite = -1;

    public BlockPistonExtension(int id, int textureId) : base(id, textureId, Material.Piston)
    {
        setSoundGroup(SoundStoneFootstep);
        setHardness(0.5F);
    }

    public void setSprite(int sprite) => pistonHeadSprite = sprite;

    public void clearSprite() => pistonHeadSprite = -1;

    public override void onBreak(OnBreakEvent @event)
    {
        base.onBreak(@event);
        int x = @event.X;
        int y = @event.Y;
        int z = @event.Z;
        int var5 = @event.World.Reader.GetBlockMeta(x, y, z);
        int var6 = PistonConstants.field_31057_a[getFacing(var5)];
        x += PistonConstants.HEAD_OFFSET_X[var6];
        y += PistonConstants.HEAD_OFFSET_Y[var6];
        z += PistonConstants.HEAD_OFFSET_Z[var6];
        int var7 = @event.World.Reader.GetBlockId(x, y, z);
        if (var7 == Piston.id || var7 == StickyPiston.id)
        {
            var5 = @event.World.Reader.GetBlockMeta(x, y, z);
            if (BlockPistonBase.isExtended(var5))
            {
                Blocks[var7].dropStacks(new OnDropEvent(@event.World, x, y, z, var5));
                @event.World.Writer.SetBlock(x, y, z, 0);
            }
        }
    }

    public override int getTexture(int side, int meta)
    {
        int var3 = getFacing(meta);
        return side == var3 ? pistonHeadSprite >= 0 ? pistonHeadSprite : (meta & 8) != 0 ? textureId - 1 : textureId : side == PistonConstants.field_31057_a[var3] ? 107 : 108;
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
        int var7 = @event.World.Reader.GetBlockId(@event.X - PistonConstants.HEAD_OFFSET_X[facing], @event.Y - PistonConstants.HEAD_OFFSET_Y[facing], @event.Z - PistonConstants.HEAD_OFFSET_Z[facing]);
        if (var7 != Piston.id && var7 != StickyPiston.id)
        {
            @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
        }
        else
        {
            Blocks[var7].neighborUpdate(new OnTickEvent(@event.World, @event.X - PistonConstants.HEAD_OFFSET_X[facing], @event.Y - PistonConstants.HEAD_OFFSET_Y[facing], @event.Z - PistonConstants.HEAD_OFFSET_Z[facing],
                @event.World.Reader.GetBlockMeta(@event.X - PistonConstants.HEAD_OFFSET_X[facing], @event.Y - PistonConstants.HEAD_OFFSET_Y[facing], @event.Z - PistonConstants.HEAD_OFFSET_Z[facing]), id));
        }
    }

    public static int getFacing(int meta) => meta & 7;
}
