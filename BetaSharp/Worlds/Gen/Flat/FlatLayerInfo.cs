namespace BetaSharp.Worlds.Gen.Flat;

public class FlatLayerInfo
{
    public int LayerCount { get; }
    public int FillBlock { get; }
    public int FillBlockMeta { get; }
    public int MinY { get; set; }

    public FlatLayerInfo(int count, int blockId)
    {
        LayerCount = count;
        FillBlock = blockId;
    }

    public FlatLayerInfo(int count, int blockId, int meta) : this(count, blockId)
    {
        FillBlockMeta = meta;
    }

    public override string ToString()
    {
        string result = "";
        if (LayerCount > 1)
        {
            result += LayerCount + "x";
        }

        result += FillBlock;

        if (FillBlockMeta > 0)
        {
            result += ":" + FillBlockMeta;
        }

        return result;
    }
}
