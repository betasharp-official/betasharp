using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.Rendering.Entities;
using BetaSharp.Entities;
using Silk.NET.OpenGL.Legacy;

namespace BetaSharp.Client.Guis;

public class GuiInventory : GuiContainer
{
    private float _mouseX;
    private float _mouseY;

    public GuiInventory(EntityPlayer player) : base(player.playerScreenHandler)
    {
        AllowUserInput = true;
        player.increaseStat(BetaSharp.Achievements.OpenInventory, 1);
    }

    protected override void DrawGuiContainerForegroundLayer()
    {
        FontRenderer.DrawString("Crafting", 86, 16, 0x404040);
    }

    protected override void OnRender(RenderEventArgs e)
    {
        base.OnRender(e);
        _mouseX = e.MouseX;
        _mouseY = e.MouseY;
    }

    protected override void DrawGuiContainerBackgroundLayer(float partialTicks)
    {
        TextureHandle texture = MC.textureManager.GetTextureId("/gui/inventory.png");
        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
        MC.textureManager.BindTexture(texture);

        int guiLeft = (Width - _xSize) / 2;
        int guiTop = (Height - _ySize) / 2;

        DrawTextureRegion(guiLeft, guiTop, 0, 0, _xSize, _ySize);
        GLManager.GL.Enable(GLEnum.RescaleNormal);
        GLManager.GL.Enable(GLEnum.ColorMaterial);
        GLManager.GL.PushMatrix();
        GLManager.GL.Translate(guiLeft + 51, guiTop + 75, 50.0F);

        float scale = 30.0F;
        GLManager.GL.Scale(-scale, scale, scale);
        GLManager.GL.Rotate(180.0F, 0.0F, 0.0F, 1.0F);

        float bodyYaw = MC.player.bodyYaw;
        float headYaw = MC.player.yaw;
        float headPitch = MC.player.pitch;
        float lookX = guiLeft + 51 - _mouseX;
        float lookY = guiTop + 75 - 50 - _mouseY;

        GLManager.GL.Rotate(135.0F, 0.0F, 1.0F, 0.0F);
        Lighting.turnOn();
        GLManager.GL.Rotate(-135.0F, 0.0F, 1.0F, 0.0F);
        GLManager.GL.Rotate(-(float)Math.Atan(lookY / 40.0F) * 20.0F, 1.0F, 0.0F, 0.0F);

        MC.player.bodyYaw = (float)Math.Atan(lookX / 40.0F) * 20.0F;
        MC.player.yaw = (float)Math.Atan(lookX / 40.0F) * 40.0F;
        MC.player.pitch = -(float)Math.Atan(lookY / 40.0F) * 20.0F;
        MC.player.minBrightness = 1.0F;

        GLManager.GL.Translate(0.0F, MC.player.standingEyeHeight, 0.0F);
        EntityRenderDispatcher.instance.playerViewY = 180.0F;
        EntityRenderDispatcher.instance.renderEntityWithPosYaw(MC.player, 0.0D, 0.0D, 0.0D, 0.0F, 1.0F);

        MC.player.minBrightness = 0.0F;
        MC.player.bodyYaw = bodyYaw;
        MC.player.yaw = headYaw;
        MC.player.pitch = headPitch;

        GLManager.GL.PopMatrix();
        Lighting.turnOff();
        GLManager.GL.Disable(GLEnum.RescaleNormal);
    }
}
