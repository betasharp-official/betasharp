namespace BetaSharp.Client.UI.Layout.Flexbox;

public partial class Flex
{
    //private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    // FloatsEqual returns true if floats are approx. equal
    public static bool FloatsEqual(float a, float b)
    {
        if (FloatIsUndefined(a))
        {
            return FloatIsUndefined(b);
        }
        return System.Math.Abs(a - b) < 0.0001f;
    }
    // Rockyfi.roundValueToPixelGrid rounds value to pixel grid
    public static float RoundValueToPixelGrid(float value, float pointScaleFactor, bool forceCeil, bool forceFloor)
    {
        float scaledValue = value * pointScaleFactor;
        float fractial = fmodf(scaledValue, 1f);
        if (FloatsEqual(fractial, 0))
        {
            // First we check if the value is already rounded
            scaledValue -= fractial;
        }
        else if (FloatsEqual(fractial, 1))
        {
            scaledValue = scaledValue - fractial + 1;
        }
        else if (forceCeil)
        {
            // Next we check if we need to use forced rounding
            scaledValue = scaledValue - fractial + 1;
        }
        else if (forceFloor)
        {
            scaledValue -= fractial;
        }
        else
        {
            // Finally we just round the value
            float f = 0;
            if (fractial >= 0.5f)
            {
                f = 1.0f;
            }
            scaledValue = scaledValue - fractial + f;
        }
        return scaledValue / pointScaleFactor;
    }

    // NodeCopyStyle copies style
    public static void NodeCopyStyle(Node dstNode, Node srcNode)
    {
        if (!styleEq(dstNode.nodeStyle, srcNode.nodeStyle))
        {
            Style.Copy(dstNode.nodeStyle, srcNode.nodeStyle);
            nodeMarkDirtyInternal(dstNode);
        }
    }


    // // Reset resets a node
    public static void Reset(ref Node node)
    {
        Flex.assertWithNode(node, node.Children.Count == 0, "Cannot reset a node which still has children attached");
        Flex.assertWithNode(node, node.Parent == null, "Cannot reset a node still attached to a parent");
        node.Children.Clear();

        var config = node.config;
        node = CreateDefaultNode();
        if (config.UseWebDefaults)
        {
            node.nodeStyle.FlexDirection = FlexDirection.Row;
            node.nodeStyle.AlignContent = Align.Stretch;
        }
        node.config = config;
    }

    public static Node CreateDefaultNode()
    {
        return new Node();
    }


    public static Node CreateDefaultNode(Style style)
    {
        return new Node(style);
    }

    public static Node CreateDefaultNode(Config config)
    {
        var node = new Node();
        if (config.UseWebDefaults)
        {
            node.nodeStyle.FlexDirection = FlexDirection.Row;
            node.nodeStyle.AlignContent = Align.Stretch;
        }
        node.config = config;
        return node;
    }

    public static Config CreateDefaultConfig()
    {
        return new Config();
    }

    // CalculateLayout calculates layout
    public static void CalculateLayout(Node node, float parentWidth, float parentHeight, Direction parentDirection)
    {
        // Increment the generation count. This will force the recursive routine to
        // visit
        // all dirty nodes at least once. Subsequent visits will be skipped if the
        // input
        // parameters don't change.
        currentGenerationCount++;

        resolveDimensions(node);

        calcStartWidth(node, parentWidth, out float width, out MeasureMode widthMeasureMode);
        calcStartHeight(node, parentWidth, parentHeight, out float height, out MeasureMode heightMeasureMode);

        if (layoutNodeInternal(node, width, height, parentDirection,
            widthMeasureMode, heightMeasureMode, parentWidth, parentHeight,
            true, "initial", node.config))
        {
            nodeSetPosition(node, node.nodeLayout.Direction, parentWidth, parentHeight, parentWidth);
            roundToPixelGrid(node, node.config.PointScaleFactor, 0, 0);

            if (gPrintTree)
            {
                // NodePrint(node, PrintOptionsLayout|PrintOptionsChildren|PrintOptionsStyle);
                System.Console.WriteLine("NodePrint(node, PrintOptionsLayout|PrintOptionsChildren|PrintOptionsStyle);");
            }
        }
    }


    readonly internal static Value ValueZero = new Value(0, Unit.Point);
    readonly internal static Value ValueUndefined = new Value(float.NaN, Unit.Undefined);

    readonly internal static Value ValueAuto = new Value(float.NaN, Unit.Auto);

    internal static bool feq(float a, float b)
    {
        if (float.IsNaN(a) && float.IsNaN(b))
            return true;

        return a == b;
    }

    internal static bool valueEq(Value v1, Value v2)
    {
        if (v1.unit != v2.unit)
            return false;
        return feq(v1.value, v2.value);
    }


    internal static Value computedEdgeValue(Value[] edges, Edge edge, Value defaultValue)
    {
        if (edges[(int)edge].unit != Unit.Undefined)
        {
            return edges[(int)edge];
        }

        bool isVertEdge = (edge == Edge.Top || edge == Edge.Bottom);
        if (isVertEdge && edges[(int)(Edge.Vertical)].unit != Unit.Undefined)
        {
            return edges[(int)Edge.Vertical];
        }

        bool isHorizEdge = (edge == Edge.Left || edge == Edge.Right || edge == Edge.Start || edge == Edge.End);
        if (isHorizEdge && edges[(int)Edge.Horizontal].unit != Unit.Undefined)
        {
            return edges[(int)Edge.Horizontal];
        }

        if (edges[(int)Edge.All].unit != Unit.Undefined)
        {
            return edges[(int)Edge.All];
        }

        if (edge == Edge.Start || edge == Edge.End)
        {
            return ValueUndefined;
        }

        return defaultValue;
    }

    internal static float resolveValue(Value value, float parentSize)
    {
        switch (value.unit)
        {
            case Unit.Undefined:
            case Unit.Auto:
                return float.NaN;
            case Unit.Point:
                return value.value;
            case Unit.Percent:
                return value.value * parentSize / 100f;
        }
        return float.NaN;
    }


    internal static float resolveValueMargin(Value value, float parentSize)
    {
        if (value.unit == Unit.Auto)
        {
            return 0;
        }

        return resolveValue(value, parentSize);
    }

    // // NewNodeWithConfig creates new node with config
    internal static Node NewNodeWithConfig(Config config)
    {
        var node = CreateDefaultNode();

        if (config.UseWebDefaults)
        {
            node.nodeStyle.FlexDirection = FlexDirection.Row;
            node.nodeStyle.AlignContent = Align.Stretch;
        }
        node.config = config;
        return node;
    }

    // // NewNode creates a new node
    internal static Node NewNode()
    {
        return NewNodeWithConfig(CreateDefaultConfig());
    }

    // internal static int Len(Node[] array)
    // {
    //     return array == null ? 0 : array.Length;
    // }


    // ConfigGetDefault returns default config, only for C#
    internal static Config ConfigGetDefault()
    {
        return CreateDefaultConfig();
    }

    // NewConfig creates new config
    internal static Config NewConfig()
    {
        return CreateDefaultConfig();
    }

    // ConfigCopy copies a config
    internal static void ConfigCopy(Config dest, Config src)
    {
        Config.Copy(dest, src);
    }

    internal static void nodeMarkDirtyInternal(Node node)
    {
        if (!node.IsDirty)
        {
            node.IsDirty = true;
            node.nodeLayout.computedFlexBasis = float.NaN;
            if (node.Parent != null)
            {
                nodeMarkDirtyInternal(node.Parent);
            }
        }
    }

    // SetMeasureFunc sets measure function
    internal static void SetMeasureFunc(Node node, MeasureFunc measureFunc)
    {
        if (measureFunc == null)
        {
            node.measureFunc = null;
            // TODO: t18095186 Move nodeType to opt-in function and mark appropriate places in Litho
            node.NodeType = NodeType.Default;
        }
        else
        {
            Flex.assertWithNode(
                node,
                node.Children.Count == 0,
                "Cannot set measure function: Nodes with measure functions cannot have children.");
            node.measureFunc = measureFunc;
            // TODO: t18095186 Move nodeType to opt-in function and mark appropriate places in Litho
            node.NodeType = NodeType.Text;
        }
    }

    // InsertChild inserts a child
    internal static void InsertChild(Node node, Node child, int idx)
    {
        Flex.assertWithNode(node, child.Parent == null, "Child already has a parent, it must be removed first.");
        Flex.assertWithNode(node, node.measureFunc == null, "Cannot add child: Nodes with measure functions cannot have children.");

        node.Children.Insert(idx, child);
        child.Parent = node;
        nodeMarkDirtyInternal(node);
    }

    // RemoveChild removes child node
    internal static void RemoveChild(Node node, Node child)
    {
        if (node.Children.Remove(child))
        {
            child.nodeLayout.ResetToDefault(); // layout is no longer valid
            child.Parent = null;
            nodeMarkDirtyInternal(node);
        }
    }

    // GetChild returns a child at a given index
    internal static Node GetChild(Node node, int idx)
    {
        return idx < node.Children.Count ? node.Children[idx] : null;
    }

    // MarkDirty marks node as dirty
    internal static void MarkDirty(Node node)
    {
        Flex.assertWithNode(node, node.measureFunc != null,
            "Only leaf nodes with custom measure functions should manually mark themselves as dirty");
        nodeMarkDirtyInternal(node);
    }

    internal static bool styleEq(Style s1, Style s2)
    {
        if (s1.Direction != s2.Direction ||
            s1.FlexDirection != s2.FlexDirection ||
            s1.JustifyContent != s2.JustifyContent ||
            s1.AlignContent != s2.AlignContent ||
            s1.AlignItems != s2.AlignItems ||
            s1.AlignSelf != s2.AlignSelf ||
            s1.PositionType != s2.PositionType ||
            s1.FlexWrap != s2.FlexWrap ||
            s1.Overflow != s2.Overflow ||
            s1.Display != s2.Display ||
            !feq(s1.FlexGrow, s2.FlexGrow) ||
            !feq(s1.FlexShrink, s2.FlexShrink) ||
            !valueEq(s1.FlexBasis, s2.FlexBasis))
        {
            return false;
        }
        for (int i = 0; i < Constant.EdgeCount; i++)
        {
            if (!valueEq(s1.Margin[i], s2.Margin[i]) ||
                !valueEq(s1.Position[i], s2.Position[i]) ||
                !valueEq(s1.Padding[i], s2.Padding[i]) ||
                !valueEq(s1.Border[i], s2.Border[i]))
            {
                return false;
            }
        }
        for (int i = 0; i < 2; i++)
        {
            if (!valueEq(s1.Dimensions[i], s2.Dimensions[i]) ||
                !valueEq(s1.MinDimensions[i], s2.MinDimensions[i]) ||
                !valueEq(s1.MaxDimensions[i], s2.MaxDimensions[i]))
            {
                return false;
            }
        }
        return true;
    }

    internal static float resolveFlexGrow(Node node)
    {
        // Root nodes flexGrow should always be 0
        if (node.Parent == null)
        {
            return 0;
        }
        if (!FloatIsUndefined(node.nodeStyle.FlexGrow))
        {
            return node.nodeStyle.FlexGrow;
        }
        return Constant.defaultFlexGrow;
    }

    internal static float nodeResolveFlexShrink(Node node)
    {
        // Root nodes flexShrink should always be 0
        if (node.Parent == null)
        {
            return 0;
        }
        if (!FloatIsUndefined(node.nodeStyle.FlexShrink))
        {
            return node.nodeStyle.FlexShrink;
        }
        if (node.config.UseWebDefaults)
        {
            return Constant.webDefaultFlexShrink;
        }
        return Constant.defaultFlexShrink;
    }

    internal static Value nodeResolveFlexBasisPtr(Node node)
    {
        var style = node.nodeStyle;
        if (style.FlexBasis.unit != Unit.Auto && style.FlexBasis.unit != Unit.Undefined)
        {
            return style.FlexBasis;
        }
        return ValueAuto;
    }

    // // see yoga_props.go

    // var (
    //     currentGenerationCount = 0
    // )
    internal static int currentGenerationCount = 0;

    // FloatIsUndefined returns true if value is undefined
    internal static bool FloatIsUndefined(float value)
    {
        return float.IsNaN(value);
    }

    // ValueEqual returns true if values are equal
    internal static bool ValueEqual(Value a, Value b)
    {
        if (a.unit != b.unit)
        {
            return false;
        }

        if (a.unit == Unit.Undefined)
        {
            return true;
        }

        return System.Math.Abs(a.value - b.value) < 0.0001f;
    }

    internal static void resolveDimensions(Node node)
    {
        for (int dim = (int)Dimension.Width; dim <= (int)Dimension.Height; dim++)
        {
            if (node.nodeStyle.MaxDimensions[dim].unit != Unit.Undefined &&
                ValueEqual(node.nodeStyle.MaxDimensions[dim], node.nodeStyle.MinDimensions[dim]))
            {
                node.resolvedDimensions[dim] = node.nodeStyle.MaxDimensions[dim];
            }
            else
            {
                node.resolvedDimensions[dim] = node.nodeStyle.Dimensions[dim];
            }
        }
    }

    // // see print.go

    // var (
    // )
    readonly internal static Edge[] leading = new Edge[4] { Edge.Top, Edge.Bottom, Edge.Left, Edge.Right };
    readonly internal static Edge[] trailing = new Edge[4] { Edge.Bottom, Edge.Top, Edge.Right, Edge.Left };
    readonly internal static Edge[] pos = new Edge[4] { Edge.Top, Edge.Bottom, Edge.Left, Edge.Right };
    readonly internal static Dimension[] dim = new Dimension[4] { Dimension.Height, Dimension.Height, Dimension.Width, Dimension.Width };

    internal static bool flexDirectionIsRow(FlexDirection flexDirection)
    {
        return flexDirection == FlexDirection.Row || flexDirection == FlexDirection.RowReverse;
    }

    internal static bool flexDirectionIsColumn(FlexDirection flexDirection)
    {
        return flexDirection == FlexDirection.Column || flexDirection == FlexDirection.ColumnReverse;
    }

    internal static float nodeLeadingMargin(Node node, FlexDirection axis, float widthSize)
    {
        if (flexDirectionIsRow(axis) && node.nodeStyle.Margin[(int)Edge.Start].unit != Unit.Undefined)
        {
            return resolveValueMargin(node.nodeStyle.Margin[(int)Edge.Start], widthSize);
        }

        var v = computedEdgeValue(node.nodeStyle.Margin, leading[(int)axis], ValueZero);
        return resolveValueMargin(v, widthSize);
    }

    internal static float nodeTrailingMargin(Node node, FlexDirection axis, float widthSize)
    {
        if (flexDirectionIsRow(axis) && node.nodeStyle.Margin[(int)Edge.End].unit != Unit.Undefined)
        {
            return resolveValueMargin(node.nodeStyle.Margin[(int)Edge.End], widthSize);
        }

        return resolveValueMargin(computedEdgeValue(node.nodeStyle.Margin, trailing[(int)axis], ValueZero),
            widthSize);
    }

    internal static float nodeLeadingPadding(Node node, FlexDirection axis, float widthSize)
    {
        if (flexDirectionIsRow(axis) && node.nodeStyle.Padding[(int)Edge.Start].unit != Unit.Undefined &&
            resolveValue(node.nodeStyle.Padding[(int)Edge.Start], widthSize) >= 0)
        {
            return resolveValue(node.nodeStyle.Padding[(int)Edge.Start], widthSize);
        }

        return fmaxf(resolveValue(computedEdgeValue(node.nodeStyle.Padding, leading[(int)axis], ValueZero), widthSize), 0);
    }

