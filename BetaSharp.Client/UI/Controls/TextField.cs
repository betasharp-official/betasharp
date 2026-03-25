using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Rendering;

namespace BetaSharp.Client.UI.Controls;

public class TextField : UIElement
{
    public string Text { get; set; } = "";
    public string Placeholder { get; set; } = "";
    public int MaxLength { get; set; } = 32;
    public Action<string>? OnTextChanged;

    private int _cursorCounter = 0;

    public TextField()
    {
        Style.Width = 200;
        Style.Height = 20;

        OnMouseDown += (e) =>
        {
            if (e.Button == MouseButton.Left)
            {
                e.Handled = true;
            }
        };

        OnKeyDown += (e) =>
        {
            if (!IsFocused || !e.IsDown) return;

            if (e.KeyCode == Input.Keyboard.KEY_BACK)
            {
                if (Text.Length > 0)
                {
                    Text = Text.Substring(0, Text.Length - 1);
                    OnTextChanged?.Invoke(Text);
                }
            }
            else if (e.KeyChar >= 32 && e.KeyChar != 127 && Text.Length < MaxLength)
            {
                Text += e.KeyChar;
                OnTextChanged?.Invoke(Text);
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

        Color borderColor = IsFocused ? Color.White : Color.GrayA0;
        renderer.DrawRect(0, 0, ComputedWidth, 1, borderColor);
        renderer.DrawRect(0, ComputedHeight - 1, ComputedWidth, 1, borderColor);
        renderer.DrawRect(0, 0, 1, ComputedHeight, borderColor);
        renderer.DrawRect(ComputedWidth - 1, 0, 1, ComputedHeight, borderColor);

        string displayText = Text;
        if (string.IsNullOrEmpty(Text) && !IsFocused)
        {
            renderer.DrawText(Placeholder, 4, ComputedHeight / 2 - 4, Color.Gray70);
        }
        else
        {
            bool showCursor = IsFocused && (_cursorCounter / 30) % 2 == 0;
            if (showCursor)
            {
                displayText += "_";
            }
            renderer.DrawText(displayText, 4, ComputedHeight / 2 - 4, Color.White);
        }

        base.Render(renderer);
    }
}
