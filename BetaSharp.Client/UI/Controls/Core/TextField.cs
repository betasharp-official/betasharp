using BetaSharp.Client.Guis;
using BetaSharp.Client.Input;
using BetaSharp.Client.UI.Rendering;

namespace BetaSharp.Client.UI.Controls.Core;

public class TextField : UIElement
{
    private string _text = "";
    public string Text
    {
        get => _text;
        set
        {
            _text = value ?? "";
            if (_text.Length > MaxLength) _text = _text[..MaxLength];
            CursorPosition = _text.Length;
        }
    }

    public string Placeholder { get; set; } = "";
    public int MaxLength { get; set; } = 32;
    public int CursorPosition { get; set; } = 0;
    public Action<string>? OnTextChanged;
    public Action? OnSubmit;

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
                //TODO: ADD CURSOR SUPPORT
            }
        };

        OnKeyDown += (e) =>
        {
            if (!IsFocused || !e.IsDown) return;

            switch (e.KeyCode)
            {
                case Keyboard.KEY_BACK:
                    if (CursorPosition > 0 && _text.Length > 0)
                    {
                        _text = _text.Remove(CursorPosition - 1, 1);
                        CursorPosition--;
                        OnTextChanged?.Invoke(_text);
                    }
                    break;

                case Keyboard.KEY_DELETE:
                    if (CursorPosition < _text.Length)
                    {
                        _text = _text.Remove(CursorPosition, 1);
                        OnTextChanged?.Invoke(_text);
                    }
                    break;

                case Keyboard.KEY_LEFT:
                    if (CursorPosition > 0)
                        CursorPosition--;
                    break;

                case Keyboard.KEY_RIGHT:
                    if (CursorPosition < _text.Length)
                        CursorPosition++;
                    break;

                case Keyboard.KEY_HOME:
                    CursorPosition = 0;
                    break;

                case Keyboard.KEY_END:
                    CursorPosition = _text.Length;
                    break;

                case Keyboard.KEY_RETURN:
                    OnSubmit?.Invoke();
                    break;

                default:
                    if (e.KeyChar >= 32 && e.KeyChar != 127 && _text.Length < MaxLength)
                    {
                        _text = _text.Insert(CursorPosition, e.KeyChar.ToString());
                        CursorPosition++;
                        OnTextChanged?.Invoke(_text);
                    }
                    break;
            }

            e.Handled = true;
        };
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
        renderer.DrawRect(0, 0, ComputedWidth, ComputedHeight, Color.Black);

        Color borderColor = IsFocused ? Color.White : (IsHovered ? Color.GrayCC : Color.GrayA0);
        renderer.DrawRect(0, 0, ComputedWidth, 1, borderColor);
        renderer.DrawRect(0, ComputedHeight - 1, ComputedWidth, 1, borderColor);
        renderer.DrawRect(0, 0, 1, ComputedHeight, borderColor);
        renderer.DrawRect(ComputedWidth - 1, 0, 1, ComputedHeight, borderColor);

        if (string.IsNullOrEmpty(_text) && !IsFocused)
        {
            renderer.DrawText(Placeholder, 4, ComputedHeight / 2 - 4, Color.Gray70);
        }
        else
        {
            renderer.DrawText(_text, 4, ComputedHeight / 2 - 4, Color.White);

            if (IsFocused && _cursorCounter / 10 % 2 == 0)
            {
                int cursorX = 4 + renderer.TextRenderer.GetStringWidth(_text.AsSpan(0, CursorPosition));
                renderer.DrawRect(cursorX, ComputedHeight / 2 - 5, 1, 10, Color.White);
            }
        }

        base.Render(renderer);
    }
}
