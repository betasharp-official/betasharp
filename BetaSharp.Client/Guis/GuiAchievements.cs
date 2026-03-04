using BetaSharp.Blocks;
using BetaSharp.Client.Input;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.Rendering.Items;
using BetaSharp.Stats;
using BetaSharp.Util.Maths;
using Silk.NET.OpenGL.Legacy;

namespace BetaSharp.Client.Guis;

public class GuiAchievements : Screen
{
    private static readonly int field_27126_s = BetaSharp.Achievements.minColumn * 24 - 112;
    private static readonly int field_27125_t = BetaSharp.Achievements.minRow * 24 - 112;
    private static readonly int field_27124_u = BetaSharp.Achievements.maxColumn * 24 - 77;
    private static readonly int field_27123_v = BetaSharp.Achievements.maxRow * 24 - 77;
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
        short var2 = 141;
        short var3 = 141;
        field_27116_m = field_27114_o = field_27112_q = BetaSharp.Achievements.OpenInventory.column * 24 - var2 / 2f - 12;
        field_27115_n = field_27113_p = field_27111_r = BetaSharp.Achievements.OpenInventory.row * 24 - var3 / 2f;

        Control container = new(Width / 2 - 128, Height / 2 - 101, 256, 202)
            { VerticalCenteringBehavior = CenteringBehavior.Middle };
        Button doneButton = new(152, 175, 80, 20, StatCollector.TranslateToLocal("gui.done"));
        doneButton.Clicked += (_, _) => MC.OpenScreen(null);
        container.AddChild(doneButton);
        AddChild(container);
    }

    protected override void OnKeyInput(KeyboardEventArgs e)
    {
        if (e.Key == MC.options.KeyBindInventory.keyCode)
        {
            MC.OpenScreen(null);
        }
    }

    protected override void OnRender(RenderEventArgs e)
    {
        if (Mouse.isButtonDown(0))
        {
            int var4 = (Width - field_27121_a) / 2;
            int var5 = (Height - field_27119_i) / 2;
            int var6 = var4 + 8;
            int var7 = var5 + 17;
            if (field_27122_w is 0 or 1 && e.MouseX >= var6 && e.MouseX < var6 + 224 && e.MouseY >= var7 && e.MouseY < var7 + 155)
            {
                if (field_27122_w == 0)
                {
                    field_27122_w = 1;
                }
                else
                {
                    field_27114_o -= e.MouseX - field_27118_j;
                    field_27113_p -= e.MouseY - field_27117_l;
                    field_27112_q = field_27116_m = field_27114_o;
                    field_27111_r = field_27115_n = field_27113_p;
                }

                field_27118_j = e.MouseX;
                field_27117_l = e.MouseY;
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
        func_27109_b(e.MouseX, e.MouseY, e.TickDelta);
        GLManager.GL.Disable(GLEnum.Lighting);
        GLManager.GL.Disable(GLEnum.DepthTest);
        func_27110_k();
        GLManager.GL.Enable(GLEnum.Lighting);
        GLManager.GL.Enable(GLEnum.DepthTest);
    }

    protected override void OnTick()
    {
        field_27116_m = field_27114_o;
        field_27115_n = field_27113_p;
        double var1 = field_27112_q - field_27114_o;
        double var3 = field_27111_r - field_27113_p;
        if (var1 * var1 + var3 * var3 < 4.0D)
        {
            field_27114_o += var1;
            field_27113_p += var3;
        }
        else
        {
            field_27114_o += var1 * 0.85D;
            field_27113_p += var3 * 0.85D;
        }
    }

    protected void func_27110_k()
    {
        int var1 = (Width - field_27121_a) / 2;
        int var2 = (Height - field_27119_i) / 2;
        FontRenderer.DrawString("Achievements", var1 + 15, var2 + 5, 0x404040);
    }

    protected void func_27109_b(int var1, int var2, float var3)
    {
        int var4 = MathHelper.Floor(field_27116_m + (field_27114_o - field_27116_m) * (double)var3);
        int var5 = MathHelper.Floor(field_27115_n + (field_27113_p - field_27115_n) * (double)var3);
        if (var4 < field_27126_s)
        {
            var4 = field_27126_s;
        }

        if (var5 < field_27125_t)
        {
            var5 = field_27125_t;
        }

        if (var4 >= field_27124_u)
        {
            var4 = field_27124_u - 1;
        }

        if (var5 >= field_27123_v)
        {
            var5 = field_27123_v - 1;
        }

        TextureHandle var6 = MC.textureManager.GetTextureId("/terrain.png");
        TextureHandle var7 = MC.textureManager.GetTextureId("/achievement/bg.png");
        int var8 = (Width - field_27121_a) / 2;
        int var9 = (Height - field_27119_i) / 2;
        int var10 = var8 + 16;
        int var11 = var9 + 17;
        ZLevel = 0.0F;
        GLManager.GL.DepthFunc(GLEnum.Gequal);
        GLManager.GL.PushMatrix();
        GLManager.GL.Translate(0.0F, 0.0F, -200.0F);
        GLManager.GL.Enable(GLEnum.Texture2D);
        GLManager.GL.Disable(GLEnum.Lighting);
        GLManager.GL.Enable(GLEnum.RescaleNormal);
        GLManager.GL.Enable(GLEnum.ColorMaterial);
        MC.textureManager.BindTexture(var6);
        int var12 = var4 + 288 >> 4;
        int var13 = var5 + 288 >> 4;
        int var14 = (var4 + 288) % 16;
        int var15 = (var5 + 288) % 16;
        JavaRandom var21 = new();

        for (int var22 = 0; var22 * 16 - var15 < 155; ++var22)
        {
            float var23 = 0.6F - (var13 + var22) / 25.0F * 0.3F;
            GLManager.GL.Color4(var23, var23, var23, 1.0F);

            for (int var24 = 0; var24 * 16 - var14 < 224; ++var24)
            {
                var21.SetSeed(1234 + var12 + var24);
                var21.NextInt();
                int var25 = var21.NextInt(1 + var13 + var22) + (var13 + var22) / 2;
                int var26 = Block.Sand.textureId;
                if (var25 <= 37 && var13 + var22 != 35)
                {
                    if (var25 == 22)
                    {
                        if (var21.NextInt(2) == 0)
                        {
                            var26 = Block.DiamondOre.textureId;
                        }
                        else
                        {
                            var26 = Block.RedstoneOre.textureId;
                        }
                    }
                    else if (var25 == 10)
                    {
                        var26 = Block.IronOre.textureId;
                    }
                    else if (var25 == 8)
                    {
                        var26 = Block.CoalOre.textureId;
                    }
                    else if (var25 > 4)
                    {
                        var26 = Block.Stone.textureId;
                    }
                    else if (var25 > 0)
                    {
                        var26 = Block.Dirt.textureId;
                    }
                }
                else
                {
                    var26 = Block.Bedrock.textureId;
                }

                DrawTextureRegion(var10 + var24 * 16 - var14, var11 + var22 * 16 - var15, var26 % 16 << 4, var26 >> 4 << 4, 16, 16);
            }
        }

        GLManager.GL.Enable(GLEnum.DepthTest);
        GLManager.GL.DepthFunc(GLEnum.Lequal);
        GLManager.GL.Disable(GLEnum.Texture2D);

        int var16;
        int var17;
        int var33;
        int var38;
        for (var12 = 0; var12 < BetaSharp.Achievements.AllAchievements.Count; ++var12)
        {
            Achievement var28 = BetaSharp.Achievements.AllAchievements[var12];
            if (var28.parent != null)
            {
                var14 = var28.column * 24 - var4 + 11 + var10;
                var15 = var28.row * 24 - var5 + 11 + var11;
                var16 = var28.parent.column * 24 - var4 + 11 + var10;
                var17 = var28.parent.row * 24 - var5 + 11 + var11;
                bool var19 = statFileWriter.HasAchievementUnlocked(var28);
                bool var20 = statFileWriter.CanUnlockAchievement(var28);
                var38 = java.lang.Math.sin(java.lang.System.currentTimeMillis() % 600L / 600.0D * Math.PI * 2.0D) > 0.6D ? 255 : 130;
                uint color;
                if (var19)
                {
                    color = 0xFF707070U;
                }
                else if (var20)
                {
                    color = 0xFF + (uint)(var38 << 24);
                }
                else
                {
                    color = 0xFF000000U;
                }

                Gui.DrawHorizontalLine(var14, var16, var15, color);
                Gui.DrawVerticalLine(var16, var15, var17, color);
            }
        }

        Achievement var27 = null;
        ItemRenderer var29 = new();
        GLManager.GL.PushMatrix();
        GLManager.GL.Rotate(180.0F, 1.0F, 0.0F, 0.0F);
        Lighting.turnOn();
        GLManager.GL.PopMatrix();
        GLManager.GL.Disable(GLEnum.Lighting);
        GLManager.GL.Enable(GLEnum.RescaleNormal);
        GLManager.GL.Enable(GLEnum.ColorMaterial);

        int var34;
        for (var14 = 0; var14 < BetaSharp.Achievements.AllAchievements.Count; ++var14)
        {
            Achievement var30 = BetaSharp.Achievements.AllAchievements[var14];
            var16 = var30.column * 24 - var4;
            var17 = var30.row * 24 - var5;
            if (var16 >= -24 && var17 >= -24 && var16 <= 224 && var17 <= 155)
            {
                float var35;
                if (statFileWriter.HasAchievementUnlocked(var30))
                {
                    var35 = 1.0F;
                    GLManager.GL.Color4(var35, var35, var35, 1.0F);
                }
                else if (statFileWriter.CanUnlockAchievement(var30))
                {
                    var35 = java.lang.Math.sin(java.lang.System.currentTimeMillis() % 600L / 600.0D * Math.PI * 2.0D) < 0.6D ? 0.6F : 0.8F;
                    GLManager.GL.Color4(var35, var35, var35, 1.0F);
                }
                else
                {
                    var35 = 0.3F;
                    GLManager.GL.Color4(var35, var35, var35, 1.0F);
                }

                MC.textureManager.BindTexture(var7);
                var33 = var10 + var16;
                var34 = var11 + var17;
                if (var30.isChallenge())
                {
                    DrawTextureRegion(var33 - 2, var34 - 2, 26, 202, 26, 26);
                }
                else
                {
                    DrawTextureRegion(var33 - 2, var34 - 2, 0, 202, 26, 26);
                }

                if (!statFileWriter.CanUnlockAchievement(var30))
                {
                    float var36 = 0.1F;
                    GLManager.GL.Color4(var36, var36, var36, 1.0F);
                    var29.useCustomDisplayColor = false;
                }

                GLManager.GL.Enable(GLEnum.Lighting);
                GLManager.GL.Enable(GLEnum.CullFace);
                var29.renderItemIntoGUI(MC.fontRenderer, MC.textureManager, var30.icon, var33 + 3, var34 + 3);
                GLManager.GL.Disable(GLEnum.Lighting);
                if (!statFileWriter.CanUnlockAchievement(var30))
                {
                    var29.useCustomDisplayColor = true;
                }

                GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
                if (var1 >= var10 && var2 >= var11 && var1 < var10 + 224 && var2 < var11 + 155 && var1 >= var33 && var1 <= var33 + 22 && var2 >= var34 && var2 <= var34 + 22)
                {
                    var27 = var30;
                }
            }
        }

        GLManager.GL.Disable(GLEnum.DepthTest);
        GLManager.GL.Enable(GLEnum.Blend);
        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
        MC.textureManager.BindTexture(var7);
        DrawTextureRegion(var8, var9, 0, 0, field_27121_a, field_27119_i);
        GLManager.GL.PopMatrix();
        ZLevel = 0.0F;
        GLManager.GL.DepthFunc(GLEnum.Lequal);
        GLManager.GL.Disable(GLEnum.DepthTest);
        GLManager.GL.Enable(GLEnum.Texture2D);

        if (var27 != null)
        {
            string? var32 = var27.getTranslatedDescription();
            string var31 = var27.StatName;
            var17 = var1 + 12;
            var33 = var2 - 4;
            if (statFileWriter.CanUnlockAchievement(var27))
            {
                var34 = java.lang.Math.max(FontRenderer.GetStringWidth(var31), 120);
                int var37 = FontRenderer.GetStringHeight(var32 ?? "", var34);
                if (statFileWriter.HasAchievementUnlocked(var27))
                {
                    var37 += 12;
                }

                Gui.DrawGradientRect(var17 - 3, var33 - 3, var17 + var34 + 3, var33 + var37 + 3 + 12, 0xC0000000U, 0xC0000000U);
                FontRenderer.DrawStringWrapped(var32, var17, var33 + 12, var34, 0xFFA0A0A0);
                if (statFileWriter.HasAchievementUnlocked(var27))
                {
                    FontRenderer.DrawStringWithShadow(StatCollector.TranslateToLocal("achievement.taken"), var17, var33 + var37 + 4, 0xFF9090FF);
                }
            }
            else
            {
                var34 = java.lang.Math.max(FontRenderer.GetStringWidth(var31), 120);
                string var39 = StatCollector.TranslateToLocalFormatted("achievement.requires", var27.parent.StatName);
                var38 = FontRenderer.GetStringHeight(var39, var34);
                Gui.DrawGradientRect(var17 - 3, var33 - 3, var17 + var34 + 3, var33 + var38 + 12 + 3, 0xC0000000, 0xC0000000);
                FontRenderer.DrawStringWrapped(var39, var17, var33 + 12, var34, 0xFF705050);
            }

            FontRenderer.DrawStringWithShadow(var31, var17, var33, statFileWriter.CanUnlockAchievement(var27) ? var27.isChallenge() ? 0xFFFFFF80 : 0xFFFFFFFF : var27.isChallenge() ? 0xFF808040 : 0xFF808080);
        }

        GLManager.GL.Enable(GLEnum.DepthTest);
        GLManager.GL.Enable(GLEnum.Lighting);
        Lighting.turnOff();
    }
}