    internal static float nodeTrailingPadding(Node node, FlexDirection axis, float widthSize)
    {
        if (flexDirectionIsRow(axis) && node.nodeStyle.Padding[(int)Edge.End].unit != Unit.Undefined &&
            resolveValue(node.nodeStyle.Padding[(int)Edge.End], widthSize) >= 0)
        {
            return resolveValue(node.nodeStyle.Padding[(int)Edge.End], widthSize);
        }

        return fmaxf(resolveValue(computedEdgeValue(node.nodeStyle.Padding, trailing[(int)axis], ValueZero), widthSize), 0);
    }

    internal static float nodeLeadingBorder(Node node, FlexDirection axis)
    {
        if (flexDirectionIsRow(axis) && node.nodeStyle.Border[(int)Edge.Start].unit != Unit.Undefined &&
            node.nodeStyle.Border[(int)Edge.Start].value >= 0)
        {
            return node.nodeStyle.Border[(int)Edge.Start].value;
        }

        return fmaxf(computedEdgeValue(node.nodeStyle.Border, leading[(int)axis], ValueZero).value, 0);
    }

    internal static float nodeTrailingBorder(Node node, FlexDirection axis)
    {
        if (flexDirectionIsRow(axis) && node.nodeStyle.Border[(int)Edge.End].unit != Unit.Undefined &&
            node.nodeStyle.Border[(int)Edge.End].value >= 0)
        {
            return node.nodeStyle.Border[(int)Edge.End].value;
        }

        return fmaxf(computedEdgeValue(node.nodeStyle.Border, trailing[(int)axis], ValueZero).value, 0);
    }

    internal static float nodeLeadingPaddingAndBorder(Node node, FlexDirection axis, float widthSize)
    {
        return nodeLeadingPadding(node, axis, widthSize) + nodeLeadingBorder(node, axis);
    }

    internal static float nodeTrailingPaddingAndBorder(Node node, FlexDirection axis, float widthSize)
    {
        return nodeTrailingPadding(node, axis, widthSize) + nodeTrailingBorder(node, axis);
    }

    internal static float nodeMarginForAxis(Node node, FlexDirection axis, float widthSize)
    {
        float leading = nodeLeadingMargin(node, axis, widthSize);
        float trailing = nodeTrailingMargin(node, axis, widthSize);
        return leading + trailing;
    }

    internal static float nodePaddingAndBorderForAxis(Node node, FlexDirection axis, float widthSize)
    {
        return nodeLeadingPaddingAndBorder(node, axis, widthSize) +
            nodeTrailingPaddingAndBorder(node, axis, widthSize);
    }

    internal static Align nodeAlignItem(Node node, Node child)
    {
        var align = child.nodeStyle.AlignSelf;
        if (child.nodeStyle.AlignSelf == Align.Auto)
        {
            align = node.nodeStyle.AlignItems;
        }
        if (align == Align.Baseline && flexDirectionIsColumn(node.nodeStyle.FlexDirection))
        {
            return Align.FlexStart;
        }
        return align;
    }

    internal static Direction nodeResolveDirection(Node node, Direction parentDirection)
    {
        if (node.nodeStyle.Direction == Direction.Inherit)
        {
            if (parentDirection > Direction.Inherit)
            {
                return parentDirection;
            }
            return Direction.LTR;
        }
        return node.nodeStyle.Direction;
    }

    // Baseline retuns baseline
    internal static float Baseline(Node node)
    {
        if (node.baselineFunc != null)
        {
            float baseline = node.baselineFunc(node, node.nodeLayout.measuredDimensions[(int)Dimension.Width], node.nodeLayout.measuredDimensions[(int)Dimension.Height]);
            Flex.assertWithNode(node, !FloatIsUndefined(baseline), "Expect custom baseline function to not return NaN");
            return baseline;
        }
        else
        {
            Node baselineChild = null;
            foreach (var child in node.Children)
            {
                if (child.lineIndex > 0)
                {
                    break;
                }
                if (child.nodeStyle.PositionType == PositionType.Absolute)
                {
                    continue;
                }
                if (nodeAlignItem(node, child) == Align.Baseline)
                {
                    baselineChild = child;
                    break;
                }

                if (baselineChild == null)
                {
                    baselineChild = child;
                }
            }

            if (baselineChild == null)
            {
                return node.nodeLayout.measuredDimensions[(int)Dimension.Height];
            }

            float baseline = Baseline(baselineChild);
            return baseline + baselineChild.nodeLayout.Position[(int)Edge.Top];
        }

    }

    internal static FlexDirection resolveFlexDirection(FlexDirection flexDirection, Direction direction)
    {
        if (direction == Direction.RTL)
        {
            if (flexDirection == FlexDirection.Row)
            {
                return FlexDirection.RowReverse;
            }
            else if (flexDirection == FlexDirection.RowReverse)
            {
                return FlexDirection.Row;
            }
        }
        return flexDirection;
    }

    internal static FlexDirection flexDirectionCross(FlexDirection flexDirection, Direction direction)
    {
        if (flexDirectionIsColumn(flexDirection))
        {
            return resolveFlexDirection(FlexDirection.Row, direction);
        }
        return FlexDirection.Column;
    }

    internal static bool nodeIsFlex(Node node)
    {
        return (node.nodeStyle.PositionType == PositionType.Relative &&
            (resolveFlexGrow(node) != 0 || nodeResolveFlexShrink(node) != 0));
    }

    internal static bool isBaselineLayout(Node node)
    {
        if (flexDirectionIsColumn(node.nodeStyle.FlexDirection))
        {
            return false;
        }
        if (node.nodeStyle.AlignItems == Align.Baseline)
        {
            return true;
        }
        foreach (var child in node.Children)
        {
            if (child.nodeStyle.PositionType == PositionType.Relative &&
                child.nodeStyle.AlignSelf == Align.Baseline)
            {
                return true;
            }
        }

        return false;
    }

    internal static float nodeDimWithMargin(Node node, FlexDirection axis, float widthSize)
    {
        return node.nodeLayout.measuredDimensions[(int)dim[(int)axis]] + nodeLeadingMargin(node, axis, widthSize) +
            nodeTrailingMargin(node, axis, widthSize);
    }

    internal static bool nodeIsStyleDimDefined(Node node, FlexDirection axis, float parentSize)
    {
        var v = node.resolvedDimensions[(int)dim[(int)axis]];
        bool isNotDefined = (v.unit == Unit.Auto ||
            v.unit == Unit.Undefined ||
            (v.unit == Unit.Point && v.value < 0) ||
            (v.unit == Unit.Percent && (v.value < 0 || FloatIsUndefined(parentSize))));
        return !isNotDefined;
    }

    internal static bool nodeIsLayoutDimDefined(Node node, FlexDirection axis)
    {
        float value = node.nodeLayout.measuredDimensions[(int)dim[(int)axis]];
        return (!FloatIsUndefined(value) && value >= 0);
    }

    internal static bool nodeIsLeadingPosDefined(Node node, FlexDirection axis)
    {
        return (flexDirectionIsRow(axis) &&
            computedEdgeValue(node.nodeStyle.Position, Edge.Start, ValueUndefined).unit !=
                Unit.Undefined) ||
            computedEdgeValue(node.nodeStyle.Position, leading[(int)axis], ValueUndefined).unit !=
                Unit.Undefined;
    }

    internal static bool nodeIsTrailingPosDefined(Node node, FlexDirection axis)
    {
        return (flexDirectionIsRow(axis) &&
            computedEdgeValue(node.nodeStyle.Position, Edge.End, ValueUndefined).unit !=
                Unit.Undefined) ||
            computedEdgeValue(node.nodeStyle.Position, trailing[(int)axis], ValueUndefined).unit !=
                Unit.Undefined;
    }

    internal static float nodeLeadingPosition(Node node, FlexDirection axis, float axisSize)
    {
        if (flexDirectionIsRow(axis))
        {
            var leadingPosition = computedEdgeValue(node.nodeStyle.Position, Edge.Start, ValueUndefined);
            if (leadingPosition.unit != Unit.Undefined)
            {
                return resolveValue(leadingPosition, axisSize);
            }
        }

        {
            var leadingPosition = computedEdgeValue(node.nodeStyle.Position, leading[(int)axis], ValueUndefined);

            if (leadingPosition.unit == Unit.Undefined)
            {
                return 0;
            }
            return resolveValue(leadingPosition, axisSize);
        }
    }

    internal static float nodeTrailingPosition(Node node, FlexDirection axis, float axisSize)
    {
        if (flexDirectionIsRow(axis))
        {
            var trailingPosition = computedEdgeValue(node.nodeStyle.Position, Edge.End, ValueUndefined);
            if (trailingPosition.unit != Unit.Undefined)
            {
                return resolveValue(trailingPosition, axisSize);
            }
        }

        {
            var trailingPosition = computedEdgeValue(node.nodeStyle.Position, trailing[(int)axis], ValueUndefined);

            if (trailingPosition.unit == Unit.Undefined)
            {
                return 0;
            }
            return resolveValue(trailingPosition, axisSize);
        }
    }

    internal static float nodeBoundAxisWithinMinAndMax(Node node, FlexDirection axis, float value, float axisSize)
    {
        float min = float.NaN;
        float max = float.NaN;

        if (flexDirectionIsColumn(axis))
        {
            min = resolveValue(node.nodeStyle.MinDimensions[(int)Dimension.Height], axisSize);
            max = resolveValue(node.nodeStyle.MaxDimensions[(int)Dimension.Height], axisSize);
        }
        else if (flexDirectionIsRow(axis))
        {
            min = resolveValue(node.nodeStyle.MinDimensions[(int)Dimension.Width], axisSize);
            max = resolveValue(node.nodeStyle.MaxDimensions[(int)Dimension.Width], axisSize);
        }

        float boundValue = value;

        if (!FloatIsUndefined(max) && max >= 0 && boundValue > max)
        {
            boundValue = max;
        }

        if (!FloatIsUndefined(min) && min >= 0 && boundValue < min)
        {
            boundValue = min;
        }

        return boundValue;
    }

    internal static Value marginLeadingValue(Node node, FlexDirection axis)
    {
        if (flexDirectionIsRow(axis) && node.nodeStyle.Margin[(int)Edge.Start].unit != Unit.Undefined)
        {
            return node.nodeStyle.Margin[(int)Edge.Start];
        }
        return node.nodeStyle.Margin[(int)leading[(int)axis]];
    }

    internal static Value marginTrailingValue(Node node, FlexDirection axis)
    {
        if (flexDirectionIsRow(axis) && node.nodeStyle.Margin[(int)Edge.End].unit != Unit.Undefined)
        {
            return node.nodeStyle.Margin[(int)Edge.End];
        }
        return node.nodeStyle.Margin[(int)trailing[(int)axis]];
    }

    // nodeBoundAxis is like nodeBoundAxisWithinMinAndMax but also ensures that
    // the value doesn't go below the padding and border amount.
    internal static float nodeBoundAxis(Node node, FlexDirection axis, float value, float axisSize, float widthSize)
    {
        return fmaxf(nodeBoundAxisWithinMinAndMax(node, axis, value, axisSize),
            nodePaddingAndBorderForAxis(node, axis, widthSize));
    }

    internal static void nodeSetChildTrailingPosition(Node node, Node child, FlexDirection axis)
    {
        float size = child.nodeLayout.measuredDimensions[(int)dim[(int)axis]];
        child.nodeLayout.Position[(int)trailing[(int)axis]] =
            node.nodeLayout.measuredDimensions[(int)dim[(int)axis]] - size - child.nodeLayout.Position[(int)pos[(int)axis]];
    }

    // If both left and right are defined, then use left. Otherwise return
    // +left or -right depending on which is defined.
    internal static float nodeRelativePosition(Node node, FlexDirection axis, float axisSize)
    {
        if (nodeIsLeadingPosDefined(node, axis))
        {
            return nodeLeadingPosition(node, axis, axisSize);
        }
        return -nodeTrailingPosition(node, axis, axisSize);
    }

    internal static void constrainMaxSizeForMode(Node node, FlexDirection axis, float parentAxisSize, float parentWidth, ref MeasureMode mode, ref float size)
    {
        float maxSize = resolveValue(node.nodeStyle.MaxDimensions[(int)dim[(int)axis]], parentAxisSize) +
            nodeMarginForAxis(node, axis, parentWidth);
        switch (mode)
        {
            case MeasureMode.Exactly:
            case MeasureMode.AtMost:
                if (FloatIsUndefined(maxSize) || size < maxSize)
                {
                    // TODO: this is redundant, but what is in original code
                    //*size = *size
                }
                else
                {
                    size = maxSize;
                }
                break;
            case MeasureMode.Undefined:
                if (!FloatIsUndefined(maxSize))
                {
                    mode = MeasureMode.AtMost;
                    size = maxSize;
                }
                break;
        }
    }

    internal static void nodeSetPosition(Node node, Direction direction, float mainSize, float crossSize, float parentWidth)
    {
        /* Root nodes should be always layouted as LTR, so we don't return negative values. */
        var directionRespectingRoot = Direction.LTR;
        if (node.Parent != null)
        {
            directionRespectingRoot = direction;
        }

        var mainAxis = resolveFlexDirection(node.nodeStyle.FlexDirection, directionRespectingRoot);
        var crossAxis = flexDirectionCross(mainAxis, directionRespectingRoot);

        float relativePositionMain = nodeRelativePosition(node, mainAxis, mainSize);
        float relativePositionCross = nodeRelativePosition(node, crossAxis, crossSize);

        float[] pos = node.nodeLayout.Position;
        pos[(int)leading[(int)mainAxis]] = nodeLeadingMargin(node, mainAxis, parentWidth) + relativePositionMain;
        pos[(int)trailing[(int)mainAxis]] = nodeTrailingMargin(node, mainAxis, parentWidth) + relativePositionMain;
        pos[(int)leading[(int)crossAxis]] = nodeLeadingMargin(node, crossAxis, parentWidth) + relativePositionCross;
        pos[(int)trailing[(int)crossAxis]] = nodeTrailingMargin(node, crossAxis, parentWidth) + relativePositionCross;
    }

