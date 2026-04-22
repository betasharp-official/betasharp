namespace BetaSharp.Client.UI.Controls.Core;

public class TextBuffer
{
    private string _text = "";
    public string Text
    {
        get => _text;
        set
        {
            _text = value ?? "";
            if (_text.Length > MaxLength) _text = _text[..MaxLength];
            CursorPosition = Math.Clamp(CursorPosition, 0, _text.Length);
            SelectionStart = Math.Clamp(SelectionStart, 0, _text.Length);
        }
    }

    public int MaxLength { get; set; } = 32;
    public int CursorPosition { get; set; } = 0;
    public int SelectionStart { get; set; } = 0;

    public bool HasSelection => SelectionStart != CursorPosition;

    public string SelectedText
    {
        get
        {
            if (!HasSelection) return "";
            int start = Math.Min(SelectionStart, CursorPosition);
            int length = Math.Abs(SelectionStart - CursorPosition);
            return _text.Substring(start, length);
        }
    }

    public void Insert(string input)
    {
        DeleteSelection();

        int remainingSpace = MaxLength - _text.Length;
        if (remainingSpace <= 0) return;

        if (input.Length > remainingSpace) input = input[..remainingSpace];

        _text = _text.Insert(CursorPosition, input);
        CursorPosition += input.Length;
        SelectionStart = CursorPosition;
    }

    public void DeleteSelection()
    {
        if (!HasSelection) return;

        int start = Math.Min(SelectionStart, CursorPosition);
        int length = Math.Max(SelectionStart, CursorPosition) - start;

        _text = _text.Remove(start, length);
        CursorPosition = start;
        SelectionStart = CursorPosition;
    }

    public void Backspace()
    {
        if (HasSelection)
        {
            DeleteSelection();
        }
        else if (CursorPosition > 0)
        {
            _text = _text.Remove(CursorPosition - 1, 1);
            CursorPosition--;
            SelectionStart = CursorPosition;
        }
    }

    public void Delete()
    {
        if (HasSelection)
        {
            DeleteSelection();
        }
        else if (CursorPosition < _text.Length)
        {
            _text = _text.Remove(CursorPosition, 1);
        }
    }

    public void MoveCursor(int delta, bool select)
    {
        CursorPosition = Math.Clamp(CursorPosition + delta, 0, _text.Length);
        if (!select) SelectionStart = CursorPosition;
    }

    public void MoveTo(int position, bool select)
    {
        CursorPosition = Math.Clamp(position, 0, _text.Length);
        if (!select) SelectionStart = CursorPosition;
    }

    public void SelectAll()
    {
        SelectionStart = 0;
        CursorPosition = _text.Length;
    }

    public void ClearSelection()
    {
        SelectionStart = CursorPosition;
    }
}
