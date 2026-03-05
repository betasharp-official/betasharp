using BetaSharp.Client.Input;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.OpenGL;

namespace BetaSharp.Client.Guis;

public abstract class GuiSlot
{
    private readonly BetaSharp _game;
    private readonly int _width;
    private readonly int _height;
    protected readonly int _top;
    protected readonly int _bottom;
    private readonly int _right;
    private readonly int _left;
    protected readonly int _posZ;

    private int _scrollUpButtonID;
    private int _scrollDownButtonID;
    private float _initialClickY = -2.0F;
    private float _scrollMultiplier;
    private float _amountScrolled;
    private int _selectedElement = -1;
    private long _lastClicked;

    private bool _showSelectionHighlight = true;
    private bool _hasHeader;
    private int _headerHeight;

    public GuiSlot(BetaSharp game, int width, int height, int top, int bottom, int posZ)
    {
        _game = game;
        _width = width;
        _height = height;
        _top = top;
        _bottom = bottom;
        _posZ = posZ;
        _left = 0;
        _right = width;
    }

    public void SetShowSelectionHighlight(bool value) => _showSelectionHighlight = value;


    protected void SetHeader(bool hasHeader, int headerHeight)
    {
        _hasHeader = hasHeader;
        _headerHeight = headerHeight;
        if (!hasHeader) _headerHeight = 0;
    }

    public abstract int GetSize();

    protected abstract void ElementClicked(int index, bool doubleClick);

    protected abstract bool IsSelected(int slotIndex);

    protected virtual int GetContentHeight() => GetSize() * _posZ + _headerHeight;

    protected abstract void DrawBackground();

    protected abstract void DrawSlot(int index, int x, int y, int height, Tessellator tess);

    protected virtual void DrawHeader(int x, int y, Tessellator tess) { }

    protected virtual void HeaderClicked(int var1, int var2)
    {
    }

    protected virtual void PostDrawScreen(int mouseX, int mouseY) { }

    public int GetSlotAt(int mouseX, int mouseY)
    {
        int minX = _width / 2 - 110;
        int maxX = _width / 2 + 110;
        int relativeY = mouseY - _top - _headerHeight + (int)_amountScrolled - 4;
        int index = relativeY / _posZ;

        return (mouseX >= minX && mouseX <= maxX && index >= 0 && relativeY >= 0 && index < GetSize())
            ? index
            : -1;
    }

    public void RegisterScrollButtons(List<GuiButton> buttons, int upId, int downId)
    {
        _scrollUpButtonID = upId;
        _scrollDownButtonID = downId;
    }

    private void BindAmountScrolled()
    {
        int maxScroll = GetContentHeight() - (_bottom - _top - 4);
        if (maxScroll < 0) maxScroll /= 2;

        if (_amountScrolled < 0.0f) _amountScrolled = 0.0f;
        if (_amountScrolled > maxScroll) _amountScrolled = maxScroll;

    }

    public void ActionPerformed(GuiButton button)
    {
        if (!button.Enabled) return;

        if (button.Id == _scrollUpButtonID)
        {
            _amountScrolled -= _posZ * 2 / 3;
            _initialClickY = -2.0f;
            BindAmountScrolled();
        }
        else if (button.Id == _scrollDownButtonID)
        {
            _amountScrolled += _posZ * 2 / 3;
            _initialClickY = -2.0f;
            BindAmountScrolled();
        }
    }

