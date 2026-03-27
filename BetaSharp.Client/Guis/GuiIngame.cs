using BetaSharp.Blocks;
using BetaSharp.Blocks.Materials;
using BetaSharp.Client.Guis.Debug;
using BetaSharp.Client.Guis.Debug.Components;
using BetaSharp.Client.Options;
using BetaSharp.Client.Rendering;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.OpenGL;
using BetaSharp.Client.Rendering.Items;
using BetaSharp.Inventorys;
using BetaSharp.Items;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.ClientData.Colors;

namespace BetaSharp.Client.Guis;

public class GuiIngame : Gui
{
    private readonly DebugOverlay _debug;
    private static readonly ItemRenderer s_itemRenderer = new();
    private readonly List<ChatLine> _chatMessageList = [];
    private readonly JavaRandom _rand = new();
    private int _chatScrollPos = 0;
    private readonly BetaSharp _game;
    public string? HoveredItemName { get; } = null;
    private int _updateCounter = 0;
    private string _recordPlaying = "";
    private int _recordPlayingUpFor = 0;
    private bool _isRecordMessageRainbow = false;
    public float _damageGuiPartialTime;
    private float _prevVignetteBrightness = 1.0F;

    public GuiIngame(BetaSharp gameInstance)
    {
        _game = gameInstance;

        _debug = new DebugOverlay(gameInstance);
        _debug.Components.Add(new DebugVersion());
    }

    private static int HSBtoRGB(float hue, float saturation, float brightness)
    {
        int r = 0, g = 0, b = 0;
        if (saturation == 0)
        {
            r = g = b = (int)(brightness * 255.0f + 0.5f);
        }
        else
        {
            float h = (hue - (float)Math.Floor(hue)) * 6.0f;
            float f = h - (float)Math.Floor(h);
            float p = brightness * (1.0f - saturation);
            float q = brightness * (1.0f - saturation * f);
            float t = brightness * (1.0f - (saturation * (1.0f - f)));
            switch ((int)h)
            {
                case 0:
                    r = (int)(brightness * 255.0f + 0.5f);
                    g = (int)(t * 255.0f + 0.5f);
                    b = (int)(p * 255.0f + 0.5f);
                    break;
                case 1:
                    r = (int)(q * 255.0f + 0.5f);
                    g = (int)(brightness * 255.0f + 0.5f);
                    b = (int)(p * 255.0f + 0.5f);
                    break;
                case 2:
                    r = (int)(p * 255.0f + 0.5f);
                    g = (int)(brightness * 255.0f + 0.5f);
                    b = (int)(t * 255.0f + 0.5f);
                    break;
                case 3:
                    r = (int)(p * 255.0f + 0.5f);
                    g = (int)(q * 255.0f + 0.5f);
                    b = (int)(brightness * 255.0f + 0.5f);
                    break;
                case 4:
                    r = (int)(t * 255.0f + 0.5f);
                    g = (int)(p * 255.0f + 0.5f);
                    b = (int)(brightness * 255.0f + 0.5f);
                    break;
                case 5:
                    r = (int)(brightness * 255.0f + 0.5f);
                    g = (int)(p * 255.0f + 0.5f);
                    b = (int)(q * 255.0f + 0.5f);
                    break;
            }
        }
        return unchecked((int)(0xFF000000 | ((uint)r << 16) | ((uint)g << 8) | ((uint)b << 0)));
    }