    internal static void nodeComputeFlexBasisForChild(Node node,
        Node child,
        float width,
        MeasureMode widthMode,
        float height,
        float parentWidth,
        float parentHeight,
        MeasureMode heightMode,
        Direction direction,
        Config config)
    {
        var mainAxis = resolveFlexDirection(node.nodeStyle.FlexDirection, direction);
        bool isMainAxisRow = flexDirectionIsRow(mainAxis);
        float mainAxisSize = height;
        float mainAxisParentSize = parentHeight;
        if (isMainAxisRow)
        {
            mainAxisSize = width;
            mainAxisParentSize = parentWidth;
        }

        float childWidth;
        float childHeight;
        MeasureMode childWidthMeasureMode;
        MeasureMode childHeightMeasureMode;

        float resolvedFlexBasis = resolveValue(nodeResolveFlexBasisPtr(child), mainAxisParentSize);
        bool isRowStyleDimDefined = nodeIsStyleDimDefined(child, FlexDirection.Row, parentWidth);
        bool isColumnStyleDimDefined = nodeIsStyleDimDefined(child, FlexDirection.Column, parentHeight);

        if (!FloatIsUndefined(resolvedFlexBasis) && !FloatIsUndefined(mainAxisSize))
        {
            if (FloatIsUndefined(child.nodeLayout.computedFlexBasis) ||
                (child.config.IsExperimentalFeatureEnabled(ExperimentalFeature.WebFlexBasis) &&
                    child.nodeLayout.computedFlexBasisGeneration != currentGenerationCount))
            {
                child.nodeLayout.computedFlexBasis =
                    fmaxf(resolvedFlexBasis, nodePaddingAndBorderForAxis(child, mainAxis, parentWidth));
            }
        }
        else if (isMainAxisRow && isRowStyleDimDefined)
        {
            // The width is definite, so use that as the flex basis.
            child.nodeLayout.computedFlexBasis =
                fmaxf(resolveValue(child.resolvedDimensions[(int)Dimension.Width], parentWidth),
                    nodePaddingAndBorderForAxis(child, FlexDirection.Row, parentWidth));
        }
        else if (!isMainAxisRow && isColumnStyleDimDefined)
        {
            // The height is definite, so use that as the flex basis.
            child.nodeLayout.computedFlexBasis =
                fmaxf(resolveValue(child.resolvedDimensions[(int)Dimension.Height], parentHeight),
                    nodePaddingAndBorderForAxis(child, FlexDirection.Column, parentWidth));
        }
        else
        {
            // Compute the flex basis and hypothetical main size (i.e. the clamped
            // flex basis).
            childWidth = float.NaN;
            childHeight = float.NaN;
            childWidthMeasureMode = MeasureMode.Undefined;
            childHeightMeasureMode = MeasureMode.Undefined;

            float marginRow = nodeMarginForAxis(child, FlexDirection.Row, parentWidth);
            float marginColumn = nodeMarginForAxis(child, FlexDirection.Column, parentWidth);

            if (isRowStyleDimDefined)
            {
                childWidth =
                    resolveValue(child.resolvedDimensions[(int)Dimension.Width], parentWidth) + marginRow;
                childWidthMeasureMode = MeasureMode.Exactly;
            }
            if (isColumnStyleDimDefined)
            {
                childHeight =
                    resolveValue(child.resolvedDimensions[(int)Dimension.Height], parentHeight) + marginColumn;
                childHeightMeasureMode = MeasureMode.Exactly;
            }

            // The W3C spec doesn't say anything about the 'overflow' property,
            // but all major browsers appear to implement the following logic.
            if ((!isMainAxisRow && node.nodeStyle.Overflow == Overflow.Scroll) ||
                node.nodeStyle.Overflow != Overflow.Scroll)
            {
                if (FloatIsUndefined(childWidth) && !FloatIsUndefined(width))
                {
                    childWidth = width;
                    childWidthMeasureMode = MeasureMode.AtMost;
                }
            }

            if ((isMainAxisRow && node.nodeStyle.Overflow == Overflow.Scroll) ||
                node.nodeStyle.Overflow != Overflow.Scroll)
            {
                if (FloatIsUndefined(childHeight) && !FloatIsUndefined(height))
                {
                    childHeight = height;
                    childHeightMeasureMode = MeasureMode.AtMost;
                }
            }

            // If child has no defined size in the cross axis and is set to stretch,
            // set the cross
            // axis to be measured exactly with the available inner width
            if (!isMainAxisRow && !FloatIsUndefined(width) && !isRowStyleDimDefined &&
                widthMode == MeasureMode.Exactly && nodeAlignItem(node, child) == Align.Stretch)
            {
                childWidth = width;
                childWidthMeasureMode = MeasureMode.Exactly;
            }
            if (isMainAxisRow && !FloatIsUndefined(height) && !isColumnStyleDimDefined &&
                heightMode == MeasureMode.Exactly && nodeAlignItem(node, child) == Align.Stretch)
            {
                childHeight = height;
                childHeightMeasureMode = MeasureMode.Exactly;
            }

            if (!FloatIsUndefined(child.nodeStyle.AspectRatio))
            {
                if (!isMainAxisRow && childWidthMeasureMode == MeasureMode.Exactly)
                {
                    child.nodeLayout.computedFlexBasis =
                        fmaxf((childWidth - marginRow) / child.nodeStyle.AspectRatio,
                            nodePaddingAndBorderForAxis(child, FlexDirection.Column, parentWidth));
                    return;
                }
                else if (isMainAxisRow && childHeightMeasureMode == MeasureMode.Exactly)
                {
                    child.nodeLayout.computedFlexBasis =
                        fmaxf((childHeight - marginColumn) * child.nodeStyle.AspectRatio,
                            nodePaddingAndBorderForAxis(child, FlexDirection.Row, parentWidth));
                    return;
                }
            }

            constrainMaxSizeForMode(
                child, FlexDirection.Row, parentWidth, parentWidth, ref childWidthMeasureMode, ref childWidth);
            constrainMaxSizeForMode(child,
                FlexDirection.Column,
                parentHeight,
                parentWidth,
                ref childHeightMeasureMode,
                ref childHeight);

            // Measure the child
            layoutNodeInternal(child,
                childWidth,
                childHeight,
                direction,
                childWidthMeasureMode,
                childHeightMeasureMode,
                parentWidth,
                parentHeight,
                false,
                "measure",
                config);

            child.nodeLayout.computedFlexBasis =
                fmaxf(child.nodeLayout.measuredDimensions[(int)dim[(int)mainAxis]],
                    nodePaddingAndBorderForAxis(child, mainAxis, parentWidth));
        }

        child.nodeLayout.computedFlexBasisGeneration = currentGenerationCount;
    }

    internal static void nodeAbsoluteLayoutChild(Node node, Node child, float width, MeasureMode widthMode, float height, Direction direction, Config config)
    {
        var mainAxis = resolveFlexDirection(node.nodeStyle.FlexDirection, direction);
        var crossAxis = flexDirectionCross(mainAxis, direction);
        bool isMainAxisRow = flexDirectionIsRow(mainAxis);

        float childWidth = float.NaN;
        float childHeight = float.NaN;
        var childWidthMeasureMode = MeasureMode.Undefined;
        var childHeightMeasureMode = MeasureMode.Undefined;

        float marginRow = nodeMarginForAxis(child, FlexDirection.Row, width);
        float marginColumn = nodeMarginForAxis(child, FlexDirection.Column, width);

        if (nodeIsStyleDimDefined(child, FlexDirection.Row, width))
        {
            childWidth = resolveValue(child.resolvedDimensions[(int)Dimension.Width], width) + marginRow;
        }
        else
        {
            // If the child doesn't have a specified width, compute the width based
            // on the left/right
            // offsets if they're defined.
            if (nodeIsLeadingPosDefined(child, FlexDirection.Row) &&
                nodeIsTrailingPosDefined(child, FlexDirection.Row))
            {
                childWidth = node.nodeLayout.measuredDimensions[(int)Dimension.Width] -
                    (nodeLeadingBorder(node, FlexDirection.Row) +
                        nodeTrailingBorder(node, FlexDirection.Row)) -
                    (nodeLeadingPosition(child, FlexDirection.Row, width) +
                        nodeTrailingPosition(child, FlexDirection.Row, width));
                childWidth = nodeBoundAxis(child, FlexDirection.Row, childWidth, width, width);
            }
        }

        if (nodeIsStyleDimDefined(child, FlexDirection.Column, height))
        {
            childHeight =
                resolveValue(child.resolvedDimensions[(int)Dimension.Height], height) + marginColumn;
        }
        else
        {
            // If the child doesn't have a specified height, compute the height
            // based on the top/bottom
            // offsets if they're defined.
            if (nodeIsLeadingPosDefined(child, FlexDirection.Column) &&
                nodeIsTrailingPosDefined(child, FlexDirection.Column))
            {
                childHeight = node.nodeLayout.measuredDimensions[(int)Dimension.Height] -
                    (nodeLeadingBorder(node, FlexDirection.Column) +
                        nodeTrailingBorder(node, FlexDirection.Column)) -
                    (nodeLeadingPosition(child, FlexDirection.Column, height) +
                        nodeTrailingPosition(child, FlexDirection.Column, height));
                childHeight = nodeBoundAxis(child, FlexDirection.Column, childHeight, height, width);
            }
        }

        // Exactly one dimension needs to be defined for us to be able to do aspect ratio
        // calculation. One dimension being the anchor and the other being flexible.
        if (FloatIsUndefined(childWidth) != FloatIsUndefined(childHeight))
        {
            if (!FloatIsUndefined(child.nodeStyle.AspectRatio))
            {
                if (FloatIsUndefined(childWidth))
                {
                    childWidth =
                        marginRow + fmaxf((childHeight - marginColumn) * child.nodeStyle.AspectRatio,
                            nodePaddingAndBorderForAxis(child, FlexDirection.Column, width));
                }
                else if (FloatIsUndefined(childHeight))
                {
                    childHeight =
                        marginColumn + fmaxf((childWidth - marginRow) / child.nodeStyle.AspectRatio,
                            nodePaddingAndBorderForAxis(child, FlexDirection.Row, width));
                }
            }
        }

        // If we're still missing one or the other dimension, measure the content.
        if (FloatIsUndefined(childWidth) || FloatIsUndefined(childHeight))
        {
            childWidthMeasureMode = MeasureMode.Exactly;
            if (FloatIsUndefined(childWidth))
            {
                childWidthMeasureMode = MeasureMode.Undefined;
            }
            childHeightMeasureMode = MeasureMode.Exactly;
            if (FloatIsUndefined(childHeight))
            {
                childHeightMeasureMode = MeasureMode.Undefined;
            }

            // If the size of the parent is defined then try to rain the absolute child to that size
            // as well. This allows text within the absolute child to wrap to the size of its parent.
            // This is the same behavior as many browsers implement.
            if (!isMainAxisRow && FloatIsUndefined(childWidth) && widthMode != MeasureMode.Undefined &&
                width > 0)
            {
                childWidth = width;
                childWidthMeasureMode = MeasureMode.AtMost;
            }

            layoutNodeInternal(child,
                childWidth,
                childHeight,
                direction,
                childWidthMeasureMode,
                childHeightMeasureMode,
                childWidth,
                childHeight,
                false,
                "abs-measure",
                config);
            childWidth = child.nodeLayout.measuredDimensions[(int)Dimension.Width] +
                nodeMarginForAxis(child, FlexDirection.Row, width);
            childHeight = child.nodeLayout.measuredDimensions[(int)Dimension.Height] +
                nodeMarginForAxis(child, FlexDirection.Column, width);
        }

        layoutNodeInternal(child,
            childWidth,
            childHeight,
            direction,
            MeasureMode.Exactly,
            MeasureMode.Exactly,
            childWidth,
            childHeight,
            true,
            "abs-layout",
            config);

        if (nodeIsTrailingPosDefined(child, mainAxis) && !nodeIsLeadingPosDefined(child, mainAxis))
        {
            float axisSize = height;
            if (isMainAxisRow)
            {
                axisSize = width;
            }
            child.nodeLayout.Position[(int)leading[(int)mainAxis]] = node.nodeLayout.measuredDimensions[(int)dim[(int)mainAxis]] -
                child.nodeLayout.measuredDimensions[(int)dim[(int)mainAxis]] -
                nodeTrailingBorder(node, mainAxis) -
                nodeTrailingMargin(child, mainAxis, width) -
                nodeTrailingPosition(child, mainAxis, axisSize);
        }
        else if (!nodeIsLeadingPosDefined(child, mainAxis) &&
          node.nodeStyle.JustifyContent == Justify.Center)
        {
            child.nodeLayout.Position[(int)leading[(int)mainAxis]] = (node.nodeLayout.measuredDimensions[(int)dim[(int)mainAxis]] -
                child.nodeLayout.measuredDimensions[(int)dim[(int)mainAxis]]) /
                2.0f;
        }
        else if (!nodeIsLeadingPosDefined(child, mainAxis) &&
          node.nodeStyle.JustifyContent == Justify.FlexEnd)
        {
            child.nodeLayout.Position[(int)leading[(int)mainAxis]] = (node.nodeLayout.measuredDimensions[(int)dim[(int)mainAxis]] -
                child.nodeLayout.measuredDimensions[(int)dim[(int)mainAxis]]);
        }

        if (nodeIsTrailingPosDefined(child, crossAxis) &&
            !nodeIsLeadingPosDefined(child, crossAxis))
        {
            float axisSize = width;
            if (isMainAxisRow)
            {
                axisSize = height;
            }

            child.nodeLayout.Position[(int)leading[(int)crossAxis]] = node.nodeLayout.measuredDimensions[(int)dim[(int)crossAxis]] -
                child.nodeLayout.measuredDimensions[(int)dim[(int)crossAxis]] -
                nodeTrailingBorder(node, crossAxis) -
                nodeTrailingMargin(child, crossAxis, width) -
                nodeTrailingPosition(child, crossAxis, axisSize);
        }
        else if (!nodeIsLeadingPosDefined(child, crossAxis) &&
          nodeAlignItem(node, child) == Align.Center)
        {
            child.nodeLayout.Position[(int)leading[(int)crossAxis]] =
                (node.nodeLayout.measuredDimensions[(int)dim[(int)crossAxis]] -
                    child.nodeLayout.measuredDimensions[(int)dim[(int)crossAxis]]) /
                    2.0f;
        }
        else if (!nodeIsLeadingPosDefined(child, crossAxis) &&
          ((nodeAlignItem(node, child) == Align.FlexEnd) != (node.nodeStyle.FlexWrap == Wrap.WrapReverse)))
        {
            child.nodeLayout.Position[(int)leading[(int)crossAxis]] = (node.nodeLayout.measuredDimensions[(int)dim[(int)crossAxis]] -
                child.nodeLayout.measuredDimensions[(int)dim[(int)crossAxis]]);
        }
    }

    // nodeWithMeasureFuncSetMeasuredDimensions sets measure dimensions for node with measure func
    internal static void nodeWithMeasureFuncSetMeasuredDimensions(Node node, float availableWidth, float availableHeight, MeasureMode widthMeasureMode, MeasureMode heightMeasureMode, float parentWidth, float parentHeight)
    {
        Flex.assertWithNode(node, node.measureFunc != null, "Expected node to have custom measure function");

        float paddingAndBorderAxisRow = nodePaddingAndBorderForAxis(node, FlexDirection.Row, availableWidth);
        float paddingAndBorderAxisColumn = nodePaddingAndBorderForAxis(node, FlexDirection.Column, availableWidth);
        float marginAxisRow = nodeMarginForAxis(node, FlexDirection.Row, availableWidth);
        float marginAxisColumn = nodeMarginForAxis(node, FlexDirection.Column, availableWidth);

        // We want to make sure we don't call measure with negative size
        float innerWidth = fmaxf(0, availableWidth - marginAxisRow - paddingAndBorderAxisRow);
        if (FloatIsUndefined(availableWidth))
        {
            innerWidth = availableWidth;
        }
        float innerHeight = fmaxf(0, availableHeight - marginAxisColumn - paddingAndBorderAxisColumn);
        if (FloatIsUndefined(availableHeight))
        {
            innerHeight = availableHeight;
        }

        if (widthMeasureMode == MeasureMode.Exactly && heightMeasureMode == MeasureMode.Exactly)
        {
            // Don't bother sizing the text if both dimensions are already defined.
            node.nodeLayout.measuredDimensions[(int)Dimension.Width] = nodeBoundAxis(
                node, FlexDirection.Row, availableWidth - marginAxisRow, parentWidth, parentWidth);
            node.nodeLayout.measuredDimensions[(int)Dimension.Height] = nodeBoundAxis(
                node, FlexDirection.Column, availableHeight - marginAxisColumn, parentHeight, parentWidth);
        }
        else
        {
            // Measure the text under the current raints.
            var measuredSize = node.measureFunc(node, innerWidth, widthMeasureMode, innerHeight, heightMeasureMode);

            float width = availableWidth - marginAxisRow;
            if (widthMeasureMode == MeasureMode.Undefined ||
                widthMeasureMode == MeasureMode.AtMost)
            {
                width = measuredSize.Width + paddingAndBorderAxisRow;

            }

            node.nodeLayout.measuredDimensions[(int)Dimension.Width] = nodeBoundAxis(node, FlexDirection.Row, width, availableWidth, availableWidth);

            float height = availableHeight - marginAxisColumn;
            if (heightMeasureMode == MeasureMode.Undefined ||
                heightMeasureMode == MeasureMode.AtMost)
            {
                height = measuredSize.Height + paddingAndBorderAxisColumn;
            }

            node.nodeLayout.measuredDimensions[(int)Dimension.Height] = nodeBoundAxis(node, FlexDirection.Column, height, availableHeight, availableWidth);
        }
    }

