using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Items;
using BetaSharp.Stats;
using Silk.NET.OpenGL;
using GLEnum = BetaSharp.Client.Rendering.Core.OpenGL.GLEnum;

namespace BetaSharp.Client.Guis;

public class GuiAchievement : Gui
{
    private static readonly long AchievementDisplayDuration = 3000L;
    private static readonly string LicenseWarningText = "Minecraft Beta 1.7.3   Unlicensed Copy :(";
    private static readonly string AltLocationWarningText = "(Or logged in from another location)";
    private static readonly string PurchasePromptText = "Purchase at minecraft.net";

    private readonly BetaSharp _theGame;
    private int _achievementWindowWidth;
    private int _achievementWindowHeight;
    private string _achievementTitle;
    private string? _achievementDescription;
    private Achievement _theAchievement;
    private long _achievementDisplayStartTime;
    private readonly ItemRenderer _itemRender;
    private bool _isAchievementInformation;

    public GuiAchievement(BetaSharp game)
    {
        _theGame = game;
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
        GLManager.GL.Ortho(0.0D, _achievementWindowWidth, _achievementWindowHeight, 0.0D, -3000.0D, 3000.0D);
        GLManager.GL.MatrixMode(GLEnum.Modelview);
        GLManager.GL.LoadIdentity();
        GLManager.GL.Translate(0.0F, 0.0F, -2000.0F);
    }

    public void updateAchievementWindow()
    {
        if (BetaSharp.hasPaidCheckTime > 0L)
        {
            displayLicenseWarning();
        }
    }

    public void RenderAchievementOverlayIfAny(int scaledWidth, int scaledHeight)
    {
        if (_theAchievement == null || _achievementDisplayStartTime == 0L)
            return;

        double elapsedTime = (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _achievementDisplayStartTime) / (double)AchievementDisplayDuration;
        if (!_isAchievementInformation && (elapsedTime < 0.0D || elapsedTime > 1.0D))
        {
            _achievementDisplayStartTime = 0L;
            return;
        }

        renderAchievementInCurrentState(scaledWidth, scaledHeight, elapsedTime);
    }

    private void renderAchievementInCurrentState(int scaledWidth, int scaledHeight, double elapsedTime)
    {
        GLManager.GL.Disable(GLEnum.DepthTest);
        GLManager.GL.DepthMask(false);
        GLManager.GL.Enable(GLEnum.Texture2D);
        GLManager.GL.Enable(GLEnum.Blend);
        GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);

        double animationProgress = calculateAnimationProgress(elapsedTime);
        int achievementX = scaledWidth - 160;
        int achievementY = 0 - (int)(animationProgress * 36.0D);

        _theGame.textureManager.BindTexture(_theGame.textureManager.GetTextureId("/achievement/bg.png"));
        _zLevel = -90.0F;
        DrawTexturedModalRect(achievementX, achievementY, 96, 202, 160, 32);
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

    private void displayLicenseWarning()
    {
        GLManager.GL.Disable(GLEnum.DepthTest);
        GLManager.GL.DepthMask(false);
        Lighting.turnOff();
        updateAchievementWindowScale();

        _theGame.fontRenderer.DrawStringWithShadow(LicenseWarningText, 2, 2, Color.White);
        _theGame.fontRenderer.DrawStringWithShadow(AltLocationWarningText, 2, 11, Color.White);
        _theGame.fontRenderer.DrawStringWithShadow(PurchasePromptText, 2, 20, Color.White);

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
            _theGame.fontRenderer.DrawStringWrapped(_achievementDescription ?? "", achievementX + 30, achievementY + 7, 120, Color.White);
        }
        else
        {
            _theGame.fontRenderer.DrawString(_achievementTitle, achievementX + 30, achievementY + 7, Color.Yellow);
            _theGame.fontRenderer.DrawString(_achievementDescription ?? "", achievementX + 30, achievementY + 18, Color.White);
        }
    }

    private long GetCurrentTimeMillis() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}
