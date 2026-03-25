using BetaSharp.Client.Input;
using BetaSharp.Client.Rendering;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Util;
using Silk.NET.OpenGL.Legacy;

namespace BetaSharp.Client.Guis.Controls;

public class TextField : Control
{
    private enum DragSelectBehavior
    {
        Character,
        Word,
        Line,
    }
    private const int LeftPad = 4;
    private const int CursorPeriod = 5;
    private readonly TextRenderer _fontRenderer;
    public override bool Focusable => true;
    public int MaxLength { get; init; }
    private int _cursorCounter;
    private int _caretPosition;
    private int _selectionStart = -1;
    private int _selectionEnd = -1;
    private int _initialSelectionPos = -1;
    private bool HasSelection => _selectionStart != -1 && _selectionEnd != -1 && _selectionStart != _selectionEnd;
    private DragSelectBehavior _dragSelectBehavior;

    public TextField(int x, int y, TextRenderer fontRenderer, string text) : base(x, y, 200, 22)
    {
        _fontRenderer = fontRenderer;
        Text = text;
    }

    public void UpdateCursorCounter() =>
        _cursorCounter = _cursorCounter > CursorPeriod ? -CursorPeriod : _cursorCounter + 1;

    protected override void OnKeyInput(KeyboardEventArgs e)
    {
        if (!e.IsKeyDown || !Enabled || !Focused) return;

        bool ctrl = Keyboard.isKeyDown(Keyboard.KEY_LCONTROL) || Keyboard.isKeyDown(Keyboard.KEY_RCONTROL);
        bool shift = Keyboard.isKeyDown(Keyboard.KEY_LSHIFT) || Keyboard.isKeyDown(Keyboard.KEY_RSHIFT);
        int[] acceptedKeys =
        [
            Keyboard.KEY_A, Keyboard.KEY_C, Keyboard.KEY_X, Keyboard.KEY_V,
            Keyboard.KEY_LEFT, Keyboard.KEY_RIGHT, Keyboard.KEY_HOME, Keyboard.KEY_END,
            Keyboard.KEY_DELETE, Keyboard.KEY_BACK,
        ];
        if (acceptedKeys.Contains(e.Key)
            || (ChatAllowedCharacters.allowedCharacters.Contains(e.KeyChar)
                && (Text.Length < MaxLength || MaxLength == 0)))
        {
            ResetCursorFlash();
        }

        switch (e.Key)
        {
            case Keyboard.KEY_A when ctrl:
                // Select all
                _selectionStart = 0;
                _selectionEnd = Text.Length;
                _caretPosition = _selectionEnd;
                return;

            case Keyboard.KEY_C when ctrl:
                CopySelectionToClipboard();
                return;

            case Keyboard.KEY_X when ctrl:
                CutSelectionToClipboard();
                return;

            case Keyboard.KEY_V when ctrl:
                PasteClipboardAtCursor();
                return;

            case Keyboard.KEY_LEFT when ctrl && shift:
                // Ctrl+Shift+Left: extend selection to start of previous word
                if (_selectionStart == -1) _selectionStart = _caretPosition;
                if (_caretPosition > 0)
                {
                    _caretPosition = GetNthWordFrom(-1, _caretPosition, true);
                }
                _selectionEnd = _caretPosition;
                return;

            case Keyboard.KEY_RIGHT when ctrl && shift:
                // Ctrl+Shift+Right: extend selection to start of next word
                if (_selectionStart == -1) _selectionStart = _caretPosition;
                if (_caretPosition < Text.Length)
                {
                    _caretPosition = GetNthWordFrom(1, _caretPosition, true);
                }
                _selectionEnd = _caretPosition;
                return;

            case Keyboard.KEY_LEFT when ctrl:
                // Ctrl+Left: move cursor to start of previous word
                if (_caretPosition > 0)
                {
                    _caretPosition = GetNthWordFrom(-1, _caretPosition, true);
                }
                ClearSelection();
                return;

            case Keyboard.KEY_RIGHT when ctrl:
                // Ctrl+Right: move cursor to start of next word
                if (_caretPosition < Text.Length)
                {
                    _caretPosition = GetNthWordFrom(1, _caretPosition, true);
                }
                ClearSelection();
                return;

            case Keyboard.KEY_LEFT when shift:
                // Shift+Left: extend selection left
                if (_selectionStart == -1) _selectionStart = _caretPosition;
                if (_caretPosition > 0) _caretPosition--;
                _selectionEnd = _caretPosition;
                return;

            case Keyboard.KEY_RIGHT when shift:
                // Shift+Right: extend selection right
                if (_selectionStart == -1) _selectionStart = _caretPosition;
                if (_caretPosition < Text.Length) _caretPosition++;
                _selectionEnd = _caretPosition;
                return;

            case Keyboard.KEY_HOME when shift:
                // Shift+Home: extend selection to start
                if (_selectionStart == -1) _selectionStart = _caretPosition;
                _caretPosition = 0;
                _selectionEnd = _caretPosition;
                return;

            case Keyboard.KEY_END when shift:
                // Shift+End: extend selection to end
                if (_selectionStart == -1) _selectionStart = _caretPosition;
                _caretPosition = Text.Length;
                _selectionEnd = _caretPosition;
                return;

            case Keyboard.KEY_LEFT:
                if (HasSelection)
                {
                    (int start, _) = GetSelectionRange();
                    _caretPosition = start;
                    ClearSelection();
                }
                else if (_caretPosition > 0)
                {
                    _caretPosition--;
                }
                return;

            case Keyboard.KEY_RIGHT:
                if (HasSelection)
                {
                    (_, int end) = GetSelectionRange();
                    _caretPosition = end;
                    ClearSelection();
                }
                else if (_caretPosition < Text.Length)
                {
                    _caretPosition++;
                }
                return;

            case Keyboard.KEY_HOME:
                _caretPosition = 0;
                ClearSelection();
                return;

            case Keyboard.KEY_END:
                _caretPosition = Text.Length;
                ClearSelection();
                return;

            case Keyboard.KEY_DELETE:
                Delete(false);
                return;

            case Keyboard.KEY_BACK:
                Delete(true);
                return;

            default:
                // Regular character input
                if (ChatAllowedCharacters.allowedCharacters.Contains(e.KeyChar) &&
                    (Text.Length < MaxLength || MaxLength == 0))
                {
                    if (HasSelection)
                    {
                        DeleteSelection();
                    }

                    SuppressTextChanged(true);
                    Text = Text.Insert(_caretPosition, e.KeyChar.ToString());
                    SuppressTextChanged(false);
                    _caretPosition++;
                }
                return;
        }
    }

