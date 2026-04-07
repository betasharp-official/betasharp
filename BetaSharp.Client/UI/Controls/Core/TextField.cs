using BetaSharp.Client.Guis;
using BetaSharp.Client.Input;
using BetaSharp.Client.Rendering;
using BetaSharp.Client.UI.Rendering;
using Microsoft.Extensions.Logging;
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

            SelectionStart = CursorPosition;
            SelectionEnd = CursorPosition;
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
                RemoveSelection();
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

        OnKeyDown += (e) =>
        {
            logger.LogInformation("key down: {KeyCode} {IsDown}", e.KeyCode, e.IsDown);
            if (!IsFocused || !e.IsDown) return;

            switch (e.KeyCode)
            {
                case Keyboard.KEY_BACK:
                    if (HasSelection)
                    {
                        ReplaceSelection("");
                    }
                    else if (CursorPosition > 0)
                    {
                        _text = _text.Remove(CursorPosition - 1, 1);
                        CursorPosition--;
                        OnTextChanged?.Invoke(_text);
                    }
                    break;

                case Keyboard.KEY_DELETE:
                    if (HasSelection)
                    {
                        ReplaceSelection("");
                    }
                    else if (CursorPosition < _text.Length)
                    {
                        _text = _text.Remove(CursorPosition, 1);
                        OnTextChanged?.Invoke(_text);
                    }
                    break;

                case Keyboard.KEY_LEFT:
                    if (HasSelection)
                    {
                        CursorPosition = realSelStart;
                        RemoveSelection();
                    }
                    else if (CursorPosition > 0)
                        CursorPosition--;
                    break;

                case Keyboard.KEY_RIGHT:
                    if (HasSelection)
                    {
                        CursorPosition = realSelEnd;
                        RemoveSelection();
                    }
                    else if (CursorPosition < _text.Length)
                        CursorPosition++;
                    break;

                case Keyboard.KEY_HOME:
                    if (HasSelection) RemoveSelection();
                    CursorPosition = 0;
                    break;

                case Keyboard.KEY_END:
                    if (HasSelection) RemoveSelection();
                    CursorPosition = _text.Length;
                    break;

                case Keyboard.KEY_RETURN:
                    if (HasSelection) RemoveSelection();
                    OnSubmit?.Invoke();
                    break;

                default:
                    if (e.KeyChar >= 32 && e.KeyChar != 127 && _text.Length < MaxLength)
                    {
                        if (Keyboard.isKeyDown(Keyboard.KEY_LCONTROL))
                        {
                            if (e.KeyCode == Keyboard.KEY_V)
                            {
                                string clip = Keyboard.GetClipboardText();
                                int allowed = MaxLength - _text.Length;

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
                            } else if (e.KeyCode == Keyboard.KEY_A)
                            {
                                SelectionStart = 0;
                                SelectionEnd = Text.Length;

                                break;
                            } else if (e.KeyCode == Keyboard.KEY_C)
                            {
                                ReadOnlySpan<char> selection = Text.AsSpan(realSelStart, realSelEnd - realSelStart);
                                Keyboard.SetClipboardText(selection.ToString());

                                break;
                            }
                        }
                        else
                        {
                            if (HasSelection)
                            {
                                ReplaceSelection(e.KeyChar.ToString());
                            } else
                            {
                                _text = _text.Insert(CursorPosition, e.KeyChar.ToString());
                                CursorPosition++;
                                OnTextChanged?.Invoke(_text);
                            }
                        }
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
            if (HasSelection)
            {
                logger.LogInformation("selection! {start}-{end} {pos}", SelectionStart, SelectionEnd, CursorPosition);
                int start = Math.Min(SelectionStart, SelectionEnd);
                int end = Math.Max(SelectionStart, SelectionEnd);

                ReadOnlySpan<char> before = Text.AsSpan(0, realSelStart);
                ReadOnlySpan<char> selection = Text.AsSpan(realSelStart, realSelEnd - realSelStart);

                renderer.DrawRect(
                    renderer.TextRenderer.GetStringWidth(before) + 4,
                    ComputedHeight / 2 - 5,
                    renderer.TextRenderer.GetStringWidth(selection),
                    10,
                    Color.Blue
                );
            }
            else logger.LogInformation("no selection");

            renderer.DrawText(_text, 4, ComputedHeight / 2 - 4, Color.White);

            if (IsFocused && _cursorCounter / 10 % 2 == 0)
            {
                int cursorX = 4 + renderer.TextRenderer.GetStringWidth(_text.AsSpan(0, CursorPosition));
                renderer.DrawRect(cursorX, ComputedHeight / 2 - 5, 1, 10, Color.White);
            }
        }

        base.Render(renderer);
    }

    private static readonly ILogger<TextField> logger = Log.Instance.For<TextField>();

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

    private void RemoveSelection()
    {
        SelectionStart = CursorPosition;
        SelectionEnd = CursorPosition;
    }

    private void ReplaceSelection(string replacement)
    {
        int length = realSelEnd - realSelStart;
        _text = _text.Remove(realSelStart, length);
        _text = _text.Insert(realSelStart, replacement);
        CursorPosition = realSelStart + replacement.Length;
        RemoveSelection();
        OnTextChanged?.Invoke(_text);
    }
}
