using BetaSharp.Client.Debug;
using BetaSharp.Client.Entities;
using BetaSharp.Client.Options;
using BetaSharp.Client.UI.Layout.Flexbox;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Client.UI.Controls;

public class DebugMenu : UIElement
{
    private readonly GameOptions _options;
    private readonly Func<ClientPlayerEntity?> _getPlayer;
    private readonly Func<World?> _getWorld;
    private readonly DebugComponentsStorage _debugStorage;
    private readonly UIElement _leftColumn;
    private readonly UIElement _rightColumn;

    public DebugMenu(
        GameOptions options,
        Func<ClientPlayerEntity?> getPlayer,
        Func<World?> getWorld,
        DebugComponentsStorage debugStorage)
    {
        _options = options;
        _getPlayer = getPlayer;
        _getWorld = getWorld;
        _debugStorage = debugStorage;

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

        DebugContext ctx = _debugStorage.Overlay.Context;
        ctx.GCMonitor.AllowUpdating = _options.ShowDebugInfo;

        if (_options.ShowDebugInfo && _getPlayer() != null && _getWorld() != null)
        {
            _leftColumn.Style.MarginTop = BetaSharp.HasPaidCheckTime > 0L ? 34 : 2;
            _rightColumn.Style.MarginTop = 2;

            foreach (DebugComponent component in _debugStorage.Overlay.Components)
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