    protected override void OnFocusChanged(FocusEventArgs e)
    {
        if (!e.Focused)
        {
            _cursorCounter = 0;
        }
    }

    protected override void OnRender(RenderEventArgs e)
    {
        Gui.DrawRect(0, 0, EffectiveWidth, EffectiveHeight, 0xFFA0A0A0);
        Gui.DrawRect(1, 1, EffectiveWidth - 1, EffectiveHeight - 1, 0xFF000000);

        GLManager.GL.PushMatrix();
        GLManager.GL.Translate(1, 0, 0); // Offset to right to account for border.
        // Don't have to do this for the Y axis because

        if (Enabled)
        {
            bool showCaret = Focused && _cursorCounter <= 0;
            int safePos = Math.Clamp(_caretPosition, 0, Text.Length);

            Gui.DrawString(_fontRenderer, Text, LeftPad, (EffectiveHeight - 8) / 2, 0xE0E0E0);

            if (showCaret)
            {
                if (_caretPosition != Text.Length)
                {
                    string textBeforeCursor = Text[..safePos];
                    int caretX = LeftPad + _fontRenderer.GetStringWidth(textBeforeCursor);

                    Gui.DrawRect(caretX - 1, 6, caretX, EffectiveHeight - LeftPad - 1, HasSelection ? 0xFFA0A0A0 : 0xFFD0D0D0);
                }
                else
                {
                    int caretX;
                    if (Text.Length > 0) caretX = 5 + _fontRenderer.GetStringWidth(Text);
                    else caretX = LeftPad;

                    Gui.DrawString(_fontRenderer, "_", caretX, (EffectiveHeight - 8) / 2, 0xE0E0E0);
                }
            }

            if (HasSelection)
            {

                (int start, int end) = GetSelectionRange();
                string textBeforeSelection = Text[..start];
                string selectedText = Text[start..end];

                int preSelectionWidth = _fontRenderer.GetStringWidth(textBeforeSelection);
                int selectionWidth = _fontRenderer.GetStringWidth(selectedText);

                int selX1 = LeftPad + preSelectionWidth - 1;
                int selX2 = selX1 + selectionWidth + 1;
                int selY1 = 6;
                int selY2 = EffectiveHeight - LeftPad - 1;

                Tessellator tess = Tessellator.instance;
                GLManager.GL.Color4(0.0f, 0.0f, 1.0f, 1.0f);
                GLManager.GL.Disable(EnableCap.Texture2D);
                GLManager.GL.Enable(EnableCap.ColorLogicOp);
                GLManager.GL.LogicOp(LogicOp.OrReverse);

                tess.startDrawingQuads();
                tess.addVertex(selX1, selY2, 0.0);
                tess.addVertex(selX2, selY2, 0.0);
                tess.addVertex(selX2, selY1, 0.0);
                tess.addVertex(selX1, selY1, 0.0);
                tess.draw();

                GLManager.GL.Disable(EnableCap.ColorLogicOp);
                GLManager.GL.Enable(EnableCap.Texture2D);
            }
        }
        else
        {
            Gui.DrawString(_fontRenderer, Text, LeftPad, (EffectiveHeight - 8) / 2, 0x707070);
        }

        GLManager.GL.PopMatrix();
    }