    public void RenderGameOverlay(float partialTicks)
    {
        ScaledResolution scaled = new(_game.options, _game.displayWidth, _game.displayHeight);
        int scaledWidth = scaled.ScaledWidth;
        int scaledHeight = scaled.ScaledHeight;
        TextRenderer font = _game.fontRenderer;
        _game.gameRenderer.setupHudRender();
        GLManager.GL.Enable(GLEnum.Blend);
        if (BetaSharp.isFancyGraphicsEnabled())
        {
            RenderVignette(_game.player.getBrightnessAtEyes(partialTicks), scaledWidth, scaledHeight);
        }

        ItemStack helmet = _game.player.inventory.armorItemInSlot(3);
        if (_game.options.CameraMode == EnumCameraMode.FirstPerson && helmet != null && helmet.itemId == Block.Pumpkin.id)
        {
            RenderPumpkinBlur(scaledWidth, scaledHeight);
        }

        float screenDistortion = _game.player.lastScreenDistortion + (_game.player.changeDimensionCooldown - _game.player.lastScreenDistortion) * partialTicks;
        if (screenDistortion > 0.0F)
        {
            RenderPortalOverlay(screenDistortion, scaledWidth, scaledHeight);
        }

        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
        _game.textureManager.BindTexture(_game.textureManager.GetTextureId("/gui/gui.png"));
        InventoryPlayer inventory = _game.player.inventory;
        _zLevel = -90.0F;
        int yOffset = _game.isControllerMode ? -40 : 0;
        DrawTexturedModalRect(scaledWidth / 2 - 91, scaledHeight - 22 + yOffset, 0, 0, 182, 22);
        DrawTexturedModalRect(scaledWidth / 2 - 91 - 1 + inventory.selectedSlot * 20, scaledHeight - 22 - 1 + yOffset, 0, 22, 24, 22);
        _game.textureManager.BindTexture(_game.textureManager.GetTextureId("/gui/icons.png"));
        if (_game.options.CameraMode == EnumCameraMode.FirstPerson)
        {
            GLManager.GL.Enable(GLEnum.Blend);
            GLManager.GL.BlendFunc(GLEnum.OneMinusDstColor, GLEnum.OneMinusSrcColor);
            DrawTexturedModalRect(scaledWidth / 2 - 7, scaledHeight / 2 - 7, 0, 0, 16, 16);
            GLManager.GL.Disable(GLEnum.Blend);
        }
        bool heartBlink = _game.player.hearts / 3 % 2 == 1;
        if (_game.player.hearts < 10)
        {
            heartBlink = false;
        }

        int health = _game.player.health;
        int lastHealth = _game.player.lastHealth;
        _rand.SetSeed(_updateCounter * 312871);
        int armorValue;
        int i;
        int j;
        if (_game.playerController.shouldDrawHUD())
        {
            armorValue = _game.player.getPlayerArmorValue();

            int k;
            for (i = 0; i < 10; ++i)
            {
                j = scaledHeight - 32 + yOffset;
                if (armorValue > 0)
                {
                    k = scaledWidth / 2 + 91 - i * 8 - 9;
                    if (i * 2 + 1 < armorValue)
                    {
                        DrawTexturedModalRect(k, j, 34, 9, 9, 9);
                    }

                    if (i * 2 + 1 == armorValue)
                    {
                        DrawTexturedModalRect(k, j, 25, 9, 9, 9);
                    }

                    if (i * 2 + 1 > armorValue)
                    {
                        DrawTexturedModalRect(k, j, 16, 9, 9, 9);
                    }
                }

                byte blinkIndex = 0;
                if (heartBlink)
                {
                    blinkIndex = 1;
                }

                int x = scaledWidth / 2 - 91 + i * 8;
                if (health <= 4)
                {
                    j += _rand.NextInt(2);
                }

                DrawTexturedModalRect(x, j, 16 + blinkIndex * 9, 0, 9, 9);
                if (heartBlink)
                {
                    if (i * 2 + 1 < lastHealth)
                    {
                        DrawTexturedModalRect(x, j, 70, 0, 9, 9);
                    }

                    if (i * 2 + 1 == lastHealth)
                    {
                        DrawTexturedModalRect(x, j, 79, 0, 9, 9);
                    }
                }

                if (i * 2 + 1 < health)
                {
                    DrawTexturedModalRect(x, j, 52, 0, 9, 9);
                }

                if (i * 2 + 1 == health)
                {
                    DrawTexturedModalRect(x, j, 61, 0, 9, 9);
                }
            }

            if (_game.player.isInFluid(Material.Water))
            {
                i = (int)Math.Ceiling((_game.player.air - 2) * 10.0D / 300.0D);
                j = (int)Math.Ceiling(_game.player.air * 10.0D / 300.0D) - i;

                for (k = 0; k < i + j; ++k)
                {
                    if (k < i)
                    {
                        DrawTexturedModalRect(scaledWidth / 2 - 91 + k * 8, scaledHeight - 32 - 9 + yOffset, 16, 18, 9, 9);
                    }
                    else
                    {
                        DrawTexturedModalRect(scaledWidth / 2 - 91 + k * 8, scaledHeight - 32 - 9 + yOffset, 25, 18, 9, 9);
                    }
                }
            }
        }

        GLManager.GL.Disable(GLEnum.Blend);
        GLManager.GL.Enable(GLEnum.RescaleNormal);
        GLManager.GL.PushMatrix();
        GLManager.GL.Rotate(120.0F, 1.0F, 0.0F, 0.0F);
        Lighting.turnOn();
        GLManager.GL.PopMatrix();

        for (armorValue = 0; armorValue < 9; ++armorValue)
        {
            i = scaledWidth / 2 - 90 + armorValue * 20 + 2;
            j = scaledHeight - 16 - 3 + yOffset;
            RenderInventorySlot(armorValue, i, j, partialTicks);
        }

        Lighting.turnOff();
        GLManager.GL.Disable(GLEnum.RescaleNormal);
        if (_game.player.getSleepTimer() > 0)
        {
            GLManager.GL.Disable(GLEnum.DepthTest);
            GLManager.GL.Disable(GLEnum.AlphaTest);
            armorValue = _game.player.getSleepTimer();
            float sleepAlpha = armorValue / 100.0F;
            if (sleepAlpha > 1.0F)
            {
                sleepAlpha = 1.0F - (armorValue - 100) / 10.0F;
            }

            j = (int)(220.0F * sleepAlpha) << 24 | 1052704;
            DrawRect(0, 0, scaledWidth, scaledHeight, Guis.Color.FromArgb((uint)j));
            GLManager.GL.Enable(GLEnum.AlphaTest);
            GLManager.GL.Enable(GLEnum.DepthTest);
        }

        string debugStr;
        if (_game.options.ShowDebugInfo)
        {
            _game.componentsStorage.Overlay.Context.GCMonitor.AllowUpdating = true;
            _game.componentsStorage.Overlay.Draw();
        }
        else
        {
            _game.componentsStorage.Overlay.Context.GCMonitor.AllowUpdating = false;
        }

        if (_recordPlayingUpFor > 0)
        {
            float t = _recordPlayingUpFor - partialTicks;
            i = (int)(t * 256.0F / 20.0F);
            if (i > 255)
            {
                i = 255;
            }

            if (i > 0)
            {
                GLManager.GL.PushMatrix();
                GLManager.GL.Translate(scaledWidth / 2, scaledHeight - 48 + yOffset, 0.0F);
                GLManager.GL.Enable(GLEnum.Blend);
                GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);

                int rainbowColor = 0xFFFFFF;
                if (_isRecordMessageRainbow)
                {
                    rainbowColor = HSBtoRGB(t / 50.0F, 0.7F, 0.6F) & 0xFFFFFF;
                }

                font.DrawString(_recordPlaying, -font.GetStringWidth(_recordPlaying) / 2, -4, Color.FromArgb((uint)(rainbowColor + (i << 24))));
                GLManager.GL.Disable(GLEnum.Blend);
                GLManager.GL.PopMatrix();
            }
        }

