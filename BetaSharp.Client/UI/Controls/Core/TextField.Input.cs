using BetaSharp.Client.Input;

namespace BetaSharp.Client.UI.Controls.Core;

public partial class TextField
{
    private void HandleKeyDown(UIKeyEvent e)
    {
        if (!IsFocused || !e.IsDown) return;

        bool control = Keyboard.isKeyDown(Keyboard.KEY_LCONTROL) || Keyboard.isKeyDown(Keyboard.KEY_RCONTROL) || Keyboard.isKeyDown(Keyboard.KEY_LMETA) || Keyboard.isKeyDown(Keyboard.KEY_RMETA);
        bool shift = Keyboard.isKeyDown(Keyboard.KEY_LSHIFT) || Keyboard.isKeyDown(Keyboard.KEY_RSHIFT);

        string oldText = _buffer.Text;
        bool handled = false;

        if (control)
        {
            handled = HandleShortcut(e.KeyCode);
        }

        if (!handled)
        {
            handled = HandleFunctionalKey(e.KeyCode, shift);
        }

        if (!handled && !control && e.KeyChar >= 32 && e.KeyChar != 127)
        {
            _buffer.Insert(e.KeyChar.ToString());
            handled = true;
        }

        if (handled)
        {
            if (oldText != _buffer.Text)
            {
                OnTextChanged?.Invoke(_buffer.Text);
            }
            e.Handled = true;
        }
    }

    private bool HandleShortcut(int keyCode)
    {
        switch (keyCode)
        {
            case Keyboard.KEY_A:
                _buffer.SelectAll();
                return true;
            case Keyboard.KEY_C:
                string selectedText = _buffer.SelectedText;
                if (!string.IsNullOrEmpty(selectedText)) Display.SetClipboardString(selectedText);
                return true;
            case Keyboard.KEY_X:
                selectedText = _buffer.SelectedText;
                if (!string.IsNullOrEmpty(selectedText))
                {
                    Display.SetClipboardString(selectedText);
                    _buffer.DeleteSelection();
                }
                return true;
            case Keyboard.KEY_V:
                string clipboardText = Display.GetClipboardString();
                if (!string.IsNullOrEmpty(clipboardText)) _buffer.Insert(clipboardText);
                return true;
        }

        return false;
    }

    private bool HandleFunctionalKey(int keyCode, bool shift)
    {
        switch (keyCode)
        {
            case Keyboard.KEY_ESCAPE:
                if (_buffer.HasSelection)
                {
                    _buffer.ClearSelection();
                    return true;
                }
                return false;

            case Keyboard.KEY_BACK:
                _buffer.Backspace();
                return true;

            case Keyboard.KEY_DELETE:
                _buffer.Delete();
                return true;

            case Keyboard.KEY_LEFT:
                _buffer.MoveCursor(-1, shift);
                return true;

            case Keyboard.KEY_RIGHT:
                _buffer.MoveCursor(1, shift);
                return true;

            case Keyboard.KEY_HOME:
                _buffer.MoveTo(0, shift);
                return true;

            case Keyboard.KEY_END:
                _buffer.MoveTo(_buffer.Text.Length, shift);
                return true;

            case Keyboard.KEY_RETURN:
                OnSubmit?.Invoke();
                return true;
        }

        return false;
    }
}
