using System;
using BetaSharp.Client.Guis.Controls;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Items;
using BetaSharp.Stats;
using Silk.NET.OpenGL.Legacy;

namespace BetaSharp.Client.Guis;

public class GuiAchievement : Control
{
    private static readonly long AchievementDisplayDuration = 3000L;
    private static readonly string LicenseWarningText = "Minecraft Beta 1.7.3   Unlicensed Copy :(";
    private static readonly string AltLocationWarningText = "(Or logged in from another location)";
    private static readonly string PurchasePromptText = "Purchase at minecraft.net";

    private readonly Minecraft _theGame;
    private int _achievementWindowWidth;
    private int _achievementWindowHeight;
    private string _achievementTitle;
    private string? _achievementDescription;
    private Achievement _theAchievement;
    private long _achievementDisplayStartTime;
    private readonly ItemRenderer _itemRender;
    private bool _isAchievementInformation;

    public GuiAchievement(Minecraft mc)
    {
        _theGame = mc;
        _itemRender = new ItemRenderer();
    }

    public void queueTakenAchievement(Achievement achievement)
    {
        _achievementTitle = StatCollector.TranslateToLocal("achievement.get");
        _achievementDescription = achievement.StatName;
        _achievementDisplayStartTime = GetCurrentTimeMillis();
        _theAchievement = achievement;
        _isAchievementInformation = false;
    }

    public void queueAchievementInformation(Achievement achievement)
    {
        _achievementTitle = achievement.StatName;
        _achievementDescription = achievement.getTranslatedDescription();
        _achievementDisplayStartTime = GetCurrentTimeMillis() - 2500L;
        _theAchievement = achievement;
        _isAchievementInformation = true;
    }

    private void updateAchievementWindowScale()
    {
        GLManager.GL.Viewport(0, 0, (uint)_theGame.displayWidth, (uint)_theGame.displayHeight);
        GLManager.GL.MatrixMode(GLEnum.Projection);
        GLManager.GL.LoadIdentity();
        GLManager.GL.MatrixMode(GLEnum.Modelview);
        GLManager.GL.LoadIdentity();

        _achievementWindowWidth = _theGame.displayWidth;
        _achievementWindowHeight = _theGame.displayHeight;
        ScaledResolution scaledResolution = new(_theGame.options, _theGame.displayWidth, _theGame.displayHeight);
        _achievementWindowWidth = scaledResolution.ScaledWidth;
        _achievementWindowHeight = scaledResolution.ScaledHeight;

        GLManager.GL.Clear(ClearBufferMask.DepthBufferBit);
        GLManager.GL.MatrixMode(GLEnum.Projection);
        GLManager.GL.LoadIdentity();
        GLManager.GL.Ortho(0.0D, _achievementWindowWidth, _achievementWindowHeight, 0.0D, 1000.0D, 3000.0D);
        GLManager.GL.MatrixMode(GLEnum.Modelview);
        GLManager.GL.LoadIdentity();
        GLManager.GL.Translate(0.0F, 0.0F, -2000.0F);
    }

    public void updateAchievementWindow()
    {
        if (Minecraft.hasPaidCheckTime > 0L)
        {
            displayLicenseWarning();
        }

        if (_theAchievement != null && _achievementDisplayStartTime != 0L)
        {
            displayAchievementNotification();
        }
    }

    private void displayLicenseWarning()
    {
        GLManager.GL.Disable(GLEnum.DepthTest);
        GLManager.GL.DepthMask(false);
        Lighting.turnOff();
        updateAchievementWindowScale();

        _theGame.fontRenderer.DrawStringWithShadow(LicenseWarningText, 2, 2, 0xFFFFFF);
        _theGame.fontRenderer.DrawStringWithShadow(AltLocationWarningText, 2, 11, 0xFFFFFF);
        _theGame.fontRenderer.DrawStringWithShadow(PurchasePromptText, 2, 20, 0xFFFFFF);

        GLManager.GL.DepthMask(true);
        GLManager.GL.Enable(GLEnum.DepthTest);
    }

    private void displayAchievementNotification()
    {
        double elapsedTime = (java.lang.System.currentTimeMillis() - _achievementDisplayStartTime) / AchievementDisplayDuration;

        if (_isAchievementInformation || _isAchievementInformation || elapsedTime >= 0.0D && elapsedTime <= 1.0D)
        {
            renderAchievementNotification(elapsedTime);
        }
        else
        {
            _achievementDisplayStartTime = 0L;
        }
    }

    private void renderAchievementNotification(double elapsedTime)
    {
        updateAchievementWindowScale();
        GLManager.GL.Disable(GLEnum.DepthTest);
        GLManager.GL.DepthMask(false);

        double animationProgress = calculateAnimationProgress(elapsedTime);
        int achievementX = _achievementWindowWidth - 160;
        int achievementY = 0 - (int)(animationProgress * 36.0D);
        int achievementTextureId = _theGame.textureManager.GetTextureId("/achievement/bg.png").Id;

        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
        GLManager.GL.Enable(GLEnum.Lighting);
        _theGame.textureManager.BindTexture(_theGame.textureManager.GetTextureId("/achievement/bg.png"));
        GLManager.GL.Disable(GLEnum.Lighting);

        DrawTextureRegion(achievementX, achievementY, 96, 202, 160, 32);
        drawAchievementText(achievementX, achievementY);

        GLManager.GL.PushMatrix();
        GLManager.GL.Rotate(180.0F, 1.0F, 0.0F, 0.0F);

        Lighting.turnOn();

        GLManager.GL.PopMatrix();
        GLManager.GL.Disable(GLEnum.Lighting);
        GLManager.GL.Enable(GLEnum.RescaleNormal);
        GLManager.GL.Enable(GLEnum.ColorMaterial);
        GLManager.GL.Enable(GLEnum.Lighting);

        _itemRender.renderItemIntoGUI(_theGame.fontRenderer, _theGame.textureManager, _theAchievement.icon, achievementX + 8, achievementY + 8);

        GLManager.GL.Disable(GLEnum.Lighting);
        GLManager.GL.DepthMask(true);
        GLManager.GL.Enable(GLEnum.DepthTest);
    }

    private double calculateAnimationProgress(double elapsedTime)
    {
        double progress = elapsedTime * 2.0D;
        if (progress > 1.0D)
        {
            progress = 2.0D - progress;
        }

        progress *= 4.0D;
        progress = 1.0D - progress;
        if (progress < 0.0D)
        {
            progress = 0.0D;
        }

        progress *= progress;
        progress *= progress;
        return progress;
    }

    private void drawAchievementText(int achievementX, int achievementY)
    {
        if (_isAchievementInformation)
        {
            _theGame.fontRenderer.DrawStringWrapped(_achievementDescription ?? "", achievementX + 30, achievementY + 7, 120, 0xFFFFFFFF);
        }
        else
        {
            _theGame.fontRenderer.DrawString(_achievementTitle, achievementX + 30, achievementY + 7, 0xFFFFFF00);
            _theGame.fontRenderer.DrawString(_achievementDescription ?? "", achievementX + 30, achievementY + 18, 0xFFFFFFFF);
        }
    }

    private long GetCurrentTimeMillis() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}
