using BetaSharp.Blocks;
using BetaSharp.Blocks.Entities;
using BetaSharp.Client.Guis;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Entities.Models;

namespace BetaSharp.Client.Rendering.Blocks.Entities;

public class BlockEntitySignRenderer : BlockEntitySpecialRenderer
{

    private readonly SignModel signModel = new();

    public void renderTileEntitySignAt(BlockEntitySign sign, double x, double y, double z, float partialTicks)
    {
        Block block = sign.getBlock();
        GLManager.GL.PushMatrix();
        float scale = 2.0F / 3.0F;
        float wallSignAngle;
        if (block == Block.Sign)
        {
            GLManager.GL.Translate((float)x + 0.5F, (float)y + 12.0F / 16.0F * scale, (float)z + 0.5F);
            float signRotation = sign.getPushedBlockData() * 360 / 16.0F;
            GLManager.GL.Rotate(-signRotation, 0.0F, 1.0F, 0.0F);
            signModel.signStick.visible = true;
        }
        else
        {
            int blockFacing = sign.getPushedBlockData();
            wallSignAngle = 0.0F;
            if (blockFacing == 2)
            {
                wallSignAngle = 180.0F;
            }

            if (blockFacing == 4)
            {
                wallSignAngle = 90.0F;
            }

            if (blockFacing == 5)
            {
                wallSignAngle = -90.0F;
            }

            GLManager.GL.Translate((float)x + 0.5F, (float)y + 12.0F / 16.0F * scale, (float)z + 0.5F);
            GLManager.GL.Rotate(-wallSignAngle, 0.0F, 1.0F, 0.0F);
            GLManager.GL.Translate(0.0F, -(5.0F / 16.0F), -(7.0F / 16.0F));
            signModel.signStick.visible = false;
        }

        bindTextureByName("/item/sign.png");
        GLManager.GL.PushMatrix();
        GLManager.GL.Scale(scale, -scale, -scale);
        signModel.Render();
        GLManager.GL.PopMatrix();
        TextRenderer fontRenderer = getFontRenderer();
        float textScale = (float)(1.0D / 60.0D) * scale;
        GLManager.GL.Translate(0.0F, 0.5F * scale, 0.07F * scale);
        GLManager.GL.Scale(textScale, -textScale, textScale);
        GLManager.GL.Normal3(0.0F, 0.0F, -1.0F * textScale);
        GLManager.GL.DepthMask(false);

        for (int row = 0; row < sign.Texts.Length; ++row)
        {
            string lineText = sign.Texts[row];
            if (row == sign.CurrentRow)
            {
                lineText = "> " + lineText + " <";
                fontRenderer.DrawString(lineText, -fontRenderer.GetStringWidth(lineText) / 2, row * 10 - sign.Texts.Length * 5, Color.Black);
            }
            else
            {
                fontRenderer.DrawString(lineText, -fontRenderer.GetStringWidth(lineText) / 2, row * 10 - sign.Texts.Length * 5, Color.Black);
            }
        }

        GLManager.GL.DepthMask(true);
        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
        GLManager.GL.PopMatrix();
    }

    public override void renderTileEntityAt(BlockEntity blockEntity, double x, double y, double z, float tickDelta)
    {
        renderTileEntitySignAt((BlockEntitySign)blockEntity, x, y, z, tickDelta);
    }
}
