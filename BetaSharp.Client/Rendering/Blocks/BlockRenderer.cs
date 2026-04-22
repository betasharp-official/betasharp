using BetaSharp.Blocks;
using BetaSharp.Client.Rendering.Blocks.Renderers;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Client.Rendering.Blocks;

public class BlockRenderer
{
    private static readonly ReedRenderer s_reed = new();
    private static readonly TorchRenderer s_torch = new();
    private static readonly FireRenderer s_fire = new();
    private static readonly FluidsRenderer s_fluids = new();
    private static readonly RedstoneWireRenderer s_wire = new();
    private static readonly CropsRenderer s_crops = new();
    private static readonly DoorRenderer s_door = new();
    private static readonly LadderRenderer s_ladder = new();
    private static readonly MinecartTrackRenderer s_track = new();
    private static readonly StairsRenderer s_stairs = new();
    private static readonly FenceRenderer s_fence = new();
    private static readonly LeverRenderer s_lever = new();
    private static readonly CactusRenderer s_cactus = new();
    private static readonly BedRenderer s_bed = new();
    private static readonly RepeaterRenderer s_repeater = new();
    private static readonly PistonBaseRenderer s_pistonBase = new();
    private static readonly PistonExtensionRenderer s_pistonExt = new();


    public static bool RenderBlockByRenderType(IBlockReader world, ILightProvider lighting, Block block, BlockPos pos, Tessellator tess, int overrideTexture = -1, bool renderAllFaces = false, bool doVariance = false)
    {
        BlockRendererType type = block.getRenderType();

        block.updateBoundingBox(world, pos.x, pos.y, pos.z);

        TextureVariance topRule = doVariance ? block.TopVariance : TextureVariance.None;
        TextureVariance botRule = doVariance ? block.BottomVariance : TextureVariance.None;
        TextureVariance sideRule = doVariance ? block.SideVariance : TextureVariance.None;

        int topHash = topRule != TextureVariance.None ? BlockRenderContext.GetTextureVarianceHash(pos.x, pos.y, pos.z) : 0;
        int botHash = botRule != TextureVariance.None ? BlockRenderContext.GetTextureVarianceHash(pos.x, pos.y - 1, pos.z) : 0;
        int sideHash = sideRule != TextureVariance.None ? BlockRenderContext.GetTextureVarianceHash(pos.x, pos.y, pos.z) : 0;

        int topRot = BlockRenderContext.ApplyVariance(topHash, topRule, out int flipTop);
        int botRot = BlockRenderContext.ApplyVariance(botHash, botRule, out int flipBot);
        int sideRot = BlockRenderContext.ApplyVariance(sideHash, sideRule, out int flipSide);

        var ctx = new BlockRenderContext(
            tess: tess,
            lighting: lighting,
            blockReader: world,
            overrideTexture: overrideTexture,
            renderAllFaces: renderAllFaces,
            flipTexture: false,
            uvTop: topRot,
            uvBottom: botRot,
            uvNorth: sideRot,
            uvSouth: sideRot,
            uvEast: sideRot,
            uvWest: sideRot,
            flipTop: flipTop,
            flipBottom: flipBot,
            flipNorth: flipSide,
            flipSouth: flipSide,
            flipEast: flipSide,
            flipWest: flipSide,
            aoBlendMode: 1,
            customFlag: type == BlockRendererType.PistonExtension
        );

        if (type == BlockRendererType.Standard)
        {
            return ctx.DrawBlock(block, pos);
        }

        return type switch
        {
            BlockRendererType.Reed => s_reed.Draw(block, pos, ref ctx),
            BlockRendererType.Torch => s_torch.Draw(block, pos, ref ctx),
            BlockRendererType.Fire => s_fire.Draw(block, pos, ref ctx),
            BlockRendererType.Fluids => s_fluids.Draw(block, pos, ref ctx),
            BlockRendererType.RedstoneWire => s_wire.Draw(block, pos, ref ctx),
            BlockRendererType.Crops => s_crops.Draw(block, pos, ref ctx),
            BlockRendererType.Door => s_door.Draw(block, pos, ref ctx),
            BlockRendererType.Ladder => s_ladder.Draw(block, pos, ref ctx),
            BlockRendererType.MinecartTrack => s_track.Draw(block, pos, ref ctx),
            BlockRendererType.Stairs => s_stairs.Draw(block, pos, ref ctx),
            BlockRendererType.Fence => s_fence.Draw(block, pos, ref ctx),
            BlockRendererType.Lever => s_lever.Draw(block, pos, ref ctx),
            BlockRendererType.Cactus => s_cactus.Draw(block, pos, ref ctx),
            BlockRendererType.Bed => s_bed.Draw(block, pos, ref ctx),
            BlockRendererType.Repeater => s_repeater.Draw(block, pos, ref ctx),
            BlockRendererType.PistonBase => s_pistonBase.Draw(block, pos, ref ctx),
            BlockRendererType.PistonExtension => s_pistonExt.Draw(block, pos, ref ctx),
            _ => false
        };
    }

