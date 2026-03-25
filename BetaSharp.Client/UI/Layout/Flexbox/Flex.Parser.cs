namespace BetaSharp.Client.UI.Layout.Flexbox;

public partial class Flex
{

    #region XXXX_ToSring
    // AlignToString returns string version of Align enum
    public static string AlignToString(Align value)
    {
        switch (value)
        {
            case Align.Auto:
                return "auto";
            case Align.FlexStart:
                return "flex-start";
            case Align.Center:
                return "center";
            case Align.FlexEnd:
                return "flex-end";
            case Align.Stretch:
                return "stretch";
            case Align.Baseline:
                return "baseline";
            case Align.SpaceBetween:
                return "space-between";
            case Align.SpaceAround:
                return "space-around";
        }
        return "unknown";
    }

    // DimensionToString returns string version of Dimension enum
    public static string DimensionToString(Dimension value)
    {
        switch (value)
        {
            case Dimension.Width:
                return "width";
            case Dimension.Height:
                return "height";
        }
        return "unknown";
    }

    // DirectionToString returns string version of Direction enum
    public static string DirectionToString(Direction value)
    {
        switch (value)
        {
            case Direction.Inherit:
                return "inherit";
            case Direction.LTR:
                return "ltr";
            case Direction.RTL:
                return "rtl";
        }
        return "unknown";
    }

    // DisplayToString returns string version of Display enum
    public static string DisplayToString(Display value)
    {
        switch (value)
        {
            case Display.Flex:
                return "flex";
            case Display.None:
                return "none";
        }
        return "unknown";
    }

    // EdgeToString returns string version of Edge enum
    public static string EdgeToString(Edge value)
    {
        switch (value)
        {
            case Edge.Left:
                return "left";
            case Edge.Top:
                return "top";
            case Edge.Right:
                return "right";
            case Edge.Bottom:
                return "bottom";
            case Edge.Start:
                return "start";
            case Edge.End:
                return "end";
            case Edge.Horizontal:
                return "horizontal";
            case Edge.Vertical:
                return "vertical";
            case Edge.All:
                return "all";
        }
        return "unknown";
    }

    // ExperimentalFeatureToString returns string version of ExperimentalFeature enum
    public static string ExperimentalFeatureToString(ExperimentalFeature value)
    {
        switch (value)
        {
            case ExperimentalFeature.WebFlexBasis:
                return "web-flex-basis";
        }
        return "unknown";
    }

    // FlexDirectionToString returns string version of FlexDirection enum
    public static string FlexDirectionToString(FlexDirection value)
    {
        switch (value)
        {
            case FlexDirection.Column:
                return "column";
            case FlexDirection.ColumnReverse:
                return "column-reverse";
            case FlexDirection.Row:
                return "row";
            case FlexDirection.RowReverse:
                return "row-reverse";
        }
        return "unknown";
    }

    // JustifyToString returns string version of Justify enum
    public static string JustifyToString(Justify value)
    {
        switch (value)
        {
            case Justify.FlexStart:
                return "flex-start";
            case Justify.Center:
                return "center";
            case Justify.FlexEnd:
                return "flex-end";
            case Justify.SpaceBetween:
                return "space-between";
            case Justify.SpaceAround:
                return "space-around";
        }
        return "unknown";
    }

    // LogLevelToString returns string version of LogLevel enum
    public static string LogLevelToString(LogLevel value)
    {
        switch (value)
        {
            case LogLevel.Error:
                return "error";
            case LogLevel.Warn:
                return "warn";
            case LogLevel.Info:
                return "info";
            case LogLevel.Debug:
                return "debug";
            case LogLevel.Verbose:
                return "verbose";
            case LogLevel.Fatal:
                return "fatal";
        }
        return "unknown";
    }

    // MeasureModeToString returns string version of MeasureMode enum
    public static string MeasureModeToString(MeasureMode value)
    {
        switch (value)
        {
            case MeasureMode.Undefined:
                return "undefined";
            case MeasureMode.Exactly:
                return "exactly";
            case MeasureMode.AtMost:
                return "at-most";
        }
        return "unknown";
    }

