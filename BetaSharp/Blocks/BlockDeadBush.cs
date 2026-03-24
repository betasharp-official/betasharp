namespace BetaSharp.Blocks;

public class BlockDeadBush : BlockPlant
{
    public static readonly float HalfSize = 0.4F;

    public BlockDeadBush(int i, int j) : base(i, j) => SetBoundingBox(0.5F - HalfSize, 0.0F, 0.5F - HalfSize, 0.5F + HalfSize, 0.8F, 0.5F + HalfSize);

    protected override bool canPlantOnTop(int id) => id == Sand.Id;

    public override int GetTexture(Side side, int meta) => TextureId;

    public override int GetDroppedItemId(int blockMeta) => -1;
}