    public static void RenderBlockOnInventory(Block block, int metadata, float brightness, Tessellator tess)
    {
        BlockRendererType renderType = block.getRenderType();
        var uiCtx = new BlockRenderContext(
            blockReader: NullBlockReader.Instance,
            tess: tess,
            lighting: null,
            renderAllFaces: true,
            enableAo: false,
            overrideTexture: -1
        );

        Vec3D origin = new Vec3D(0, 0, 0);
        FaceColors dummyColors = new FaceColors();

        if (renderType == BlockRendererType.Standard || renderType == BlockRendererType.PistonBase)
        {
            bool isPiston = renderType == BlockRendererType.PistonBase;

            void SetFaceColor(int face)
            {
                int c = block.getColorForFace(metadata, face);
                GLManager.GL.Color4(
                    (c >> 16 & 255) / 255.0F * brightness,
                    (c >> 8 & 255) / 255.0F * brightness,
                    (c & 255) / 255.0F * brightness,
                    1.0F);
            }

            block.setupRenderBoundingBox();
            GLManager.GL.Translate(-0.5F, -0.5F, -0.5F);

            tess.startDrawingQuads();
            tess.setNormal(0.0F, -1.0F, 0.0F);
            SetFaceColor(0);
            uiCtx.DrawBottomFace(block, origin, dummyColors, isPiston ? block.GetTexture(Side.Down) : block.GetTexture(Side.Down, metadata));
            tess.draw();

            tess.startDrawingQuads();
            tess.setNormal(0.0F, 1.0F, 0.0F);
            SetFaceColor(1);
            uiCtx.DrawTopFace(block, origin, dummyColors,
                isPiston ? block.GetTexture(Side.Up) : block.GetTexture(Side.Up, metadata));
            tess.draw();

            tess.startDrawingQuads();
            tess.setNormal(0.0F, 0.0F, -1.0F);
            SetFaceColor(2);
            uiCtx.DrawEastFace(block, origin, dummyColors,
                isPiston ? block.GetTexture(Side.North) : block.GetTexture(Side.North, metadata));
            tess.draw();

            tess.startDrawingQuads();
            tess.setNormal(0.0F, 0.0F, 1.0F);
            SetFaceColor(3);
            uiCtx.DrawWestFace(block, origin, dummyColors,
                isPiston ? block.GetTexture(Side.South) : block.GetTexture(Side.South, metadata));
            tess.draw();

            tess.startDrawingQuads();
            tess.setNormal(-1.0F, 0.0F, 0.0F);
            SetFaceColor(4);
            uiCtx.DrawNorthFace(block, origin, dummyColors,
                isPiston ? block.GetTexture(Side.West) : block.GetTexture(Side.West, metadata));
            tess.draw();

            tess.startDrawingQuads();
            tess.setNormal(1.0F, 0.0F, 0.0F);
            SetFaceColor(5);
            uiCtx.DrawSouthFace(block, origin, dummyColors,
                isPiston ? block.GetTexture(Side.East) : block.GetTexture(Side.East, metadata));
            tess.draw();

            GLManager.GL.Translate(0.5F, 0.5F, 0.5F);
        }
        else
        {
            int color = block.getColor(metadata);
            GLManager.GL.Color4(
                (color >> 16 & 255) / 255.0F * brightness,
                (color >> 8 & 255) / 255.0F * brightness,
                (color & 255) / 255.0F * brightness,
                1.0F);
            GLManager.GL.Translate(-0.5F, -0.5F, -0.5F);
            var itemWorld = new ItemRenderBlockAccess(block.id, metadata, brightness);
            BlockPos itemPos = new(0, 0, 0);
            tess.startDrawingQuads();
            tess.setNormal(0.0F, 1.0F, 0.0F);
            RenderBlockByRenderType(itemWorld, itemWorld, block, itemPos, tess, uiCtx.OverrideTexture, true);
            tess.draw();
            GLManager.GL.Translate(0.5F, 0.5F, 0.5F);
        }
    }