    // NodeTypeToString returns string version of NodeType enum
    public static string NodeTypeToString(NodeType value)
    {
        switch (value)
        {
            case NodeType.Default:
                return "default";
            case NodeType.Text:
                return "text";
        }
        return "unknown";
    }

    // OverflowToString returns string version of Overflow enum
    public static string OverflowToString(Overflow value)
    {
        switch (value)
        {
            case Overflow.Visible:
                return "visible";
            case Overflow.Hidden:
                return "hidden";
            case Overflow.Scroll:
                return "scroll";
        }
        return "unknown";
    }

    // PositionType returns string version of PositionType enum
    public static string PositionTypeToString(PositionType value)
    {
        switch (value)
        {
            case PositionType.Relative:
                return "relative";
            case PositionType.Absolute:
                return "absolute";
        }
        return "unknown";
    }

    // PrintOptionsToString returns string version of PrintOptions enum
    public static string PrintOptionsToString(PrintOptions value)
    {
        switch (value)
        {
            case PrintOptions.Layout:
                return "layout";
            case PrintOptions.Style:
                return "style";
            case PrintOptions.Children:
                return "children";
        }
        return "unknown";
    }

    // UnitToString returns string version of Unit enum
    public static string UnitToString(Unit value)
    {
        switch (value)
        {
            case Unit.Undefined:
                return "undefined";
            case Unit.Point:
                return "point";
            case Unit.Percent:
                return "percent";
            case Unit.Auto:
                return "auto";
        }
        return "unknown";
    }

    // WrapToString returns string version of Wrap enum
    public static string WrapToString(Wrap value)
    {
        switch (value)
        {
            case Wrap.NoWrap:
                return "no-wrap";
            case Wrap.Wrap:
                return "wrap";
            case Wrap.WrapReverse:
                return "wrap-reverse";
        }
        return "unknown";
    }
    #endregion


    #region StringTo_XXXX

    // AlignToString returns string version of Align enum
    public static bool StringToAlign(string value, out Align result)
    {
        switch (value)
        {
            case "auto": result = Align.Auto; return true;
            case "flex-start": result = Align.FlexStart; return true;
            case "center": result = Align.Center; return true;
            case "flex-end": result = Align.FlexEnd; return true;
            case "stretch": result = Align.Stretch; return true;
            case "baseline": result = Align.Baseline; return true;
            case "space-between": result = Align.SpaceBetween; return true;
            case "space-around": result = Align.SpaceAround; return true;
        }
        result = Align.Auto;
        return false;
    }

    // DimensionToString returns string version of Dimension enum
    public static bool StringToDimension(string value, out Dimension result)
    {
        switch (value)
        {
            case "width": result = Dimension.Width; return true;
            case "height": result = Dimension.Height; return true;
        }
        result = Dimension.Width;
        return false;
    }

    // DirectionToString returns string version of Direction enum
    public static bool StringToDirection(string value, out Direction result)
    {
        switch (value)
        {
            case "inherit": result = Direction.Inherit; return true;
            case "ltr": result = Direction.LTR; return true;
            case "rtl": result = Direction.RTL; return true;
        }
        result = Direction.Inherit;
        return false;
    }

    // DisplayToString returns string version of Display enum
    public static bool StringToDisplay(string value, out Display result)
    {
        switch (value)
        {
            case "flex": result = Display.Flex; return true;
            case "none": result = Display.None; return true;
        }
        result = Display.Flex;
        return false;
    }

    // EdgeToString returns string version of Edge enum
    public static bool StringToEdge(string value, out Edge result)
    {
        switch (value)
        {
            case "left": result = Edge.Left; return true;
            case "top": result = Edge.Top; return true;
            case "right": result = Edge.Right; return true;
            case "bottom": result = Edge.Bottom; return true;
            case "start": result = Edge.Start; return true;
            case "end": result = Edge.End; return true;
            case "horizontal": result = Edge.Horizontal; return true;
            case "vertical": result = Edge.Vertical; return true;
            case "all": result = Edge.All; return true;
        }
        result = Edge.Left;
        return false;
    }

    // ExperimentalFeatureToString returns string version of ExperimentalFeature enum
    public static bool StringToExperimentalFeature(string value, out ExperimentalFeature result)
    {
        switch (value)
        {
            case "web-flex-basis": result = ExperimentalFeature.WebFlexBasis; return true;
        }
        result = ExperimentalFeature.WebFlexBasis;
        return false;
    }

