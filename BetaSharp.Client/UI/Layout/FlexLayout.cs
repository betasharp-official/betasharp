using BetaSharp.Client.UI.Layout.Flexbox;

namespace BetaSharp.Client.UI.Layout;

public static class FlexLayout
{
    public struct LayoutContext
    {
        public UIElement Root;
        public float AvailableWidth;
        public float AvailableHeight;
        public Func<string, float> MeasureString;
    }

    public static void ApplyLayout(LayoutContext context)
    {
        Node rootNode = BuildTree(context.Root, context.MeasureString);
        rootNode.CalculateLayout(context.AvailableWidth, context.AvailableHeight, Direction.LTR);
        ApplyResults(rootNode, context.Root, context.MeasureString);
    }

    private static Node BuildTree(UIElement element, Func<string, float> measureString)
    {
        Node node = new();

        // Map constraints
        node.nodeStyle.FlexDirection = element.Style.FlexDirection;
        node.nodeStyle.AlignItems = element.Style.AlignItems;
        node.nodeStyle.AlignSelf = element.Style.AlignSelf;
        node.nodeStyle.JustifyContent = element.Style.JustifyContent;
        node.nodeStyle.FlexWrap = element.Style.FlexWrap;

        node.nodeStyle.FlexGrow = element.Style.FlexGrow;
        node.nodeStyle.FlexShrink = element.Style.FlexShrink;

        if (element.Style.Width.HasValue)
            node.nodeStyle.Dimensions[(int)Dimension.Width] = new Value(element.Style.Width.Value, Unit.Point);

        if (element.Style.Height.HasValue)
            node.nodeStyle.Dimensions[(int)Dimension.Height] = new Value(element.Style.Height.Value, Unit.Point);

        if (element.Style.MaxHeight.HasValue)
            node.nodeStyle.MaxDimensions[(int)Dimension.Height] = new Value(element.Style.MaxHeight.Value, Unit.Point);

        node.nodeStyle.Margin[(int)Edge.Top] = new Value(element.Style.MarginTop, Unit.Point);
        node.nodeStyle.Margin[(int)Edge.Bottom] = new Value(element.Style.MarginBottom, Unit.Point);
        node.nodeStyle.Margin[(int)Edge.Left] = new Value(element.Style.MarginLeft, Unit.Point);
        node.nodeStyle.Margin[(int)Edge.Right] = new Value(element.Style.MarginRight, Unit.Point);

        node.nodeStyle.Padding[(int)Edge.Top] = new Value(element.Style.PaddingTop, Unit.Point);
        node.nodeStyle.Padding[(int)Edge.Bottom] = new Value(element.Style.PaddingBottom, Unit.Point);
        node.nodeStyle.Padding[(int)Edge.Left] = new Value(element.Style.PaddingLeft, Unit.Point);
        node.nodeStyle.Padding[(int)Edge.Right] = new Value(element.Style.PaddingRight, Unit.Point);

        // Map absolute positioning
        node.StyleSetPositionType(element.Style.Position);
        if (element.Style.Top.HasValue) node.StyleSetPosition(Edge.Top, element.Style.Top.Value);
        if (element.Style.Bottom.HasValue) node.StyleSetPosition(Edge.Bottom, element.Style.Bottom.Value);
        if (element.Style.Left.HasValue) node.StyleSetPosition(Edge.Left, element.Style.Left.Value);
        if (element.Style.Right.HasValue) node.StyleSetPosition(Edge.Right, element.Style.Right.Value);

        // Add custom text bounds callbacks for leaves
        if (element.DoTextMeasuring)
        {
            node.SetMeasureFunc((n, w, wm, h, hm) =>
            {
                UIElement.MeasureContext measureContext = new()
                {
                    AvailableWidth = w,
                    AvailableHeight = h,
                    MeasureString = measureString
                };
                element.Measure(measureContext);
                return new Size(element.ComputedWidth, element.ComputedHeight);
            });
        }

        foreach (UIElement child in element.Children)
        {
            node.AddChild(BuildTree(child, measureString));
        }

        return node;
    }

    private static void ApplyResults(Node node, UIElement element, Func<string, float> measureString)
    {
        element.ComputedWidth = node.layout.width;
        element.ComputedHeight = node.layout.height;
        element.ComputedX = node.layout.left;
        element.ComputedY = node.layout.top;

        element.OnLayoutApplied(new() { MeasureString = measureString });

        for (int i = 0; i < element.Children.Count; i++)
        {
            ApplyResults(node.GetChild(i), element.Children[i], measureString);
        }
    }

}