    public static void RenderBlockFallingSand(Block block, IWorldContext world, int x, int y, int z, Tessellator tess)
    {
        // Directional shading multipliers for fake 3D depth
        float lightBottom = 0.5F;
        float lightTop = 1.0F;
        float lightZ = 0.8F; // East/West faces
        float lightX = 0.6F; // North/South faces

        var entityCtx = new BlockRenderContext(
            blockReader: world.Reader,
            lighting: world.Lighting,
            tess: tess,
            renderAllFaces: true,
            enableAo: false
        );

        tess.startDrawingQuads();

        // Base luminance at the entity's current position
        float currentLuminance = block.getLuminance(world.Lighting, x, y, z);
        Vec3D localOrigin = new Vec3D(-0.5, -0.5, -0.5);
        FaceColors dummyColors = new FaceColors();

        // Bottom Face
        float faceLum = Math.Max(currentLuminance, block.getLuminance(world.Lighting, x, y - 1, z));
        tess.setColorOpaque_F(lightBottom * faceLum, lightBottom * faceLum, lightBottom * faceLum);
        entityCtx.DrawBottomFace(block, localOrigin, dummyColors, block.GetTexture(Side.Down));

        // Top Face
        faceLum = Math.Max(currentLuminance, block.getLuminance(world.Lighting, x, y + 1, z));
        tess.setColorOpaque_F(lightTop * faceLum, lightTop * faceLum, lightTop * faceLum);
        entityCtx.DrawTopFace(block, localOrigin, dummyColors, block.GetTexture(Side.Up));

        // East/West Faces
        faceLum = Math.Max(currentLuminance, block.getLuminance(world.Lighting, x, y, z - 1));
        tess.setColorOpaque_F(lightZ * faceLum, lightZ * faceLum, lightZ * faceLum);
        entityCtx.DrawEastFace(block, localOrigin, dummyColors, block.GetTexture(Side.North));

        faceLum = Math.Max(currentLuminance, block.getLuminance(world.Lighting, x, y, z + 1));
        tess.setColorOpaque_F(lightZ * faceLum, lightZ * faceLum, lightZ * faceLum);
        entityCtx.DrawWestFace(block, localOrigin, dummyColors, block.GetTexture(Side.South));

        // North/South Faces
        faceLum = Math.Max(currentLuminance, block.getLuminance(world.Lighting, x - 1, y, z));
        tess.setColorOpaque_F(lightX * faceLum, lightX * faceLum, lightX * faceLum);
        entityCtx.DrawNorthFace(block, localOrigin, dummyColors, block.GetTexture(Side.West));

        faceLum = Math.Max(currentLuminance, block.getLuminance(world.Lighting, x + 1, y, z));
        tess.setColorOpaque_F(lightX * faceLum, lightX * faceLum, lightX * faceLum);
        entityCtx.DrawSouthFace(block, localOrigin, dummyColors, block.GetTexture(Side.East));

        tess.draw();
    }

    public static bool IsSideLit(BlockRendererType renderType)
    {
        return renderType == BlockRendererType.Standard ||
               renderType == BlockRendererType.Stairs ||
               renderType == BlockRendererType.Fence ||
               renderType == BlockRendererType.Cactus ||
               renderType == BlockRendererType.PistonBase;
    }
}
