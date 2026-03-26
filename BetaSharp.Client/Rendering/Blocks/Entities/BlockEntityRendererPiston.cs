using BetaSharp.Blocks;
using BetaSharp.Blocks.Entities;
using BetaSharp.Client.Rendering.Blocks.Renderers;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.OpenGL;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering.Blocks.Entities;

public class BlockEntityRendererPiston : BlockEntitySpecialRenderer
{
    private readonly PistonBaseRenderer _pistonBaseRenderer = new();
    private readonly PistonExtensionRenderer _pistonExtensionRenderer = new();

    public override void renderTileEntityAt(BlockEntity blockEntity, double x, double y, double z, float tickDelta)
    {
        if (blockEntity is not BlockEntityPiston piston)
        {
            throw new ArgumentException("BlockEntity is not a Piston");
        }

        Block block = Block.Blocks[piston.getPushedBlockId()];
        if (piston.getProgress(tickDelta) < 1.0F)
        {
            Tessellator tess = Tessellator.instance;
            bindTextureByName("/terrain.png");
            Lighting.turnOff();
            GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
            GLManager.GL.Enable(GLEnum.Blend);
            GLManager.GL.Disable(GLEnum.CullFace);

            GLManager.GL.ShadeModel(BetaSharp.isAmbientOcclusionEnabled() ? GLEnum.Smooth : GLEnum.Flat);

            tess.startDrawingQuads();
            tess.setTranslationD(
                x - piston.X + piston.getRenderOffsetX(tickDelta),
                y - piston.Y + piston.getRenderOffsetY(tickDelta),
                z - piston.Z + piston.getRenderOffsetZ(tickDelta)
            );

            tess.setColorOpaque(1, 1, 1);

            var baseCtx = new BlockRenderContext(
                blockReader: piston.World.Reader,
                lighting: piston.World.Lighting,
                tess: tess,
                renderAllFaces: true,
                aoBlendMode: BetaSharp.isAmbientOcclusionEnabled() ? 1 : 0
            );

            BlockPos pos = new(piston.X, piston.Y, piston.Z);

            if (block == Block.PistonHead && piston.getProgress(tickDelta) < 0.5F)
            {
                var ctx = baseCtx with { CustomFlag = true };
                _pistonExtensionRenderer.Draw(block, pos, ref ctx);
            }
            else if (piston.isSource() && !piston.isExtending())
            {
                var headCtx = baseCtx with { OverrideTexture = ((BlockPistonBase)block).getTopTexture(), CustomFlag = piston.getProgress(tickDelta) < 0.5F };

                _pistonExtensionRenderer.Draw(Block.PistonHead, pos, ref headCtx);

                tess.setTranslationD(x - piston.X, y - piston.Y, z - piston.Z);

                var basePartCtx = baseCtx with { CustomFlag = true };
                _pistonBaseRenderer.Draw(block, pos, ref basePartCtx);
            }
            else
            {
                baseCtx.DrawBlock(block, pos);
            }

            tess.setTranslationD(0.0D, 0.0D, 0.0D);
            tess.draw();
            Lighting.turnOn();
        }
    }
}