    // FlexDirectionToString returns string version of FlexDirection enum
    public static bool StringToFlexDirection(string value, out FlexDirection result)
    {
        switch (value)
        {
            case "column": result = FlexDirection.Column; return true;
            case "column-reverse": result = FlexDirection.ColumnReverse; return true;
            case "row": result = FlexDirection.Row; return true;
            case "row-reverse": result = FlexDirection.RowReverse; return true;
        }
        result = FlexDirection.Column;
        return false;
    }

    // JustifyToString returns string version of Justify enum
    public static bool StringToJustify(string value, out Justify result)
    {
        switch (value)
        {
            case "flex-start": result = Justify.FlexStart; return true;
            case "center": result = Justify.Center; return true;
            case "flex-end": result = Justify.FlexEnd; return true;
            case "space-between": result = Justify.SpaceBetween; return true;
            case "space-around": result = Justify.SpaceAround; return true;
        }
        result = Justify.FlexStart;
        return false;
    }

    public static bool StringToLogLevel(string value, out LogLevel result)
    {
        switch (value)
        {
            case "error": result = LogLevel.Error; return true;
            case "warn": result = LogLevel.Warn; return true;
            case "info": result = LogLevel.Info; return true;
            case "debug": result = LogLevel.Debug; return true;
            case "verbose": result = LogLevel.Verbose; return true;
            case "fatal": result = LogLevel.Fatal; return true;
        }
        result = LogLevel.Error;
        return false;
    }

    public static bool StringToMeasureMode(string value, out MeasureMode result)
    {
        switch (value)
        {
            case "undefined": result = MeasureMode.Undefined; return true;
            case "exactly": result = MeasureMode.Exactly; return true;
            case "at-most": result = MeasureMode.AtMost; return true;
        }
        result = MeasureMode.Undefined;
        return false;
    }

    public static bool StringToNodeType(string value, out NodeType result)
    {
        switch (value)
        {
            case "default": result = NodeType.Default; return true;
            case "text": result = NodeType.Text; return true;
        }
        result = NodeType.Default;
        return false;
    }

    public static bool StringToOverflow(string value, out Overflow result)
    {
        switch (value)
        {
            case "visible": result = Overflow.Visible; return true;
            case "hidden": result = Overflow.Hidden; return true;
            case "scroll": result = Overflow.Scroll; return true;
        }
        result = Overflow.Visible;
        return false;
    }

    public static bool StringToPositionType(string value, out PositionType result)
    {
        switch (value)
        {
            case "relative": result = PositionType.Relative; return true;
            case "absolute": result = PositionType.Absolute; return true;
        }
        result = PositionType.Relative;
        return false;
    }

    // PrintOptionsToString returns string version of PrintOptions enum
    public static bool StringToPrintOptions(string value, out PrintOptions result)
    {
        switch (value)
        {
            case "layout": result = PrintOptions.Layout; return true;
            case "style": result = PrintOptions.Style; return true;
            case "children": result = PrintOptions.Children; return true;
        }
        result = PrintOptions.Layout;
        return false;
    }

    // UnitToString returns string version of Unit enum
    public static bool StringToUnit(string value, out Unit result)
    {
        switch (value)
        {
            case "undefined": result = Unit.Undefined; return true;
            case "point": result = Unit.Point; return true;
            case "percent": result = Unit.Percent; return true;
            case "auto": result = Unit.Auto; return true;
        }
        result = Unit.Undefined;
        return false;
    }

    // WrapToString returns string version of Wrap enum
    public static bool StringToWrap(string value, out Wrap result)
    {
        switch (value)
        {
            case "nowrap": result = Wrap.NoWrap; return true;
            case "wrap": result = Wrap.Wrap; return true;
            case "wrap-reverse": result = Wrap.WrapReverse; return true;
        }
        result = Wrap.NoWrap;
        return false;
    }

    #endregion

    readonly static Dictionary<string, Value> valueDictionaryCache = [];

