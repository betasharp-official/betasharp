namespace BetaSharp.Client.UI.Controls.Core;

/// <summary>
/// Helper class for TextField to implement cursor stuff.
/// </summary>
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

    /// <summary>
    /// Marks the current current position (index to insert at when typing). 0 is start, while this being
    /// at Text.Length makes it at the end.
    /// </summary>
    public int CursorPosition { get; set; } = 0;

    /// <summary>
    /// Provides the start of the selection. The end of the selection is marked by CursorPosition!
    /// </summary>
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

    /// <summary>
    /// Insert text at the current position.
    /// Deletes selection if we have it, to override it!
    /// </summary>
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

    /// <summary>
    /// Delete the part of the current selection.
    /// </summary>
    public void DeleteSelection()
    {
        if (!HasSelection) return;

        int start = Math.Min(SelectionStart, CursorPosition);
        int length = Math.Max(SelectionStart, CursorPosition) - start;

        _text = _text.Remove(start, length);
        CursorPosition = start;
        SelectionStart = CursorPosition;
    }

    /// <summary>
    /// Remove one character behind the cursor, or delete a selection.
    /// </summary>
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


    /// <summary>
    /// Remove one character in front of the cursor, or delete a selection.
    /// </summary>
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

    /// <summary>
    /// Move the cursor a amount of steps.
    /// </summary>
    /// <param name="select">If it should select (e.g. shift pressed)</param>
    public void MoveCursor(int delta, bool select)
    {
        CursorPosition = Math.Clamp(CursorPosition + delta, 0, _text.Length);
        if (!select) SelectionStart = CursorPosition;
    }

    /// <summary>
    /// Move the cursor to a position.
    /// </summary>
    /// <param name="select">If it should select (e.g. shift pressed)</param>
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