    protected override void OnTextChanged(TextEventArgs e)
    {
        if (ShouldSuppressTextChanged) return;
        _caretPosition = e.Text.Length;
        _selectionStart = -1;
        _selectionEnd = -1;
    }

    /// <summary>
    /// Calculates the character position in the text based on a pixel X coordinate.
    /// </summary>
    private int GetCharPositionFromPixelX(float pixelX)
    {
        float relativeX = Math.Max(0, pixelX - AbsX - LeftPad - 1);
        int pos = 0;

        int width = 0;
        while (pos <= Text.Length)
        {
            width = _fontRenderer.GetStringWidth(Text.AsSpan()[..pos]);
            if (width > relativeX)
            {
                break;
            }
            pos++;
        }
        pos--;

        // Put caret at the end of the character if click is past its middle
        if (pos < Text.Length)
        {
            int charWidth = _fontRenderer.GetStringWidth(Text[pos].ToString()) - 1;
            if (charWidth / 2f + relativeX >= width - 1)
            {
                pos++;
            }
        }

        return Math.Clamp(pos, 0, Text.Length);
    }

    protected override void OnMousePress(MouseEventArgs e)
    {
        if (!Enabled) return;
        ResetCursorFlash();

        int pos = GetCharPositionFromPixelX(e.PixelX);

        if (e.Clicks == 2) // Double-click selects word
        {
            (_selectionStart, _selectionEnd) = GetLogicalWordBoundaries(pos, true);
            _caretPosition = _selectionEnd;
            _dragSelectBehavior = DragSelectBehavior.Word;
            return;
        }
        if (e.Clicks >= 3) // Triple-click selects all
        {
            _selectionStart = 0;
            _selectionEnd = Text.Length;
            _caretPosition = pos;
            _dragSelectBehavior = DragSelectBehavior.Line;
            return;
        }

        if (Keyboard.isKeyDown(Keyboard.KEY_LSHIFT) || Keyboard.isKeyDown(Keyboard.KEY_RSHIFT))
        {
            if (_selectionStart == -1)
                _selectionStart = _caretPosition;
            _selectionEnd = pos;
        }
        else
        {
            ClearSelection();
        }
        _caretPosition = pos;
    }

    protected override void OnMouseRelease(MouseEventArgs e)
    {
        _dragSelectBehavior = DragSelectBehavior.Character;
    }

    protected override void OnMouseDrag(MouseEventArgs e)
    {
        if (!Enabled) return;
        ResetCursorFlash();

        int pos = GetCharPositionFromPixelX(e.PixelX);
        switch (_dragSelectBehavior)
        {
            case DragSelectBehavior.Character:
                {
                    if (_initialSelectionPos == -1)
                        _initialSelectionPos = _caretPosition;
                    _selectionStart = _initialSelectionPos;
                    _selectionEnd = pos;
                    _caretPosition = pos;
                    break;
                }
            case DragSelectBehavior.Word:
                {
                    (int wordStart, int wordEnd) = GetLogicalWordBoundaries(pos);
                    if (_initialSelectionPos == -1)
                    {
                        _initialSelectionPos = wordStart;
                    }

                    if (pos < _initialSelectionPos)
                    {
                        _selectionStart = GetLogicalWordBoundaries(_initialSelectionPos).end;
                        _selectionEnd = wordStart;
                        _caretPosition = wordStart;
                    }
                    else
                    {
                        _selectionStart = _initialSelectionPos;
                        _selectionEnd = wordEnd;
                        _caretPosition = wordEnd;
                    }

                    break;
                }
            case DragSelectBehavior.Line:
                // TODO: Multiline support
                _selectionStart = 0;
                _selectionEnd = Text.Length;
                break;
        }
    }

    protected override void OnTick()
    {
        UpdateCursorCounter();
    }

    private (int start, int end) GetSelectionRange()
    {
        if (!HasSelection) return (0, 0);
        int s = Math.Min(_selectionStart, _selectionEnd);
        int e = Math.Max(_selectionStart, _selectionEnd);
        s = Math.Max(0, Math.Min(s, Text.Length));
        e = Math.Max(0, Math.Min(e, Text.Length));
        return (s, e);
    }

    private string GetSelectedText()
    {
        if (!HasSelection) return "";
        (int start, int end) = GetSelectionRange();
        return Text[start..end];
    }

    private void Delete(bool backspace)
    {
        if (HasSelection)
        {
            DeleteSelection();
        }
        else if (backspace ? (_caretPosition > 0) : (_caretPosition < Text.Length))
        {
            if (backspace) _caretPosition--;

            SuppressTextChanged(true);
            Text = Text.Remove(_caretPosition, 1);
            SuppressTextChanged(false);
        }
        ClearSelection();
    }