    // nodeEmptyContainerSetMeasuredDimensions sets measure dimensions for empty container
    // For nodes with no children, use the available values if they were provided,
    // or the minimum size as indicated by the padding and border sizes.
    internal static void nodeEmptyContainerSetMeasuredDimensions(Node node, float availableWidth, float availableHeight, MeasureMode widthMeasureMode, MeasureMode heightMeasureMode, float parentWidth, float parentHeight)
    {
        float paddingAndBorderAxisRow = nodePaddingAndBorderForAxis(node, FlexDirection.Row, parentWidth);
        float paddingAndBorderAxisColumn = nodePaddingAndBorderForAxis(node, FlexDirection.Column, parentWidth);
        float marginAxisRow = nodeMarginForAxis(node, FlexDirection.Row, parentWidth);
        float marginAxisColumn = nodeMarginForAxis(node, FlexDirection.Column, parentWidth);

        float width = availableWidth - marginAxisRow;
        if (widthMeasureMode == MeasureMode.Undefined || widthMeasureMode == MeasureMode.AtMost)
        {
            width = paddingAndBorderAxisRow;
        }
        node.nodeLayout.measuredDimensions[(int)Dimension.Width] = nodeBoundAxis(node, FlexDirection.Row, width, parentWidth, parentWidth);

        float height = availableHeight - marginAxisColumn;
        if (heightMeasureMode == MeasureMode.Undefined || heightMeasureMode == MeasureMode.AtMost)
        {
            height = paddingAndBorderAxisColumn;
        }
        node.nodeLayout.measuredDimensions[(int)Dimension.Height] = nodeBoundAxis(node, FlexDirection.Column, height, parentHeight, parentWidth);
    }

    internal static bool nodeFixedSizeSetMeasuredDimensions(Node node,
        float availableWidth,
        float availableHeight,
        MeasureMode widthMeasureMode,
        MeasureMode heightMeasureMode,
        float parentWidth,
        float parentHeight)
    {
        if ((widthMeasureMode == MeasureMode.AtMost && availableWidth <= 0) ||
            (heightMeasureMode == MeasureMode.AtMost && availableHeight <= 0) ||
            (widthMeasureMode == MeasureMode.Exactly && heightMeasureMode == MeasureMode.Exactly))
        {
            float marginAxisColumn = nodeMarginForAxis(node, FlexDirection.Column, parentWidth);
            float marginAxisRow = nodeMarginForAxis(node, FlexDirection.Row, parentWidth);

            float width = availableWidth - marginAxisRow;
            if (FloatIsUndefined(availableWidth) || (widthMeasureMode == MeasureMode.AtMost && availableWidth < 0))
            {
                width = 0;
            }
            node.nodeLayout.measuredDimensions[(int)Dimension.Width] =
                nodeBoundAxis(node, FlexDirection.Row, width, parentWidth, parentWidth);

            float height = availableHeight - marginAxisColumn;
            if (FloatIsUndefined(availableHeight) || (heightMeasureMode == MeasureMode.AtMost && availableHeight < 0))
            {
                height = 0;
            }
            node.nodeLayout.measuredDimensions[(int)Dimension.Height] =
                nodeBoundAxis(node, FlexDirection.Column, height, parentHeight, parentWidth);

            return true;
        }

        return false;
    }

    // zeroOutLayoutRecursivly zeros out layout recursively
    internal static void zeroOutLayoutRecursivly(Node node)
    {
        node.nodeLayout.Dimensions[(int)Dimension.Height] = 0;
        node.nodeLayout.Dimensions[(int)Dimension.Width] = 0;
        node.nodeLayout.Position[(int)Edge.Top] = 0;
        node.nodeLayout.Position[(int)Edge.Bottom] = 0;
        node.nodeLayout.Position[(int)Edge.Left] = 0;
        node.nodeLayout.Position[(int)Edge.Right] = 0;
        node.nodeLayout.cachedLayout.availableHeight = 0;
        node.nodeLayout.cachedLayout.availableWidth = 0;
        node.nodeLayout.cachedLayout.heightMeasureMode = MeasureMode.Exactly;
        node.nodeLayout.cachedLayout.widthMeasureMode = MeasureMode.Exactly;
        node.nodeLayout.cachedLayout.computedWidth = 0;
        node.nodeLayout.cachedLayout.computedHeight = 0;
        node.hasNewLayout = true;
        foreach (var child in node.Children)
        {
            zeroOutLayoutRecursivly(child);
        }
    }

