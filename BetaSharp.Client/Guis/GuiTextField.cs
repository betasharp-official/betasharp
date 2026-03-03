using BetaSharp.Client.Input;
using BetaSharp.Client.Rendering;
using BetaSharp.Util;
using java.awt;
using java.awt.datatransfer;

namespace BetaSharp.Client.Guis;

public class GuiTextField : Gui
{
    private readonly TextRenderer _fontRenderer;
    private readonly int _xPos;
    private readonly int _yPos;
    private readonly int _width;
    private readonly int _height;
    private string _text;
    private int _maxStringLength;
    private int _cursorCounter;
    private int _cursorPosition = 0;
    private int _selectionStart = -1;
    private int _selectionEnd = -1;
    public bool IsFocused = false;
    public bool IsEnabled = true;
    private readonly GuiScreen _parentGuiScreen;

    public GuiTextField(GuiScreen parentGuiScreen, TextRenderer fontRenderer, int xPos, int yPos, int width, int height, string text)
    {
        _parentGuiScreen = parentGuiScreen;
        _fontRenderer = fontRenderer;
        _xPos = xPos;
        _yPos = yPos;
        _width = width;
        _height = height;
        SetText(text);
    }

    public void SetText(string text)
    {
        _text = text;
        _cursorPosition = text?.Length ?? 0;
        _selectionStart = -1;
        _selectionEnd = -1;
    }

    public string GetText() => _text;

    public void updateCursorCounter() => _cursorCounter++;

    public void textboxKeyTyped(char eventChar, int eventKey)
    {
        if (!IsEnabled || !IsFocused) return;

        // Check for Ctrl combos first
        bool ctrlDown = Keyboard.isKeyDown(Keyboard.KEY_LCONTROL) || Keyboard.isKeyDown(Keyboard.KEY_RCONTROL);
        bool shiftDown = Keyboard.isKeyDown(Keyboard.KEY_LSHIFT) || Keyboard.isKeyDown(Keyboard.KEY_RSHIFT);

        if (ctrlDown)
        {
            switch (eventKey)
            {
                case Keyboard.KEY_A:
                    // Select all
                    _selectionStart = 0;
                    _selectionEnd = _text?.Length ?? 0;
                    _cursorPosition = _selectionEnd;
                    return;
                case Keyboard.KEY_C:
                    // Copy
                    CopySelectionToClipboard();
                    return;
                case Keyboard.KEY_X:
                    // Cut
                    CutSelectionToClipboard();
                    return;
                case Keyboard.KEY_V:
                    // Paste
                    PasteClipboardAtCursor();
                    return;
            }
        }

        // Handle Shift+Left/Right for selection
        if (shiftDown)
        {
            switch (eventKey)
            {
                case Keyboard.KEY_LEFT:
                    if (_selectionStart == -1)
                    {
                        _selectionStart = _cursorPosition;
                    }
                    if (_cursorPosition > 0) _cursorPosition--;
                    _selectionEnd = _cursorPosition;
                    return;
                case Keyboard.KEY_RIGHT:
                    if (_selectionStart == -1)
                    {
                        _selectionStart = _cursorPosition;
                    }
                    if (_cursorPosition < _text.Length) _cursorPosition++;
                    _selectionEnd = _cursorPosition;
                    return;
            }
        }

        // Handle regular keys
        switch (eventKey)
        {
            case Keyboard.KEY_LEFT:
                if (_cursorPosition > 0) _cursorPosition--;
                ClearSelection();
                return;
            case Keyboard.KEY_RIGHT:
                if (_cursorPosition < _text.Length) _cursorPosition++;
                ClearSelection();
                return;
            case Keyboard.KEY_HOME:
                _cursorPosition = 0;
                ClearSelection();
                return;
            case Keyboard.KEY_END:
                _cursorPosition = _text.Length;
                ClearSelection();
                return;
            case Keyboard.KEY_DELETE:
                if (HasSelection())
                {
                    DeleteSelection();
                }
                else if (_cursorPosition < _text.Length)
                {
                    _text = _text.Substring(0, _cursorPosition) + _text.Substring(_cursorPosition + 1);
                }
                ClearSelection();
                return;
            case Keyboard.KEY_BACK:
                HandleBackspace();
                return;
            case Keyboard.KEY_TAB:
                _parentGuiScreen.SelectNextField();
                return;
        }

        // Tab key
        if (eventChar == 9 || eventKey == Keyboard.KEY_TAB)
        {
            _parentGuiScreen.SelectNextField();
            return;
        }

        // Backspace
        if (eventKey == Keyboard.KEY_BACK)
        {
            string var3 = GuiScreen.GetClipboardString();
            var3 ??= "";

            int var4 = 32 - _text.Length;
            if (var4 > var3.Length)
            {
                DeleteSelection();
            }
            else if (_text.Length > 0 && _cursorPosition > 0)
            {
                _cursorPosition--;
                _text = _text.Substring(0, _cursorPosition) + _text.Substring(_cursorPosition + 1);
            }
            ClearSelection();
            return;
        }

        // Regular character input
        if (ChatAllowedCharacters.IsAllowedCharacter(eventChar) && (_text.Length < _maxStringLength || _maxStringLength == 0))
        {
            if (HasSelection())
            {
                DeleteSelection();
            }

            _text = _text.Substring(0, _cursorPosition) + eventChar + _text.Substring(_cursorPosition);
            _cursorPosition++;
            ClearSelection();
        }
    }