    public void DrawScreen(int mouseX, int mouseY, float partialTicks)
    {
        DrawBackground();

        int listSize = GetSize();
        int scrollbarXStart = _width / 2 + 124;
        int scrollbarXEnd = scrollbarXStart + 6;

        if (Mouse.isButtonDown(0))
        {
            if (_initialClickY == -1.0f)
            {
                bool shouldCaptureMouse = true;

                if (mouseY >= _top && mouseY <= _bottom)
                {
                    int contentMinX = _width / 2 - 110;
                    int contentMaxX = _width / 2 + 110;
                    int relativeY = mouseY - _top - _headerHeight + (int)_amountScrolled - 4;
                    int slotIndex = relativeY / _posZ;

                    if (mouseX >= contentMinX && mouseX <= contentMaxX && slotIndex >= 0 && relativeY >= 0 && slotIndex < listSize)
                    {
                        bool isDoubleClick = slotIndex == _selectedElement && (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
 - _lastClicked < 250L);
                        ElementClicked(slotIndex, isDoubleClick);
                        _selectedElement = slotIndex;
                        _lastClicked = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
;
                    }
                    else if (mouseX >= contentMinX && mouseX <= contentMaxX && relativeY < 0)
                    {
                        HeaderClicked(mouseX - contentMinX, mouseY - _top + (int)_amountScrolled - 4);
                        shouldCaptureMouse = false;
                    }

                    if (mouseX >= scrollbarXStart && mouseX <= scrollbarXEnd)
                    {
                        _scrollMultiplier = -1.0f;
                        int maxScroll = Math.Max(1, GetContentHeight() - (_bottom - _top - 4));
                        int viewHeight = _bottom - _top;
                        int barHeight = Math.Clamp((viewHeight * viewHeight) / GetContentHeight(), 32, viewHeight - 8);

                        _scrollMultiplier /= (float)(viewHeight - barHeight) / maxScroll;
                    }
                    else
                    {
                        _scrollMultiplier = 1.0f;
                    }

                    _initialClickY = shouldCaptureMouse ? mouseY : -2.0f;
                }
                else
                {
                    _initialClickY = -2.0f;
                }
            }
            else if (_initialClickY >= 0.0f)
            {
                _amountScrolled -= (mouseY - _initialClickY) * _scrollMultiplier;
                _initialClickY = mouseY;
            }
        }
        else
        {
            _initialClickY = -1.0f;
        }

        BindAmountScrolled();

        GLManager.GL.Disable(GLEnum.Lighting);
        GLManager.GL.Disable(GLEnum.Fog);
        var tess = Tessellator.instance;

        _game.textureManager.BindTexture(_game.textureManager.GetTextureId("/gui/background.png"));
        GLManager.GL.Color4(1.0f, 1.0f, 1.0f, 1.0f);
        const float textureScale = 32.0f;

        tess.startDrawingQuads();
        tess.setColorOpaque_I(0x202020);
        tess.addVertexWithUV(_left, _bottom, 0.0, _left / textureScale, (_bottom + (int)_amountScrolled) / textureScale);
        tess.addVertexWithUV(_right, _bottom, 0.0, _right / textureScale, (_bottom + (int)_amountScrolled) / textureScale);
        tess.addVertexWithUV(_right, _top, 0.0, _right / textureScale, (_top + (int)_amountScrolled) / textureScale);
        tess.addVertexWithUV(_left, _top, 0.0, _left / textureScale, (_top + (int)_amountScrolled) / textureScale);
        tess.draw();

        int startX = _width / 2 - 92 - 16;
        int startY = _top + 4 - (int)_amountScrolled;

        if (_hasHeader)
        {
            DrawHeader(startX, startY, tess);
        }

        for (int i = 0; i < listSize; ++i)
        {
            int slotY = startY + i * _posZ + _headerHeight;
            int slotHeight = _posZ - 4;

            if (slotY > _bottom || slotY + slotHeight < _top) continue;

            if (_showSelectionHighlight && IsSelected(i))
            {
                int minX = _width / 2 - 110;
                int maxX = _width / 2 + 110;
                GLManager.GL.Color4(1.0f, 1.0f, 1.0f, 1.0f);
                GLManager.GL.Disable(GLEnum.Texture2D);

                tess.startDrawingQuads();
                tess.setColorOpaque_I(0x808080); // Outer border (Gray)
                tess.addVertexWithUV(minX, slotY + slotHeight + 2, 0.0, 0.0, 1.0);
                tess.addVertexWithUV(maxX, slotY + slotHeight + 2, 0.0, 1.0, 1.0);
                tess.addVertexWithUV(maxX, slotY - 2, 0.0, 1.0, 0.0);
                tess.addVertexWithUV(minX, slotY - 2, 0.0, 0.0, 0.0);
                tess.setColorOpaque_I(0x000000); // Inner background (Black)
                tess.addVertexWithUV(minX + 1, slotY + slotHeight + 1, 0.0, 0.0, 1.0);
                tess.addVertexWithUV(maxX - 1, slotY + slotHeight + 1, 0.0, 1.0, 1.0);
                tess.addVertexWithUV(maxX - 1, slotY - 1, 0.0, 1.0, 0.0);
                tess.addVertexWithUV(minX + 1, slotY - 1, 0.0, 0.0, 0.0);
                tess.draw();
                GLManager.GL.Enable(GLEnum.Texture2D);
            }

            DrawSlot(i, startX, slotY, slotHeight, tess);
        }

        GLManager.GL.Disable(GLEnum.DepthTest);
        OverlayBackground(0, _top, 255, 255);
        OverlayBackground(_bottom, _height, 255, 255);

        GLManager.GL.Enable(GLEnum.Blend);
        GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
        GLManager.GL.Disable(GLEnum.AlphaTest);
        GLManager.GL.ShadeModel(GLEnum.Smooth);
        GLManager.GL.Disable(GLEnum.Texture2D);

        // Top/Bottom gradient shadows
        const int shadowHeight = 4;
        tess.startDrawingQuads();
        tess.setColorRGBA_I(0x000000, 0);
        tess.addVertexWithUV(_left, _top + shadowHeight, 0.0, 0.0, 1.0);
        tess.addVertexWithUV(_right, _top + shadowHeight, 0.0, 1.0, 1.0);
        tess.setColorOpaque_I(0x000000);
        tess.addVertexWithUV(_right, _top, 0.0, 1.0, 0.0);
        tess.addVertexWithUV(_left, _top, 0.0, 0.0, 0.0);
        tess.draw();

        tess.startDrawingQuads();
        tess.setColorOpaque_I(0x000000);
        tess.addVertexWithUV(_left, _bottom, 0.0, 0.0, 1.0);
        tess.addVertexWithUV(_right, _bottom, 0.0, 1.0, 1.0);
        tess.setColorRGBA_I(0x000000, 0);
        tess.addVertexWithUV(_right, _bottom - shadowHeight, 0.0, 1.0, 0.0);
        tess.addVertexWithUV(_left, _bottom - shadowHeight, 0.0, 0.0, 0.0);
        tess.draw();

        // Scrollbar Rendering
        int scrollRange = GetContentHeight() - (_bottom - _top - 4);
        if (scrollRange > 0)
        {
            int viewHeight = _bottom - _top;
            int barHeight = Math.Clamp((viewHeight * viewHeight) / GetContentHeight(), 32, viewHeight - 8);
            int barY = (int)_amountScrolled * (viewHeight - barHeight) / scrollRange + _top;
            barY = Math.Max(barY, _top);

            // Bar Background
            tess.startDrawingQuads();
            tess.setColorOpaque_I(0x000000);
            tess.addVertexWithUV(scrollbarXStart, _bottom, 0.0, 0.0, 1.0);
            tess.addVertexWithUV(scrollbarXEnd, _bottom, 0.0, 1.0, 1.0);
            tess.addVertexWithUV(scrollbarXEnd, _top, 0.0, 1.0, 0.0);
            tess.addVertexWithUV(scrollbarXStart, _top, 0.0, 0.0, 0.0);
            tess.draw();

            // Bar Body
            tess.startDrawingQuads();
            tess.setColorOpaque_I(0x808080);
            tess.addVertexWithUV(scrollbarXStart, barY + barHeight, 0.0, 0.0, 1.0);
            tess.addVertexWithUV(scrollbarXEnd, barY + barHeight, 0.0, 1.0, 1.0);
            tess.addVertexWithUV(scrollbarXEnd, barY, 0.0, 1.0, 0.0);
            tess.addVertexWithUV(scrollbarXStart, barY, 0.0, 0.0, 0.0);
            tess.draw();

            // Bar Highlight
            tess.startDrawingQuads();
            tess.setColorOpaque_I(0xC0C0C0);
            tess.addVertexWithUV(scrollbarXStart, barY + barHeight - 1, 0.0, 0.0, 1.0);
            tess.addVertexWithUV(scrollbarXEnd - 1, barY + barHeight - 1, 0.0, 1.0, 1.0);
            tess.addVertexWithUV(scrollbarXEnd - 1, barY, 0.0, 1.0, 0.0);
            tess.addVertexWithUV(scrollbarXStart, barY, 0.0, 0.0, 0.0);
            tess.draw();
        }

        PostDrawScreen(mouseX, mouseY);

        GLManager.GL.Enable(GLEnum.Texture2D);
        GLManager.GL.ShadeModel(GLEnum.Flat);
        GLManager.GL.Enable(GLEnum.AlphaTest);
        GLManager.GL.Disable(GLEnum.Blend);
    }

    private void OverlayBackground(int startY, int endY, int alphaStart, int alphaEnd)
    {
        var tess = Tessellator.instance;
        var textureId = (uint)_game.textureManager.GetTextureId("/gui/background.png").Id;

        _game.textureManager.BindTexture(_game.textureManager.GetTextureId("/gui/background.png"));
        GLManager.GL.Color4(1.0f, 1.0f, 1.0f, 1.0f);

        const float textureScale = 32.0f;

        tess.startDrawingQuads();

        // Bottom vertices
        tess.setColorRGBA_I(0x404040, alphaEnd);
        tess.addVertexWithUV(0.0, endY, 0.0, 0.0, endY / (double)textureScale);
        tess.addVertexWithUV(_width, endY, 0.0, _width / textureScale, endY / (double)textureScale);

        // Top vertices
        tess.setColorRGBA_I(0x404040, alphaStart);
        tess.addVertexWithUV(_width, startY, 0.0, _width / textureScale, startY / (double)textureScale);
        tess.addVertexWithUV(0.0, startY, 0.0, 0.0, startY / (double)textureScale);

        tess.draw();
    }
}