        byte linesToShow = 10;
        bool chatOpen = false;
        if (_game.currentScreen is GuiChat)
        {
            linesToShow = 20;
            chatOpen = true;
        }

        GLManager.GL.Enable(GLEnum.Blend);
        GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
        GLManager.GL.Disable(GLEnum.AlphaTest);
        GLManager.GL.PushMatrix();
        GLManager.GL.Translate(0.0F, scaledHeight - 48 + yOffset, 0.0F);

        for (j = 0; j < _chatMessageList.Count && j < linesToShow; ++j)
        {
            int index = j + (chatOpen ? _chatScrollPos : 0);
            if (index >= _chatMessageList.Count) break;

            ChatLine cl = _chatMessageList[index];
            if (cl.UpdateCounter < 200 || chatOpen)
            {
                double d = cl.UpdateCounter / 200.0D;
                d = 1.0D - d;
                d *= 10.0D;
                if (d < 0.0D)
                {
                    d = 0.0D;
                }

                if (d > 1.0D)
                {
                    d = 1.0D;
                }

                d *= d;
                int alpha = (int)(255.0D * d);
                if (chatOpen)
                {
                    alpha = 255;
                }

                if (alpha > 0)
                {
                    byte left = 2;
                    int y = -j * 9;
                    debugStr = cl.Message;
                    DrawRect(left, y - 1, left + 320, y + 8, Color.FromArgb((uint)(alpha / 2 << 24)));
                    GLManager.GL.Enable(GLEnum.Blend);
                    font.DrawStringWithShadow(debugStr, left, y, Color.FromArgb(0xFFFFFF + (uint)(alpha << 24)));
                }
            }
        }

        GLManager.GL.PopMatrix();
        GLManager.GL.Enable(GLEnum.AlphaTest);
        GLManager.GL.Disable(GLEnum.Blend);

