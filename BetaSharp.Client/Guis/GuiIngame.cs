using BetaSharp.Client.Options;
using BetaSharp.Blocks;
using BetaSharp.Blocks.Materials;
using BetaSharp.Client.Guis.Controls;
using BetaSharp.Client.Guis.Layout;
using BetaSharp.Client.Rendering;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Items;
using BetaSharp.Inventorys;
using BetaSharp.Items;
using BetaSharp.Util;
using BetaSharp.Util.Maths;
using java.awt;
using Silk.NET.OpenGL.Legacy;

namespace BetaSharp.Client.Guis;

public class GuiIngame : Control
{
    private readonly GCMonitor _gcMonitor;
    private static readonly ItemRenderer _itemRenderer = new();
    private readonly List<ChatLine> _chatMessageList = new();
    private readonly JavaRandom _rand = new();
    private int _chatScrollPos = 0;
    private bool _chatScrollbarDragging = false;
    private readonly Minecraft _mc;
    public string _hoveredItemName = null;
    private int _updateCounter = 0;
    private string _recordPlaying = "";
    private int _recordPlayingUpFor = 0;
    private bool _isRecordMessageRainbow = false;
    public float _damageGuiPartialTime;
    float PrevVignetteBrightness = 1.0F;

    public GuiIngame(Minecraft gameInstance)
    {
        Anchor = Anchors.Bottom | Anchors.Left | Anchors.Right | Anchors.Top;
        EffectiveSize = new(gameInstance.displayWidth, gameInstance.displayHeight);
        _mc = gameInstance;
        _gcMonitor = new GCMonitor();
    }

