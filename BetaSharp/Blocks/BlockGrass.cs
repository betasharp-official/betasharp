using BetaSharp.Blocks.Materials;
using BetaSharp.Worlds.ClientData.Colors;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

public class BlockGrass : Block
{
    public BlockGrass(int id) : base(id, Material.SolidOrganic)
    {
        textureId = 3;
        setTickRandomly(true);
    }

    public override int getTexture(int side)
    {
        if (side == 1)
        {
            return 0; // top: grass green
        }

        if (side == 0)
        {
            return 2; // bottom: dirt
        }

        return 3; // sides: grass+dirt edge
    }

    public override int getColorForFace(int meta, int face) => face == 1 ? GrassColors.getDefaultColor() : 0xFFFFFF;

    public override int getTextureId(IBlockReader iBlockReader, int x, int y, int z, int side)
    {
        if (side == 1)
        {
            return 0;
        }

        if (side == 0)
        {
            return 2;
        }

        Material materialAbove = iBlockReader.GetMaterial(x, y + 1, z);
        return materialAbove != Material.SnowLayer && materialAbove != Material.SnowBlock ? 3 : 68;
    }

    public override int getColorMultiplier(IBlockReader iBlockReader, int x, int y, int z)
    {
        iBlockReader.GetBiomeSource().GetBiomesInArea(x, z, 1, 1);
        double temperature = iBlockReader.GetBiomeSource().TemperatureMap[0];
        double downfall = iBlockReader.GetBiomeSource().DownfallMap[0];
        return GrassColors.getColor(temperature, downfall);
    }

    public override void onTick(OnTickEvt ctx)
    {
        if (!ctx.IsRemote)
        {
            if (ctx.Lighting.GetLightLevel(ctx.X, ctx.Y + 1, ctx.Z) < 4 && BlockLightOpacity[ctx.WorldRead.GetBlockId(ctx.X, ctx.Y + 1, ctx.Z)] > 2)
            {
                if (ctx.Random.NextInt(4) != 0)
                {
                    return;
                }

                ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, Dirt.id);
            }
            else if (ctx.Lighting.GetLightLevel(ctx.X, ctx.Y + 1, ctx.Z) >= 9)
            {
                int spreadX = ctx.X + ctx.Random.NextInt(3) - 1;
                int spreadY = ctx.Y + ctx.Random.NextInt(5) - 3;
                int spreadZ = ctx.Z + ctx.Random.NextInt(3) - 1;
                int blockAboveId = ctx.WorldRead.GetBlockId(spreadX, spreadY + 1, spreadZ);
                if (ctx.WorldRead.GetBlockId(spreadX, spreadY, spreadZ) == Dirt.id && ctx.Lighting.GetLightLevel(spreadX, spreadY + 1, spreadZ) >= 4 && BlockLightOpacity[blockAboveId] <= 2)
                {
                    ctx.WorldWrite.SetBlock(spreadX, spreadY, spreadZ, GrassBlock.id);
                }
            }
        }
    }

    public override int getDroppedItemId(int blocKMeta) => Dirt.getDroppedItemId(0);
}
