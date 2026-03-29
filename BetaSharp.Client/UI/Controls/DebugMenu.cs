using BetaSharp.Client.Debug;
using BetaSharp.Client.UI.Layout.Flexbox;

namespace BetaSharp.Client.UI.Controls;

public class DebugMenu : UIElement
{
    private readonly BetaSharp _game;
    private readonly UIElement _leftColumn;
    private readonly UIElement _rightColumn;

    public DebugMenu(BetaSharp game)
    {
        _game = game;

        Style.FlexDirection = FlexDirection.Row;
        Style.JustifyContent = Justify.SpaceBetween;

        _leftColumn = new UIElement();
        _leftColumn.Style.FlexDirection = FlexDirection.Column;
        _leftColumn.Style.AlignItems = Align.FlexStart;
        AddChild(_leftColumn);

        _rightColumn = new UIElement();
        _rightColumn.Style.FlexDirection = FlexDirection.Column;
        _rightColumn.Style.AlignItems = Align.FlexEnd;
        AddChild(_rightColumn);
    }

    public override void Update(float partialTicks)
    {
        while (_leftColumn.Children.Count > 0)
            _leftColumn.RemoveChild(_leftColumn.Children[0]);
        while (_rightColumn.Children.Count > 0)
            _rightColumn.RemoveChild(_rightColumn.Children[0]);

        if (_game.options.ShowDebugInfo && _game.player != null && _game.world != null)
        {
            _leftColumn.Style.MarginTop = BetaSharp.hasPaidCheckTime > 0L ? 34 : 2;
            _rightColumn.Style.MarginTop = 2;

            DebugContext ctx = _game.componentsStorage.Overlay.Context;
            ctx.GCMonitor.AllowUpdating = true;

            foreach (DebugComponent component in _game.componentsStorage.Overlay.Components)
            {
                UIElement column = component.Right ? _rightColumn : _leftColumn;
                foreach (DebugRowData row in component.GetRows(ctx))
                {
                    column.AddChild(row.IsSpacer ? DebugRow.Spacer() : new DebugRow(row.Text!, row.TextColor));
                }
            }
        }

        base.Update(partialTicks);
    }
}
