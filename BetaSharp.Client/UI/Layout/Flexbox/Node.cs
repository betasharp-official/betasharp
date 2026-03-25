namespace BetaSharp.Client.UI.Layout.Flexbox;

public partial class Node
{
    public Style nodeStyle = new();
    readonly internal Flex.Layout nodeLayout = new();
    internal int lineIndex;

    private Layout? _layout = null;
    public Layout layout
    {
        get
        {
            if (_layout == null)
                _layout = new Layout(this);
            return (Layout)_layout;
        }
    }

    internal Node Parent = null;
    internal readonly List<Node> Children = new();

    public int ChildrenCount { get { return Children.Count; } }

    public Node firstChild { get { return Children.Count > 0 ? Children.First() : null; } }
    public Node lastChild { get { return Children.Count > 0 ? Children.Last() : null; } }

    internal Node NextChild;

    internal MeasureFunc measureFunc;
    internal BaselineFunc baselineFunc;
    internal PrintFunc printFunc;
    internal Config config = Constant.configDefaults;

    public bool IsDirty
    {
        get;
        internal set;
    }
    internal bool hasNewLayout = true;
    internal NodeType NodeType = NodeType.Default;

    internal readonly Value[] resolvedDimensions = new Value[2] { Flex.ValueUndefined, Flex.ValueUndefined };
    public object Context;

    public Node()
    {

    }

    public Node(Style style)
    {
        nodeStyle = style;
    }


    public void CalculateLayout(float parentWidth, float parentHeight, Direction parentDirection)
    {
        _layout = null;
        Flex.CalculateLayout(this, parentWidth, parentHeight, parentDirection);
    }
    public void MarkAsDirty()
    {
        Flex.nodeMarkDirtyInternal(this);
    }


    #region Layout

    internal float LayoutGetX()
    {
        var x = this.nodeLayout.Position[(int)Edge.Left];
        if (this.Parent != null)
            x += this.Parent.LayoutGetX();
        return x;
    }

    internal float LayoutGetY()
    {
        var y = this.nodeLayout.Position[(int)Edge.Top];
        if (this.Parent != null)
            y += this.Parent.LayoutGetY();
        return y;
    }
    // LayoutGetLeft gets left
    internal float LayoutGetLeft()
    {
        return this.nodeLayout.Position[(int)Edge.Left];
    }

    // LayoutGetTop gets top
    internal float LayoutGetTop()
    {

        return this.nodeLayout.Position[(int)Edge.Top];
    }

    // LayoutGetRight gets right
    internal float LayoutGetRight()
    {
        return this.nodeLayout.Position[(int)Edge.Right];
    }

    // LayoutGetBottom gets bottom
    internal float LayoutGetBottom()
    {
        return this.nodeLayout.Position[(int)Edge.Bottom];
    }

    // LayoutGetWidth gets width
    internal float LayoutGetWidth()
    {
        return this.nodeLayout.Dimensions[(int)Dimension.Width];
    }

    // LayoutGetHeight gets height
    internal float LayoutGetHeight()
    {
        return this.nodeLayout.Dimensions[(int)Dimension.Height];
    }

    // LayoutGetMargin gets margin
    internal float LayoutGetMargin(Edge edge)
    {
        Flex.assertWithNode(this, edge < Edge.End, "Cannot get layout properties of multi-edge shorthands");
        if (edge == Edge.Left)
        {
            if (this.nodeLayout.Direction == Direction.RTL)
            {
                return this.nodeLayout.Margin[(int)Edge.End];
            }
            return this.nodeLayout.Margin[(int)Edge.Start];
        }
        if (edge == Edge.Right)
        {
            if (this.nodeLayout.Direction == Direction.RTL)
            {
                return this.nodeLayout.Margin[(int)Edge.Start];
            }
            return this.nodeLayout.Margin[(int)Edge.End];
        }
        return this.nodeLayout.Margin[(int)edge];
    }

    // LayoutGetBorder gets border
    internal float LayoutGetBorder(Edge edge)
    {
        Flex.assertWithNode(this, edge < Edge.End,
            "Cannot get layout properties of multi-edge shorthands");
        if (edge == Edge.Left)
        {
            if (this.nodeLayout.Direction == Direction.RTL)
            {
                return this.nodeLayout.Border[(int)Edge.End];
            }
            return this.nodeLayout.Border[(int)Edge.Start];
        }
        if (edge == Edge.Right)
        {
            if (this.nodeLayout.Direction == Direction.RTL)
            {
                return this.nodeLayout.Border[(int)Edge.Start];
            }
            return this.nodeLayout.Border[(int)Edge.End];
        }
        return this.nodeLayout.Border[(int)edge];
    }

    // LayoutGetPadding gets padding
    internal float LayoutGetPadding(Edge edge)
    {
        Flex.assertWithNode(this, edge < Edge.End,
            "Cannot get layout properties of multi-edge shorthands");
        if (edge == Edge.Left)
        {
            if (this.nodeLayout.Direction == Direction.RTL)
            {
                return this.nodeLayout.Padding[(int)Edge.End];
            }
            return this.nodeLayout.Padding[(int)Edge.Start];
        }
        if (edge == Edge.Right)
        {
            if (this.nodeLayout.Direction == Direction.RTL)
            {
                return this.nodeLayout.Padding[(int)Edge.Start];
            }
            return this.nodeLayout.Padding[(int)Edge.End];
        }
        return this.nodeLayout.Padding[(int)edge];
    }

    internal Direction LayoutGetDirection()
    {
        return this.nodeLayout.Direction;
    }

    internal bool LayoutGetHadOverflow()
    {
        return this.nodeLayout.HadOverflow;
    }

    #endregion

    #region other props

    public void SetMeasureFunc(MeasureFunc measureFunc)
    {
        Flex.SetMeasureFunc(this, measureFunc);
    }

    public MeasureFunc GetMeasureFunc()
    {
        return this.measureFunc;
    }

    public void SetBaselineFunc(BaselineFunc baselineFunc)
    {
        this.baselineFunc = baselineFunc;
    }

    public BaselineFunc GetBaselineFunc()
    {
        return this.baselineFunc;
    }

    public void SetPrintFunc(PrintFunc printFunc)
    {
        this.printFunc = printFunc;
    }

    public PrintFunc GetPrintFunc()
    {
        return this.printFunc;
    }
    #endregion

    #region tree
    public Node GetChild(int idx)
    {
        return Flex.GetChild(this, idx);
    }
    public void AddChild(Node child)
    {
        Flex.InsertChild(this, child, ChildrenCount);
    }
    public void InsertChild(Node child, int idx)
    {
        Flex.InsertChild(this, child, idx);
    }
    public void RemoveChild(Node child)
    {
        Flex.RemoveChild(this, child);
    }
    #endregion


}
