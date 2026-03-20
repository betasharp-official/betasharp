using BetaSharp.Blocks;
using BetaSharp.Client.Input;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.OpenGL;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.Rendering.Items;
using BetaSharp.Stats;
using BetaSharp.Util.Maths;
using Silk.NET.GLFW;

namespace BetaSharp.Client.Guis;

public class GuiAchievements : GuiScreen
{

    private static readonly int field_27126_s = global::BetaSharp.Achievements.minColumn * 24 - 112;
    private static readonly int field_27125_t = global::BetaSharp.Achievements.minRow * 24 - 112;
    private static readonly int field_27124_u = global::BetaSharp.Achievements.maxColumn * 24 - 77;
    private static readonly int field_27123_v = global::BetaSharp.Achievements.maxRow * 24 - 77;
    protected int field_27121_a = 256;
    protected int field_27119_i = 202;
    protected int field_27118_j;
    protected int field_27117_l;
    protected double field_27116_m;
    protected double field_27115_n;
    protected double field_27114_o;
    protected double field_27113_p;
    protected double field_27112_q;
    protected double field_27111_r;
    private int field_27122_w;
    private readonly StatFileWriter statFileWriter;

    public GuiAchievements(StatFileWriter statFileWriter)
    {
        this.statFileWriter = statFileWriter;
        short halfWidth = 141;
        short halfHeight = 141;
        field_27116_m = field_27114_o = field_27112_q = global::BetaSharp.Achievements.OpenInventory.column * 24 - halfWidth / 2 - 12;
        field_27115_n = field_27113_p = field_27111_r = global::BetaSharp.Achievements.OpenInventory.row * 24 - halfHeight / 2;
    }

    public override void InitGui()
    {
        _controlList.Clear();
        _controlList.Add(new GuiSmallButton(1, Width / 2 + 24, Height / 2 + 74, 80, 20, StatCollector.TranslateToLocal("gui.done")));
    }

    protected override void ActionPerformed(GuiButton button)
    {
        if (button.Id == 1)
        {
            Game.displayGuiScreen(null);
            Game.setIngameFocus();
        }

        base.ActionPerformed(button);
    }

    protected override void KeyTyped(char eventChar, int eventKey)
    {
        if (eventKey == Game.options.KeyBindInventory.keyCode)
        {
            Game.displayGuiScreen(null);
            Game.setIngameFocus();
        }
        else
        {
            base.KeyTyped(eventChar, eventKey);
        }

    }

    public override void Render(int mouseX, int mouseY, float partialTicks)
    {
        if (Mouse.isButtonDown(0) || (Game.isControllerMode && Controller.IsButtonDown(GamepadButton.A)))
        {
            int guiLeft = (Width - field_27121_a) / 2;
            int guiTop = (Height - field_27119_i) / 2;
            int scrollAreaLeft = guiLeft + 8;
            int scrollAreaTop = guiTop + 17;
            if ((field_27122_w == 0 || field_27122_w == 1) && mouseX >= scrollAreaLeft && mouseX < scrollAreaLeft + 224 && mouseY >= scrollAreaTop && mouseY < scrollAreaTop + 155)
            {
                if (field_27122_w == 0)
                {
                    field_27122_w = 1;
                }
                else
                {
                    field_27114_o -= mouseX - field_27118_j;
                    field_27113_p -= mouseY - field_27117_l;
                    field_27112_q = field_27116_m = field_27114_o;
                    field_27111_r = field_27115_n = field_27113_p;
                }

                field_27118_j = mouseX;
                field_27117_l = mouseY;
            }

            if (field_27112_q < field_27126_s)
            {
                field_27112_q = field_27126_s;
            }

            if (field_27111_r < field_27125_t)
            {
                field_27111_r = field_27125_t;
            }

            if (field_27112_q >= field_27124_u)
            {
                field_27112_q = field_27124_u - 1;
            }

            if (field_27111_r >= field_27123_v)
            {
                field_27111_r = field_27123_v - 1;
            }
        }
        else
        {
            field_27122_w = 0;
        }

        DrawDefaultBackground();
        func_27109_b(mouseX, mouseY, partialTicks);
        GLManager.GL.Disable(GLEnum.Lighting);
        GLManager.GL.Disable(GLEnum.DepthTest);
        func_27110_k();
        GLManager.GL.Enable(GLEnum.Lighting);
        GLManager.GL.Enable(GLEnum.DepthTest);
    }