    private void DeleteSelection()
    {
        if (!HasSelection) return;
        (int start, int end) = GetSelectionRange();
        Text = Text[..start] + Text[end..];
        _caretPosition = start;
        ClearSelection();
    }

    private void ClearSelection()
    {
        _selectionStart = -1;
        _selectionEnd = -1;
        _initialSelectionPos = -1;
    }

    private void CopySelectionToClipboard()
    {
        if (!HasSelection) return;
        string sel = GetSelectedText();
        Screen.SetClipboardString(sel);
    }

    private void CutSelectionToClipboard()
    {
        if (!HasSelection) return;
        CopySelectionToClipboard();
        DeleteSelection();
    }

    private void PasteClipboardAtCursor()
    {
        string clip = Screen.GetClipboardString();
        if (HasSelection) DeleteSelection();
        int maxInsert = Math.Max(0, (MaxLength > 0 ? MaxLength : 32) - Text.Length);
        if (clip.Length > maxInsert) clip = clip[..maxInsert];
        SuppressTextChanged(true);
        Text = Text.Insert(_caretPosition, clip);
        SuppressTextChanged(false);
        _caretPosition += clip.Length;
        ClearSelection();
    }

    /// <summary>
    /// Returns the index of the start of the nth word from the given location.
    /// If n is positive, it looks forward; if n is negative, it looks backward.
    /// If skipEmptyWords is true, it will skip over consecutive spaces and return
    /// the start of the next non-empty word. If false, it will treat spaces as words
    /// and return their boundaries.
    /// </summary>
    /// <param name="n">The number of words to move (positive or negative)</param>
    /// <param name="loc">The starting index to search from</param>
    /// <param name="skipEmptyWords">Whether to skip over empty words (spaces)</param>
    /// <returns>The index of the start of the nth word from the given location</returns>
    public int GetNthWordFrom(int n, int loc, bool skipEmptyWords)
    {
        int i = loc;
        bool forward = n < 0;
        int absN = Math.Abs(n);

        for (int k = 0; k < absN; k++)
        {
            if (!forward)
            {
                int length = Text.Length;
                i = Text.IndexOf(' ', i);

                if (i == -1)
                {
                    i = length;
                }
                else
                {
                    while (skipEmptyWords && i < length && Text[i] == ' ')
                    {
                        i++;
                    }
                }
            }
            else
            {
                while (skipEmptyWords && i > 0 && Text[i - 1] == ' ')
                {
                    i--;
                }

                while (i > 0 && Text[i - 1] != ' ')
                {
                    i--;
                }
            }
        }

        return i;
    }

    /// <summary>
    /// Returns the start and end indices of the word at the given position.<br/>
    /// If the position is between words, it returns the boundaries of the next word.
    /// If the position is in the middle of multiple consecutive spaces,
    /// it treats them as a word and returns their boundaries.
    /// If the position is at the end of the text, it returns the boundaries of the last word.
    /// If the last character is a space, it returns the boundaries of that space word, even
    /// if there's only one space.
    /// </summary>
    /// <param name="pos">The index to find the word boundaries for</param>
    /// <param name="includeTrailingSpaces">
    /// If the position is on a word and there are spaces after it, whether to put the end index after the spaces.
    /// </param>
    /// <returns>A tuple of (start, end) indices of the word</returns>
    private (int start, int end) GetLogicalWordBoundaries(int pos, bool includeTrailingSpaces = false)
    {
        if (Text.Length == 0) return (0, 0);
        if (pos >= Text.Length) pos = Text.Length - 1;

        int start = pos;
        int end = pos;

        bool onSpace = Text[pos] == ' ';
        bool prevIsSpace = pos > 0 && Text[pos - 1] == ' ';
        bool nextIsSpace = pos < Text.Length - 1 && Text[pos + 1] == ' ';
        if (onSpace && (prevIsSpace || nextIsSpace))
        {
            // If we're on a space, find the boundaries of this space word
            while (start > 0 && Text[start - 1] == ' ')
            {
                start--;
            }
            while (end < Text.Length && Text[end] == ' ')
            {
                end++;
            }
        }
        else
        {
            // If we're on a non-space, find the boundaries of this word
            while (start > 0 && Text[start - 1] != ' ')
            {
                start--;
            }
            while (end < Text.Length && Text[end] != ' ')
            {
                end++;
            }
            if (includeTrailingSpaces)
            {
                while (end < Text.Length && Text[end] == ' ')
                {
                    end++;
                }
            }
        }

        return (start, end);
    }

    private void ResetCursorFlash()
    {
        _cursorCounter = -CursorPeriod * 3;
    }
}
