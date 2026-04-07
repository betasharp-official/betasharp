using BetaSharp.Client.Guis;
using BetaSharp.Client.Input;
using BetaSharp.Client.Rendering;
using BetaSharp.Client.UI.Rendering;

namespace BetaSharp.Client.UI.Controls.Core;

public class TextField : UIElement
{
    public string Text
    {
        get => _text;
        set
        {
            _text = value ?? "";
            if (_text.Length > MaxLength) _text = _text[..MaxLength];
            CursorPosition = _text.Length;
            SelectionStart = CursorPosition;
        }
    }

    public string Placeholder { get; set; } = "";
    public int MaxLength { get; set; } = 32;
    public int CursorPosition { get; set; } = 0;
    public int SelectionStart { get; set; } = 0;

    public Action<string>? OnTextChanged;
    public Action? OnSubmit;

    private bool _isDragging = false;
    private TextRenderer? _textRenderer;
    private int _cursorCounter = 0;
    private string _text = "";

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
                    CursorPosition = GetCursorIndexAt(e.MouseX - ScreenX);
                    SelectionStart = CursorPosition;
                    _isDragging = true;
                }
            }
        };

        OnMouseMove += (e) =>
        {
            if (_isDragging && _textRenderer is not null)
            {
                CursorPosition = GetCursorIndexAt(e.MouseX - ScreenX);
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

    private void HandleKeyDown(UIKeyEvent e)
    {
        if (!IsFocused || !e.IsDown) return;

        bool control = Keyboard.isKeyDown(Keyboard.KEY_LCONTROL) || Keyboard.isKeyDown(Keyboard.KEY_RCONTROL) || Keyboard.isKeyDown(Keyboard.KEY_LMETA) || Keyboard.isKeyDown(Keyboard.KEY_RMETA);
        bool shift = Keyboard.isKeyDown(Keyboard.KEY_LSHIFT) || Keyboard.isKeyDown(Keyboard.KEY_RSHIFT);

        switch (e.KeyCode)
        {
            case Keyboard.KEY_A:
                if (control)
                {
                    SelectionStart = 0;
                    CursorPosition = _text.Length;
                    e.Handled = true;
                    return;
                }
                goto default;

            case Keyboard.KEY_C:
                if (control)
                {
                    if (SelectionStart != CursorPosition)
                    {
                        int start = Math.Min(SelectionStart, CursorPosition);
                        int length = Math.Abs(SelectionStart - CursorPosition);
                        Display.SetClipboardString(_text.Substring(start, length));
                    }
                    e.Handled = true;
                    return;
                }
                goto default;

            case Keyboard.KEY_X:
                if (control)
                {
                    if (SelectionStart != CursorPosition)
                    {
                        int start = Math.Min(SelectionStart, CursorPosition);
                        int length = Math.Abs(SelectionStart - CursorPosition);
                        Display.SetClipboardString(_text.Substring(start, length));
                        DeleteSelection();
                    }
                    e.Handled = true;
                    return;
                }
                goto default;

            case Keyboard.KEY_V:
                if (control)
                {
                    string clipboardText = Display.GetClipboardString();
                    if (!string.IsNullOrEmpty(clipboardText))
                    {
                        InsertText(clipboardText);
                    }
                    e.Handled = true;
                    return;
                }
                goto default;

            case Keyboard.KEY_BACK:
                if (SelectionStart != CursorPosition)
                {
                    DeleteSelection();
                }
                else if (CursorPosition > 0 && _text.Length > 0)
                {
                    _text = _text.Remove(CursorPosition - 1, 1);
                    CursorPosition--;
                    SelectionStart = CursorPosition;
                    OnTextChanged?.Invoke(_text);
                }
                break;

            case Keyboard.KEY_DELETE:
                if (SelectionStart != CursorPosition)
                {
                    DeleteSelection();
                }
                else if (CursorPosition < _text.Length)
                {
                    _text = _text.Remove(CursorPosition, 1);
                    OnTextChanged?.Invoke(_text);
                }
                break;

            case Keyboard.KEY_LEFT:
                if (CursorPosition > 0)
                {
                    CursorPosition--;
                }
                if (!shift) SelectionStart = CursorPosition;
                break;

            case Keyboard.KEY_RIGHT:
                if (CursorPosition < _text.Length)
                {
                    CursorPosition++;
                }
                if (!shift) SelectionStart = CursorPosition;
                break;

            case Keyboard.KEY_HOME:
                CursorPosition = 0;
                if (!shift) SelectionStart = CursorPosition;
                break;

            case Keyboard.KEY_END:
                CursorPosition = _text.Length;
                if (!shift) SelectionStart = CursorPosition;
                break;

            case Keyboard.KEY_RETURN:
                OnSubmit?.Invoke();
                break;

            default:
                if (!control && e.KeyChar >= 32 && e.KeyChar != 127)
                {
                    InsertText(e.KeyChar.ToString());
                }
                break;
        }

        e.Handled = true;
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
        }

        base.Update(partialTicks);
    }

    public override void Render(UIRenderer renderer)
    {
        _textRenderer = renderer.TextRenderer;

        DrawBox(renderer);

        if (string.IsNullOrEmpty(_text) && !IsFocused)
        {
            renderer.DrawText(Placeholder, 4, ComputedHeight / 2 - 4, Color.Gray70);
        }
        else
        {
            // Selection Highlight
            if (SelectionStart != CursorPosition)
            {
                DrawSelectionHighlight(renderer);
            }

            renderer.DrawText(_text, 4, ComputedHeight / 2 - 4, Color.White);

            if (IsFocused && _cursorCounter / 10 % 2 == 0)
            {
                int cursorX = 4 + renderer.TextRenderer.GetStringWidth(_text.AsSpan(0, CursorPosition));
                renderer.DrawRect(cursorX, ComputedHeight / 2 - 5, 1, 10, Color.White);
            }
        }

        base.Render(renderer);
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
        int start = Math.Min(SelectionStart, CursorPosition);
        int end = Math.Max(SelectionStart, CursorPosition);
        int x1 = 4 + renderer.TextRenderer.GetStringWidth(_text.AsSpan(0, start));
        int x2 = 4 + renderer.TextRenderer.GetStringWidth(_text.AsSpan(0, end));
        renderer.DrawRect(x1, ComputedHeight / 2 - 5, x2 - x1, 10, new Color(0, 0, 255, 128));
    }

    public override List<string> GetInspectorProperties()
    {
        List<string> props = base.GetInspectorProperties();
        props.Add($"Text:     \"{_text}\"");
        props.Add($"Placeholder: \"{Placeholder}\"");
        props.Add($"MaxLength: {MaxLength}   Cursor: {CursorPosition}  SelectionStart: {SelectionStart}");
        return props;
    }

    private void DeleteSelection()
    {
        if (SelectionStart == CursorPosition) return;

        int start = Math.Min(SelectionStart, CursorPosition);
        int end = Math.Max(SelectionStart, CursorPosition);
        int length = end - start;

        _text = _text.Remove(start, length);
        CursorPosition = start;
        SelectionStart = CursorPosition;
        OnTextChanged?.Invoke(_text);
    }

    private void InsertText(string text)
    {
        DeleteSelection();

        int remainingSpace = MaxLength - _text.Length;
        if (remainingSpace <= 0) return;

        if (text.Length > remainingSpace) text = text[..remainingSpace];

        _text = _text.Insert(CursorPosition, text);
        CursorPosition += text.Length;
        SelectionStart = CursorPosition;
        OnTextChanged?.Invoke(_text);
    }

    private int GetCursorIndexAt(float localX)
    {
        if (_textRenderer == null)
        {
            return 0;
        }

        float xOffset = 4; // Padding
        if (string.IsNullOrEmpty(_text)) return 0;

        int bestIndex = 0;
        float bestDist = float.MaxValue;

        for (int i = 0; i <= _text.Length; i++)
        {
            float width = _textRenderer.GetStringWidth(_text.AsSpan(0, i));
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