    public static bool ParseValueFromString(string text, out Value result)
    {
        bool parsed = true;
        if (text == "auto")
        {
            result = new Value(0, Unit.Auto);
            return true;
        }

        if (valueDictionaryCache.TryGetValue(text, out var cachedValue))
        {
            result = cachedValue;
            return true;
        }

        var res = Value.UndefinedValue;
        string dig = text;
        Unit uu = Unit.Point;

        if (text.EndsWith("%"))
        {
            dig = text.Substring(0, text.Length - 1);
            uu = Unit.Percent;
        }
        else if (text.EndsWith("px"))
        {
            dig = text.Substring(0, text.Length - 2);
            uu = Unit.Point;
        }

        if (float.TryParse(dig, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out res.value))
        {
            res.unit = uu;
        }
        else
        {
            parsed = false;
            res.unit = Unit.Undefined;
        }
        valueDictionaryCache.Add(text, res);
        result = res;
        return parsed;
    }
    /// <summary>
    /// margin="2px" | margin="12px 13px" | margin="12px 13px 1px" | margin="1px 2px 3px 4px"
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public static bool ParseFourValueFromString(string text, out Value[] result)
    {
        // Edge.Left = 0;
        // Edge.Top = 1;
        // Edge.Right = 2;
        // Edge.Bottom = 3;
        result = null;
        var vStr = text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (vStr.Length > 4)
            return false;

        Value[] res = new Value[vStr.Length];
        for (int i = 0; i < res.Length; i++)
            if (!ParseValueFromString(vStr[i], out res[i]))
                return false;

        /*
        When one value is specified, it applies the same margin to all four sides.
        When two values are specified, the first margin applies to the top and bottom, the second to the left and right.
        When three values are specified, the first margin applies to the top, the second to the left and right, the third to the bottom.
        When four values are specified, the margins apply to the top, right, bottom, and left in that order (clockwise).
        */
        switch (res.Length)
        {
            case 0:
                result = null;
                break;
            case 1:
                result = new Value[] { res[0], res[0], res[0], res[0], Value.UndefinedValue, Value.UndefinedValue, Value.UndefinedValue, Value.UndefinedValue, Value.UndefinedValue };
                break;
            case 2:
                result = new Value[] { res[1], res[0], res[1], res[0], Value.UndefinedValue, Value.UndefinedValue, Value.UndefinedValue, Value.UndefinedValue, Value.UndefinedValue };
                break;
            case 3:
                result = new Value[] { res[1], res[0], res[1], res[2], Value.UndefinedValue, Value.UndefinedValue, Value.UndefinedValue, Value.UndefinedValue, Value.UndefinedValue };
                break;
            case 4:
                result = new Value[] { res[3], res[0], res[1], res[2], Value.UndefinedValue, Value.UndefinedValue, Value.UndefinedValue, Value.UndefinedValue, Value.UndefinedValue };
                break;
            default:
                result = null;
                break;
        }
        return result != null;
    }

    static bool ParseBreakWork(string input, out string head, out string tail)
    {
        // margin --> ("margin", "")
        // margin-left --> ("margin", "left")
        // border-width --> ("border", "")
        // border-left-width -> ("border", "left")
        head = "";
        tail = "";
        var t = input.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
        switch (t.Length)
        {
            case 1:
                head = t[0];
                return true;
            case 2:
            case 3: // border-edge-width
                head = t[0];
                tail = t[1];
                if (head == "border" && tail == "width") tail = "";
                return true;
        }
        return false;
    }
    public static string RenderStyleAttrValue(Style style, string attrKey)
    {
        switch (attrKey)
        {
            case "direction":
                break;
            case "flex-direction":
                break;
            case "justify-content":
                break;
            case "align-content":
                break;
            case "align-items":
                break;
            case "align-self":
                break;
            case "flex-wrap":
                break;
            case "overflow":
                break;
            case "display":
                break;
            case "flex-grow":
                break;
            case "flex-shrink":
                break;
            case "flex-basis":
                break;
            case "position":
                break;
            case "width":
                break;
            case "height":
                break;
            case "min-width":
                break;
            case "min-height":
                break;
            case "max-width":
                break;
            case "max-height":
                break;
        }

        return "";
    }

