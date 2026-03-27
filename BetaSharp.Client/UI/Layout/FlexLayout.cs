using BetaSharp.Client.UI.Layout.Flexbox;

namespace BetaSharp.Client.UI.Layout;

public static class FlexLayout
{
    public static void ApplyLayout(UIElement root, float availableWidth, float availableHeight)
    {
        Node rootNode = BuildTree(root);
        rootNode.CalculateLayout(availableWidth, availableHeight, Direction.LTR);
        ApplyResults(rootNode, root);
    }

    private static Node BuildTree(UIElement element)
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
        if (element is Controls.Core.Label || element is Controls.Core.Button || element is Controls.Core.TextField || element is Controls.MainMenu.MainMenuSplash)
        {
            node.SetMeasureFunc((n, w, wm, h, hm) =>
            {
                element.Measure(w, h);
                return new Size(element.ComputedWidth, element.ComputedHeight);
            });
        }

        foreach (UIElement child in element.Children)
        {
            node.AddChild(BuildTree(child));
        }

        return node;
    }

    private static void ApplyResults(Node node, UIElement element)
    {
        element.ComputedWidth = node.layout.width;
        element.ComputedHeight = node.layout.height;
        element.ComputedX = node.layout.left;
        element.ComputedY = node.layout.top;

        element.OnLayoutApplied();

        for (int i = 0; i < element.Children.Count; i++)
        {
            ApplyResults(node.GetChild(i), element.Children[i]);
        }
    }

}