    // This is the main routine that implements a subset of the flexbox layout
    // algorithm
    // described in the W3C YG documentation: https://www.w3.org/TR/YG3-flexbox/.
    //
    // Limitations of this algorithm, compared to the full standard:
    //  * Display property is always assumed to be 'flex' except for Text nodes,
    //  which
    //    are assumed to be 'inline-flex'.
    //  * The 'zIndex' property (or any form of z ordering) is not supported. Nodes
    //  are
    //    stacked in document order.
    //  * The 'order' property is not supported. The order of flex items is always
    //  defined
    //    by document order.
    //  * The 'visibility' property is always assumed to be 'visible'. Values of
    //  'collapse'
    //    and 'hidden' are not supported.
    //  * There is no support for forced breaks.
    //  * It does not support vertical inline directions (top-to-bottom or
    //  bottom-to-top text).
    //
    // Deviations from standard:
    //  * Section 4.5 of the spec indicates that all flex items have a default
    //  minimum
    //    main size. For text blocks, for example, this is the width of the widest
    //    word.
    //    Calculating the minimum width is expensive, so we forego it and assume a
    //    default
    //    minimum main size of 0.
    //  * Min/Max sizes in the main axis are not honored when resolving flexible
    //  lengths.
    //  * The spec indicates that the default value for 'flexDirection' is 'row',
    //  but
    //    the algorithm below assumes a default of 'column'.
    //
    // Input parameters:
    //    - node: current node to be sized and layed out
    //    - availableWidth & availableHeight: available size to be used for sizing
    //    the node
    //      or Undefined if the size is not available; interpretation depends on
    //      layout
    //      flags
    //    - parentDirection: the inline (text) direction within the parent
    //    (left-to-right or
    //      right-to-left)
    //    - widthMeasureMode: indicates the sizing rules for the width (see below
    //    for explanation)
    //    - heightMeasureMode: indicates the sizing rules for the height (see below
    //    for explanation)
    //    - performLayout: specifies whether the caller is interested in just the
    //    dimensions
    //      of the node or it requires the entire node and its subtree to be layed
    //      out
    //      (with final positions)
    //
    // Details:
    //    This routine is called recursively to lay out subtrees of flexbox
    //    elements. It uses the
    //    information in node.style, which is treated as a read-only input. It is
    //    responsible for
    //    setting the layout.direction and layout.measuredDimensions fields for the
    //    input node as well
    //    as the layout.position and layout.lineIndex fields for its child nodes.
    //    The
    //    layout.measuredDimensions field includes any border or padding for the
    //    node but does
    //    not include margins.
    //
    //    The spec describes four different layout modes: "fill available", "max
    //    content", "min
    //    content",
    //    and "fit content". Of these, we don't use "min content" because we don't
    //    support default
    //    minimum main sizes (see above for details). Each of our measure modes maps
    //    to a layout mode
    //    from the spec (https://www.w3.org/TR/YG3-sizing/#terms):
    //      - YGMeasureModeUndefined: max content
    //      - YGMeasureModeExactly: fill available
    //      - YGMeasureModeAtMost: fit content
    //
    //    When calling nodelayoutImpl and YGLayoutNodeInternal, if the caller passes
    //    an available size of
    //    undefined then it must also pass a measure mode of YGMeasureModeUndefined
    //    in that dimension.
    internal static void nodelayoutImpl(Node node, float availableWidth, float availableHeight,
        Direction parentDirection, MeasureMode widthMeasureMode,
        MeasureMode heightMeasureMode, float parentWidth, float parentHeight,
        bool performLayout, Config config)
    {
        // Rockyfi.assertWithNode(node, YGFloatIsUndefined(availableWidth) ? widthMeasureMode == YGMeasureModeUndefined : true, "availableWidth is indefinite so widthMeasureMode must be YGMeasureModeUndefined");
        //Rockyfi.assertWithNode(node, YGFloatIsUndefined(availableHeight) ? heightMeasureMode == YGMeasureModeUndefined : true, "availableHeight is indefinite so heightMeasureMode must be YGMeasureModeUndefined");

        // Set the resolved resolution in the node's layout.
        var direction = nodeResolveDirection(node, parentDirection);
        node.nodeLayout.Direction = direction;

        var flexRowDirection = resolveFlexDirection(FlexDirection.Row, direction);
        var flexColumnDirection = resolveFlexDirection(FlexDirection.Column, direction);

        node.nodeLayout.Margin[(int)Edge.Start] = nodeLeadingMargin(node, flexRowDirection, parentWidth);
        node.nodeLayout.Margin[(int)Edge.End] = nodeTrailingMargin(node, flexRowDirection, parentWidth);
        node.nodeLayout.Margin[(int)Edge.Top] = nodeLeadingMargin(node, flexColumnDirection, parentWidth);
        node.nodeLayout.Margin[(int)Edge.Bottom] = nodeTrailingMargin(node, flexColumnDirection, parentWidth);

        node.nodeLayout.Border[(int)Edge.Start] = nodeLeadingBorder(node, flexRowDirection);
        node.nodeLayout.Border[(int)Edge.End] = nodeTrailingBorder(node, flexRowDirection);
        node.nodeLayout.Border[(int)Edge.Top] = nodeLeadingBorder(node, flexColumnDirection);
        node.nodeLayout.Border[(int)Edge.Bottom] = nodeTrailingBorder(node, flexColumnDirection);

        node.nodeLayout.Padding[(int)Edge.Start] = nodeLeadingPadding(node, flexRowDirection, parentWidth);
        node.nodeLayout.Padding[(int)Edge.End] = nodeTrailingPadding(node, flexRowDirection, parentWidth);
        node.nodeLayout.Padding[(int)Edge.Top] = nodeLeadingPadding(node, flexColumnDirection, parentWidth);
        node.nodeLayout.Padding[(int)Edge.Bottom] = nodeTrailingPadding(node, flexColumnDirection, parentWidth);

        if (node.measureFunc != null)
        {
            nodeWithMeasureFuncSetMeasuredDimensions(node, availableWidth, availableHeight, widthMeasureMode, heightMeasureMode, parentWidth, parentHeight);
            return;
        }

        int childCount = node.Children.Count;
        if (childCount == 0)
        {
            nodeEmptyContainerSetMeasuredDimensions(node, availableWidth, availableHeight, widthMeasureMode, heightMeasureMode, parentWidth, parentHeight);
            return;
        }

        // If we're not being asked to perform a full layout we can skip the algorithm if we already know
        // the size
        if (!performLayout && nodeFixedSizeSetMeasuredDimensions(node, availableWidth, availableHeight, widthMeasureMode, heightMeasureMode, parentWidth, parentHeight))
        {
            return;
        }

        // Reset layout flags, as they could have changed.
        node.nodeLayout.HadOverflow = false;

        // STEP 1: CALCULATE VALUES FOR REMAINDER OF ALGORITHM
        var mainAxis = resolveFlexDirection(node.nodeStyle.FlexDirection, direction);
        var crossAxis = flexDirectionCross(mainAxis, direction);
        bool isMainAxisRow = flexDirectionIsRow(mainAxis);
        var justifyContent = node.nodeStyle.JustifyContent;
        bool isNodeFlexWrap = node.nodeStyle.FlexWrap != Wrap.NoWrap;

        float mainAxisParentSize = parentHeight;
        float crossAxisParentSize = parentWidth;
        if (isMainAxisRow)
        {
            mainAxisParentSize = parentWidth;
            crossAxisParentSize = parentHeight;
        }

        Node firstAbsoluteChild = null;
        Node currentAbsoluteChild = null;

        float leadingPaddingAndBorderMain = nodeLeadingPaddingAndBorder(node, mainAxis, parentWidth);
        float trailingPaddingAndBorderMain = nodeTrailingPaddingAndBorder(node, mainAxis, parentWidth);
        float leadingPaddingAndBorderCross = nodeLeadingPaddingAndBorder(node, crossAxis, parentWidth);
        float paddingAndBorderAxisMain = nodePaddingAndBorderForAxis(node, mainAxis, parentWidth);
        float paddingAndBorderAxisCross = nodePaddingAndBorderForAxis(node, crossAxis, parentWidth);

        var measureModeMainDim = heightMeasureMode;
        var measureModeCrossDim = widthMeasureMode;

        if (isMainAxisRow)
        {
            measureModeMainDim = widthMeasureMode;
            measureModeCrossDim = heightMeasureMode;
        }

        float paddingAndBorderAxisRow = paddingAndBorderAxisCross;
        float paddingAndBorderAxisColumn = paddingAndBorderAxisMain;
        if (isMainAxisRow)
        {
            paddingAndBorderAxisRow = paddingAndBorderAxisMain;
            paddingAndBorderAxisColumn = paddingAndBorderAxisCross;
        }

        float marginAxisRow = nodeMarginForAxis(node, FlexDirection.Row, parentWidth);
        float marginAxisColumn = nodeMarginForAxis(node, FlexDirection.Column, parentWidth);

        // STEP 2: DETERMINE AVAILABLE SIZE IN MAIN AND CROSS DIRECTIONS
        float minInnerWidth = resolveValue(node.nodeStyle.MinDimensions[(int)Dimension.Width], parentWidth) - marginAxisRow -
            paddingAndBorderAxisRow;
        float maxInnerWidth = resolveValue(node.nodeStyle.MaxDimensions[(int)Dimension.Width], parentWidth) - marginAxisRow -
            paddingAndBorderAxisRow;
        float minInnerHeight = resolveValue(node.nodeStyle.MinDimensions[(int)Dimension.Height], parentHeight) -
            marginAxisColumn - paddingAndBorderAxisColumn;
        float maxInnerHeight = resolveValue(node.nodeStyle.MaxDimensions[(int)Dimension.Height], parentHeight) -
            marginAxisColumn - paddingAndBorderAxisColumn;

        float minInnerMainDim = minInnerHeight;
        float maxInnerMainDim = maxInnerHeight;
        if (isMainAxisRow)
        {
            minInnerMainDim = minInnerWidth;
            maxInnerMainDim = maxInnerWidth;
        }

        // Max dimension overrides predefined dimension value; Min dimension in turn overrides both of the
        // above
        float availableInnerWidth = availableWidth - marginAxisRow - paddingAndBorderAxisRow;
        if (!FloatIsUndefined(availableInnerWidth))
        {
            // We want to make sure our available width does not violate min and max raints
            availableInnerWidth = fmaxf(fminf(availableInnerWidth, maxInnerWidth), minInnerWidth);
        }

        float availableInnerHeight = availableHeight - marginAxisColumn - paddingAndBorderAxisColumn;
        if (!FloatIsUndefined(availableInnerHeight))
        {
            // We want to make sure our available height does not violate min and max raints
            availableInnerHeight = fmaxf(fminf(availableInnerHeight, maxInnerHeight), minInnerHeight);
        }

        float availableInnerMainDim = availableInnerHeight;
        float availableInnerCrossDim = availableInnerWidth;
        if (isMainAxisRow)
        {
            availableInnerMainDim = availableInnerWidth;
            availableInnerCrossDim = availableInnerHeight;
        }

        // If there is only one child with flexGrow + flexShrink it means we can set the
        // computedFlexBasis to 0 instead of measuring and shrinking / flexing the child to exactly
        // match the remaining space
        Node singleFlexChild = null;
        if (measureModeMainDim == MeasureMode.Exactly)
        {
            foreach (var child in node.Children)
            {
                if (singleFlexChild != null)
                {
                    if (nodeIsFlex(child))
                    {
                        // There is already a flexible child, abort.
                        singleFlexChild = null;
                        break;
                    }
                }
                else if (resolveFlexGrow(child) > 0 && nodeResolveFlexShrink(child) > 0)
                {
                    singleFlexChild = child;
                }
            }
        }

        float totalOuterFlexBasis = 0;

        // STEP 3: DETERMINE FLEX BASIS FOR EACH ITEM
        foreach (var child in node.Children)
        {
            if (child.nodeStyle.Display == Display.None)
            {
                zeroOutLayoutRecursivly(child);
                child.hasNewLayout = true;
                child.IsDirty = false;
                continue;
            }
            resolveDimensions(child);
            if (performLayout)
            {
                // Set the initial position (relative to the parent).
                var childDirection = nodeResolveDirection(child, direction);
                nodeSetPosition(child,
                    childDirection,
                    availableInnerMainDim,
                    availableInnerCrossDim,
                    availableInnerWidth);
            }

            // Absolute-positioned children don't participate in flex layout. Add them
            // to a list that we can process later.
            if (child.nodeStyle.PositionType == PositionType.Absolute)
            {
                // Store a private linked list of absolutely positioned children
                // so that we can efficiently traverse them later.
                if (firstAbsoluteChild == null)
                {
                    firstAbsoluteChild = child;
                }
                if (currentAbsoluteChild != null)
                {
                    currentAbsoluteChild.NextChild = child;
                }
                currentAbsoluteChild = child;
                child.NextChild = null;
            }
            else
            {
                if (child == singleFlexChild)
                {
                    child.nodeLayout.computedFlexBasisGeneration = currentGenerationCount;
                    child.nodeLayout.computedFlexBasis = 0;
                }
                else
                {
                    nodeComputeFlexBasisForChild(node,
                        child,
                        availableInnerWidth,
                        widthMeasureMode,
                        availableInnerHeight,
                        availableInnerWidth,
                        availableInnerHeight,
                        heightMeasureMode,
                        direction,
                        config);
                }
                ;
            }

            totalOuterFlexBasis +=
                child.nodeLayout.computedFlexBasis + nodeMarginForAxis(child, mainAxis, availableInnerWidth);

        }

        bool flexBasisOverflows = totalOuterFlexBasis > availableInnerMainDim;
        if (measureModeMainDim == MeasureMode.Undefined)
        {
            flexBasisOverflows = false;
        }
        if (isNodeFlexWrap && flexBasisOverflows && measureModeMainDim == MeasureMode.AtMost)
        {
            measureModeMainDim = MeasureMode.Exactly;
        }

        // STEP 4: COLLECT FLEX ITEMS INTO FLEX LINES

        // Indexes of children that represent the first and last items in the line.
        int startOfLineIndex = 0;
        int endOfLineIndex = 0;

        // Number of lines.
        int lineCount = 0;

        // Accumulated cross dimensions of all lines so far.
        float totalLineCrossDim = 0;

        // Max main dimension of all the lines.
        float maxLineMainDim = 0;

        while (endOfLineIndex < childCount)
        {
            // Number of items on the currently line. May be different than the
            // difference
            // between start and end indicates because we skip over absolute-positioned
            // items.
            int itemsOnLine = 0;

            // sizeConsumedOnCurrentLine is accumulation of the dimensions and margin
            // of all the children on the current line. This will be used in order to
            // either set the dimensions of the node if none already exist or to compute
            // the remaining space left for the flexible children.
            float sizeConsumedOnCurrentLine = 0;
            float sizeConsumedOnCurrentLineIncludingMinConstraint = 0;

            float totalFlexGrowFactors = 0;
            float totalFlexShrinkScaledFactors = 0;

            // Maintain a linked list of the child nodes that can shrink and/or grow.
            Node firstRelativeChild = null;
            Node currentRelativeChild = null;

            // Add items to the current line until it's full or we run out of items.
            for (int i = startOfLineIndex; i < childCount; i++)
            {
                var child = node.Children[i];
                if (child.nodeStyle.Display == Display.None)
                {
                    endOfLineIndex++;
                    continue;
                }
                child.lineIndex = lineCount;

                if (child.nodeStyle.PositionType != PositionType.Absolute)
                {
                    float childMarginMainAxis = nodeMarginForAxis(child, mainAxis, availableInnerWidth);
                    float flexBasisWithMaxConstraints = fminf(resolveValue(child.nodeStyle.MaxDimensions[(int)dim[(int)mainAxis]], mainAxisParentSize), child.nodeLayout.computedFlexBasis);
                    float flexBasisWithMinAndMaxConstraints = fmaxf(resolveValue(child.nodeStyle.MinDimensions[(int)dim[(int)mainAxis]], mainAxisParentSize), flexBasisWithMaxConstraints);

                    // If this is a multi-line flow and this item pushes us over the
                    // available size, we've
                    // hit the end of the current line. Break out of the loop and lay out
                    // the current line.
                    if (sizeConsumedOnCurrentLineIncludingMinConstraint + flexBasisWithMinAndMaxConstraints +
                        childMarginMainAxis >
                        availableInnerMainDim &&
                        isNodeFlexWrap && itemsOnLine > 0)
                    {
                        break;
                    }

                    sizeConsumedOnCurrentLineIncludingMinConstraint +=
                        flexBasisWithMinAndMaxConstraints + childMarginMainAxis;
                    sizeConsumedOnCurrentLine += flexBasisWithMinAndMaxConstraints + childMarginMainAxis;
                    itemsOnLine++;

                    if (nodeIsFlex(child))
                    {
                        totalFlexGrowFactors += resolveFlexGrow(child);

                        // Unlike the grow factor, the shrink factor is scaled relative to the child dimension.
                        totalFlexShrinkScaledFactors +=
                            -nodeResolveFlexShrink(child) * child.nodeLayout.computedFlexBasis;
                    }

                    // Store a private linked list of children that need to be layed out.
                    if (firstRelativeChild == null)
                    {
                        firstRelativeChild = child;
                    }
                    if (currentRelativeChild != null)
                    {
                        currentRelativeChild.NextChild = child;
                    }
                    currentRelativeChild = child;
                    child.NextChild = null;
                }
                endOfLineIndex++;
            }

            // The total flex factor needs to be floored to 1.
            if (totalFlexGrowFactors > 0 && totalFlexGrowFactors < 1)
            {
                totalFlexGrowFactors = 1;
            }

            // The total flex shrink factor needs to be floored to 1.
            if (totalFlexShrinkScaledFactors > 0 && totalFlexShrinkScaledFactors < 1)
            {
                totalFlexShrinkScaledFactors = 1;
            }

            // If we don't need to measure the cross axis, we can skip the entire flex
            // step.
            bool canSkipFlex = !performLayout && measureModeCrossDim == MeasureMode.Exactly;

            // In order to position the elements in the main axis, we have two
            // controls. The space between the beginning and the first element
            // and the space between each two elements.
            float leadingMainDim = 0;
            float betweenMainDim = 0;

            // STEP 5: RESOLVING FLEXIBLE LENGTHS ON MAIN AXIS
            // Calculate the remaining available space that needs to be allocated.
            // If the main dimension size isn't known, it is computed based on
            // the line length, so there's no more space left to distribute.

            // If we don't measure with exact main dimension we want to ensure we don't violate min and max
            if (measureModeMainDim != MeasureMode.Exactly)
            {
                if (!FloatIsUndefined(minInnerMainDim) && sizeConsumedOnCurrentLine < minInnerMainDim)
                {
                    availableInnerMainDim = minInnerMainDim;
                }
                else if (!FloatIsUndefined(maxInnerMainDim) &&
                  sizeConsumedOnCurrentLine > maxInnerMainDim)
                {
                    availableInnerMainDim = maxInnerMainDim;
                }
                else
                {
                    if (!node.config.UseLegacyStretchBehaviour &&
                        (totalFlexGrowFactors == 0 || resolveFlexGrow(node) == 0))
                    {
                        // If we don't have any children to flex or we can't flex the node itself,
                        // space we've used is all space we need. Root node also should be shrunk to minimum
                        availableInnerMainDim = sizeConsumedOnCurrentLine;
                    }
                }
            }

            float remainingFreeSpace = 0;
            if (!FloatIsUndefined(availableInnerMainDim))
            {
                remainingFreeSpace = availableInnerMainDim - sizeConsumedOnCurrentLine;
            }
            else if (sizeConsumedOnCurrentLine < 0)
            {
                // availableInnerMainDim is indefinite which means the node is being sized based on its
                // content.
                // sizeConsumedOnCurrentLine is negative which means the node will allocate 0 points for
                // its content. Consequently, remainingFreeSpace is 0 - sizeConsumedOnCurrentLine.
                remainingFreeSpace = -sizeConsumedOnCurrentLine;
            }

            float originalRemainingFreeSpace = remainingFreeSpace;
            float deltaFreeSpace = 0;

            if (!canSkipFlex)
            {
                float childFlexBasis;
                float flexShrinkScaledFactor;
                float flexGrowFactor;
                float baseMainSize;
                float boundMainSize;

                // Do two passes over the flex items to figure out how to distribute the
                // remaining space.
                // The first pass finds the items whose min/max raints trigger,
                // freezes them at those
                // sizes, and excludes those sizes from the remaining space. The second
                // pass sets the size
                // of each flexible item. It distributes the remaining space amongst the
                // items whose min/max
                // raints didn't trigger in pass 1. For the other items, it sets
                // their sizes by forcing
                // their min/max raints to trigger again.
                //
                // This two pass approach for resolving min/max raints deviates from
                // the spec. The
                // spec (https://www.w3.org/TR/YG-flexbox-1/#resolve-flexible-lengths)
                // describes a process
                // that needs to be repeated a variable number of times. The algorithm
                // implemented here
                // won't handle all cases but it was simpler to implement and it mitigates
                // performance
                // concerns because we know exactly how many passes it'll do.

                // First pass: detect the flex items whose min/max raints trigger
                float deltaFlexShrinkScaledFactors = 0;
                float deltaFlexGrowFactors = 0;
                currentRelativeChild = firstRelativeChild;
                while (currentRelativeChild != null)
                {
                    childFlexBasis =
                        fminf(resolveValue(currentRelativeChild.nodeStyle.MaxDimensions[(int)dim[(int)mainAxis]],
                            mainAxisParentSize),
                            fmaxf(resolveValue(currentRelativeChild.nodeStyle.MinDimensions[(int)dim[(int)mainAxis]],
                                mainAxisParentSize),
                                currentRelativeChild.nodeLayout.computedFlexBasis));

                    if (remainingFreeSpace < 0)
                    {
                        flexShrinkScaledFactor = -nodeResolveFlexShrink(currentRelativeChild) * childFlexBasis;

                        // Is this child able to shrink?
                        if (flexShrinkScaledFactor != 0)
                        {
                            baseMainSize =
                                childFlexBasis +
                                    remainingFreeSpace / totalFlexShrinkScaledFactors * flexShrinkScaledFactor;
                            boundMainSize = nodeBoundAxis(currentRelativeChild,
                                mainAxis,
                                baseMainSize,
                                availableInnerMainDim,
                                availableInnerWidth);
                            if (baseMainSize != boundMainSize)
                            {
                                // By excluding this item's size and flex factor from remaining,
                                // this item's
                                // min/max raints should also trigger in the second pass
                                // resulting in the
                                // item's size calculation being identical in the first and second
                                // passes.
                                deltaFreeSpace -= boundMainSize - childFlexBasis;
                                deltaFlexShrinkScaledFactors -= flexShrinkScaledFactor;
                            }
                        }
                    }
                    else if (remainingFreeSpace > 0)
                    {
                        flexGrowFactor = resolveFlexGrow(currentRelativeChild);

                        // Is this child able to grow?
                        if (flexGrowFactor != 0)
                        {
                            baseMainSize =
                                childFlexBasis + remainingFreeSpace / totalFlexGrowFactors * flexGrowFactor;
                            boundMainSize = nodeBoundAxis(currentRelativeChild,
                                mainAxis,
                                baseMainSize,
                                availableInnerMainDim,
                                availableInnerWidth);

                            if (baseMainSize != boundMainSize)
                            {
                                // By excluding this item's size and flex factor from remaining,
                                // this item's
                                // min/max raints should also trigger in the second pass
                                // resulting in the
                                // item's size calculation being identical in the first and second
                                // passes.
                                deltaFreeSpace -= boundMainSize - childFlexBasis;
                                deltaFlexGrowFactors -= flexGrowFactor;
                            }
                        }
                    }

                    currentRelativeChild = currentRelativeChild.NextChild;
                }

                totalFlexShrinkScaledFactors += deltaFlexShrinkScaledFactors;
                totalFlexGrowFactors += deltaFlexGrowFactors;
                remainingFreeSpace += deltaFreeSpace;

                // Second pass: resolve the sizes of the flexible items
                deltaFreeSpace = 0;
                currentRelativeChild = firstRelativeChild;
                while (currentRelativeChild != null)
                {
                    childFlexBasis =
                        fminf(resolveValue(currentRelativeChild.nodeStyle.MaxDimensions[(int)dim[(int)mainAxis]],
                            mainAxisParentSize),
                            fmaxf(resolveValue(currentRelativeChild.nodeStyle.MinDimensions[(int)dim[(int)mainAxis]],
                                mainAxisParentSize),
                                currentRelativeChild.nodeLayout.computedFlexBasis));
                    float updatedMainSize = childFlexBasis;

                    if (remainingFreeSpace < 0)
                    {
                        flexShrinkScaledFactor = -nodeResolveFlexShrink(currentRelativeChild) * childFlexBasis;
                        // Is this child able to shrink?
                        if (flexShrinkScaledFactor != 0)
                        {
                            float childSize = 0;

                            if (totalFlexShrinkScaledFactors == 0)
                            {
                                childSize = childFlexBasis + flexShrinkScaledFactor;
                            }
                            else
                            {
                                childSize =
                                    childFlexBasis +
                                        (remainingFreeSpace / totalFlexShrinkScaledFactors) * flexShrinkScaledFactor;
                            }

                            updatedMainSize = nodeBoundAxis(currentRelativeChild,
                                mainAxis,
                                childSize,
                                availableInnerMainDim,
                                availableInnerWidth);
                        }
                    }
                    else if (remainingFreeSpace > 0)
                    {
                        flexGrowFactor = resolveFlexGrow(currentRelativeChild);

                        // Is this child able to grow?
                        if (flexGrowFactor != 0)
                        {
                            updatedMainSize =
                                nodeBoundAxis(currentRelativeChild,
                                    mainAxis,
                                    childFlexBasis +
                                        remainingFreeSpace / totalFlexGrowFactors * flexGrowFactor,
                                    availableInnerMainDim,
                                    availableInnerWidth);
                        }
                    }

                    deltaFreeSpace -= updatedMainSize - childFlexBasis;

                    float marginMain = nodeMarginForAxis(currentRelativeChild, mainAxis, availableInnerWidth);
                    float marginCross = nodeMarginForAxis(currentRelativeChild, crossAxis, availableInnerWidth);

                    float childCrossSize = 0;
                    float childMainSize = updatedMainSize + marginMain;
                    MeasureMode childCrossMeasureMode = MeasureMode.Undefined; // TODO : no init vaule ?
                    var childMainMeasureMode = MeasureMode.Exactly;

                    if (!FloatIsUndefined(availableInnerCrossDim) &&
                        !nodeIsStyleDimDefined(currentRelativeChild, crossAxis, availableInnerCrossDim) &&
                        measureModeCrossDim == MeasureMode.Exactly &&
                        !(isNodeFlexWrap && flexBasisOverflows) &&
                        nodeAlignItem(node, currentRelativeChild) == Align.Stretch)
                    {
                        childCrossSize = availableInnerCrossDim;
                        childCrossMeasureMode = MeasureMode.Exactly;
                    }
                    else if (!nodeIsStyleDimDefined(currentRelativeChild,
                      crossAxis,
                      availableInnerCrossDim))
                    {
                        childCrossSize = availableInnerCrossDim;
                        childCrossMeasureMode = MeasureMode.AtMost;
                        if (FloatIsUndefined(childCrossSize))
                        {
                            childCrossMeasureMode = MeasureMode.Undefined;
                        }
                    }
                    else
                    {
                        childCrossSize = resolveValue(currentRelativeChild.resolvedDimensions[(int)dim[(int)crossAxis]],
                            availableInnerCrossDim) +
                            marginCross;
                        bool isLoosePercentageMeasurement = currentRelativeChild.resolvedDimensions[(int)dim[(int)crossAxis]].unit == Unit.Percent &&
                            measureModeCrossDim != MeasureMode.Exactly;
                        childCrossMeasureMode = MeasureMode.Exactly;
                        if (FloatIsUndefined(childCrossSize) || isLoosePercentageMeasurement)
                        {
                            childCrossMeasureMode = MeasureMode.Undefined;
                        }
                    }

                    if (!FloatIsUndefined(currentRelativeChild.nodeStyle.AspectRatio))
                    {
                        float v = (childMainSize - marginMain) * currentRelativeChild.nodeStyle.AspectRatio;
                        if (isMainAxisRow)
                        {
                            v = (childMainSize - marginMain) / currentRelativeChild.nodeStyle.AspectRatio;
                        }
                        childCrossSize = fmaxf(v, nodePaddingAndBorderForAxis(currentRelativeChild, crossAxis, availableInnerWidth));
                        childCrossMeasureMode = MeasureMode.Exactly;

                        // Parent size raint should have higher priority than flex
                        if (nodeIsFlex(currentRelativeChild))
                        {
                            childCrossSize = fminf(childCrossSize - marginCross, availableInnerCrossDim);
                            childMainSize = marginMain;
                            if (isMainAxisRow)
                            {
                                childMainSize += childCrossSize * currentRelativeChild.nodeStyle.AspectRatio;
                            }
                            else
                            {
                                childMainSize += childCrossSize / currentRelativeChild.nodeStyle.AspectRatio;
                            }
                        }

                        childCrossSize += marginCross;
                    }

                    constrainMaxSizeForMode(currentRelativeChild,
                        mainAxis,
                        availableInnerMainDim,
                        availableInnerWidth,
                        ref childMainMeasureMode,
                        ref childMainSize);
                    constrainMaxSizeForMode(currentRelativeChild,
                        crossAxis,
                        availableInnerCrossDim,
                        availableInnerWidth,
                        ref childCrossMeasureMode,
                        ref childCrossSize);

                    bool requiresStretchLayout = !nodeIsStyleDimDefined(currentRelativeChild, crossAxis, availableInnerCrossDim) &&
                        nodeAlignItem(node, currentRelativeChild) == Align.Stretch;

                    float childWidth = childCrossSize;
                    if (isMainAxisRow)
                    {
                        childWidth = childMainSize;
                    }
                    float childHeight = childCrossSize;
                    if (!isMainAxisRow)
                    {
                        childHeight = childMainSize;
                    }

                    var childWidthMeasureMode = childCrossMeasureMode;
                    if (isMainAxisRow)
                    {
                        childWidthMeasureMode = childMainMeasureMode;
                    }
                    var childHeightMeasureMode = childCrossMeasureMode;
                    if (!isMainAxisRow)
                    {
                        childHeightMeasureMode = childMainMeasureMode;
                    }

                    // Recursively call the layout algorithm for this child with the updated
                    // main size.
                    layoutNodeInternal(currentRelativeChild,
                        childWidth,
                        childHeight,
                        direction,
                        childWidthMeasureMode,
                        childHeightMeasureMode,
                        availableInnerWidth,
                        availableInnerHeight,
                        performLayout && !requiresStretchLayout,
                        "flex",
                        config);
                    if (currentRelativeChild.nodeLayout.HadOverflow)
                    {
                        node.nodeLayout.HadOverflow = true;
                    }

                    currentRelativeChild = currentRelativeChild.NextChild;
                }
            }

            remainingFreeSpace = originalRemainingFreeSpace + deltaFreeSpace;
            if (remainingFreeSpace < 0)
            {
                node.nodeLayout.HadOverflow = true;
            }

            // STEP 6: MAIN-AXIS JUSTIFICATION & CROSS-AXIS SIZE DETERMINATION

            // At this point, all the children have their dimensions set in the main
            // axis.
            // Their dimensions are also set in the cross axis with the exception of
            // items
            // that are aligned "stretch". We need to compute these stretch values and
            // set the final positions.

            // If we are using "at most" rules in the main axis. Calculate the remaining space when
            // raint by the min size defined for the main axis.

            if (measureModeMainDim == MeasureMode.AtMost && remainingFreeSpace > 0)
            {
                if (node.nodeStyle.MinDimensions[(int)dim[(int)mainAxis]].unit != Unit.Undefined &&
                    resolveValue(node.nodeStyle.MinDimensions[(int)dim[(int)mainAxis]], mainAxisParentSize) >= 0)
                {
                    remainingFreeSpace =
                        fmaxf(0,
                            resolveValue(node.nodeStyle.MinDimensions[(int)dim[(int)mainAxis]], mainAxisParentSize) -
                                (availableInnerMainDim - remainingFreeSpace));
                }
                else
                {
                    remainingFreeSpace = 0;
                }
            }

            int numberOfAutoMarginsOnCurrentLine = 0;
            for (int i = startOfLineIndex; i < endOfLineIndex; i++)
            {
                var child = node.Children[i];
                if (child.nodeStyle.PositionType == PositionType.Relative)
                {
                    if (marginLeadingValue(child, mainAxis).unit == Unit.Auto)
                    {
                        numberOfAutoMarginsOnCurrentLine++;
                    }
                    if (marginTrailingValue(child, mainAxis).unit == Unit.Auto)
                    {
                        numberOfAutoMarginsOnCurrentLine++;
                    }
                }
            }

            if (numberOfAutoMarginsOnCurrentLine == 0)
            {
                switch (justifyContent)
                {
                    case Justify.Center:
                        leadingMainDim = remainingFreeSpace / 2;
                        break;
                    case Justify.FlexEnd:
                        leadingMainDim = remainingFreeSpace;
                        break;
                    case Justify.SpaceBetween:
                        if (itemsOnLine > 1)
                        {
                            betweenMainDim = fmaxf(remainingFreeSpace, 0) / (float)(itemsOnLine - 1);
                        }
                        else
                        {
                            betweenMainDim = 0;
                        }
                        break;
                    case Justify.SpaceAround:
                        // Space on the edges is half of the space between elements
                        betweenMainDim = remainingFreeSpace / (float)(itemsOnLine);
                        leadingMainDim = betweenMainDim / 2;
                        break;
                    case Justify.FlexStart:
                        break;
                }
            }

            float mainDim = leadingPaddingAndBorderMain + leadingMainDim;
            float crossDim = 0;

            for (int i = startOfLineIndex; i < endOfLineIndex; i++)
            {
                var child = node.Children[i];
                if (child.nodeStyle.Display == Display.None)
                {
                    continue;
                }
                if (child.nodeStyle.PositionType == PositionType.Absolute &&
                    nodeIsLeadingPosDefined(child, mainAxis))
                {
                    if (performLayout)
                    {
                        // In case the child is position absolute and has left/top being
                        // defined, we override the position to whatever the user said
                        // (and margin/border).
                        child.nodeLayout.Position[(int)pos[(int)mainAxis]] =
                            nodeLeadingPosition(child, mainAxis, availableInnerMainDim) +
                                nodeLeadingBorder(node, mainAxis) +
                                nodeLeadingMargin(child, mainAxis, availableInnerWidth);
                    }
                }
                else
                {
                    // Now that we placed the element, we need to update the variables.
                    // We need to do that only for relative elements. Absolute elements
                    // do not take part in that phase.
                    if (child.nodeStyle.PositionType == PositionType.Relative)
                    {
                        if (marginLeadingValue(child, mainAxis).unit == Unit.Auto)
                        {
                            mainDim += remainingFreeSpace / (float)(numberOfAutoMarginsOnCurrentLine);
                        }

                        if (performLayout)
                        {
                            child.nodeLayout.Position[(int)pos[(int)mainAxis]] += mainDim;
                        }

                        if (marginTrailingValue(child, mainAxis).unit == Unit.Auto)
                        {
                            mainDim += remainingFreeSpace / (float)(numberOfAutoMarginsOnCurrentLine);
                        }

                        if (canSkipFlex)
                        {
                            // If we skipped the flex step, then we can't rely on the
                            // measuredDims because
                            // they weren't computed. This means we can't call YGNodeDimWithMargin.
                            mainDim += betweenMainDim + nodeMarginForAxis(child, mainAxis, availableInnerWidth) +
                                child.nodeLayout.computedFlexBasis;
                            crossDim = availableInnerCrossDim;
                        }
                        else
                        {
                            // The main dimension is the sum of all the elements dimension plus the spacing.
                            mainDim += betweenMainDim + nodeDimWithMargin(child, mainAxis, availableInnerWidth);

                            // The cross dimension is the max of the elements dimension since
                            // there can only be one element in that cross dimension.
                            crossDim = fmaxf(crossDim, nodeDimWithMargin(child, crossAxis, availableInnerWidth));
                        }
                    }
                    else if (performLayout)
                    {
                        child.nodeLayout.Position[(int)pos[(int)mainAxis]] +=
                            nodeLeadingBorder(node, mainAxis) + leadingMainDim;
                    }
                }
            }

            mainDim += trailingPaddingAndBorderMain;

            float containerCrossAxis = availableInnerCrossDim;
            if (measureModeCrossDim == MeasureMode.Undefined ||
                measureModeCrossDim == MeasureMode.AtMost)
            {
                // Compute the cross axis from the max cross dimension of the children.
                containerCrossAxis = nodeBoundAxis(node,
                    crossAxis,
                    crossDim + paddingAndBorderAxisCross,
                    crossAxisParentSize,
                    parentWidth) -
                    paddingAndBorderAxisCross;
            }

            // If there's no flex wrap, the cross dimension is defined by the container.
            if (!isNodeFlexWrap && measureModeCrossDim == MeasureMode.Exactly)
            {
                crossDim = availableInnerCrossDim;
            }

            // Clamp to the min/max size specified on the container.
            crossDim = nodeBoundAxis(node,
                crossAxis,
                crossDim + paddingAndBorderAxisCross,
                crossAxisParentSize,
                parentWidth) -
                paddingAndBorderAxisCross;

            // STEP 7: CROSS-AXIS ALIGNMENT
            // We can skip child alignment if we're just measuring the container.
            if (performLayout)
            {
                for (int i = startOfLineIndex; i < endOfLineIndex; i++)
                {
                    var child = node.Children[i];
                    if (child.nodeStyle.Display == Display.None)
                    {
                        continue;
                    }
                    if (child.nodeStyle.PositionType == PositionType.Absolute)
                    {
                        // If the child is absolutely positioned and has a
                        // top/left/bottom/right
                        // set, override all the previously computed positions to set it
                        // correctly.
                        if (nodeIsLeadingPosDefined(child, crossAxis))
                        {
                            child.nodeLayout.Position[(int)pos[(int)crossAxis]] =
                                nodeLeadingPosition(child, crossAxis, availableInnerCrossDim) +
                                    nodeLeadingBorder(node, crossAxis) +
                                    nodeLeadingMargin(child, crossAxis, availableInnerWidth);
                        }
                        else
                        {
                            child.nodeLayout.Position[(int)pos[(int)crossAxis]] =
                                nodeLeadingBorder(node, crossAxis) +
                                    nodeLeadingMargin(child, crossAxis, availableInnerWidth);
                        }
                    }
                    else
                    {
                        float leadingCrossDim = leadingPaddingAndBorderCross;

                        // For a relative children, we're either using alignItems (parent) or
                        // alignSelf (child) in order to determine the position in the cross
                        // axis
                        var alignItem = nodeAlignItem(node, child);

                        // If the child uses align stretch, we need to lay it out one more
                        // time, this time
                        // forcing the cross-axis size to be the computed cross size for the
                        // current line.
                        if (alignItem == Align.Stretch &&
                            marginLeadingValue(child, crossAxis).unit != Unit.Auto &&
                            marginTrailingValue(child, crossAxis).unit != Unit.Auto)
                        {
                            // If the child defines a definite size for its cross axis, there's
                            // no need to stretch.
                            if (!nodeIsStyleDimDefined(child, crossAxis, availableInnerCrossDim))
                            {
                                float childMainSize = child.nodeLayout.measuredDimensions[(int)dim[(int)mainAxis]];
                                float childCrossSize = crossDim;
                                if (!FloatIsUndefined(child.nodeStyle.AspectRatio))
                                {
                                    childCrossSize = nodeMarginForAxis(child, crossAxis, availableInnerWidth);
                                    if (isMainAxisRow)
                                    {
                                        childCrossSize += childMainSize / child.nodeStyle.AspectRatio;
                                    }
                                    else
                                    {
                                        childCrossSize += childMainSize * child.nodeStyle.AspectRatio;
                                    }
                                }

                                childMainSize += nodeMarginForAxis(child, mainAxis, availableInnerWidth);

                                var childMainMeasureMode = MeasureMode.Exactly;
                                var childCrossMeasureMode = MeasureMode.Exactly;
                                constrainMaxSizeForMode(child,
                                    mainAxis,
                                    availableInnerMainDim,
                                    availableInnerWidth,
                                    ref childMainMeasureMode,
                                    ref childMainSize);
                                constrainMaxSizeForMode(child,
                                    crossAxis,
                                    availableInnerCrossDim,
                                    availableInnerWidth,
                                    ref childCrossMeasureMode,
                                    ref childCrossSize);

                                float childWidth = childCrossSize;
                                if (isMainAxisRow)
                                {
                                    childWidth = childMainSize;
                                }
                                float childHeight = childCrossSize;
                                if (!isMainAxisRow)
                                {
                                    childHeight = childMainSize;
                                }

                                var childWidthMeasureMode = MeasureMode.Exactly;
                                if (FloatIsUndefined(childWidth))
                                {
                                    childWidthMeasureMode = MeasureMode.Undefined;
                                }

                                var childHeightMeasureMode = MeasureMode.Exactly;
                                if (FloatIsUndefined(childHeight))
                                {
                                    childHeightMeasureMode = MeasureMode.Undefined;
                                }

                                layoutNodeInternal(child,
                                    childWidth,
                                    childHeight,
                                    direction,
                                    childWidthMeasureMode,
                                    childHeightMeasureMode,
                                    availableInnerWidth,
                                    availableInnerHeight,
                                    true,
                                    "stretch",
                                    config);
                            }
                        }
                        else
                        {
                            float remainingCrossDim = containerCrossAxis - nodeDimWithMargin(child, crossAxis, availableInnerWidth);

                            if (marginLeadingValue(child, crossAxis).unit == Unit.Auto &&
                                marginTrailingValue(child, crossAxis).unit == Unit.Auto)
                            {
                                leadingCrossDim += fmaxf(0, remainingCrossDim / 2);
                            }
                            else if (marginTrailingValue(child, crossAxis).unit == Unit.Auto)
                            {
                                // No-Op
                            }
                            else if (marginLeadingValue(child, crossAxis).unit == Unit.Auto)
                            {
                                leadingCrossDim += fmaxf(0, remainingCrossDim);
                            }
                            else if (alignItem == Align.FlexStart)
                            {
                                // No-Op
                            }
                            else if (alignItem == Align.Center)
                            {
                                leadingCrossDim += remainingCrossDim / 2;
                            }
                            else
                            {
                                leadingCrossDim += remainingCrossDim;
                            }
                        }
                        // And we apply the position
                        child.nodeLayout.Position[(int)pos[(int)crossAxis]] += totalLineCrossDim + leadingCrossDim;
                    }
                }
            }

            totalLineCrossDim += crossDim;
            maxLineMainDim = fmaxf(maxLineMainDim, mainDim);

            lineCount++;
            startOfLineIndex = endOfLineIndex;

        }

        // STEP 8: MULTI-LINE CONTENT ALIGNMENT
        if (performLayout && (lineCount > 1 || isBaselineLayout(node)) &&
            !FloatIsUndefined(availableInnerCrossDim))
        {
            float remainingAlignContentDim = availableInnerCrossDim - totalLineCrossDim;

            float crossDimLead = 0;
            float currentLead = leadingPaddingAndBorderCross;

            switch (node.nodeStyle.AlignContent)
            {
                case Align.FlexEnd:
                    currentLead += remainingAlignContentDim;
                    break;
                case Align.Center:
                    currentLead += remainingAlignContentDim / 2;
                    break;
                case Align.Stretch:
                    if (availableInnerCrossDim > totalLineCrossDim)
                    {
                        crossDimLead = remainingAlignContentDim / (float)(lineCount);
                    }
                    break;
                case Align.SpaceAround:
                    if (availableInnerCrossDim > totalLineCrossDim)
                    {
                        currentLead += remainingAlignContentDim / (float)(2 * lineCount);
                        if (lineCount > 1)
                        {
                            crossDimLead = remainingAlignContentDim / (float)(lineCount);
                        }
                    }
                    else
                    {
                        currentLead += remainingAlignContentDim / 2;
                    }
                    break;
                case Align.SpaceBetween:
                    if (availableInnerCrossDim > totalLineCrossDim && lineCount > 1)
                    {
                        crossDimLead = remainingAlignContentDim / (float)(lineCount - 1);
                    }
                    break;
                case Align.Auto:
                case Align.FlexStart:
                case Align.Baseline:
                    break;
            }

            int endIndex = 0;
            for (int i = 0; i < lineCount; i++)
            {
                int startIndex = endIndex;
                int ii = 0;

                // compute the line's height and find the endInde.x
                float lineHeight = 0;
                float maxAscentForCurrentLine = 0;
                float maxDescentForCurrentLine = 0;
                for (ii = startIndex; ii < childCount; ii++)
                {
                    var child = node.Children[ii];
                    if (child.nodeStyle.Display == Display.None)
                    {
                        continue;
                    }
                    if (child.nodeStyle.PositionType == PositionType.Relative)
                    {
                        if (child.lineIndex != i)
                        {
                            break;
                        }
                        if (nodeIsLayoutDimDefined(child, crossAxis))
                        {
                            lineHeight = fmaxf(lineHeight,
                                child.nodeLayout.measuredDimensions[(int)dim[(int)crossAxis]] +
                                    nodeMarginForAxis(child, crossAxis, availableInnerWidth));
                        }
                        if (nodeAlignItem(node, child) == Align.Baseline)
                        {
                            float ascent = Baseline(child) + nodeLeadingMargin(child, FlexDirection.Column, availableInnerWidth);
                            float descent = child.nodeLayout.measuredDimensions[(int)Dimension.Height] + nodeMarginForAxis(child, FlexDirection.Column, availableInnerWidth) - ascent;
                            maxAscentForCurrentLine = fmaxf(maxAscentForCurrentLine, ascent);
                            maxDescentForCurrentLine = fmaxf(maxDescentForCurrentLine, descent);
                            lineHeight = fmaxf(lineHeight, maxAscentForCurrentLine + maxDescentForCurrentLine);
                        }
                    }
                }
                endIndex = ii;
                lineHeight += crossDimLead;

                if (performLayout)
                {
                    for (ii = startIndex; ii < endIndex; ii++)
                    {
                        var child = node.Children[ii];
                        if (child.nodeStyle.Display == Display.None)
                        {
                            continue;
                        }
                        if (child.nodeStyle.PositionType == PositionType.Relative)
                        {
                            switch (nodeAlignItem(node, child))
                            {
                                case Align.FlexStart:
                                    {
                                        child.nodeLayout.Position[(int)pos[(int)crossAxis]] =
                                            currentLead + nodeLeadingMargin(child, crossAxis, availableInnerWidth);
                                    }
                                    break;
                                case Align.FlexEnd:
                                    {
                                        child.nodeLayout.Position[(int)pos[(int)crossAxis]] =
                                            currentLead + lineHeight -
                                                nodeTrailingMargin(child, crossAxis, availableInnerWidth) -
                                                child.nodeLayout.measuredDimensions[(int)dim[(int)crossAxis]];
                                    }
                                    break;
                                case Align.Center:
                                    {
                                        float childHeight = child.nodeLayout.measuredDimensions[(int)dim[(int)crossAxis]];
                                        child.nodeLayout.Position[(int)pos[(int)crossAxis]] = currentLead + (lineHeight - childHeight) / 2;
                                    }
                                    break;
                                case Align.Stretch:
                                    {
                                        child.nodeLayout.Position[(int)pos[(int)crossAxis]] =
                                            currentLead + nodeLeadingMargin(child, crossAxis, availableInnerWidth);

                                        // Remeasure child with the line height as it as been only measured with the
                                        // parents height yet.
                                        if (!nodeIsStyleDimDefined(child, crossAxis, availableInnerCrossDim))
                                        {
                                            float childWidth = lineHeight;
                                            if (isMainAxisRow)
                                            {
                                                childWidth = child.nodeLayout.measuredDimensions[(int)Dimension.Width] +
                                                    nodeMarginForAxis(child, mainAxis, availableInnerWidth);
                                            }

                                            float childHeight = lineHeight;
                                            if (!isMainAxisRow)
                                            {
                                                childHeight = child.nodeLayout.measuredDimensions[(int)Dimension.Height] +
                                                    nodeMarginForAxis(child, crossAxis, availableInnerWidth);
                                            }

                                            if (!(FloatsEqual(childWidth,
                                                child.nodeLayout.measuredDimensions[(int)Dimension.Width]) &&
                                                FloatsEqual(childHeight,
                                                    child.nodeLayout.measuredDimensions[(int)Dimension.Height])))
                                            {
                                                layoutNodeInternal(child,
                                                    childWidth,
                                                    childHeight,
                                                    direction,
                                                    MeasureMode.Exactly,
                                                    MeasureMode.Exactly,
                                                    availableInnerWidth,
                                                    availableInnerHeight,
                                                    true,
                                                    "multiline-stretch",
                                                    config);
                                            }
                                        }
                                    }
                                    break;
                                case Align.Baseline:
                                    {
                                        child.nodeLayout.Position[(int)Edge.Top] =
                                            currentLead + maxAscentForCurrentLine - Baseline(child) +
                                                nodeLeadingPosition(child, FlexDirection.Column, availableInnerCrossDim);
                                    }
                                    break;
                                case Align.Auto:
                                case Align.SpaceBetween:
                                case Align.SpaceAround:
                                    break;
                            }
                        }
                    }
                }

                currentLead += lineHeight;
            }
        }

        //   STEP 9: COMPUTING FINAL DIMENSIONS
        node.nodeLayout.measuredDimensions[(int)Dimension.Width] = nodeBoundAxis(
            node, FlexDirection.Row, availableWidth - marginAxisRow, parentWidth, parentWidth);
        node.nodeLayout.measuredDimensions[(int)Dimension.Height] = nodeBoundAxis(
            node, FlexDirection.Column, availableHeight - marginAxisColumn, parentHeight, parentWidth);

        // If the user didn't specify a width or height for the node, set the
        // dimensions based on the children.
        if (measureModeMainDim == MeasureMode.Undefined ||
            (node.nodeStyle.Overflow != Overflow.Scroll && measureModeMainDim == MeasureMode.AtMost))
        {
            // Clamp the size to the min/max size, if specified, and make sure it
            // doesn't go below the padding and border amount.
            node.nodeLayout.measuredDimensions[(int)dim[(int)mainAxis]] =
                nodeBoundAxis(node, mainAxis, maxLineMainDim, mainAxisParentSize, parentWidth);
        }
        else if (measureModeMainDim == MeasureMode.AtMost &&
          node.nodeStyle.Overflow == Overflow.Scroll)
        {
            node.nodeLayout.measuredDimensions[(int)dim[(int)mainAxis]] = fmaxf(
                fminf(availableInnerMainDim + paddingAndBorderAxisMain,
                    nodeBoundAxisWithinMinAndMax(node, mainAxis, maxLineMainDim, mainAxisParentSize)),
                paddingAndBorderAxisMain);
        }

        if (measureModeCrossDim == MeasureMode.Undefined ||
            (node.nodeStyle.Overflow != Overflow.Scroll && measureModeCrossDim == MeasureMode.AtMost))
        {
            // Clamp the size to the min/max size, if specified, and make sure it
            // doesn't go below the padding and border amount.
            node.nodeLayout.measuredDimensions[(int)dim[(int)crossAxis]] =
                nodeBoundAxis(node,
                    crossAxis,
                    totalLineCrossDim + paddingAndBorderAxisCross,
                    crossAxisParentSize,
                    parentWidth);
        }
        else if (measureModeCrossDim == MeasureMode.AtMost &&
          node.nodeStyle.Overflow == Overflow.Scroll)
        {
            node.nodeLayout.measuredDimensions[(int)dim[(int)crossAxis]] =
                fmaxf(fminf(availableInnerCrossDim + paddingAndBorderAxisCross,
                    nodeBoundAxisWithinMinAndMax(node,
                        crossAxis,
                        totalLineCrossDim + paddingAndBorderAxisCross,
                        crossAxisParentSize)),
                    paddingAndBorderAxisCross);
        }

        // As we only wrapped in normal direction yet, we need to reverse the positions on wrap-reverse.
        if (performLayout && node.nodeStyle.FlexWrap == Wrap.WrapReverse)
        {
            foreach (var child in node.Children)
            {
                if (child.nodeStyle.PositionType == PositionType.Relative)
                {
                    child.nodeLayout.Position[(int)pos[(int)crossAxis]] = node.nodeLayout.measuredDimensions[(int)dim[(int)crossAxis]] -
                        child.nodeLayout.Position[(int)pos[(int)crossAxis]] -
                        child.nodeLayout.measuredDimensions[(int)dim[(int)crossAxis]];
                }
            }
        }

        if (performLayout)
        {
            // STEP 10: SIZING AND POSITIONING ABSOLUTE CHILDREN
            for (currentAbsoluteChild = firstAbsoluteChild; currentAbsoluteChild != null; currentAbsoluteChild = currentAbsoluteChild.NextChild)
            {
                var mode = measureModeCrossDim;
                if (isMainAxisRow)
                {
                    mode = measureModeMainDim;
                }

                nodeAbsoluteLayoutChild(node,
                    currentAbsoluteChild,
                    availableInnerWidth,
                    mode,
                    availableInnerHeight,
                    direction,
                    config);
            }

            // STEP 11: SETTING TRAILING POSITIONS FOR CHILDREN
            bool needsMainTrailingPos = mainAxis == FlexDirection.RowReverse || mainAxis == FlexDirection.ColumnReverse;
            bool needsCrossTrailingPos = crossAxis == FlexDirection.RowReverse || crossAxis == FlexDirection.ColumnReverse;

            // Set trailing position if necessary.
            if (needsMainTrailingPos || needsCrossTrailingPos)
            {
                foreach (var child in node.Children)
                {
                    if (child.nodeStyle.Display == Display.None)
                    {
                        continue;
                    }
                    if (needsMainTrailingPos)
                    {
                        nodeSetChildTrailingPosition(node, child, mainAxis);
                    }

                    if (needsCrossTrailingPos)
                    {
                        nodeSetChildTrailingPosition(node, child, crossAxis);
                    }
                }
            }
        }
    }