        if (chatOpen)
        {
            int linesToShowAbs = 20;
            int left = 2;
            int chatWidth = 320;
            int scrollbarX = left + chatWidth - 5;
            int scrollbarWidth = 6;
            int bottom = scaledHeight - 48 + 6 + yOffset;
            int top = scaledHeight - 48 - (linesToShowAbs - 1) * 9 + yOffset;
            int trackHeight = bottom - top;

            int totalLines = _chatMessageList.Count;
            int maxScroll = totalLines - linesToShowAbs;
            if (maxScroll < 0) maxScroll = 0;

            if (maxScroll > 0)
            {
                int thumbHeight = 8;
                if (totalLines > 0)
                {
                    int calc = trackHeight * linesToShowAbs / totalLines;
                    if (calc > thumbHeight) thumbHeight = calc;
                }

                int thumbY;
                int range = Math.Max(1, trackHeight - thumbHeight);
                thumbY = top + (int)((long)(maxScroll - _chatScrollPos) * range / maxScroll);

                DrawRect(scrollbarX + 1, thumbY, scrollbarX + scrollbarWidth - 1, thumbY + thumbHeight, Color.GrayCC);
            }
        }

        _game.guiAchievement.RenderAchievementOverlayIfAny(scaledWidth, scaledHeight);
        ControlTooltip.Render(_game, scaledWidth, scaledHeight, partialTicks);

