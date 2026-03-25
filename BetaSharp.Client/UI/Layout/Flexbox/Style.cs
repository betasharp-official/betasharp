namespace BetaSharp.Client.UI.Layout.Flexbox;

public class Style
{


    internal Direction Direction = Direction.Inherit;
    internal FlexDirection FlexDirection = FlexDirection.Row;
    internal Justify JustifyContent = Justify.FlexStart;
    internal Align AlignContent = Align.Stretch;
    internal Align AlignItems = Align.Stretch;
    internal Align AlignSelf = Align.Auto;
    internal PositionType PositionType = PositionType.Relative;
    internal Wrap FlexWrap = Wrap.NoWrap;
    internal Overflow Overflow = Overflow.Visible;
    internal Display Display = Display.Flex;
    internal float FlexGrow = 0f;
    internal float FlexShrink = 1f;
    internal Value FlexBasis = CreateAutoValue();
    internal Value[] Margin = CreateDefaultEdgeValuesUnit();
    internal Value[] Position = CreateDefaultEdgeValuesUnit();
    internal Value[] Padding = CreateDefaultEdgeValuesUnit();
    internal Value[] Border = CreateDefaultEdgeValuesUnit();
    internal Value[] Dimensions = [CreateAutoValue(), CreateAutoValue()];
    internal Value[] MinDimensions = [Value.UndefinedValue, Value.UndefinedValue];
    internal Value[] MaxDimensions = [Value.UndefinedValue, Value.UndefinedValue];
    // Yoga specific properties, not compatible with flexbox specification
    internal float AspectRatio = float.NaN;


    // default values of supported attrs
    protected static readonly Dictionary<string, string> layoutAttributeDefault = new()
    {
        {"display", "flex"},
        {"overflow", "visible"},
        {"position", "relative"},
        {"align-content", "stretch"},
        {"align-items", "stretch"},
        {"align-self", "auto"},
        {"flex-direction", "row"},
        {"flex-wrap", "no-wrap"},
        {"flex-basis", "auto"},
        {"flex-shrink", "1"},
        {"flex-grow", "0"},
        {"justify-content", "flex-start"},
        {"direction", "inherit"},
        {"left", "auto"},
        {"top", "auto"},
        {"right", "auto"},
        {"bottom", "auto"},
        {"width", "auto"},
        {"height", "auto"},
        {"min-width", "auto"},
        {"min-height", "auto"},
        {"max-width", "auto"},
        {"max-height", "auto"},
        {"margin", "skip"},
        {"margin-left", "0"},
        {"margin-right", "0"},
        {"margin-top", "0"},
        {"margin-bottom", "0"},
        {"padding", "skip"},
        {"padding-left", "0"},
        {"padding-right", "0"},
        {"padding-top", "0"},
        {"padding-bottom", "0"},
        {"border-width", "skip"},
        {"border-left-width", "0"},
        {"border-right-width", "0"},
        {"border-top-width", "0"},
        {"border-bottom-width", "0"},
    };

    // default values for inherit attrs
    protected static readonly Dictionary<string, string> layoutAttributeInherit = new()
    {
        {"direction", "ltr"}
    };

    // change logic for track changes when calls Set() 
    protected bool setMode = false;

    // use to store affected attrs
    protected readonly Dictionary<string, string> layoutAttribute = [];
    // use to store previous values for changed attributes (relative Apply(), Set() and this[] )
    protected readonly Dictionary<string, string> layoutAttributeChanged = [];
    // use to store attrs values before Set() was called. Thus attributeChanged represents changed values relative values before Set() was called.
    protected readonly Dictionary<string, string> layoutAttributeWas = [];
    // use to store attrs values which changed by animation(see ApplyAnimation())
    protected readonly Dictionary<string, string> layoutAttributeAnimated = [];


    public static void Copy(Style dest, Style src)
    {
        dest.Direction = src.Direction;
        dest.FlexDirection = src.FlexDirection;
        dest.JustifyContent = src.JustifyContent;
        dest.AlignContent = src.AlignContent;
        dest.AlignItems = src.AlignItems;
        dest.AlignSelf = src.AlignSelf;
        dest.PositionType = src.PositionType;
        dest.FlexWrap = src.FlexWrap;
        dest.Overflow = src.Overflow;
        dest.Display = src.Display;
        dest.FlexGrow = src.FlexGrow;
        dest.FlexShrink = src.FlexShrink;
        dest.FlexBasis = src.FlexBasis.Clone();

        Value.CopyValue(dest.Margin, src.Margin);
        Value.CopyValue(dest.Position, src.Position);
        Value.CopyValue(dest.Padding, src.Padding);
        Value.CopyValue(dest.Border, src.Border);

        Value.CopyValue(dest.Dimensions, src.Dimensions);
        Value.CopyValue(dest.MinDimensions, src.MinDimensions);
        Value.CopyValue(dest.MaxDimensions, src.MaxDimensions);

        dest.AspectRatio = src.AspectRatio;
    }
    internal static Value CreateAutoValue()
    {
        return new Value(float.NaN, Unit.Auto);
    }

    internal static Value[] CreateDefaultEdgeValuesUnit()
    {
        return [
            Value.UndefinedValue,
            Value.UndefinedValue,
            Value.UndefinedValue,
            Value.UndefinedValue,
            Value.UndefinedValue,
            Value.UndefinedValue,
            Value.UndefinedValue,
            Value.UndefinedValue,
            Value.UndefinedValue,
        ];
    }

    public virtual Style Clone()
    {
        var clone = new Style();
        Style.Copy(this, clone);
        return clone;
    }
}