    const string spacerStr = "";

    // spacer returns spacer string
    internal static string spacer(int level)
    {
        if (level > spacerStr.Length)
        {
            level = spacerStr.Length;
        }
        return spacerStr.Substring(0, level);
    }

    // measureModeName returns name of measure mode
    internal static string measureModeName(MeasureMode mode, bool performLayout)
    {

        if ((int)mode >= Constant.measureModeCount)
        {
            return "";
        }

        if (performLayout)
        {
            return Constant.layoutModeNames[(int)mode];
        }
        return Constant.measureModeNames[(int)mode];
    }

    internal static bool measureModeSizeIsExactAndMatchesOldMeasuredSize(MeasureMode sizeMode, float size, float lastComputedSize)
    {
        return sizeMode == MeasureMode.Exactly && FloatsEqual(size, lastComputedSize);
    }

    internal static bool measureModeOldSizeIsUnspecifiedAndStillFits(MeasureMode sizeMode, float size, MeasureMode lastSizeMode, float lastComputedSize)
    {
        return sizeMode == MeasureMode.AtMost && lastSizeMode == MeasureMode.Undefined &&
            (size >= lastComputedSize || FloatsEqual(size, lastComputedSize));
    }

    internal static bool measureModeNewMeasureSizeIsStricterAndStillValid(MeasureMode sizeMode, float size, MeasureMode lastSizeMode, float lastSize, float lastComputedSize)
    {
        return lastSizeMode == MeasureMode.AtMost && sizeMode == MeasureMode.AtMost &&
            lastSize > size && (lastComputedSize <= size || FloatsEqual(size, lastComputedSize));
    }


