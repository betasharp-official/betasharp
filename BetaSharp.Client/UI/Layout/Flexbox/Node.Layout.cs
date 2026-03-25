namespace BetaSharp.Client.UI.Layout.Flexbox;

public partial class Node
{
    public struct Layout8D : IEquatable<Layout8D>
    {
        public int left, right, top, bottom;

        //Margin/Border/Padding Edge
        public int x, y, width, height;

        internal Layout8D(int left, int right, int top, int bottom)
        {
            this.left = left;
            this.right = right;
            this.top = top;
            this.bottom = bottom;

            x = 0;
            y = 0;
            width = 0;
            height = 0;

        }
        internal Layout8D(int left, int right, int top, int bottom, int x, int y, int width, int height)
        {
            this.left = left;
            this.right = right;
            this.top = top;
            this.bottom = bottom;

            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        internal void SetInnerEdge(Layout8D layout)
        {
            x = layout.x + layout.left;
            y = layout.y + layout.top;
            width = layout.width - (layout.left + layout.right);
            height = layout.height - (layout.top + layout.bottom);
        }

        internal void SetOuterEdge(Layout8D layout)
        {
            x = layout.x - left;
            y = layout.y - top;
            width = layout.width + (left + right);
            height = layout.height + (top + bottom);
        }

        public override readonly string ToString()
        {
            return string.Format("(x:{0} y:{1} w:{2} h:{3}) (l:{4} t:{5} r:{6} b:{7}))", x, y, width, height, left, top, right, bottom);
        }

        public readonly bool Equals(Layout8D l)
        {
            return x == l.x
                && y == l.y
                && width == l.width
                && height == l.height
                && left == l.left
                && right == l.right
                && top == l.top
                && bottom == l.bottom;
        }
        public static bool operator ==(Layout8D a, Layout8D b) => a.Equals(b);
        public static bool operator !=(Layout8D a, Layout8D b) => !a.Equals(b);
        public override readonly bool Equals(object l) { return base.Equals(l); }
        public override readonly int GetHashCode() { return base.GetHashCode(); }
    }
    public struct Layout : IEquatable<Layout>
    {
        //! remake
        public bool setted;

        public int left, right, top, bottom;
        //Content Edge
        public int x, y, width, height;

        public Layout8D margin;
        public Layout8D border;
        public Layout8D padding;
        public Layout8D content;

        public bool hadOverflow;
        public Direction direction;



        internal Layout(Node node)
        {
            setted = true;
            x = (int)node.LayoutGetX();
            y = (int)node.LayoutGetY();
            left = (int)node.LayoutGetLeft();
            right = (int)node.LayoutGetRight();
            top = (int)node.LayoutGetTop();
            bottom = (int)node.LayoutGetBottom();
            width = (int)node.LayoutGetWidth();
            height = (int)node.LayoutGetHeight();

            // https://yogalayout.com/docs/margins-paddings-borders
            // Padding in Yoga acts as if box-sizing: border-box; was set
            // Border in Yoga acts exactly like padding 
            border = new Layout8D((int)node.LayoutGetBorder(Edge.Left), (int)node.LayoutGetBorder(Edge.Right), (int)node.LayoutGetBorder(Edge.Top), (int)node.LayoutGetBorder(Edge.Bottom),
                x, y, width, height);
            padding = new Layout8D((int)node.LayoutGetPadding(Edge.Left), (int)node.LayoutGetPadding(Edge.Right), (int)node.LayoutGetPadding(Edge.Top), (int)node.LayoutGetPadding(Edge.Bottom));
            padding.SetInnerEdge(border);
            content = new Layout8D(0, 0, 0, 0);
            content.SetInnerEdge(padding);

            margin = new Layout8D((int)node.LayoutGetMargin(Edge.Left), (int)node.LayoutGetMargin(Edge.Right), (int)node.LayoutGetMargin(Edge.Top), (int)node.LayoutGetMargin(Edge.Bottom));
            margin.SetOuterEdge(border);


            hadOverflow = node.LayoutGetHadOverflow();
            direction = node.LayoutGetDirection();
        }

        public override string ToString() { return ToStr(0); }
        public string ToStr(int indent)
        {
            string line = "{\n";
            indent++;
            string tab = new System.String(' ', indent * 2);
            line += tab + "box = " + string.Format("(x:{0} y:{1} w:{2} h:{3}) (l:{4} t:{5} r:{6} b:{7})", x, y, width, height, left, top, right, bottom) + "\n";
            line += tab + "margin = " + margin.ToString() + "\n";
            line += tab + "border = " + border.ToString() + "\n";
            line += tab + "padding = " + padding.ToString() + "\n";
            line += tab + "content = " + content.ToString() + "\n";
            indent--;
            line += new System.String(' ', indent * 2) + "}";
            return line;
        }
        public static bool operator ==(Layout a, Layout b) => a.Equals(b);
        public static bool operator !=(Layout a, Layout b) => !a.Equals(b);
        public readonly bool Equals(Layout l)
        {
            return x == l.x
                 && y == l.y
                 && width == l.width
                 && height == l.height
                 && left == l.left
                 && right == l.right
                 && top == l.top
                 && bottom == l.bottom
                 && margin == l.margin
                 && border == l.border
                 && padding == l.padding
                 && content == l.content
                 && hadOverflow == l.hadOverflow
                 && direction == l.direction;

        }
        public override readonly bool Equals(object l) { return base.Equals(l); }
        public override readonly int GetHashCode() { return base.GetHashCode(); }
    }
}
