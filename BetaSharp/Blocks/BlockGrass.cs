using BetaSharp.Blocks.Materials;
using BetaSharp.Worlds.ClientData.Colors;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

public class BlockGrass : Block
{
    public BlockGrass(int id) : base(id, Material.SolidOrganic)
    {
        TextureId = BlockTextures.GrassSide;
        SetTickRandomly(true);
    }

    public override int GetTexture(Side side) =>
        side switch
        {
            Side.Up => BlockTextures.GrassTop,
            Side.Down => BlockTextures.Dirt,
            _ => BlockTextures.GrassSide
        };

    public override int GetColorForFace(int meta, int face) => face == 1 ? GrassColors.getDefaultColor() : 0xFFFFFF;

    public override int GetTextureId(IBlockReader iBlockReader, int x, int y, int z, Side side)
    {
        switch (side)
        {
            case Side.Up:
                return BlockTextures.GrassTop;
            case Side.Down:
                return BlockTextures.Dirt;
            default:
                {
                    Material materialAbove = iBlockReader.GetMaterial(x, y + 1, z);
                    return materialAbove != Material.SnowLayer && materialAbove != Material.SnowBlock ? BlockTextures.GrassSide : BlockTextures.GrassSideSnowy;
                }
        }
    }

    public override int GetColorMultiplier(IBlockReader iBlockReader, int x, int y, int z)
    {
        iBlockReader.GetBiomeSource().GetBiomesInArea(x, z, 1, 1);
        double temperature = iBlockReader.GetBiomeSource().TemperatureMap[0];
        double downfall = iBlockReader.GetBiomeSource().DownfallMap[0];
        return GrassColors.getColor(temperature, downfall);
    }

    public override void OnTick(OnTickEvent ctx)
    {
        if (ctx.World.IsRemote)
        {
            return;
        }

        if (ctx.World.Lighting.GetLightLevel(ctx.X, ctx.Y + 1, ctx.Z) < 4 && BlockLightOpacity[ctx.World.Reader.GetBlockId(ctx.X, ctx.Y + 1, ctx.Z)] > 2)
        {
            if (Random.Shared.Next(4) != 0)
            {
                return;
            }

            ctx.World.Writer.SetBlock(ctx.X, ctx.Y, ctx.Z, Dirt.ID);
        }
        else if (ctx.World.Lighting.GetLightLevel(ctx.X, ctx.Y + 1, ctx.Z) >= 9)
        {
            int spreadX = ctx.X + Random.Shared.Next(3) - 1;
            int spreadY = ctx.Y + Random.Shared.Next(5) - 3;
            int spreadZ = ctx.Z + Random.Shared.Next(3) - 1;
            int blockAboveId = ctx.World.Reader.GetBlockId(spreadX, spreadY + 1, spreadZ);
            if (ctx.World.Reader.GetBlockId(spreadX, spreadY, spreadZ) == Dirt.ID && ctx.World.Lighting.GetLightLevel(spreadX, spreadY + 1, spreadZ) >= 4 && BlockLightOpacity[blockAboveId] <= 2)
            {
                ctx.World.Writer.SetBlock(spreadX, spreadY, spreadZ, GrassBlock.ID);
            }
        }
    }

    public override int GetDroppedItemId(int blocKMeta) => Dirt.GetDroppedItemId(0);
}