    protected override void OnRender(RenderEventArgs e)
    {
        ScaledResolution scaled = new(_mc.options, _mc.displayWidth, _mc.displayHeight);
        int scaledWidth = scaled.ScaledWidth;
        int scaledHeight = scaled.ScaledHeight;
        TextRenderer font = _mc.fontRenderer;
        _mc.gameRenderer.setupHudRender();
        GLManager.GL.Enable(GLEnum.Blend);
        if (Minecraft.isFancyGraphicsEnabled())
        {
            renderVignette(_mc.player.getBrightnessAtEyes(e.TickDelta), scaledWidth, scaledHeight);
        }

        ItemStack helmet = _mc.player.inventory.armorItemInSlot(3);
        if (_mc.options.CameraMode == EnumCameraMode.FirstPerson && helmet != null && helmet.itemId == Block.Pumpkin.id)
        {
            renderPumpkinBlur(scaledWidth, scaledHeight);
        }

        float screenDistortion = _mc.player.lastScreenDistortion + (_mc.player.changeDimensionCooldown - _mc.player.lastScreenDistortion) * e.TickDelta;
        if (screenDistortion > 0.0F)
        {
            renderPortalOverlay(screenDistortion, scaledWidth, scaledHeight);
        }

        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
        _mc.textureManager.BindTexture(_mc.textureManager.GetTextureId("/gui/gui.png"));
        InventoryPlayer inventory = _mc.player.inventory;
        ZLevel = -90.0F;
        DrawTextureRegion(scaledWidth / 2 - 91, scaledHeight - 22, 0, 0, 182, 22);
        DrawTextureRegion(scaledWidth / 2 - 91 - 1 + inventory.selectedSlot * 20, scaledHeight - 22 - 1, 0, 22, 24, 22);
        _mc.textureManager.BindTexture(_mc.textureManager.GetTextureId("/gui/icons.png"));
        if (_mc.options.CameraMode == EnumCameraMode.FirstPerson)
        {
            GLManager.GL.Enable(GLEnum.Blend);
            GLManager.GL.BlendFunc(GLEnum.OneMinusDstColor, GLEnum.OneMinusSrcColor);
            DrawTextureRegion(scaledWidth / 2 - 7, scaledHeight / 2 - 7, 0, 0, 16, 16);
            GLManager.GL.Disable(GLEnum.Blend);
        }
        bool heartBlink = _mc.player.hearts / 3 % 2 == 1;
        if (_mc.player.hearts < 10)
        {
            heartBlink = false;
        }

        int health = _mc.player.health;
        int lastHealth = _mc.player.lastHealth;
        _rand.SetSeed(_updateCounter * 312871);
        int armorValue;
        int i;
        int j;
        if (_mc.playerController.shouldDrawHUD())
        {
            armorValue = _mc.player.getPlayerArmorValue();

            int k;
            for (i = 0; i < 10; ++i)
            {
                j = scaledHeight - 32;
                if (armorValue > 0)
                {
                    k = scaledWidth / 2 + 91 - i * 8 - 9;
                    if (i * 2 + 1 < armorValue)
                    {
                        DrawTextureRegion(k, j, 34, 9, 9, 9);
                    }

                    if (i * 2 + 1 == armorValue)
                    {
                        DrawTextureRegion(k, j, 25, 9, 9, 9);
                    }

                    if (i * 2 + 1 > armorValue)
                    {
                        DrawTextureRegion(k, j, 16, 9, 9, 9);
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

                DrawTextureRegion(x, j, 16 + blinkIndex * 9, 0, 9, 9);
                if (heartBlink)
                {
                    if (i * 2 + 1 < lastHealth)
                    {
                        DrawTextureRegion(x, j, 70, 0, 9, 9);
                    }

                    if (i * 2 + 1 == lastHealth)
                    {
                        DrawTextureRegion(x, j, 79, 0, 9, 9);
                    }
                }

                if (i * 2 + 1 < health)
                {
                    DrawTextureRegion(x, j, 52, 0, 9, 9);
                }

                if (i * 2 + 1 == health)
                {
                    DrawTextureRegion(x, j, 61, 0, 9, 9);
                }
            }

            if (_mc.player.isInFluid(Material.Water))
            {
                i = (int)java.lang.Math.ceil((_mc.player.air - 2) * 10.0D / 300.0D);
                j = (int)java.lang.Math.ceil(_mc.player.air * 10.0D / 300.0D) - i;

                for (k = 0; k < i + j; ++k)
                {
                    if (k < i)
                    {
                        DrawTextureRegion(scaledWidth / 2 - 91 + k * 8, scaledHeight - 32 - 9, 16, 18, 9, 9);
                    }
                    else
                    {
                        DrawTextureRegion(scaledWidth / 2 - 91 + k * 8, scaledHeight - 32 - 9, 25, 18, 9, 9);
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
            j = scaledHeight - 16 - 3;
            renderInventorySlot(armorValue, i, j, e.TickDelta);
        }

        Lighting.turnOff();
        GLManager.GL.Disable(GLEnum.RescaleNormal);
        if (_mc.player.getSleepTimer() > 0)
        {
            GLManager.GL.Disable(GLEnum.DepthTest);
            GLManager.GL.Disable(GLEnum.AlphaTest);
            armorValue = _mc.player.getSleepTimer();
            float sleepAlpha = armorValue / 100.0F;
            if (sleepAlpha > 1.0F)
            {
                sleepAlpha = 1.0F - (armorValue - 100) / 10.0F;
            }

            j = (int)(220.0F * sleepAlpha) << 24 | 1052704;
            Gui.DrawRect(0, 0, scaledWidth, scaledHeight, (uint)j);
            GLManager.GL.Enable(GLEnum.AlphaTest);
            GLManager.GL.Enable(GLEnum.DepthTest);
        }

        string debugStr;
        if (_mc.options.ShowDebugInfo)
        {
            _gcMonitor.AllowUpdating = true;
            GLManager.GL.PushMatrix();
            if (Minecraft.hasPaidCheckTime > 0L)
                GLManager.GL.Translate(0.0F, 32.0F, 0.0F);

            font.DrawStringWithShadow("Minecraft Beta 1.7.3 (" + _mc.debug + ")", 2, 2, 0xFFFFFF);
            font.DrawStringWithShadow(_mc.getEntityDebugInfo(), 2, 22, 0xFFFFFF);
            font.DrawStringWithShadow(_mc.getParticleAndEntityCountDebugInfo(), 2, 32, 0xFFFFFF);
            font.DrawStringWithShadow(_mc.getWorldDebugInfo(), 2, 42, 0xFFFFFF);
            long maxMem = _gcMonitor.MaxMemoryBytes;
            long usedMem = _gcMonitor.UsedMemoryBytes;
            long heapMem = _gcMonitor.UsedHeapBytes;
            debugStr = "Used memory: " + usedMem * 100L / maxMem + "% (" + usedMem / 1024L / 1024L + "MB) of " + maxMem / 1024L / 1024L + "MB";
            Gui.DrawString(font, debugStr, scaledWidth - font.GetStringWidth(debugStr) - 2, 2, 0xE0E0E0);
            debugStr = "GC heap: " + heapMem * 100L / maxMem + "% (" + heapMem / 1024L / 1024L + "MB)";
            Gui.DrawString(font, debugStr, scaledWidth - font.GetStringWidth(debugStr) - 2, 12, 0xE0E0E0);
            Gui.DrawString(font, "x: " + _mc.player.x, 2, 64, 0xE0E0E0);
            Gui.DrawString(font, "y: " + _mc.player.y, 2, 72, 0xE0E0E0);
            Gui.DrawString(font, "z: " + _mc.player.z, 2, 80, 0xE0E0E0);
            Gui.DrawString(font, "f: " + (MathHelper.Floor((double)(_mc.player.yaw * 4.0F / 360.0F) + 0.5D) & 3), 2, 88, 0xE0E0E0);

            if (_mc.internalServer != null)
            {
                Gui.DrawString(font, $"Server TPS: {_mc.internalServer.Tps:F1}", 2, 104, 0xE0E0E0);
            }

            GLManager.GL.PopMatrix();
        }
        else
        {
            _gcMonitor.AllowUpdating = false;
        }

        if (_recordPlayingUpFor > 0)
        {
            float t = _recordPlayingUpFor - e.TickDelta;
            i = (int)(t * 256.0F / 20.0F);
            if (i > 255)
            {
                i = 255;
            }

            if (i > 0)
            {
                GLManager.GL.PushMatrix();
                GLManager.GL.Translate(scaledWidth / 2, scaledHeight - 48, 0.0F);
                GLManager.GL.Enable(GLEnum.Blend);
                GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
                j = 0xFFFFFF;
                if (_isRecordMessageRainbow)
                {
                    j = Color.HSBtoRGB(t / 50.0F, 0.7F, 0.6F) & 0xFFFFFF;
                }

                font.DrawString(_recordPlaying, -font.GetStringWidth(_recordPlaying) / 2, -4, (uint)(j + (i << 24)));
                GLManager.GL.Disable(GLEnum.Blend);
                GLManager.GL.PopMatrix();
            }
        }

        byte linesToShow = 10;
        bool chatOpen = false;
        if (_mc.currentScreen is GuiChat)
        {
            linesToShow = 20;
            chatOpen = true;
        }

        GLManager.GL.Enable(GLEnum.Blend);
        GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
        GLManager.GL.Disable(GLEnum.AlphaTest);
        GLManager.GL.PushMatrix();
        GLManager.GL.Translate(0.0F, scaledHeight - 48, 0.0F);

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
                    Gui.DrawRect(left, y - 1, left + 320, y + 8, (uint)(alpha / 2 << 24));
                    GLManager.GL.Enable(GLEnum.Blend);
                    font.DrawStringWithShadow(debugStr, left, y, 0xFFFFFF + (uint)(alpha << 24));
                }
            }
        }

        // Scrollbar rendering moved below (use absolute GUI coords)

        GLManager.GL.PopMatrix();
        GLManager.GL.Enable(GLEnum.AlphaTest);
        GLManager.GL.Disable(GLEnum.Blend);

        // Absolute GUI-coordinate scrollbar (matches mouse input coordinates)
        if (chatOpen)
        {
            int linesToShowAbs = 20;
            int left = 2;
            int chatWidth = 320;
            int scrollbarX = left + chatWidth - 5;
            int scrollbarWidth = 6;
            int bottom = scaledHeight - 48 + 6; // 2 pixels before message end
            int top = scaledHeight - 48 - (linesToShowAbs - 1) * 9;
            int trackHeight = bottom - top;

            int totalLines = _chatMessageList.Count;
            int maxScroll = totalLines - linesToShowAbs;
            if (maxScroll < 0) maxScroll = 0;

            // Only draw scrollbar if there's something to scroll
            if (maxScroll > 0)
            {
                int thumbHeight = 8;
                if (totalLines > 0)
                {
                    int calc = trackHeight * linesToShowAbs / totalLines;
                    if (calc > thumbHeight) thumbHeight = calc;
                }

                int thumbY = top;
                int range = Math.Max(1, trackHeight - thumbHeight);
                // Inverted: Bottom is newest (0), Top is oldest (maxScroll)
                thumbY = top + (int)((long)(maxScroll - _chatScrollPos) * range / maxScroll);

                uint thumbColor = _chatScrollbarDragging ? 0xFFAAAAAA : 0xFFCCCCCC;
                Gui.DrawRect(scrollbarX + 1, thumbY, scrollbarX + scrollbarWidth - 1, thumbY + thumbHeight, thumbColor);
            }
        }
    }

    private void renderPumpkinBlur(int screenWidth, int screenHeight)
    {
        GLManager.GL.Disable(GLEnum.DepthTest);
        GLManager.GL.DepthMask(false);
        GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
        GLManager.GL.Disable(GLEnum.AlphaTest);
        _mc.textureManager.BindTexture(_mc.textureManager.GetTextureId("%blur%/misc/pumpkinblur.png"));
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

    private void renderVignette(float darkness, int screenWidth, int screenHeight)
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

        PrevVignetteBrightness = (float)(PrevVignetteBrightness + (double)(darkness - PrevVignetteBrightness) * 0.01D);
        GLManager.GL.Disable(GLEnum.DepthTest);
        GLManager.GL.DepthMask(false);
        GLManager.GL.BlendFunc(GLEnum.Zero, GLEnum.OneMinusSrcColor);
        GLManager.GL.Color4(PrevVignetteBrightness, PrevVignetteBrightness, PrevVignetteBrightness, 1.0F);
        _mc.textureManager.BindTexture(_mc.textureManager.GetTextureId("%blur%/misc/vignette.png"));
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

    private void renderPortalOverlay(float portalStrength, int screenWidth, int screenHeight)
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
        _mc.textureManager.BindTexture(_mc.textureManager.GetTextureId("/terrain.png"));
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

    private void renderInventorySlot(int slotIndex, int x, int y, float tickDelta)
    {
        ItemStack stack = _mc.player.inventory.main[slotIndex];
        if (stack != null)
        {
            float bob = stack.bobbingAnimationTime - tickDelta;
            if (bob > 0.0F)
            {
                GLManager.GL.PushMatrix();
                float scale = 1.0F + bob / 5.0F;
                GLManager.GL.Translate(x + 8, y + 12, 0.0F);
                GLManager.GL.Scale(1.0F / scale, (scale + 1.0F) / 2.0F, 1.0F);
                GLManager.GL.Translate(-(x + 8), -(y + 12), 0.0F);
            }

            _itemRenderer.renderItemIntoGUI(_mc.fontRenderer, _mc.textureManager, stack, x, y);
            if (bob > 0.0F)
            {
                GLManager.GL.PopMatrix();
            }

            _itemRenderer.renderItemOverlayIntoGUI(_mc.fontRenderer, _mc.textureManager, stack, x, y);
        }
    }

    public void updateTick()
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

    public void ClearChat()
    {
        _chatMessageList.Clear();
    }

    public void addChatMessage(string message)
    {
        foreach (string line in message.Split("\n"))
        {
            addWrappedChatMessage(line);
        }
    }

    private void addWrappedChatMessage(string message)
    {
        while (_mc.fontRenderer.GetStringWidth(message) > 320)
        {
            int i;
            for (i = 1; i < message.Length && _mc.fontRenderer.GetStringWidth(message.Substring(0, i + 1)) <= 320; ++i)
            {
            }

            _chatMessageList.Insert(0, new ChatLine(message.Substring(0, i)));
            message = message.Substring(i);
        }

        _chatMessageList.Insert(0, new ChatLine(message));
        // Reset scroll to show newest messages when new message arrives
        _chatScrollPos = 0;

        // Keep recent history (increase to 64 messages)
        while (_chatMessageList.Count > 64)
        {
            _chatMessageList.RemoveAt(_chatMessageList.Count - 1);
        }
    }

    public void setRecordPlayingMessage(string recordName)
    {
        _recordPlaying = "Now playing: " + recordName;
        _recordPlayingUpFor = 60;
        _isRecordMessageRainbow = true;
    }

    public void addChatMessageTranslate(string key)
    {
        TranslationStorage translations = TranslationStorage.Instance;
        string translated = translations.TranslateKey(key);
        addChatMessage(translated);
    }

    public void scrollChat(int amount)
    {
        if (amount == 0) return;
        // When scrolling, assume chat open with up to 20 visible lines
        int linesToShow = 20;
        int maxScroll = _chatMessageList.Count - linesToShow;
        if (maxScroll < 0) maxScroll = 0;
        _chatScrollPos += amount;
        if (_chatScrollPos < 0) _chatScrollPos = 0;
        if (_chatScrollPos > maxScroll) _chatScrollPos = maxScroll;
    }

}
