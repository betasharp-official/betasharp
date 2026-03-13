using BetaSharp.Blocks;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering.Blocks.Renderers;

public class RepeaterRenderer : IBlockRenderer
{
    public bool Draw(Block block, in BlockPos pos, ref BlockRenderContext ctx)
    {
        int metadata = ctx.BlockReader.GetBlockMeta(pos.x, pos.y, pos.z);
        int direction = metadata & 3;
        int delay = (metadata & 12) >> 2;
        // 1. Base Rendering
        var slabCtx = ctx with { EnableAo = true, AoBlendMode = 0, UvRotateTop = direction % 4 };

        slabCtx.DrawBlock(block, pos);

        // 2. Prepare Torch Rendering
        float luminance = block.getLuminance(ctx.Lighting, pos.x, pos.y, pos.z);
        if (Block.BlocksLightLuminance[block.id] > 0)
        {
            luminance = (luminance + 1.0F) * 0.5F;
        }

        ctx.Tess.setColorOpaque_F(luminance, luminance, luminance);

        // Torch pins are rendered slightly below the slab surface so they sit inside it
        float torchVerticalOffset = -0.1875F;
        float staticTorchX = 0.0F;
        float staticTorchZ = 0.0F;
        float delayTorchX = 0.0F;
        float delayTorchZ = 0.0F;

        switch (direction)
        {
            case 0: // South
                delayTorchZ = -0.3125f;
                staticTorchZ = BlockRedstoneRepeater.RenderOffset[delay];
                break;
            case 1: // West
                delayTorchX = 0.3125f;
                staticTorchX = -BlockRedstoneRepeater.RenderOffset[delay];
                break;
            case 2: // North
                delayTorchZ = 0.3125f;
                staticTorchZ = -BlockRedstoneRepeater.RenderOffset[delay];
                break;
            case 3: // East
                delayTorchX = -0.3125f;
                staticTorchX = BlockRedstoneRepeater.RenderOffset[delay];
                break;
        }

        // 3. Render the two torch pins
        slabCtx.DrawTorch(block, new Vec3D(pos.x + staticTorchX, pos.y + torchVerticalOffset, pos.z + staticTorchZ), 0.0f, 0.0f);
        slabCtx.DrawTorch(block, new Vec3D(pos.x + delayTorchX, pos.y + torchVerticalOffset, pos.z + delayTorchZ), 0.0f, 0.0f);
        return true;
    }
}
