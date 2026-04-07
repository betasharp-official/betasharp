using BetaSharp.Client.Guis;
using BetaSharp.Client.Input;
using BetaSharp.Client.Rendering;
using BetaSharp.Client.UI.Rendering;
using Microsoft.Extensions.Logging;
using Silk.NET.Core;
using Silk.NET.GLFW;

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

            ResetSelection();
        }
    }

    public string Placeholder { get; set; } = "";
    public int MaxLength { get; set; } = 32;
    public int CursorPosition { get; set; } = 0;
    public Action<string>? OnTextChanged;
    public Action? OnSubmit;

    public int SelectionStart { get; set; } = 0;
    public int SelectionEnd { get; set; } = 0;
    public bool HasSelection => SelectionStart != SelectionEnd;

    private bool _control = false;
    private bool _down = false;

    private int realSelStart => SelectionStart > SelectionEnd ? SelectionEnd : SelectionStart;
    private int realSelEnd => SelectionEnd < SelectionStart ? SelectionStart : SelectionEnd;

    public override List<string> GetInspectorProperties()
    {
        List<string> props = base.GetInspectorProperties();
        props.Add($"Text:     \"{_text}\"");
        props.Add($"Placeholder: \"{Placeholder}\"");
        props.Add($"MaxLength: {MaxLength}   Cursor: {CursorPosition}");
        return props;
    }

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
                _down = true;

                CursorPosition = GetIndexFromCursorX(e.MouseX - (int)ComputedX - 4, e.Renderer.TextRenderer);
                ResetSelection();
                _cursorCounter = 0;
            }
        };

        OnMouseUp += (e) =>
        {
            if (e.Button == MouseButton.Left)
            {
                e.Handled = false;
                _down = false;
            }
        };

        OnMouseMove += (e) =>
        {
            if (_down)
            {
                CursorPosition = GetIndexFromCursorX(e.MouseX - (int)ComputedX - 4, e.Renderer.TextRenderer);
                _cursorCounter = 0;

                SelectionEnd = CursorPosition;
            }
        };

        OnKeyDown += KeyDown;
    }

    private void KeyDown(UIKeyEvent e)
    {
        if (!IsFocused || !e.IsDown) return;

        void Key()
        {
            if (Keyboard.isKeyDown(Keyboard.KEY_LCONTROL))
            {
                switch (e.KeyCode)
                {
                    case Keyboard.KEY_V:
                        string clip = Keyboard.GetClipboardText();
                        int allowed = 0;

                        if (HasSelection) allowed = MaxLength - Text.Length + (realSelEnd - realSelStart);
                        else allowed = MaxLength - Text.Length;

                        if (allowed > 0)
                        {
                            if (HasSelection)
                            {
                                ReplaceSelection(clip);
                            }
                            else
                            {
                                _text = _text.Insert(CursorPosition, clip);
                                CursorPosition += clip.Length;
                                OnTextChanged?.Invoke(_text);
                            }
                        }

                        break;
                    case Keyboard.KEY_A:
                        SelectionStart = 0;
                        SelectionEnd = Text.Length;
                        CursorPosition = Text.Length;

                        break;
                    case Keyboard.KEY_C:
                        if (HasSelection)
                        {
                            ReadOnlySpan<char> selection = Selection;
                            Keyboard.SetClipboardText(selection.ToString());
                        }
                        break;
                    case Keyboard.KEY_X:
                        if (HasSelection)
                        {
                            ReadOnlySpan<char> selection = Selection;
                            Keyboard.SetClipboardText(selection.ToString());
                            DeleteSelection();
                            CursorPosition = realSelStart;
                            ResetSelection();
                        }
                        break;
                }
                return;
            }
            else if (e.KeyChar >= 32 && e.KeyChar != 127 && _text.Length < MaxLength)
            {
                if (HasSelection)
                {
                    ReplaceSelection(e.KeyChar.ToString());
                }
                else
                {
                    _text = _text.Insert(CursorPosition, e.KeyChar.ToString());
                    CursorPosition++;
                    OnTextChanged?.Invoke(_text);
                }
            }
        }

        if (HasSelection &&
            (e.KeyCode == Keyboard.KEY_BACK || e.KeyCode == Keyboard.KEY_DELETE))
        {
            DeleteSelection();
            CursorPosition = realSelStart;
            ResetSelection();
            return;
        }

        switch (e.KeyCode)
        {
            case Keyboard.KEY_BACK:
                _text = _text.Remove(CursorPosition - 1, 1);
                CursorPosition--;
                OnTextChanged?.Invoke(_text);
                break;

            case Keyboard.KEY_DELETE:
                _text = _text.Remove(CursorPosition, 1);
                OnTextChanged?.Invoke(_text);
                break;

            case Keyboard.KEY_LEFT:
                if (HasSelection)
                {
                    CursorPosition = realSelStart;
                    ResetSelection();
                }
                else if (CursorPosition > 0)
                    CursorPosition--;
                break;

            case Keyboard.KEY_RIGHT:
                if (HasSelection)
                {
                    CursorPosition = realSelEnd;
                    ResetSelection();
                }
                else if (CursorPosition < _text.Length)
                    CursorPosition++;
                break;

            case Keyboard.KEY_HOME:
                if (HasSelection) ResetSelection();
                CursorPosition = 0;
                break;

            case Keyboard.KEY_END:
                if (HasSelection) ResetSelection();
                CursorPosition = _text.Length;
                break;

            case Keyboard.KEY_RETURN:
                if (HasSelection) ResetSelection();
                OnSubmit?.Invoke();
                break;

            default:
                Key();
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
            if (HasSelection)
            {
                ReadOnlySpan<char> before = Text.AsSpan(0, realSelStart);
                ReadOnlySpan<char> selection = Selection;

                renderer.DrawRect(
                    renderer.TextRenderer.GetStringWidth(before) + 4,
                    ComputedHeight / 2 - 5,
                    renderer.TextRenderer.GetStringWidth(selection),
                    10,
                    Color.Blue
                );
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

    private int GetIndexFromCursorX(int mouseX, TextRenderer render)
    {
        if (Text.Length == 0) return 0;
        int currentX = 0;

        for (int i = 0; i < Text.Length; i++)
        {
            int width = render.GetStringWidth(Text[i].ToString());
            currentX += width;

            if (currentX >= mouseX) {
                int diff = currentX - mouseX;
                if (diff > width / 2) return i;
                else return i + 1;
            }
        }

        return Text.Length;
    }

    private void ResetSelection()
    {
        SelectionStart = CursorPosition;
        SelectionEnd = CursorPosition;
    }

    private void DeleteSelection()
    {
        int length = realSelEnd - realSelStart;
        _text = _text.Remove(realSelStart, length);
    }

    private void ReplaceSelection(string replacement)
    {
        DeleteSelection();
        _text = _text.Insert(realSelStart, replacement);
        CursorPosition = realSelStart + replacement.Length;
        ResetSelection();
        OnTextChanged?.Invoke(_text);
    }

    private ReadOnlySpan<char> Selection => Text.AsSpan(realSelStart, realSelEnd - realSelStart);
}