        if (_game.options.ShowWTHIT)
            RenderWTHIT(scaledWidth);
    }

    private void RenderWTHIT(int scaledWidth)
    {
        BetaSharp g = _game;
        HitResult hit = g.objectMouseOver;
        if (hit.Type != HitResultType.TILE || g.world == null) return;

        int blockId;
        Block? block = null;
        string blockName = "Unknown";

        int width = 150;
        int height = 25;

        if (hit.Type == HitResultType.TILE)
        {
            blockId = g.world.Reader.GetBlockId(hit.BlockX, hit.BlockY, hit.BlockZ);
            block = Block.Blocks[blockId];

            if (block is BlockTallGrass)
            {
                blockName = "Tall Grass";
            }
            else
            {
                string translatedName = block.translateBlockName();
                if (!string.IsNullOrWhiteSpace(translatedName))
                {
                    blockName = translatedName;
                }
                else if (!string.IsNullOrWhiteSpace(block.getBlockName()))
                {
                    blockName = block.getBlockName();
                }
            }

            width = 30 + g.fontRenderer.GetStringWidth(blockName);
        }

        if (block is null) return;

        int x = scaledWidth / 2 - width / 2;
        int y = 10;

        Color bg = new(16, 16, 16);
        Color outline = Color.Gray40;

        DrawHorizontalLine(x + 1, x + width - 1, y, bg);
        DrawHorizontalLine(x + 1, x + width - 1, y + height - 1, bg);
        DrawVerticalLine(x, y, y + height - 1, bg);
        DrawVerticalLine(x + width, y, y + height - 1, bg);

        DrawHorizontalLine(x + 1, x + width - 1, y + 1, outline);
        DrawHorizontalLine(x + 1, x + width - 1, y + height - 2, outline);
        DrawVerticalLine(x + 1, y, y + height - 1, outline);
        DrawVerticalLine(x + width - 1, y, y + height - 1, outline);

        DrawRect(x + 2, y + 2, x + width - 1, y + height - 2, bg);

        if (hit.Type == HitResultType.TILE)
        {
            if (block is BlockTallGrass)
            {
                GLManager.GL.Disable(GLEnum.DepthTest);
                GLManager.GL.DepthMask(false);
                GLManager.GL.Enable(GLEnum.Blend);
                GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);

                int c = GrassColors.getDefaultColor();
                GLManager.GL.Color3(((c >> 16) & 0xFF) / 255f, ((c >> 8) & 0xFF) / 255f, (c & 0xFF) / 255f);

                GLManager.GL.Disable(GLEnum.AlphaTest);
                _game.textureManager.BindTexture(_game.textureManager.GetTextureId("/terrain.png"));
                DrawTexturedModalRect(x + 4, y + 4, 112, 32, 16, 16);
                GLManager.GL.DepthMask(true);
                GLManager.GL.Enable(GLEnum.DepthTest);
                GLManager.GL.Enable(GLEnum.AlphaTest);
                GLManager.GL.Disable(GLEnum.Blend);
                GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);

                DrawString(g.fontRenderer, blockName, x + 25, y + (height / 2 - 4), Color.White);

                return;
            }
            GLManager.GL.Enable(GLEnum.RescaleNormal);
            GLManager.GL.PushMatrix();
            GLManager.GL.Rotate(120.0F, 1.0F, 0.0F, 0.0F);
            GLManager.GL.Rotate(-90.0F, 0.0F, 1.0F, 0.0F);
            Lighting.turnOn();
            GLManager.GL.PopMatrix();
            GLManager.GL.Enable(GLEnum.Lighting);
            GLManager.GL.Enable(GLEnum.DepthTest);

            GLManager.GL.Translate(0.0F, 0.0F, 32.0F);
            ItemStack stack = new(block);
            s_itemRenderer.renderItemIntoGUI(g.fontRenderer, g.textureManager, stack, x + 4, y + 4);

            Lighting.turnOff();
            GLManager.GL.Disable(GLEnum.Lighting);
            GLManager.GL.Disable(GLEnum.DepthTest);
            GLManager.GL.Disable(GLEnum.RescaleNormal);


            DrawString(g.fontRenderer, blockName, x + 25, y + (height / 2 - 4), Color.White);

            if (g.terrainRenderer.damagePartialTime != 0)
                DrawHorizontalLine(x + 1, x + 1 + (int)Math.Ceiling((width - 2) * g.terrainRenderer.damagePartialTime), y + height - 1, Color.White);
        }

        // TODO: support entites
    }

    private void RenderPumpkinBlur(int screenWidth, int screenHeight)
    {
        GLManager.GL.Disable(GLEnum.DepthTest);
        GLManager.GL.DepthMask(false);
        GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
        GLManager.GL.Disable(GLEnum.AlphaTest);
        _game.textureManager.BindTexture(_game.textureManager.GetTextureId("%blur%/misc/pumpkinblur.png"));
        Tessellator tess = Tessellator.instance;
        tess.startDrawingQuads();
        tess.addVertexWithUV(0.0D, screenHeight, -90.0D, 0.0D, 1.0D);
        tess.addVertexWithUV(screenWidth, screenHeight, -90.0D, 1.0D, 1.0D);
        tess.addVertexWithUV(screenWidth, 0.0D, -90.0D, 1.0D, 0.0D);
        tess.addVertexWithUV(0.0D, 0.0D, -90.0D, 0.0D, 0.0D);
        tess.draw();
        GLManager.GL.DepthMask(true);
        GLManager.GL.Enable(GLEnum.DepthTest);
        GLManager.GL.Enable(GLEnum.AlphaTest);
        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
    }

    private void RenderVignette(float darkness, int screenWidth, int screenHeight)
    {
        darkness = 1.0F - darkness;
        if (darkness < 0.0F)
        {
            darkness = 0.0F;
        }

        if (darkness > 1.0F)
        {
            darkness = 1.0F;
        }

        _prevVignetteBrightness = (float)(_prevVignetteBrightness + (double)(darkness - _prevVignetteBrightness) * 0.01D);
        GLManager.GL.Disable(GLEnum.DepthTest);
        GLManager.GL.DepthMask(false);
        GLManager.GL.BlendFunc(GLEnum.Zero, GLEnum.OneMinusSrcColor);
        GLManager.GL.Color4(_prevVignetteBrightness, _prevVignetteBrightness, _prevVignetteBrightness, 1.0F);
        _game.textureManager.BindTexture(_game.textureManager.GetTextureId("%blur%/misc/vignette.png"));
        Tessellator tess = Tessellator.instance;
        tess.startDrawingQuads();
        tess.addVertexWithUV(0.0D, screenHeight, -90.0D, 0.0D, 1.0D);
        tess.addVertexWithUV(screenWidth, screenHeight, -90.0D, 1.0D, 1.0D);
        tess.addVertexWithUV(screenWidth, 0.0D, -90.0D, 1.0D, 0.0D);
        tess.addVertexWithUV(0.0D, 0.0D, -90.0D, 0.0D, 0.0D);
        tess.draw();
        GLManager.GL.DepthMask(true);
        GLManager.GL.Enable(GLEnum.DepthTest);
        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
        GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
    }

    private void RenderPortalOverlay(float portalStrength, int screenWidth, int screenHeight)
    {
        if (portalStrength < 1.0F)
        {
            portalStrength *= portalStrength;
            portalStrength *= portalStrength;
            portalStrength = portalStrength * 0.8F + 0.2F;
        }

        GLManager.GL.Disable(GLEnum.AlphaTest);
        GLManager.GL.Disable(GLEnum.DepthTest);
        GLManager.GL.DepthMask(false);
        GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, portalStrength);
        _game.textureManager.BindTexture(_game.textureManager.GetTextureId("/terrain.png"));
        float u1 = Block.NetherPortal.textureId % 16 / 16.0F;
        float v1 = Block.NetherPortal.textureId / 16 / 16.0F;
        float u2 = (Block.NetherPortal.textureId % 16 + 1) / 16.0F;
        float v2 = (Block.NetherPortal.textureId / 16 + 1) / 16.0F;
        Tessellator tess = Tessellator.instance;
        tess.startDrawingQuads();
        tess.addVertexWithUV(0.0D, screenHeight, -90.0D, (double)u1, (double)v2);
        tess.addVertexWithUV(screenWidth, screenHeight, -90.0D, (double)u2, (double)v2);
        tess.addVertexWithUV(screenWidth, 0.0D, -90.0D, (double)u2, (double)v1);
        tess.addVertexWithUV(0.0D, 0.0D, -90.0D, (double)u1, (double)v1);
        tess.draw();
        GLManager.GL.DepthMask(true);
        GLManager.GL.Enable(GLEnum.DepthTest);
        GLManager.GL.Enable(GLEnum.AlphaTest);
        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
    }

    private void RenderInventorySlot(int slotIndex, int x, int y, float partialTicks)
    {
        ItemStack stack = _game.player.inventory.main[slotIndex];
        if (stack != null)
        {
            float bob = stack.bobbingAnimationTime - partialTicks;
            if (bob > 0.0F)
            {
                GLManager.GL.PushMatrix();
                float scale = 1.0F + bob / 5.0F;
                GLManager.GL.Translate(x + 8, y + 12, 0.0F);
                GLManager.GL.Scale(1.0F / scale, (scale + 1.0F) / 2.0F, 1.0F);
                GLManager.GL.Translate(-(x + 8), -(y + 12), 0.0F);
            }

            s_itemRenderer.renderItemIntoGUI(_game.fontRenderer, _game.textureManager, stack, x, y);
            if (bob > 0.0F)
            {
                GLManager.GL.PopMatrix();
            }

            s_itemRenderer.renderItemOverlayIntoGUI(_game.fontRenderer, _game.textureManager, stack, x, y);
        }
    }

    public void UpdateTick()
    {
        if (_recordPlayingUpFor > 0)
        {
            --_recordPlayingUpFor;
        }

        ++_updateCounter;

        for (int i = 0; i < _chatMessageList.Count; ++i)
        {
            ++_chatMessageList[i].UpdateCounter;
        }

    }

    public void ClearChatMessages()
    {
        _chatMessageList.Clear();
    }

    public void AddChatMessage(string message)
    {
        foreach (string line in message.Split("\n"))
        {
            AddWrappedChatMessage(line);
        }
    }

    private void AddWrappedChatMessage(string message)
    {
        while (_game.fontRenderer.GetStringWidth(message) > 320)
        {
            int i;
            for (i = 1; i < message.Length && _game.fontRenderer.GetStringWidth(message.AsSpan(0, i + 1)) <= 320; ++i)
            {
            }

            _chatMessageList.Insert(0, new ChatLine(message.Substring(0, i)));
            message = message.Substring(i);
        }

        _chatMessageList.Insert(0, new ChatLine(message));

        _chatScrollPos = 0;

        while (_chatMessageList.Count > 64)
        {
            _chatMessageList.RemoveAt(_chatMessageList.Count - 1);
        }
    }

    public void SetRecordPlayingMessage(string recordName)
    {
        _recordPlaying = "Now playing: " + recordName;
        _recordPlayingUpFor = 60;
        _isRecordMessageRainbow = true;
    }

    public void AddChatMessageTranslate(string key)
    {
        TranslationStorage translations = TranslationStorage.Instance;
        string translated = translations.TranslateKey(key);
        AddChatMessage(translated);
    }

    public void ScrollChat(int amount)
    {
        if (amount == 0) return;

        int linesToShow = 20;
        int maxScroll = _chatMessageList.Count - linesToShow;
        if (maxScroll < 0) maxScroll = 0;
        _chatScrollPos += amount;
        if (_chatScrollPos < 0) _chatScrollPos = 0;
        if (_chatScrollPos > maxScroll) _chatScrollPos = maxScroll;
    }
}
