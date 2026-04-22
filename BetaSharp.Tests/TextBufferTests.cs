using BetaSharp.Client.UI.Controls.Core;

namespace BetaSharp.Tests;

public class TextBufferTests
{
    [Fact]
    public void Insert_AtEnd_IncreasesLengthAndMovesCursor()
    {
        var buffer = new TextBuffer { MaxLength = 10 };
        buffer.Insert("abc");

        Assert.Equal("abc", buffer.Text);
        Assert.Equal(3, buffer.CursorPosition);
        Assert.Equal(3, buffer.SelectionStart);
    }

    [Fact]
    public void Insert_InMiddle_CorrectlyPlacesText()
    {
        var buffer = new TextBuffer { MaxLength = 10 };
        buffer.Text = "ac";
        buffer.CursorPosition = 1;
        buffer.SelectionStart = 1;

        buffer.Insert("b");

        Assert.Equal("abc", buffer.Text);
        Assert.Equal(2, buffer.CursorPosition);
    }

    [Fact]
    public void Insert_RespectsMaxLength()
    {
        var buffer = new TextBuffer { MaxLength = 3 };
        buffer.Insert("abcd");

        Assert.Equal("abc", buffer.Text);
        Assert.Equal(3, buffer.CursorPosition);
    }

    [Fact]
    public void Backspace_AtEnd_RemovesLastCharacter()
    {
        var buffer = new TextBuffer { Text = "abc" };
        buffer.CursorPosition = 3;
        buffer.SelectionStart = 3;

        buffer.Backspace();

        Assert.Equal("ab", buffer.Text);
        Assert.Equal(2, buffer.CursorPosition);
    }

    [Fact]
    public void Backspace_WithSelection_DeletesOnlySelectedRange()
    {
        var buffer = new TextBuffer { Text = "abcdef" };
        buffer.SelectionStart = 1;
        buffer.CursorPosition = 4; // "bcd" selected

        buffer.Backspace();

        Assert.Equal("aef", buffer.Text);
        Assert.Equal(1, buffer.CursorPosition);
        Assert.Equal(1, buffer.SelectionStart);
    }

    [Fact]
    public void Delete_WithSelection_DeletesSelectedRange()
    {
        var buffer = new TextBuffer { Text = "abcdef" };
        buffer.SelectionStart = 4;
        buffer.CursorPosition = 1; // "bcd" selected

        buffer.Delete();

        Assert.Equal("aef", buffer.Text);
        Assert.Equal(1, buffer.CursorPosition);
    }

    [Fact]
    public void MoveCursor_WithShift_CreatesSelection()
    {
        var buffer = new TextBuffer { Text = "abcde" };
        buffer.CursorPosition = 2;
        buffer.SelectionStart = 2;

        buffer.MoveCursor(2, true); // Selection from 2 to 4

        Assert.Equal(4, buffer.CursorPosition);
        Assert.Equal(2, buffer.SelectionStart);
        Assert.Equal("cd", buffer.SelectedText);
    }

    [Fact]
    public void SelectAll_SetsCorrectRange()
    {
        var buffer = new TextBuffer { Text = "hello" };
        buffer.SelectAll();

        Assert.Equal(0, buffer.SelectionStart);
        Assert.Equal(5, buffer.CursorPosition);
        Assert.Equal("hello", buffer.SelectedText);
    }

    [Fact]
    public void TextSetter_ClampsCursorAndSelection()
    {
        var buffer = new TextBuffer { Text = "verylongtext" };
        buffer.CursorPosition = 10;
        buffer.SelectionStart = 10;

        buffer.Text = "short";

        Assert.Equal(5, buffer.CursorPosition);
        Assert.Equal(5, buffer.SelectionStart);
    }
}