    // nodeCanUseCachedMeasurement returns true if can use cached measurement
    internal static bool nodeCanUseCachedMeasurement(MeasureMode widthMode, float width, MeasureMode heightMode, float height, MeasureMode lastWidthMode, float lastWidth, MeasureMode lastHeightMode, float lastHeight, float lastComputedWidth, float lastComputedHeight, float marginRow, float marginColumn, Config config)
    {
        if (lastComputedHeight < 0 || lastComputedWidth < 0)
        {
            return false;
        }
        bool useRoundedComparison = config != null && config.PointScaleFactor != 0;
        float effectiveWidth = width;
        float effectiveHeight = height;
        float effectiveLastWidth = lastWidth;
        float effectiveLastHeight = lastHeight;

        if (useRoundedComparison)
        {
            effectiveWidth = Flex.RoundValueToPixelGrid(width, config.PointScaleFactor, false, false);
            effectiveHeight = Flex.RoundValueToPixelGrid(height, config.PointScaleFactor, false, false);
            effectiveLastWidth = Flex.RoundValueToPixelGrid(lastWidth, config.PointScaleFactor, false, false);
            effectiveLastHeight = Flex.RoundValueToPixelGrid(lastHeight, config.PointScaleFactor, false, false);
        }

        bool hasSameWidthSpec = lastWidthMode == widthMode && FloatsEqual(effectiveLastWidth, effectiveWidth);
        bool hasSameHeightSpec = lastHeightMode == heightMode && FloatsEqual(effectiveLastHeight, effectiveHeight);

        bool widthIsCompatible =
            hasSameWidthSpec
            || measureModeSizeIsExactAndMatchesOldMeasuredSize(widthMode, width - marginRow, lastComputedWidth)
            || measureModeOldSizeIsUnspecifiedAndStillFits(widthMode, width - marginRow, lastWidthMode, lastComputedWidth)
            || measureModeNewMeasureSizeIsStricterAndStillValid(widthMode, width - marginRow, lastWidthMode, lastWidth, lastComputedWidth);

        bool heightIsCompatible =
            hasSameHeightSpec
            || measureModeSizeIsExactAndMatchesOldMeasuredSize(heightMode, height - marginColumn, lastComputedHeight)
            || measureModeOldSizeIsUnspecifiedAndStillFits(heightMode, height - marginColumn, lastHeightMode, lastComputedHeight)
            || measureModeNewMeasureSizeIsStricterAndStillValid(heightMode, height - marginColumn, lastHeightMode, lastHeight, lastComputedHeight);

        return widthIsCompatible && heightIsCompatible;
    }

