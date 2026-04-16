using BetaSharp.Blocks;
using BetaSharp.Blocks.Entities;
using BetaSharp.Client.Guis;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Entities.Models;
using BetaSharp.Client.Rendering.Legacy;

namespace BetaSharp.Client.Rendering.Blocks.Entities;

public class BlockEntitySignRenderer : BlockEntitySpecialRenderer
{
    private readonly SignModel signModel = new();

    public void renderTileEntitySignAt(BlockEntitySign sign, double x, double y, double z, float tickDelta)
    {
        Block block = sign.getBlock();
        GLManager.GL.PushMatrix();
        float signScale = 2.0F / 3.0F;
        float yawRotation;
        if (block == Block.Sign)
        {
            GLManager.GL.Translate((float)x + 0.5F, (float)y + 12.0F / 16.0F * signScale, (float)z + 0.5F);
            float standingRotation = sign.PushedBlockData * 360 / 16.0F;
            GLManager.GL.Rotate(-standingRotation, 0.0F, 1.0F, 0.0F);
            signModel.signStick.visible = true;
        }
        else
        {
            int wallFacing = sign.PushedBlockData;
            yawRotation = 0.0F;
            if (wallFacing == 2)
            {
                yawRotation = 180.0F;
            }

            if (wallFacing == 4)
            {
                yawRotation = 90.0F;
            }

            if (wallFacing == 5)
            {
                yawRotation = -90.0F;
            }

            GLManager.GL.Translate((float)x + 0.5F, (float)y + 12.0F / 16.0F * signScale, (float)z + 0.5F);
            GLManager.GL.Rotate(-yawRotation, 0.0F, 1.0F, 0.0F);
            GLManager.GL.Translate(0.0F, -(5.0F / 16.0F), -(7.0F / 16.0F));
            signModel.signStick.visible = false;
        }

        bindTextureByName("/item/sign.png");
        GLManager.GL.PushMatrix();
        GLManager.GL.Scale(signScale, -signScale, -signScale);
        signModel.Render(Scene);
        GLManager.GL.PopMatrix();
        ITextRenderer textRenderer = getFontRenderer();
        float textScale = (float)(1.0D / 60.0D) * signScale;
        GLManager.GL.Translate(0.0F, 0.5F * signScale, 0.07F * signScale);
        GLManager.GL.Scale(textScale, -textScale, textScale);
        GLManager.GL.Normal3(0.0F, 0.0F, -1.0F * textScale);
        GLManager.GL.DepthMask(false);

        for (int rowIndex = 0; rowIndex < sign.Texts.Length; ++rowIndex)
        {
            string rowText = sign.Texts[rowIndex];
            if (rowIndex == sign.CurrentRow)
            {
                rowText = "> " + rowText + " <";
                textRenderer.DrawString(rowText, -textRenderer.GetStringWidth(rowText) / 2,
                    rowIndex * 10 - sign.Texts.Length * 5, Color.Black);
            }
            else
            {
                textRenderer.DrawString(rowText, -textRenderer.GetStringWidth(rowText) / 2,
                    rowIndex * 10 - sign.Texts.Length * 5, Color.Black);
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
