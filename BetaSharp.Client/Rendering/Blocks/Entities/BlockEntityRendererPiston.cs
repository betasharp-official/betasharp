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

        Block block = Block.Blocks[piston.PushedBlockId];
        if (piston.GetProgress(tickDelta) < 1.0F)
        {
            Tessellator tess = Tessellator.instance;
            bindTextureByName("/terrain.png");
            Lighting.turnOff();
            GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
            GLManager.GL.Enable(GLEnum.Blend);
            GLManager.GL.Disable(GLEnum.CullFace);

            GLManager.GL.ShadeModel(GLEnum.Smooth);

            tess.startDrawingQuads();
            tess.setTranslationD(
                x - piston.X + piston.GetRenderOffsetX(tickDelta),
                y - piston.Y + piston.GetRenderOffsetY(tickDelta),
                z - piston.Z + piston.GetRenderOffsetZ(tickDelta)
            );

            tess.setColorOpaque(1, 1, 1);

            var baseCtx = new BlockRenderContext(
                blockReader: piston.World.Reader,
                lighting: piston.World.Lighting,
                tess: tess,
                renderAllFaces: true,
                aoBlendMode: 1
            );

            BlockPos pos = new(piston.X, piston.Y, piston.Z);

            if (block == Block.PistonHead && piston.GetProgress(tickDelta) < 0.5F)
            {
                var ctx = baseCtx with { CustomFlag = true };
                _pistonExtensionRenderer.Draw(block, pos, ref ctx);
            }
            else if (piston.IsSource && !piston.IsExtending)
            {
                var headCtx = baseCtx with { OverrideTexture = ((BlockPistonBase)block).GetTopTexture(), CustomFlag = piston.GetProgress(tickDelta) < 0.5F };

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
