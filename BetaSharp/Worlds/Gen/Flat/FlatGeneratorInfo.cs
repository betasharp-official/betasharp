using System.Text;
using BetaSharp.Blocks;

namespace BetaSharp.Worlds.Gen.Flat;

public class FlatGeneratorInfo
{
    public int Biome { get; set; } = 1;

    public Dictionary<string, Dictionary<string, string>> WorldFeatures { get; } = [];
    public List<FlatLayerInfo> FlatLayers { get; } = [];

    public void UpdateLayerHeights()
    {
        int totalHeight = 0;
        foreach (FlatLayerInfo layer in FlatLayers)
        {
            layer.MinY = totalHeight;
            totalHeight += layer.LayerCount;
        }
    }

    public override string ToString()
    {
        StringBuilder sb = new();
        sb.Append(2); // Version 2
        sb.Append(";");

        for (int i = 0; i < FlatLayers.Count; ++i)
        {
            if (i > 0) sb.Append(",");
            sb.Append(FlatLayers[i].ToString());
        }

        sb.Append(";");
        sb.Append(Biome);

        if (WorldFeatures.Count > 0)
        {
            sb.Append(";");
            int i = 0;
            foreach (KeyValuePair<string, Dictionary<string, string>> feature in WorldFeatures)
            {
                if (i++ > 0) sb.Append(",");
                sb.Append(feature.Key.ToLower());

                if (feature.Value.Count > 0)
                {
                    sb.Append("(");
                    int j = 0;
                    foreach (KeyValuePair<string, string> param in feature.Value)
                    {
                        if (j++ > 0) sb.Append(" ");
                        sb.Append(param.Key);
                        sb.Append("=");
                        sb.Append(param.Value);
                    }
                    sb.Append(")");
                }
            }
        }
        else
        {
            sb.Append(";");
        }

        return sb.ToString();
    }

    private static FlatLayerInfo? ParseLayer(string input, int minY)
    {
        string[] parts = input.Split('x');
        int count = 1;
        int meta = 0;

        int blockId;
        try
        {
            if (parts.Length == 2)
            {
                count = int.Parse(parts[0]);
            }

            string blockData = parts[^1];
            string[] blockParts = blockData.Split(':');
            blockId = int.Parse(blockParts[0]);

            if (blockParts.Length > 1)
            {
                meta = int.Parse(blockParts[1]);
            }

            if (blockId < 0 || blockId >= 256 || Block.Blocks[blockId] == null)
            {
                blockId = 0;
                meta = 0;
            }

            if (meta < 0 || meta > 15) meta = 0;
        }
        catch
        {
            return null;
        }

        return new FlatLayerInfo(count, blockId, meta) { MinY = minY };
    }

    public static FlatGeneratorInfo CreateFromString(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return GetDefault();
        }

        string[] parts = input.Split(';');
        int version = parts.Length > 0 ? int.Parse(parts[0]) : 0;

        FlatGeneratorInfo info = new();
        int partIndex = parts.Length == 1 ? 0 : 1;

        if (parts.Length > partIndex)
        {
            string[] layers = parts[partIndex++].Split(',');
            int currentY = 0;
            foreach (string layerStr in layers)
            {
                FlatLayerInfo? layer = ParseLayer(layerStr, currentY);
                if (layer != null)
                {
                    info.FlatLayers.Add(layer);
                    currentY += layer.LayerCount;
                }
            }
        }

        if (version > 0 && parts.Length > partIndex)
        {
            info.Biome = int.Parse(parts[partIndex++]);
        }

        if (version > 0 && parts.Length > partIndex)
        {
            string[] features = parts[partIndex++].ToLower().Split(',');
            foreach (string featureStr in features)
            {
                string[] featureParts = featureStr.Split('(');
                string featureName = featureParts[0];
                if (string.IsNullOrEmpty(featureName)) continue;

                var featureParams = new Dictionary<string, string>();
                info.WorldFeatures[featureName] = featureParams;

                if (featureParts.Length > 1 && featureParts[1].EndsWith(")"))
                {
                    string paramsStr = featureParts[1][..^1];
                    string[] paramPairs = paramsStr.Split(' ');
                    foreach (string pair in paramPairs)
                    {
                        string[] kv = pair.Split('=');
                        if (kv.Length == 2)
                        {
                            featureParams[kv[0]] = kv[1];
                        }
                    }
                }
            }
        }
        else
        {
            info.WorldFeatures["village"] = [];
        }

        return info;
    }

    public static FlatGeneratorInfo GetDefault()
    {
        FlatGeneratorInfo info = new()
        {
            Biome = 1
        };

        info.FlatLayers.Add(new FlatLayerInfo(1, Block.Bedrock.id));
        info.FlatLayers.Add(new FlatLayerInfo(2, Block.Dirt.id));
        info.FlatLayers.Add(new FlatLayerInfo(1, Block.GrassBlock.id));
        info.UpdateLayerHeights();
        info.WorldFeatures["village"] = [];
        return info;
    }
}
