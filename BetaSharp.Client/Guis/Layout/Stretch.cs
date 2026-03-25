namespace BetaSharp.Client.Guis.Layout;

public enum Stretch
{
    /// Do not stretch, use the control's preferred size
    None,
    /// Stretch horizontally
    Horizontal,
    /// Stretch vertically
    Vertical,
    /// Stretch to fill the available space, ignoring aspect ratio
    Fill,
    /// Maintain aspect ratio, but stretch to fit within the available space
    UniformMin,
    /// Maintain aspect ratio, but stretch to fill the available space (cropping if necessary)
    UniformMax,
    /// Maintain aspect ratio, but stretch to fill the available horizontal space (cropping if necessary)
    UniformHorizontal,
    /// Maintain aspect ratio, but stretch to fill the available vertical space (cropping if necessary)
    UniformVertical,
}