    public override void UpdateScreen()
    {
        field_27116_m = field_27114_o;
        field_27115_n = field_27113_p;
        double deltaX = field_27112_q - field_27114_o;
        double deltaY = field_27111_r - field_27113_p;
        if (deltaX * deltaX + deltaY * deltaY < 4.0D)
        {
            field_27114_o += deltaX;
            field_27113_p += deltaY;
        }
        else
        {
            field_27114_o += deltaX * 0.85D;
            field_27113_p += deltaY * 0.85D;
        }

    }

    protected void func_27110_k()
    {
        int guiLeft = (Width - field_27121_a) / 2;
        int guiTop = (Height - field_27119_i) / 2;
        FontRenderer.DrawString("Achievements", guiLeft + 15, guiTop + 5, Color.Gray40);
    }

    protected void func_27109_b(int mouseX, int mouseY, float partialTicks)
    {
        int scrollX = MathHelper.Floor(field_27116_m + (field_27114_o - field_27116_m) * (double)partialTicks);
        int scrollY = MathHelper.Floor(field_27115_n + (field_27113_p - field_27115_n) * (double)partialTicks);
        if (scrollX < field_27126_s)
        {
            scrollX = field_27126_s;
        }

        if (scrollY < field_27125_t)
        {
            scrollY = field_27125_t;
        }

        if (scrollX >= field_27124_u)
        {
            scrollX = field_27124_u - 1;
        }

        if (scrollY >= field_27123_v)
        {
            scrollY = field_27123_v - 1;
        }

        TextureHandle terrainTexture = Game.textureManager.GetTextureId("/terrain.png");
        TextureHandle bgTexture = Game.textureManager.GetTextureId("/achievement/bg.png");
        int guiLeft = (Width - field_27121_a) / 2;
        int guiTop = (Height - field_27119_i) / 2;
        int scrollAreaLeft = guiLeft + 16;
        int scrollAreaTop = guiTop + 17;
        _zLevel = 0.0F;
        GLManager.GL.DepthFunc(GLEnum.Gequal);
        GLManager.GL.PushMatrix();
        GLManager.GL.Translate(0.0F, 0.0F, -200.0F);
        GLManager.GL.Enable(GLEnum.Texture2D);
        GLManager.GL.Disable(GLEnum.Lighting);
        GLManager.GL.Enable(GLEnum.RescaleNormal);
        GLManager.GL.Enable(GLEnum.ColorMaterial);
        Game.textureManager.BindTexture(terrainTexture);
        int tileStartX = scrollX + 288 >> 4;
        int tileStartY = scrollY + 288 >> 4;
        int tileFracX = (scrollX + 288) % 16;
        int tileFracY = (scrollY + 288) % 16;
        JavaRandom random = new();

        for (int tileRow = 0; tileRow * 16 - tileFracY < 155; ++tileRow)
        {
            float rowBrightness = 0.6F - (tileStartY + tileRow) / 25.0F * 0.3F;
            GLManager.GL.Color4(rowBrightness, rowBrightness, rowBrightness, 1.0F);

            for (int tileCol = 0; tileCol * 16 - tileFracX < 224; ++tileCol)
            {
                random.SetSeed(1234 + tileStartX + tileCol);
                random.NextInt();
                int tileDepth = random.NextInt(1 + tileStartY + tileRow) + (tileStartY + tileRow) / 2;
                int tileTextureId = Block.Sand.textureId;
                if (tileDepth <= 37 && tileStartY + tileRow != 35)
                {
                    if (tileDepth == 22)
                    {
                        if (random.NextInt(2) == 0)
                        {
                            tileTextureId = Block.DiamondOre.textureId;
                        }
                        else
                        {
                            tileTextureId = Block.RedstoneOre.textureId;
                        }
                    }
                    else if (tileDepth == 10)
                    {
                        tileTextureId = Block.IronOre.textureId;
                    }
                    else if (tileDepth == 8)
                    {
                        tileTextureId = Block.CoalOre.textureId;
                    }
                    else if (tileDepth > 4)
                    {
                        tileTextureId = Block.Stone.textureId;
                    }
                    else if (tileDepth > 0)
                    {
                        tileTextureId = Block.Dirt.textureId;
                    }
                }
                else
                {
                    tileTextureId = Block.Bedrock.textureId;
                }

                DrawTexturedModalRect(scrollAreaLeft + tileCol * 16 - tileFracX, scrollAreaTop + tileRow * 16 - tileFracY, tileTextureId % 16 << 4, tileTextureId >> 4 << 4, 16, 16);
            }
        }

        GLManager.GL.Enable(GLEnum.DepthTest);
        GLManager.GL.DepthFunc(GLEnum.Lequal);
        GLManager.GL.Disable(GLEnum.Texture2D);

        int screenX;
        int screenY;
        int iconScreenX;
        for (int achievementLineIdx = 0; achievementLineIdx < global::BetaSharp.Achievements.AllAchievements.Count; ++achievementLineIdx)
        {
            Achievement lineAchievement = global::BetaSharp.Achievements.AllAchievements[achievementLineIdx];
            if (lineAchievement.parent != null)
            {
                int childScreenX = lineAchievement.column * 24 - scrollX + 11 + scrollAreaLeft;
                int childScreenY = lineAchievement.row * 24 - scrollY + 11 + scrollAreaTop;
                screenX = lineAchievement.parent.column * 24 - scrollX + 11 + scrollAreaLeft;
                screenY = lineAchievement.parent.row * 24 - scrollY + 11 + scrollAreaTop;
                bool unlocked = statFileWriter.HasAchievementUnlocked(lineAchievement);
                bool canUnlock = statFileWriter.CanUnlockAchievement(lineAchievement);
                Color color;
                if (unlocked)
                {
                    color = Color.Gray70;
                }
                else if (canUnlock)
                {
                    color = Math.Sin(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 600L / 600.0D * Math.PI * 2.0D) > 0.6D ?
                        Color.Blue :
                        Color.BlueAlpha;
                }
                else
                {
                    color = Color.Black;
                }

                DrawHorizontalLine(childScreenX, screenX, childScreenY, color);
                DrawVerticalLine(screenX, childScreenY, screenY, color);
            }
        }

        Achievement? hoveredAchievement = null;
        ItemRenderer itemRenderer = new();
        GLManager.GL.PushMatrix();
        GLManager.GL.Rotate(180.0F, 1.0F, 0.0F, 0.0F);
        Lighting.turnOn();
        GLManager.GL.PopMatrix();
        GLManager.GL.Disable(GLEnum.Lighting);
        GLManager.GL.Enable(GLEnum.RescaleNormal);
        GLManager.GL.Enable(GLEnum.ColorMaterial);

        int iconScreenY;
        for (int achievementIconIdx = 0; achievementIconIdx < global::BetaSharp.Achievements.AllAchievements.Count; ++achievementIconIdx)
        {
            Achievement iconAchievement = global::BetaSharp.Achievements.AllAchievements[achievementIconIdx];
            screenX = iconAchievement.column * 24 - scrollX;
            screenY = iconAchievement.row * 24 - scrollY;
            if (screenX >= -24 && screenY >= -24 && screenX <= 224 && screenY <= 155)
            {
                float brightness;
                if (statFileWriter.HasAchievementUnlocked(iconAchievement))
                {
                    brightness = 1.0F;
                    GLManager.GL.Color4(brightness, brightness, brightness, 1.0F);
                }
                else if (statFileWriter.CanUnlockAchievement(iconAchievement))
                {
                    brightness = Math.Sin(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 600L / 600.0D * Math.PI * 2.0D) < 0.6D ? 0.6F : 0.8F;
                    GLManager.GL.Color4(brightness, brightness, brightness, 1.0F);
                }
                else
                {
                    brightness = 0.3F;
                    GLManager.GL.Color4(brightness, brightness, brightness, 1.0F);
                }

                Game.textureManager.BindTexture(bgTexture);
                iconScreenX = scrollAreaLeft + screenX;
                iconScreenY = scrollAreaTop + screenY;
                if (iconAchievement.isChallenge())
                {
                    DrawTexturedModalRect(iconScreenX - 2, iconScreenY - 2, 26, 202, 26, 26);
                }
                else
                {
                    DrawTexturedModalRect(iconScreenX - 2, iconScreenY - 2, 0, 202, 26, 26);
                }

                if (!statFileWriter.CanUnlockAchievement(iconAchievement))
                {
                    float dimAlpha = 0.1F;
                    GLManager.GL.Color4(dimAlpha, dimAlpha, dimAlpha, 1.0F);
                    itemRenderer.useCustomDisplayColor = false;
                }

                GLManager.GL.Enable(GLEnum.Lighting);
                GLManager.GL.Enable(GLEnum.CullFace);
                itemRenderer.renderItemIntoGUI(Game.fontRenderer, Game.textureManager, iconAchievement.icon, iconScreenX + 3, iconScreenY + 3);
                GLManager.GL.Disable(GLEnum.Lighting);
                if (!statFileWriter.CanUnlockAchievement(iconAchievement))
                {
                    itemRenderer.useCustomDisplayColor = true;
                }

                GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
                if (mouseX >= scrollAreaLeft && mouseY >= scrollAreaTop && mouseX < scrollAreaLeft + 224 && mouseY < scrollAreaTop + 155 && mouseX >= iconScreenX && mouseX <= iconScreenX + 22 && mouseY >= iconScreenY && mouseY <= iconScreenY + 22)
                {
                    hoveredAchievement = iconAchievement;
                }
            }
        }

        GLManager.GL.Disable(GLEnum.DepthTest);
        GLManager.GL.Enable(GLEnum.Blend);
        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
        Game.textureManager.BindTexture(bgTexture);
        DrawTexturedModalRect(guiLeft, guiTop, 0, 0, field_27121_a, field_27119_i);
        GLManager.GL.PopMatrix();
        _zLevel = 0.0F;
        GLManager.GL.DepthFunc(GLEnum.Lequal);
        GLManager.GL.Disable(GLEnum.DepthTest);
        GLManager.GL.Enable(GLEnum.Texture2D);
        base.Render(mouseX, mouseY, partialTicks);
        if (hoveredAchievement != null)
        {
            string? description = hoveredAchievement.getTranslatedDescription();
            string statName = hoveredAchievement.StatName;
            int tooltipX = mouseX + 12;
            int tooltipY = mouseY - 4;
            if (statFileWriter.CanUnlockAchievement(hoveredAchievement))
            {
                int tooltipWidth = Math.Max(FontRenderer.GetStringWidth(statName), 120);
                int tooltipHeight = FontRenderer.GetStringHeight(description ?? "", tooltipWidth);
                if (statFileWriter.HasAchievementUnlocked(hoveredAchievement))
                {
                    tooltipHeight += 12;
                }

                DrawGradientRect(tooltipX - 3, tooltipY - 3, tooltipX + tooltipWidth + 3, tooltipY + tooltipHeight + 3 + 12, Color.BlackAlphaC0, Color.BlackAlphaC0);
                FontRenderer.DrawStringWrapped(description, tooltipX, tooltipY + 12, tooltipWidth, Color.GrayA0);
                if (statFileWriter.HasAchievementUnlocked(hoveredAchievement))
                {
                    FontRenderer.DrawStringWithShadow(StatCollector.TranslateToLocal("achievement.taken"), tooltipX, tooltipY + tooltipHeight + 4, Color.AchievementTakenBlue);
                }
            }
            else
            {
                int tooltipWidth = Math.Max(FontRenderer.GetStringWidth(statName), 120);
                string requiresText = StatCollector.TranslateToLocalFormatted("achievement.requires", new object[] { hoveredAchievement.parent.StatName });
                int requiresHeight = FontRenderer.GetStringHeight(requiresText, tooltipWidth);
                DrawGradientRect(tooltipX - 3, tooltipY - 3, tooltipX + tooltipWidth + 3, tooltipY + requiresHeight + 12 + 3, Color.BlackAlphaC0, Color.BlackAlphaC0);
                FontRenderer.DrawStringWrapped(requiresText, tooltipX, tooltipY + 12, tooltipWidth, Color.AchievementRequiresRed);
            }

            FontRenderer.DrawStringWithShadow(statName, tooltipX, tooltipY, statFileWriter.CanUnlockAchievement(hoveredAchievement) ? hoveredAchievement.isChallenge() ? Color.AchievementChallengeYellow : Color.White : hoveredAchievement.isChallenge() ? Color.AchievementChallengeLockedYellow : Color.Gray80);
        }

        GLManager.GL.Enable(GLEnum.DepthTest);
        GLManager.GL.Enable(GLEnum.Lighting);
        Lighting.turnOff();
    }
}