    public static bool ParseStyleAttr(Style style, string attrKey, string attrValue)
    {
        bool parsed = true;
        switch (attrKey)
        {
            case "direction":
                parsed = Flex.StringToDirection(attrValue, out style.Direction);
                break;
            case "flex-direction":
                parsed = Flex.StringToFlexDirection(attrValue, out style.FlexDirection);
                break;
            case "justify-content":
                parsed = Flex.StringToJustify(attrValue, out style.JustifyContent);
                break;
            case "align-content":
                parsed = Flex.StringToAlign(attrValue, out style.AlignContent);
                break;
            case "align-items":
                parsed = Flex.StringToAlign(attrValue, out style.AlignItems);
                break;
            case "align-self":
                parsed = Flex.StringToAlign(attrValue, out style.AlignSelf);
                break;
            case "flex-wrap":
                parsed = Flex.StringToWrap(attrValue, out style.FlexWrap);
                break;
            case "overflow":
                parsed = Flex.StringToOverflow(attrValue, out style.Overflow);
                break;
            case "display":
                parsed = Flex.StringToDisplay(attrValue, out style.Display);
                break;
            case "flex-grow":
                parsed = float.TryParse(attrValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out style.FlexGrow);
                break;
            case "flex-shrink":
                parsed = float.TryParse(attrValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out style.FlexShrink);
                break;
            case "flex-basis":
                parsed = ParseValueFromString(attrValue, out style.FlexBasis);
                break;
            case "position":
                parsed = Flex.StringToPositionType(attrValue, out style.PositionType);
                break;
            case "left":
                parsed = ParseValueFromString(attrValue, out style.Position[(int)Edge.Left]);
                break;
            case "top":
                parsed = ParseValueFromString(attrValue, out style.Position[(int)Edge.Top]);
                break;
            case "right":
                parsed = ParseValueFromString(attrValue, out style.Position[(int)Edge.Right]);
                break;
            case "bottom":
                parsed = ParseValueFromString(attrValue, out style.Position[(int)Edge.Bottom]);
                break;
            case "width":
                parsed = ParseValueFromString(attrValue, out style.Dimensions[(int)Dimension.Width]);
                break;
            case "height":
                parsed = ParseValueFromString(attrValue, out style.Dimensions[(int)Dimension.Height]);
                break;
            case "min-width":
                parsed = ParseValueFromString(attrValue, out style.MinDimensions[(int)Dimension.Width]);
                break;
            case "min-height":
                parsed = ParseValueFromString(attrValue, out style.MinDimensions[(int)Dimension.Height]);
                break;
            case "max-width":
                parsed = ParseValueFromString(attrValue, out style.MaxDimensions[(int)Dimension.Width]);
                break;
            case "max-height":
                parsed = ParseValueFromString(attrValue, out style.MaxDimensions[(int)Dimension.Height]);
                break;
            case "margin":
            case "margin-left":
            case "margin-right":
            case "margin-top":
            case "margin-bottom":
            case "padding":
            case "padding-left":
            case "padding-right":
            case "padding-top":
            case "padding-bottom":
            case "border-width":
            case "border-left-width":
            case "border-right-width":
            case "border-top-width":
            case "border-bottom-width":
                // parse [margin|padding|border]-[Edgexxxx]
                if (ParseBreakWork(attrKey, out string head, out string tail))
                {
                    if (tail == "")
                    {
                        switch (head)
                        {
                            case "margin": parsed = ParseFourValueFromString(attrValue, out style.Margin); break;
                            case "padding": parsed = ParseFourValueFromString(attrValue, out style.Padding); break;
                            case "border": parsed = ParseFourValueFromString(attrValue, out style.Border); break;
                            default: parsed = false; break;
                        }
                    }
                    else if (Flex.StringToEdge(tail, out Edge edge))
                    {
                        switch (head)
                        {
                            case "margin": parsed = ParseValueFromString(attrValue, out style.Margin[(int)edge]); break;
                            case "padding": parsed = ParseValueFromString(attrValue, out style.Padding[(int)edge]); break;
                            case "border": parsed = ParseValueFromString(attrValue, out style.Border[(int)edge]); break;
                            default: parsed = false; break;
                        }
                    }
                    else
                        parsed = false;
                }
                else
                    parsed = false;
                break;
            default:
                parsed = false;
                break;
        }

        return parsed;
    }
}