    internal static int gDepth = 0;
    internal static bool gPrintTree = false;
    internal static bool gPrintChanges = false;
    internal static bool gPrintSkips = false;

    // layoutNodeInternal is a wrapper around the YGNodelayoutImpl function. It determines
    // whether the layout request is redundant and can be skipped.
    //
    // Parameters:
    //  Input parameters are the same as YGNodelayoutImpl (see above)
    //  Return parameter is true if layout was performed, false if skipped
    internal static bool layoutNodeInternal(Node node, float availableWidth, float availableHeight,
        Direction parentDirection, MeasureMode widthMeasureMode,
        MeasureMode heightMeasureMode, float parentWidth, float parentHeight,
        bool performLayout, string reason, Config config)
    {
        var layout = node.nodeLayout;

        gDepth++;

        bool needToVisitNode =
            (node.IsDirty && layout.generationCount != currentGenerationCount) ||
                layout.lastParentDirection != parentDirection;

        if (needToVisitNode)
        {
            // Invalidate the cached results.
            layout.nextCachedMeasurementsIndex = 0;
            layout.cachedLayout.widthMeasureMode = MeasureMode.NeverUsed_1;
            layout.cachedLayout.heightMeasureMode = MeasureMode.NeverUsed_1;
            layout.cachedLayout.computedWidth = -1;
            layout.cachedLayout.computedHeight = -1;
        }

        CachedMeasurement cachedResults = null;

        // Determine whether the results are already cached. We maintain a separate
        // cache for layouts and measurements. A layout operation modifies the
        // positions
        // and dimensions for nodes in the subtree. The algorithm assumes that each
        // node
        // gets layed out a maximum of one time per tree layout, but multiple
        // measurements
        // may be required to resolve all of the flex dimensions.
        // We handle nodes with measure functions specially here because they are the
        // most
        // expensive to measure, so it's worth avoiding redundant measurements if at
        // all possible.
        if (node.measureFunc != null)
        {
            float marginAxisRow = nodeMarginForAxis(node, FlexDirection.Row, parentWidth);
            float marginAxisColumn = nodeMarginForAxis(node, FlexDirection.Column, parentWidth);

            // First, try to use the layout cache.
            if (nodeCanUseCachedMeasurement(widthMeasureMode,
                availableWidth,
                heightMeasureMode,
                availableHeight,
                layout.cachedLayout.widthMeasureMode,
                layout.cachedLayout.availableWidth,
                layout.cachedLayout.heightMeasureMode,
                layout.cachedLayout.availableHeight,
                layout.cachedLayout.computedWidth,
                layout.cachedLayout.computedHeight,
                marginAxisRow,
                marginAxisColumn,
                config))
            {
                cachedResults = layout.cachedLayout;
            }
            else
            {
                // Try to use the measurement cache.
                for (int i = 0; i < layout.nextCachedMeasurementsIndex; i++)
                {
                    if (nodeCanUseCachedMeasurement(widthMeasureMode,
                        availableWidth,
                        heightMeasureMode,
                        availableHeight,
                        layout.cachedMeasurements[i].widthMeasureMode,
                        layout.cachedMeasurements[i].availableWidth,
                        layout.cachedMeasurements[i].heightMeasureMode,
                        layout.cachedMeasurements[i].availableHeight,
                        layout.cachedMeasurements[i].computedWidth,
                        layout.cachedMeasurements[i].computedHeight,
                        marginAxisRow,
                        marginAxisColumn,
                        config))
                    {
                        cachedResults = layout.cachedMeasurements[i];
                        break;
                    }
                }
            }
        }
        else if (performLayout)
        {
            if (FloatsEqual(layout.cachedLayout.availableWidth, availableWidth) &&
                FloatsEqual(layout.cachedLayout.availableHeight, availableHeight) &&
                layout.cachedLayout.widthMeasureMode == widthMeasureMode &&
                layout.cachedLayout.heightMeasureMode == heightMeasureMode)
            {
                cachedResults = layout.cachedLayout;
            }
        }
        else
        {
            for (int i = 0; i < layout.nextCachedMeasurementsIndex; i++)
            {
                if (FloatsEqual(layout.cachedMeasurements[i].availableWidth, availableWidth) &&
                    FloatsEqual(layout.cachedMeasurements[i].availableHeight, availableHeight) &&
                    layout.cachedMeasurements[i].widthMeasureMode == widthMeasureMode &&
                    layout.cachedMeasurements[i].heightMeasureMode == heightMeasureMode)
                {
                    cachedResults = layout.cachedMeasurements[i];
                    break;
                }
            }
        }

        if (!needToVisitNode && cachedResults != null)
        {
            layout.measuredDimensions[(int)Dimension.Width] = cachedResults.computedWidth;
            layout.measuredDimensions[(int)Dimension.Height] = cachedResults.computedHeight;

            if (gPrintChanges && gPrintSkips)
            {
                // fmt.Printf("%s%d.{[skipped] ", spacer(gDepth), gDepth);
                System.Console.WriteLine($"{spacer(gDepth)}{gDepth}.{{[skipped]");
                if (node.printFunc != null)
                {
                    node.printFunc(node);
                }
                // fmt.Printf("wm: %s, hm: %s, aw: %f ah: %f => d: (%f, %f) %s\n",
                //     measureModeName(widthMeasureMode, performLayout),
                //     measureModeName(heightMeasureMode, performLayout),
                //     availableWidth,
                //     availableHeight,
                //     cachedResults.computedWidth,
                //     cachedResults.computedHeight,
                //     reason);
                System.Console.WriteLine("wm: {0}, hm: {1}, aw: {2} ah: {3} => d: ({4}, {5}) {6}\n",
                    measureModeName(widthMeasureMode, performLayout),
                    measureModeName(heightMeasureMode, performLayout),
                    availableWidth,
                    availableHeight,
                    cachedResults.computedWidth,
                    cachedResults.computedHeight,
                    reason
                );
            }
        }
        else
        {
            if (gPrintChanges)
            {
                string s = "";
                if (needToVisitNode)
                {
                    s = "*";
                }
                // fmt.Printf("%s%d.{%s", spacer(gDepth), gDepth, s);
                System.Console.WriteLine($"{spacer(gDepth)}{gDepth}.{{{s}");
                if (node.printFunc != null)
                {
                    node.printFunc(node);
                }
                // fmt.Printf("wm: %s, hm: %s, aw: %f ah: %f %s\n",
                //     measureModeName(widthMeasureMode, performLayout),
                //     measureModeName(heightMeasureMode, performLayout),
                //     availableWidth,
                //     availableHeight,
                //     reason);
                System.Console.WriteLine("wm: {0}, hm: {1}, aw: {2} ah: {3} {4}\n",
                    measureModeName(widthMeasureMode, performLayout),
                    measureModeName(heightMeasureMode, performLayout),
                    availableWidth,
                    availableHeight,
                    reason
                );
            }

            nodelayoutImpl(node,
                availableWidth,
                availableHeight,
                parentDirection,
                widthMeasureMode,
                heightMeasureMode,
                parentWidth,
                parentHeight,
                performLayout,
                config);

            if (gPrintChanges)
            {
                string s = "";
                if (needToVisitNode)
                {
                    s = "*";
                }
                // fmt.Printf("%s%d.}%s", spacer(gDepth), gDepth, s);
                System.Console.WriteLine($"{spacer(gDepth)}{gDepth}.}}{s}");
                if (node.printFunc != null)
                {
                    node.printFunc(node);
                }
                // fmt.Printf("wm: %s, hm: %s, d: (%f, %f) %s\n",
                //     measureModeName(widthMeasureMode, performLayout),
                //     measureModeName(heightMeasureMode, performLayout),
                //     layout.measuredDimensions[Dimension.Width],
                //     layout.measuredDimensions[Dimension.Height],
                //     reason);
                System.Console.WriteLine("wm: {0}, hm: {1}, d: ({2}, {3}) {4}\n",
                    measureModeName(widthMeasureMode, performLayout),
                    measureModeName(heightMeasureMode, performLayout),
                    layout.measuredDimensions[(int)Dimension.Width],
                    layout.measuredDimensions[(int)Dimension.Height],
                    reason
                );
            }

            layout.lastParentDirection = parentDirection;

            if (cachedResults == null)
            {
                if (layout.nextCachedMeasurementsIndex == Constant.MaxCachedResultCount)
                {
                    if (gPrintChanges)
                    {
                        System.Console.WriteLine("Out of cache entries!\n");
                    }
                    layout.nextCachedMeasurementsIndex = 0;
                }

                CachedMeasurement newCacheEntry = null;
                if (performLayout)
                {
                    // Use the single layout cache entry.
                    newCacheEntry = layout.cachedLayout;
                }
                else
                {
                    // Allocate a new measurement cache entry.
                    newCacheEntry = layout.cachedMeasurements[layout.nextCachedMeasurementsIndex];
                    layout.nextCachedMeasurementsIndex++;
                }

                newCacheEntry.availableWidth = availableWidth;
                newCacheEntry.availableHeight = availableHeight;
                newCacheEntry.widthMeasureMode = widthMeasureMode;
                newCacheEntry.heightMeasureMode = heightMeasureMode;
                newCacheEntry.computedWidth = layout.measuredDimensions[(int)Dimension.Width];
                newCacheEntry.computedHeight = layout.measuredDimensions[(int)Dimension.Height];
            }
        }

        if (performLayout)
        {
            node.nodeLayout.Dimensions[(int)Dimension.Width] = node.nodeLayout.measuredDimensions[(int)Dimension.Width];
            node.nodeLayout.Dimensions[(int)Dimension.Height] = node.nodeLayout.measuredDimensions[(int)Dimension.Height];
            node.hasNewLayout = true;
            node.IsDirty = false;
        }

        gDepth--;
        layout.generationCount = currentGenerationCount;
        return needToVisitNode || cachedResults == null;
    }

    internal static void roundToPixelGrid(Node node, float pointScaleFactor, float absoluteLeft, float absoluteTop)
    {
        if (pointScaleFactor == 0.0)
        {
            return;
        }

        float nodeLeft = node.nodeLayout.Position[(int)Edge.Left];
        float nodeTop = node.nodeLayout.Position[(int)Edge.Top];

        float nodeWidth = node.nodeLayout.Dimensions[(int)Dimension.Width];
        float nodeHeight = node.nodeLayout.Dimensions[(int)Dimension.Height];

        float absoluteNodeLeft = absoluteLeft + nodeLeft;
        float absoluteNodeTop = absoluteTop + nodeTop;

        float absoluteNodeRight = absoluteNodeLeft + nodeWidth;
        float absoluteNodeBottom = absoluteNodeTop + nodeHeight;

        // If a node has a custom measure function we never want to round down its size as this could
        // lead to unwanted text truncation.
        bool textRounding = node.NodeType == NodeType.Text;

        node.nodeLayout.Position[(int)Edge.Left] = Flex.RoundValueToPixelGrid(nodeLeft, pointScaleFactor, false, textRounding);
        node.nodeLayout.Position[(int)Edge.Top] = Flex.RoundValueToPixelGrid(nodeTop, pointScaleFactor, false, textRounding);

        // We multiply dimension by scale factor and if the result is close to the whole number, we don't have any fraction
        // To verify if the result is close to whole number we want to check both floor and ceil numbers
        bool hasFractionalWidth = !FloatsEqual(fmodf(nodeWidth * pointScaleFactor, 1), 0) &&
            !FloatsEqual(fmodf(nodeWidth * pointScaleFactor, 1), 1);
        bool hasFractionalHeight = !FloatsEqual(fmodf(nodeHeight * pointScaleFactor, 1), 0) &&
            !FloatsEqual(fmodf(nodeHeight * pointScaleFactor, 1), 1);

        node.nodeLayout.Dimensions[(int)Dimension.Width] =
            Flex.RoundValueToPixelGrid(
                absoluteNodeRight,
                pointScaleFactor,
                (textRounding && hasFractionalWidth),
                (textRounding && !hasFractionalWidth)) -
                Flex.RoundValueToPixelGrid(absoluteNodeLeft, pointScaleFactor, false, textRounding);
        node.nodeLayout.Dimensions[(int)Dimension.Height] =
            Flex.RoundValueToPixelGrid(
                absoluteNodeBottom,
                pointScaleFactor,
                (textRounding && hasFractionalHeight),
                (textRounding && !hasFractionalHeight)) -
                Flex.RoundValueToPixelGrid(absoluteNodeTop, pointScaleFactor, false, textRounding);

        foreach (var child in node.Children)
        {
            roundToPixelGrid(child, pointScaleFactor, absoluteNodeLeft, absoluteNodeTop);
        }
    }

    internal static void calcStartWidth(Node node, float parentWidth, out float out_width, out MeasureMode out_measureMode)
    {
        if (nodeIsStyleDimDefined(node, FlexDirection.Row, parentWidth))
        {
            float width = resolveValue(node.resolvedDimensions[(int)dim[(int)FlexDirection.Row]], parentWidth);
            float margin = nodeMarginForAxis(node, FlexDirection.Row, parentWidth);
            out_width = width + margin;
            out_measureMode = MeasureMode.Exactly;
            return;
        }
        if (resolveValue(node.nodeStyle.MaxDimensions[(int)Dimension.Width], parentWidth) >= 0f)
        {
            out_width = resolveValue(node.nodeStyle.MaxDimensions[(int)Dimension.Width], parentWidth);
            out_measureMode = MeasureMode.AtMost;
            return;
        }

        {
            float width = parentWidth;
            var widthMeasureMode = MeasureMode.Exactly;
            if (FloatIsUndefined(width))
            {
                widthMeasureMode = MeasureMode.Undefined;
            }
            out_width = width;
            out_measureMode = widthMeasureMode;
            return;
        }
    }
    internal static void calcStartHeight(Node node, float parentWidth, float parentHeight,
        out float out_height, out MeasureMode out_measureMode)
    {
        if (nodeIsStyleDimDefined(node, FlexDirection.Column, parentHeight))
        {
            float height = resolveValue(node.resolvedDimensions[(int)dim[(int)FlexDirection.Column]], parentHeight);
            float margin = nodeMarginForAxis(node, FlexDirection.Column, parentWidth);
            out_height = height + margin;
            out_measureMode = MeasureMode.Exactly;
            return;
        }
        if (resolveValue(node.nodeStyle.MaxDimensions[(int)Dimension.Height], parentHeight) >= 0)
        {
            out_height = resolveValue(node.nodeStyle.MaxDimensions[(int)Dimension.Height], parentHeight);
            out_measureMode = MeasureMode.AtMost;
            return;
        }

        {

            float height = parentHeight;
            var heightMeasureMode = MeasureMode.Exactly;
            if (FloatIsUndefined(height))
            {
                heightMeasureMode = MeasureMode.Undefined;
            }
            out_height = height;
            out_measureMode = heightMeasureMode;
            return;
        }
    }



    internal static void log(Node node, LogLevel level, string format, params object[] args)
    {
        System.Console.WriteLine(format, args);
    }

    internal static void assertCond(bool cond, string format, params object[] args)
    {
        if (!cond)
        {
            throw new System.Exception(string.Format(format, args));
        }
    }

    internal static void assertWithNode(Node node, bool cond, string format, params object[] args)
    {
        assertCond(cond, format, args);
    }


    internal static float fmodf(float a, float b)
    {
        return a % b;
    }
    static internal float fmaxf(float a, float b)
    {
        if (float.IsNaN(a))
        {
            return b;
        }
        if (float.IsNaN(b))
        {
            return a;
        }
        // TODO: signed zeros
        if (a > b)
        {
            return a;
        }
        return b;
    }
    static internal float fminf(float a, float b)
    {
        if (float.IsNaN(a))
        {
            return b;
        }
        if (float.IsNaN(b))
        {
            return a;
        }
        // TODO: signed zeros
        if (a < b)
        {
            return a;
        }
        return b;
    }
}
