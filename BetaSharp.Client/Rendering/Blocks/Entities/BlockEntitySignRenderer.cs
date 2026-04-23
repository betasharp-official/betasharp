using BetaSharp.Blocks;
using BetaSharp.Blocks.Entities;
using BetaSharp.Client.Guis;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Entities.Models;

namespace BetaSharp.Client.Rendering.Blocks.Entities;

public class BlockEntitySignRenderer : BlockEntitySpecialRenderer
{

    private readonly SignModel signModel = new();

    public void renderTileEntitySignAt(BlockEntitySign sign, double x, double y, double z, float tickDelta)
    {
        Block signBlock = sign.getBlock();
        GLManager.GL.PushMatrix();
        float modelScale = 2.0F / 3.0F;
        float rotationYaw;
        if (signBlock == Block.Sign)
        {
            GLManager.GL.Translate((float)x + 0.5F, (float)y + 12.0F / 16.0F * modelScale, (float)z + 0.5F);
            float rotationDegrees = sign.PushedBlockData * 360 / 16.0F;
            GLManager.GL.Rotate(-rotationDegrees, 0.0F, 1.0F, 0.0F);
            signModel.signStick.visible = true;
        }
        else
        {
            int wallFacing = sign.PushedBlockData;
            rotationYaw = 0.0F;
            if (wallFacing == 2)
            {
                rotationYaw = 180.0F;
            }

            if (wallFacing == 4)
            {
                rotationYaw = 90.0F;
            }

            if (wallFacing == 5)
            {
                rotationYaw = -90.0F;
            }

            GLManager.GL.Translate((float)x + 0.5F, (float)y + 12.0F / 16.0F * modelScale, (float)z + 0.5F);
            GLManager.GL.Rotate(-rotationYaw, 0.0F, 1.0F, 0.0F);
            GLManager.GL.Translate(0.0F, -(5.0F / 16.0F), -(7.0F / 16.0F));
            signModel.signStick.visible = false;
        }

        bindTextureByName("/item/sign.png");
        GLManager.GL.PushMatrix();
        GLManager.GL.Scale(modelScale, -modelScale, -modelScale);
        signModel.Render();
        GLManager.GL.PopMatrix();
        TextRenderer fontRenderer = getFontRenderer();
        rotationYaw = (float)(1.0D / 60.0D) * modelScale;
        GLManager.GL.Translate(0.0F, 0.5F * modelScale, 0.07F * modelScale);
        GLManager.GL.Scale(rotationYaw, -rotationYaw, rotationYaw);
        GLManager.GL.Normal3(0.0F, 0.0F, -1.0F * rotationYaw);
        GLManager.GL.DepthMask(false);

        for (int lineIndex = 0; lineIndex < sign.Texts.Length; ++lineIndex)
        {
            string lineText = sign.Texts[lineIndex];
            if (lineIndex == sign.CurrentRow)
            {
                lineText = "> " + lineText + " <";
                fontRenderer.DrawString(lineText, -fontRenderer.GetStringWidth(lineText) / 2, lineIndex * 10 - sign.Texts.Length * 5, Color.Black);
            }
            else
            {
                fontRenderer.DrawString(lineText, -fontRenderer.GetStringWidth(lineText) / 2, lineIndex * 10 - sign.Texts.Length * 5, Color.Black);
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