    public void MouseClicked(int x, int y, int button)
    {
        bool isFocused = IsEnabled && x >= _xPos && x < _xPos + _width && y >= _yPos && y < _yPos + _height;
        SetFocused(isFocused);
    }

    public void SetFocused(bool isFocused)
    {
        if (isFocused && !IsFocused)
        {
            _cursorCounter = 0;
        }

        IsFocused = isFocused;
    }

    public void DrawTextBox()
    {
        DrawRect(_xPos - 1, _yPos - 1, _xPos + _width + 1, _yPos + _height + 1, Color.GrayA0);
        DrawRect(_xPos, _yPos, _xPos + _width, _yPos + _height, Color.Black);

        if (IsEnabled)
        {
            string cursor = IsFocused && _cursorCounter / 6 % 2 == 0 ? "_" : string.Empty;
            int safePos = Math.Clamp(_cursorPosition, 0, _text.Length);

            string renderText = _text.Insert(safePos, cursor);

            DrawString(_fontRenderer, renderText, _xPos + 4, _yPos + (_height - 8) / 2, Color.Gray80);
        }
        else
        {
            DrawString(_fontRenderer, _text, _xPos + 4, _yPos + (_height - 8) / 2, Color.Gray70);
        }
    }

    public void SetMaxStringLength(int maxStringLength)
    {
        _maxStringLength = maxStringLength;
    }

    private bool HasSelection()
    {
        return _selectionStart != -1 && _selectionEnd != -1 && _selectionStart != _selectionEnd;
    }

    private (int start, int end) GetSelectionRange()
    {
        if (!HasSelection()) return (0, 0);
        int s = Math.Min(_selectionStart, _selectionEnd);
        int e = Math.Max(_selectionStart, _selectionEnd);
        s = Math.Max(0, Math.Min(s, _text.Length));
        e = Math.Max(0, Math.Min(e, _text.Length));
        return (s, e);
    }

    private string GetSelectedText()
    {
        if (!HasSelection()) return "";
        var (s, e) = GetSelectionRange();
        return _text.Substring(s, e - s);
    }

    private void HandleBackspace()
    {
        if (HasSelection())
        {
            DeleteSelection();
        }
        else if (_cursorPosition > 0)
        {
            _cursorPosition--;
            _text = _text.Remove(_cursorPosition, 1);
        }
        ClearSelection();
    }

    private void DeleteSelection()
    {
        if (!HasSelection()) return;
        var (s, e) = GetSelectionRange();
        _text = _text.Substring(0, s) + _text.Substring(e);
        _cursorPosition = s;
        ClearSelection();
    }

    private void ClearSelection()
    {
        _selectionStart = -1;
        _selectionEnd = -1;
    }

    private void CopySelectionToClipboard()
    {
        if (!HasSelection()) return;
        try
        {
            string sel = GetSelectedText();
            GuiScreen.SetClipboardString(sel);
        }
        catch (Exception)
        {
        }
    }

    private void CutSelectionToClipboard()
    {
        if (!HasSelection()) return;
        CopySelectionToClipboard();
        DeleteSelection();
    }

    private void PasteClipboardAtCursor()
    {
        try
        {
            string clip = GuiScreen.GetClipboardString();
            clip ??= "";
            if (HasSelection()) DeleteSelection();
            int maxInsert = Math.Max(0, (_maxStringLength > 0 ? _maxStringLength : 32) - _text.Length);
            if (clip.Length > maxInsert) clip = clip.Substring(0, maxInsert);
            _text = _text.Substring(0, _cursorPosition) + clip + _text.Substring(_cursorPosition);
            _cursorPosition += clip.Length;
            ClearSelection();
        }
        catch (Exception)
        {
        }
    }
}
