using BetaSharp.Client.Guis.Layout;
using BetaSharp.Client.Rendering.Core;

namespace BetaSharp.Client.Guis.Controls;

public class Grid : Control
{
    public static readonly Property<int> ColumnProperty = new();
    public static readonly Property<int> RowProperty = new();
    public static void SetColumn(Control control, int column) => control.SetProperty(ColumnProperty, column);
    public static void SetRow(Control control, int row) => control.SetProperty(RowProperty, row);

    private readonly List<(GridUnit Unit, double Size)> _columns = [];
    private readonly List<(GridUnit Unit, double Size)> _rows = [];

    private readonly List<double> _calculatedColumnSizes = [];
    private readonly List<double> _calculatedRowSizes = [];

    public Grid(int x, int y, int width, int height) : base(x, y, width, height) { }

    protected override Point GetChildPositionOffset(Control child)
    {
        child.TryGetProperty(ColumnProperty, out int column);
        child.TryGetProperty(RowProperty, out int row);

        double offsetX = _calculatedColumnSizes.Take(column).Sum();
        double offsetY = _calculatedRowSizes.Take(row).Sum();
        return new((int)offsetX + Padding.Left, (int)offsetY + Padding.Top);
    }

    protected override void AfterLayoutChildren()
    {
        RecalculateSizes();
    }

    protected override void BeforeLayoutChildren()
    {
        RecalculateSizes();
    }

    protected override void BeforeChildParentSet(Control imminentChild)
    {
        // We need to make sure any auto-sized columns/rows have their sizes set before the child is added, otherwise
        // the child won't be able to measure itself correctly (the _x/_yRatio fields in particular will divide by zero)
        RecalculateSizes();
    }

    protected override Size GetChildLayoutBounds(Control child)
    {
        child.TryGetProperty(ColumnProperty, out int column);
        child.TryGetProperty(RowProperty, out int row);

        double cellWidth = _calculatedColumnSizes.ElementAtOrDefault(column);
        double cellHeight = _calculatedRowSizes.ElementAtOrDefault(row);
        return new((int)cellWidth, (int)cellHeight);
    }

    public override Rect GetDevToolsHighlightAt(Point point)
    {
        int left = (int)_calculatedColumnSizes.TakeWhile(c => c <= point.X).Sum();
        int top = (int)_calculatedRowSizes.TakeWhile(r => r <= point.Y).Sum();
        int column = _calculatedColumnSizes.TakeWhile(c => c <= point.X).Count();
        int row = _calculatedRowSizes.TakeWhile(r => r <= point.Y).Count();
        double width = _calculatedColumnSizes.ElementAtOrDefault(column);
        double height = _calculatedRowSizes.ElementAtOrDefault(row);
        return new(left, top, (int)width, (int)height);
    }

    public void AddColumn(GridUnit unit, double size)
    {
        _columns.Add((unit, size));
        RecalculateSizes();
    }
    public void AddRow(GridUnit unit, double size)
    {
        _rows.Add((unit, size));
        RecalculateSizes();
    }

    private void RecalculateSizes()
    {
        _calculatedColumnSizes.Clear();
        _calculatedRowSizes.Clear();

        double totalStarColumns = _columns.Where(c => c.Unit == GridUnit.Star).Sum(c => c.Size);
        double totalStarRows = _rows.Where(r => r.Unit == GridUnit.Star).Sum(r => r.Size);

        double availableWidth = EffectiveWidth;
        var nonStarColumns = _columns.Where(c => c.Unit != GridUnit.Star).ToList();
        Dictionary<int, double> autoColumnSizes = new();
        for (int i = 0; i < nonStarColumns.Count; i++)
        {
            (GridUnit unit, double size) = nonStarColumns[i];
            if (unit == GridUnit.Pixel)
                availableWidth -= size;
            else if (unit == GridUnit.Auto)
                availableWidth -= autoColumnSizes[i] = CalculateAutoSizeForColumn(i);
        }

        double availableHeight = EffectiveHeight;
        var nonStarRows = _rows.Where(r => r.Unit != GridUnit.Star).ToList();
        Dictionary<int, double> autoRowSizes = new();
        for (int i = 0; i < nonStarRows.Count; i++)
        {
            (GridUnit unit, double size) = nonStarRows[i];
            if (unit == GridUnit.Pixel)
                availableHeight -= size;
            else if (unit == GridUnit.Auto)
                availableHeight -= autoRowSizes[i] = CalculateAutoSizeForRow(i);
        }

        for (int i = 0; i < _columns.Count; i++)
        {
            (GridUnit unit, double size) = _columns[i];
            _calculatedColumnSizes.Add(unit switch
            {
                GridUnit.Pixel => size,
                GridUnit.Auto => autoColumnSizes[i],
                GridUnit.Star => (size / totalStarColumns) * availableWidth,
                _ => throw new InvalidOperationException("Invalid unit kind"),
            });
        }

        for (int i = 0; i < _rows.Count; i++)
        {
            (GridUnit unit, double size) = _rows[i];
            _calculatedRowSizes.Add(unit switch
            {
                GridUnit.Pixel => size,
                GridUnit.Auto => autoRowSizes[i],
                GridUnit.Star => (size / totalStarRows) * availableHeight,
                _ => throw new InvalidOperationException("Invalid unit kind"),
            });
        }
    }

    private double CalculateAutoSizeForColumn(int columnIndex)
    {
        double maxSize = 0;
        foreach (Control child in Children)
        {
            child.TryGetProperty(ColumnProperty, out int col);
            if (col == columnIndex)
            {
                maxSize = Math.Max(maxSize, child.EffectiveWidth + child.EffectiveX);
            }
        }
        return maxSize;
    }

    private double CalculateAutoSizeForRow(int rowIndex)
    {
        double maxSize = 0;
        foreach (Control child in Children)
        {
            child.TryGetProperty(RowProperty, out int row);
            if (row == rowIndex)
            {
                maxSize = Math.Max(maxSize, child.EffectiveHeight + child.EffectiveY);
            }
        }
        return maxSize;
    }
}

public enum GridUnit
{
    Pixel,
    Auto,
    Star,
}
