namespace BetaSharp.Client.Guis;

public readonly partial struct Color
{
    public static Color Black { get; private set; } = new(0, 0, 0);

    /// <summary>
    /// TextColorTitle
    /// </summary>
    public static Color White { get; private set; } = new(0xFF, 0xFF, 0xFF);

    public static Color Blue { get; private set; } = new(0x00, 0x00, 0xFF);
    public static Color BlueAlpha { get; private set; } = new(0x00, 0x00, 0xFF, 130);

    /// <summary>
    /// TextColorNormal
    /// </summary>
    public static Color GrayE0 { get; private set; } = new(0xE0, 0xE0, 0xE0);

    // ReSharper disable once InconsistentNaming
    public static Color GrayCC { get; private set; } = new(0xCC, 0xCC, 0xCC);

    // ReSharper disable once InconsistentNaming
    public static Color GrayAA { get; private set; } = new(0xAA, 0xAA, 0xAA);

    /// <summary>
    /// TextColorKey
    /// </summary>
    public static Color GrayA0 { get; private set; } = new(0xA0, 0xA0, 0xA0);

    public static Color Gray90 { get; private set; } = new(0x90, 0x90, 0x90);
    public static Color Gray80 { get; private set; } = new(0x80, 0x80, 0x80);
    public static Color Gray70 { get; private set; } = new(0x70, 0x70, 0x70);
    public static Color Gray50 { get; private set; } = new(0x40, 0x40, 0x40);
    public static Color Gray40 { get; private set; } = new(0x40, 0x40, 0x40);
    public static Color Yellow { get; private set; } = new(0xFF, 0xFF, 0);
    public static Color BlackAlphaC0 { get; private set; } = new(0, 0, 0, 0xC0);

    /// <summary>
    /// BlackAlpha80
    /// </summary>
    public static Color BackgroundBlackAlpha { get; private set; } = new(0, 0, 0, 0x80);

    /// <summary>
    /// WhiteAlpha80
    /// </summary>
    public static Color BackgroundWhiteAlpha { get; private set; } = new(0xFF, 0xFF, 0xFF, 0x80);

    public static Color WhiteAlpha20 { get; private set; } = new(0xFF, 0xFF, 0xFF, 0x20);
    public static Color GameOverBackgroundDarkRed { get; private set; } = new(0, 0, 0x50, 0x60);
    public static Color GameOverBackgroundRed { get; private set; } = new(0x30, 0x30, 0x80, 0x60);
    public static Color WorldBackgroundDark { get; private set; } = new(0x10, 0x10, 0x10, 0xC0);
    public static Color WorldBackground { get; private set; } = new(0x10, 0x10, 0x10, 0xD0);

    public static Color HoverYellow { get; private set; } = new(0xFF, 0xFF, 0xA0);

    public static Color AchievementTakenBlue { get; private set; } = new(0x90, 0x90, 0xFF);
    public static Color AchievementRequiresRed { get; private set; } = new(0x70, 0x50, 0x50);
    public static Color AchievementChallengeYellow { get; private set; } = new(0xFF, 0xFF, 0x80);
    public static Color AchievementChallengeLockedYellow { get; private set; } = new(0x80, 0x80, 0x40);
}
