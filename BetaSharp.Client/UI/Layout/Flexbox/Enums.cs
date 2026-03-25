namespace BetaSharp.Client.UI.Layout.Flexbox;

public enum Align
{
    Auto,
    FlexStart,
    Center,
    FlexEnd,
    Stretch,
    Baseline,
    SpaceBetween,
    SpaceAround,
}

public enum Dimension
{
    Width,
    Height,
}

public enum Direction
{

    Inherit = 0,
    LTR,
    RTL,
    NeverUsed_1 = -1,
}

public enum Display
{
    Flex,
    None,
}

public enum Edge : int
{
    Left,
    Top,
    Right,
    Bottom,
    Start,
    End,
    Horizontal,
    Vertical,
    All,
}


public enum ExperimentalFeature
{
    WebFlexBasis,
}

public enum FlexDirection
{
    Column,
    ColumnReverse,
    Row,
    RowReverse,
}

public enum Justify
{
    FlexStart,
    Center,
    FlexEnd,
    SpaceBetween,
    SpaceAround,
}

public enum LogLevel
{
    Error,
    Warn,
    Info,
    Debug,
    Verbose,
    Fatal,
}

public enum MeasureMode : int
{
    Undefined = 0,
    Exactly,
    AtMost,
    NeverUsed_1 = -1,
}

public enum NodeType
{
    Default,
    Text,
}

public enum Overflow
{
    Visible,
    Hidden,
    Scroll,
}

public enum PositionType
{
    Relative,
    Absolute,
}

public enum PrintOptions
{
    Layout,
    Style,
    Children,
}

public enum Unit
{
    Undefined,
    Point,
    Percent,
    Auto,
}

public enum Wrap
{
    NoWrap,
    Wrap,
    WrapReverse,
}
