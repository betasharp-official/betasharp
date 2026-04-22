using BetaSharp.Client.Guis;
using BetaSharp.Client.Rendering;
using BetaSharp.Client.UI.Rendering;

namespace BetaSharp.Client.UI.Controls.Core;

public partial class TextField : UIElement
{
    private readonly TextBuffer _buffer = new();

    public string Text
    {
        get => _buffer.Text;
        set => _buffer.Text = value;
    }

    public string Placeholder { get; set; } = "";

    public override bool DoTextMeasuring => true;
    
    public int MaxLength
    {
        get => _buffer.MaxLength;
        set => _buffer.MaxLength = value;
    }

    public int CursorPosition
    {
        get => _buffer.CursorPosition;
        set => _buffer.CursorPosition = value;
    }

    public int SelectionStart
    {
        get => _buffer.SelectionStart;
        set => _buffer.SelectionStart = value;
    }

    public Action<string>? OnTextChanged;
    public Action? OnSubmit;

    private bool _isDragging = false;
    private TextRenderer? _textRenderer;
    private int _cursorCounter = 0;

    public TextField()
    {
        Style.Width = 200;
        Style.Height = 20;

        OnMouseEnter += (_) => IsHovered = true;
        OnMouseLeave += (_) => IsHovered = false;

        OnMouseDown += (e) =>
        {
            if (e.Button == MouseButton.Left)
            {
                e.Handled = true;
                if (_textRenderer is not null)
                {
                    _buffer.MoveTo(GetCursorIndexAt(e.MouseX - ScreenX), false);
                    _isDragging = true;
                }
            }
        };

        OnMouseMove += (e) =>
        {
            if (_isDragging && _textRenderer is not null)
            {
                _buffer.MoveTo(GetCursorIndexAt(e.MouseX - ScreenX), true);
            }
        };

        OnMouseUp += (e) =>
        {
            if (e.Button == MouseButton.Left)
            {
                _isDragging = false;
            }
        };

        OnKeyDown += HandleKeyDown;
    }

    public override void Update(float partialTicks)
    {
        if (IsFocused)
        {
            _cursorCounter++;
        }
        else
        {
            _cursorCounter = 0;
            _buffer.ClearSelection();
        }

        base.Update(partialTicks);
    }

    public override void Render(UIRenderer renderer)
    {
        _textRenderer = renderer.TextRenderer;

        DrawBox(renderer);

        if (string.IsNullOrEmpty(Text) && !IsFocused)
        {
            renderer.DrawText(Placeholder, 4, ComputedHeight / 2 - 4, Color.Gray70);
        }
        else
        {
            // Selection Highlight
            if (_buffer.HasSelection)
            {
                DrawSelectionHighlight(renderer);
            }

            renderer.DrawText(Text, 4, ComputedHeight / 2 - 4, Color.White);

            if (IsFocused && _cursorCounter / 10 % 2 == 0)
            {
                int cursorX = 4 + renderer.TextRenderer.GetStringWidth(Text.AsSpan(0, _buffer.CursorPosition));
                renderer.DrawRect(cursorX, ComputedHeight / 2 - 5, 1, 10, Color.White);
            }
        }

        base.Render(renderer);
    }

    public override List<string> GetInspectorProperties()
    {
        List<string> props = base.GetInspectorProperties();
        props.Add($"Text:     \"{_buffer.Text}\"");
        props.Add($"Placeholder: \"{Placeholder}\"");
        props.Add($"MaxLength: {MaxLength}   Cursor: {CursorPosition}  SelectionStart: {_buffer.SelectionStart}");
        return props;
    }

    private void DrawBox(UIRenderer renderer)
    {
        renderer.DrawRect(0, 0, ComputedWidth, ComputedHeight, Color.Black);

        Color borderColor = IsFocused ? Color.White : (IsHovered ? Color.GrayCC : Color.GrayA0);
        renderer.DrawRect(0, 0, ComputedWidth, 1, borderColor);
        renderer.DrawRect(0, ComputedHeight - 1, ComputedWidth, 1, borderColor);
        renderer.DrawRect(0, 0, 1, ComputedHeight, borderColor);
        renderer.DrawRect(ComputedWidth - 1, 0, 1, ComputedHeight, borderColor);
    }

    private void DrawSelectionHighlight(UIRenderer renderer)
    {
        int start = Math.Min(_buffer.SelectionStart, _buffer.CursorPosition);
        int end = Math.Max(_buffer.SelectionStart, _buffer.CursorPosition);
        int x1 = 4 + renderer.TextRenderer.GetStringWidth(Text.AsSpan(0, start));
        int x2 = 4 + renderer.TextRenderer.GetStringWidth(Text.AsSpan(0, end));
        renderer.DrawRect(x1, ComputedHeight / 2 - 5, x2 - x1, 10, new Color(0, 0, 255, 128));
    }

    private int GetCursorIndexAt(float localX)
    {
        if (_textRenderer == null)
        {
            return 0;
        }

        float xOffset = 4; // Padding
        if (string.IsNullOrEmpty(Text)) return 0;

        int bestIndex = 0;
        float bestDist = float.MaxValue;

        for (int i = 0; i <= Text.Length; i++)
        {
            float width = _textRenderer.GetStringWidth(Text.AsSpan(0, i));
            float dist = MathF.Abs(xOffset + width - localX);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestIndex = i;
            }
        }

        return bestIndex;
    }
}
