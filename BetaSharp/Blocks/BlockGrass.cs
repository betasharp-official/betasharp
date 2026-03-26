using BetaSharp.Blocks.Materials;
using BetaSharp.Worlds.ClientData.Colors;
using BetaSharp.Worlds.Core.Systems;

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

    public override void onTick(OnTickEvent ctx)
    {
        if (!ctx.World.IsRemote)
        {
            if (ctx.World.Lighting.GetLightLevel(ctx.X, ctx.Y + 1, ctx.Z) < 4 && BlockLightOpacity[ctx.World.Reader.GetBlockId(ctx.X, ctx.Y + 1, ctx.Z)] > 2)
            {
                if (Random.Shared.Next(4) != 0)
                {
                    return;
                }

                ctx.World.Writer.SetBlock(ctx.X, ctx.Y, ctx.Z, Dirt.id);
            }
            else if (ctx.World.Lighting.GetLightLevel(ctx.X, ctx.Y + 1, ctx.Z) >= 9)
            {
                int spreadX = ctx.X + Random.Shared.Next(3) - 1;
                int spreadY = ctx.Y + Random.Shared.Next(5) - 3;
                int spreadZ = ctx.Z + Random.Shared.Next(3) - 1;
                int blockAboveId = ctx.World.Reader.GetBlockId(spreadX, spreadY + 1, spreadZ);
                if (ctx.World.Reader.GetBlockId(spreadX, spreadY, spreadZ) == Dirt.id && ctx.World.Lighting.GetLightLevel(spreadX, spreadY + 1, spreadZ) >= 4 && BlockLightOpacity[blockAboveId] <= 2)
                {
                    ctx.World.Writer.SetBlock(spreadX, spreadY, spreadZ, GrassBlock.id);
                }
            }
        }
    }

    public override int getDroppedItemId(int blocKMeta) => Dirt.getDroppedItemId(0);
}
