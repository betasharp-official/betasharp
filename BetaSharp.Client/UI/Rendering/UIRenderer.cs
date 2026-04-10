using BetaSharp.Blocks;
using BetaSharp.Blocks.Entities;
using BetaSharp.Client.Guis;
using BetaSharp.Client.Options;
using BetaSharp.Client.Rendering;
using BetaSharp.Client.Rendering.Blocks;
using BetaSharp.Client.Rendering.Blocks.Entities;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.Rendering.Entities;
using BetaSharp.Client.Rendering.Items;
using BetaSharp.Entities;
using BetaSharp.Items;
using Silk.NET.Maths;
using SixLabors.Fonts;

namespace BetaSharp.Client.UI.Rendering;

public class UIRenderer(
    ITextRenderer textRenderer,
    ITextureManager textureManager,
    IEntityRenderDispatcher entityRenderDispatcher,
    IBlockEntityRenderDispatcher blockEntityRenderDispatcher,
    IUiRenderBackend uiRenderBackend,
    GameOptions gameOptions,
    Func<Vector2D<int>> getDisplaySize)
{
    public ITextureManager TextureManager { get; } = textureManager;
    public ITextRenderer TextRenderer { get; } = textRenderer;
    private readonly IEntityRenderDispatcher _entityRenderDispatcher = entityRenderDispatcher;
    private readonly IBlockEntityRenderDispatcher _blockEntityRenderDispatcher = blockEntityRenderDispatcher;
    private readonly IUiRenderBackend _uiRenderBackend = uiRenderBackend;
    private readonly ItemRenderer _itemRenderer = new();
    private readonly GameOptions _gameOptions = gameOptions;

    private float _translateX = 0;
    private float _translateY = 0;
    private readonly Stack<Vector2D<float>> _translationStack = new();

    public void Begin()
    {
        _uiRenderBackend.BeginFrame();

        _translateX = 0;
        _translateY = 0;
        _translationStack.Clear();
    }

    public void End()
    {
        _uiRenderBackend.EndFrame();
    }

    public void PushColor(Color color)
    {
        _uiRenderBackend.SetColor(color);
    }

    public void PopColor()
    {
        _uiRenderBackend.ResetColor();
    }

    public void SetDepthMask(bool flag) => _uiRenderBackend.SetDepthMask(flag);
    public void SetAlphaTest(bool flag)
    {
        _uiRenderBackend.SetAlphaTest(flag);
    }

    public void PushBlend(UiBlendFactor s, UiBlendFactor d)
    {
        _uiRenderBackend.SetBlendFunction(s, d);
    }

    public void PopBlend()
    {
        _uiRenderBackend.ResetBlendFunction();
    }

    public void ClearDepth()
    {
        _uiRenderBackend.ClearDepthBuffer();
    }

    public void PushTranslate(float x, float y)
    {
        _translationStack.Push(new(_translateX, _translateY));
        _translateX += x;
        _translateY += y;
    }

    public void PopTranslate()
    {
        if (_translationStack.Count > 0)
        {
            Vector2D<float> prev = _translationStack.Pop();
            _translateX = prev.X;
            _translateY = prev.Y;
        }
        else
        {
            _translateX = 0;
            _translateY = 0;
        }

        // Stability
        if (MathF.Abs(_translateX) < 0.0001f) _translateX = 0;
        if (MathF.Abs(_translateY) < 0.0001f) _translateY = 0;
    }

    public void EnableClipping(int x, int y, int width, int height)
    {
        Vector2D<int> displaySize = getDisplaySize();
        ScaledResolution res = new(_gameOptions, displaySize.X, displaySize.Y);

        int scale = res.ScaleFactor;
        int scaledWindowHeight = displaySize.Y;

        int physicalX = (int)((x + _translateX) * scale);
        int physicalWidth = width * scale;
        int physicalHeight = height * scale;
        int physicalY = scaledWindowHeight - (int)((y + _translateY) * scale) - physicalHeight;

        _uiRenderBackend.EnableScissor(physicalX, physicalY, (uint)physicalWidth, (uint)physicalHeight);
    }

    public void DisableClipping()
    {
        _uiRenderBackend.DisableScissor();
    }

    public void DrawRect(float x, float y, float width, float height, Color color)
    {
        int ix1 = (int)MathF.Floor(x + _translateX);
        int iy1 = (int)MathF.Floor(y + _translateY);
        int ix2 = (int)MathF.Floor(x + _translateX + width);
        int iy2 = (int)MathF.Floor(y + _translateY + height);
        DrawRectRaw(ix1, iy1, ix2, iy2, color);
    }

    public void DrawGradientRect(float x, float y, float width, float height, Color topColor, Color bottomColor)
    {
        int ix1 = (int)MathF.Floor(x + _translateX);
        int iy1 = (int)MathF.Floor(y + _translateY);
        int ix2 = (int)MathF.Floor(x + _translateX + width);
        int iy2 = (int)MathF.Floor(y + _translateY + height);
        DrawGradientRectRaw(ix1, iy1, ix2, iy2, topColor, bottomColor);
    }

    public void DrawText(string text, float x, float y, Color color, float scale = 1.0f, bool shadow = true)
    {
        if (scale == 1.0f)
        {
            if (shadow)
            {
                TextRenderer.DrawStringWithShadow(text, (int)MathF.Floor(x + _translateX), (int)MathF.Floor(y + _translateY), color);
            }
            else
            {
                TextRenderer.DrawString(text, (int)MathF.Floor(x + _translateX), (int)MathF.Floor(y + _translateY), color);
            }
            return;
        }

        _uiRenderBackend.PushMatrix();
        _uiRenderBackend.Translate(MathF.Floor(x + _translateX), MathF.Floor(y + _translateY), 0);
        _uiRenderBackend.Scale(scale, scale, 1);
        if (shadow)
        {
            TextRenderer.DrawStringWithShadow(text, 0, 0, color);
        }
        else
        {
            TextRenderer.DrawString(text, 0, 0, color);
        }
        _uiRenderBackend.PopMatrix();
    }

    public void DrawTextWrapped(string text, float x, float y, float maxWidth, Color color)
    {
        TextRenderer.DrawStringWrapped(text, (int)MathF.Floor(x + _translateX), (int)MathF.Floor(y + _translateY), (int)maxWidth, color);
    }

    public void DrawCenteredText(string text, float x, float y, Color color, float rotation = 0, float scale = 1.0f, bool shadow = true)
    {
        if (rotation == 0 && scale == 1.0f)
        {
            if (shadow)
            {
                DrawCenteredStringRaw(text, (int)MathF.Floor(x + _translateX), (int)MathF.Floor(y + _translateY), color);
            }
            else
            {
                TextRenderer.DrawString(text, (int)MathF.Floor(x + _translateX), (int)MathF.Floor(y + _translateY), color, HorizontalAlignment.Center);
            }
            return;
        }

        _uiRenderBackend.PushMatrix();
        _uiRenderBackend.Translate(MathF.Floor(x + _translateX), MathF.Floor(y + _translateY), 0);
        if (rotation != 0) _uiRenderBackend.Rotate(rotation, 0, 0, 1);
        if (scale != 1.0f) _uiRenderBackend.Scale(scale, scale, 1);

        if (shadow)
        {
            DrawCenteredStringRaw(text, 0, 0, color);
        }
        else
        {
            TextRenderer.DrawString(text, 0, 0, color, HorizontalAlignment.Center);
        }

        _uiRenderBackend.PopMatrix();
    }

    public void DrawTexture(TextureHandle texture, float x, float y, float width, float height)
    {
        TextureManager.BindTexture(texture);
        DrawBoundTexture(x, y, width, height);
    }

    public void DrawBoundTexture(float x, float y, float width, float height)
    {
        float finalX = MathF.Floor(x + _translateX);
        float finalY = MathF.Floor(y + _translateY);
        _uiRenderBackend.DrawTexturedQuad(
            finalX,
            finalY,
            finalX + width,
            finalY + height,
            0.0f,
            0.0,
            0.0,
            1.0,
            1.0);
    }

    public void DrawTexturedModalRect(TextureHandle texture, float x, float y, float u, float v, float width, float height)
    {
        DrawTexturedModalRect(texture, x, y, u, v, width, height, width, height, 0.0f);
    }

    public void DrawTexturedModalRect(TextureHandle texture, float x, float y, float u, float v, float width, float height, float uvWidth, float uvHeight)
    {
        DrawTexturedModalRect(texture, x, y, u, v, width, height, uvWidth, uvHeight, 0.0f);
    }

    public void DrawTexturedModalRect(TextureHandle texture, float x, float y, float u, float v, float width, float height, float uvWidth, float uvHeight, float z)
    {
        TextureManager.BindTexture(texture);
        float f = 0.00390625F;
        float finalX = MathF.Floor(x + _translateX);
        float finalY = MathF.Floor(y + _translateY);
        _uiRenderBackend.DrawTexturedQuad(
            finalX,
            finalY,
            finalX + width,
            finalY + height,
            z,
            (u + 0) * f,
            (v + 0) * f,
            (u + uvWidth) * f,
            (v + uvHeight) * f);
    }

    public void DrawRepeatingTexture(TextureHandle texture, float x, float y, float width, float height, float textureScale, float scrollOffsetY = 0f)
    {
        TextureManager.BindTexture(texture);

        float finalX = MathF.Floor(x + _translateX);
        float finalY = MathF.Floor(y + _translateY);

        _uiRenderBackend.SetColor(new Color(64, 64, 64, 255));
        _uiRenderBackend.DrawTexturedQuad(
            finalX,
            finalY,
            finalX + width,
            finalY + height,
            0.0f,
            finalX / textureScale,
            (finalY + scrollOffsetY) / textureScale,
            (finalX + width) / textureScale,
            (finalY + height + scrollOffsetY) / textureScale);
        _uiRenderBackend.ResetColor();
    }

    public void DrawItemIntoGui(ItemRenderer itemRenderer, int itemId, int itemMeta, int textureId, float x, float y)
    {
        itemRenderer.drawItemIntoGui(TextRenderer, TextureManager, itemId, itemMeta, textureId, (int)(x + _translateX), (int)(y + _translateY));
    }

    public void DrawItem(ItemStack stack, float x, float y)
    {
        if (stack == null) return;

        bool isBlock = stack.ItemId < 256 && BlockRenderer.IsSideLit(Block.Blocks[stack.ItemId].getRenderType());

        if (isBlock)
        {
            _uiRenderBackend.PushMatrix();
            _uiRenderBackend.Translate(0, 0, 32.0f);

            _uiRenderBackend.SetCullFace(false);
            _uiRenderBackend.SetRescaleNormal(true);
            _uiRenderBackend.SetDepthTest(true);

            _uiRenderBackend.TurnOnGuiLighting();
            _itemRenderer.renderItemIntoGUI(TextRenderer, TextureManager, stack, (int)(x + _translateX), (int)(y + _translateY));
            _uiRenderBackend.TurnOffLighting();

            _uiRenderBackend.SetCullFace(false);
            _uiRenderBackend.SetDepthTest(false);
            _uiRenderBackend.SetRescaleNormal(false);
            _uiRenderBackend.PopMatrix();
        }
        else
        {
            _uiRenderBackend.TurnOffLighting();
            _uiRenderBackend.SetDepthTest(false);
            _itemRenderer.renderItemIntoGUI(TextRenderer, TextureManager, stack, (int)(x + _translateX), (int)(y + _translateY));
        }
    }

    public void DrawItemOverlay(ItemStack stack, float x, float y)
    {
        if (stack == null) return;

        _uiRenderBackend.TurnOffLighting();
        _uiRenderBackend.SetDepthTest(false);
        _itemRenderer.renderItemOverlayIntoGUI(TextRenderer, TextureManager, stack, (int)(x + _translateX), (int)(y + _translateY));
    }

    public void DrawEntity(Entity entity, float x, float y, float scale, float mouseX, float mouseY)
    {
        _uiRenderBackend.SetRescaleNormal(true);
        _uiRenderBackend.SetColorMaterial(true);
        _uiRenderBackend.SetDepthTest(true);
        _uiRenderBackend.PushMatrix();
        _uiRenderBackend.Translate(x + _translateX, y + _translateY, 50.0F);

        _uiRenderBackend.Scale(-scale, scale, scale);
        _uiRenderBackend.Rotate(180.0F, 0.0F, 0.0F, 1.0F);
        _uiRenderBackend.SetCullFace(false);

        float bodyYaw = entity is EntityLiving el ? el.bodyYaw : entity.yaw;
        float headYaw = entity.yaw;
        float headPitch = entity.pitch;
        float lookX = x + _translateX - mouseX;
        float lookY = y + _translateY - 50 - mouseY;

        _uiRenderBackend.Rotate(135.0F, 0.0F, 1.0F, 0.0F);
        _uiRenderBackend.TurnOnLighting();
        _uiRenderBackend.Rotate(-135.0F, 0.0F, 1.0F, 0.0F);
        _uiRenderBackend.Rotate(-(float)Math.Atan(lookY / 40.0F) * 20.0F, 1.0F, 0.0F, 0.0F);

        if (entity is EntityLiving el2)
        {
            el2.bodyYaw = (float)Math.Atan(lookX / 40.0F) * 20.0F;
        }
        entity.yaw = (float)Math.Atan(lookX / 40.0F) * 40.0F;
        entity.pitch = -(float)Math.Atan(lookY / 40.0F) * 20.0F;
        entity.minBrightness = 1.0F;

        _uiRenderBackend.Translate(0.0F, entity.standingEyeHeight, 0.0F);
        _entityRenderDispatcher.PlayerViewY = 180.0F;
        _entityRenderDispatcher.RenderEntityWithPosYaw(entity, 0.0D, 0.0D, 0.0D, 0.0F, 1.0F);

        entity.minBrightness = 0.0F;
        if (entity is EntityLiving el3)
        {
            el3.bodyYaw = bodyYaw;
        }
        entity.yaw = headYaw;
        entity.pitch = headPitch;

        _uiRenderBackend.PopMatrix();
        _uiRenderBackend.TurnOffLighting();
        _uiRenderBackend.SetCullFace(false);
        _uiRenderBackend.SetDepthTest(false);
        _uiRenderBackend.SetRescaleNormal(false);
        _uiRenderBackend.SetColorMaterial(false);
    }

    private void DrawRectRaw(int x1, int y1, int x2, int y2, Color color)
    {
        if (x1 < x2) (x1, x2) = (x2, x1);
        if (y1 < y2) (y1, y2) = (y2, y1);
        _uiRenderBackend.DrawSolidQuad(x1, y1, x2, y2, color);
    }

    private void DrawGradientRectRaw(int right, int bottom, int left, int top, Color topColor, Color bottomColor)
    {
        _uiRenderBackend.DrawGradientQuad(left, top, right, bottom, topColor, bottomColor);
    }

    private void DrawCenteredStringRaw(string text, int x, int y, Color color)
    {
        TextRenderer.DrawStringWithShadow(text, x - TextRenderer.GetStringWidth(text) / 2, y, color);
    }

    public void DrawSign(BlockEntitySign sign, float x, float y, float scale)
    {
        _uiRenderBackend.SetRescaleNormal(true);
        _uiRenderBackend.SetDepthTest(true);
        _uiRenderBackend.PushMatrix();
        _uiRenderBackend.Translate(x + _translateX, y + _translateY, 50.0F);

        _uiRenderBackend.Scale(-scale, -scale, -scale);
        _uiRenderBackend.Rotate(180.0F, 0.0F, 1.0F, 0.0F);

        Block signBlock = sign.getBlock();
        if (signBlock == Block.Sign)
        {
            float rotation = sign.PushedBlockData * 360 / 16.0F;
            _uiRenderBackend.Rotate(rotation, 0.0F, 1.0F, 0.0F);
            _uiRenderBackend.Translate(0.0F, -1.0625F, 0.0F);
        }
        else
        {
            int rotationIndex = sign.PushedBlockData;
            float angle = 0.0F;
            if (rotationIndex == 2) angle = 180.0F;
            if (rotationIndex == 4) angle = 90.0F;
            if (rotationIndex == 5) angle = -90.0F;

            _uiRenderBackend.Rotate(angle, 0.0F, 1.0F, 0.0F);
            _uiRenderBackend.Translate(0.0F, -1.0625F, 0.0F);
        }

        _blockEntityRenderDispatcher.RenderTileEntityAt(sign, -0.5D, -0.75D, -0.5D, 0.0F);
        _uiRenderBackend.PopMatrix();
        _uiRenderBackend.SetDepthTest(false);
        _uiRenderBackend.SetRescaleNormal(false);
    }
}
