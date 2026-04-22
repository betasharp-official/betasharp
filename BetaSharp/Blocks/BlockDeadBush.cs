namespace BetaSharp.Blocks;

public class BlockDeadBush : BlockPlant
{
    private const float HalfSize = 0.4F;
    public BlockDeadBush(int i, int j) : base(i, j)
    {
        setBoundingBox(0.5F - HalfSize, 0.0F, 0.5F - HalfSize, 0.5F + HalfSize, 0.8F, 0.5F + HalfSize);
    }

    protected override bool canPlantOnTop(int id) => id == Sand.id;

    public override int GetTexture(Side side, int meta) => TextureId;

    public override int getDroppedItemId(int blockMeta) => -1;
}
