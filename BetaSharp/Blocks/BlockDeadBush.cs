namespace BetaSharp.Blocks;

public class BlockDeadBush : BlockPlant
{
    public BlockDeadBush(int i, int j) : base(i, j)
    {
        float halfSize = 0.4F;
        setBoundingBox(0.5F - halfSize, 0.0F, 0.5F - halfSize, 0.5F + halfSize, 0.8F, 0.5F + halfSize);
    }

    protected override bool canPlantOnTop(int id)
    {
        return id == Sand.id;
    }

    public override int getTexture(int side, int meta)
    {
        return textureId;
    }

    public override int getDroppedItemId(int blockMeta)
    {
        return -1;
    }
}
