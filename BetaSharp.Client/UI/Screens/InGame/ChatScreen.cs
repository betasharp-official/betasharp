using System.Text;
using BetaSharp.Client.Entities;
using BetaSharp.Client.Guis;
using BetaSharp.Client.Input;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Controls.HUD;
using BetaSharp.Client.UI.Layout.Flexbox;

namespace BetaSharp.Client.UI.Screens.InGame;

public class ChatScreen(
    UIContext context,
    ChatOverlay chat,
    ClientPlayerEntity player,
    string prefix = "") : UIScreen(context)
{
    private static readonly List<string> s_history = [];
    private int _historyIndex = 0;
    private TextField _textField = null!;

    public override bool PausesGame => false;

    protected override void Init()
    {
        Root.Style.JustifyContent = Justify.FlexEnd;
        Root.Style.AlignItems = Align.Stretch;
        Root.Style.PaddingLeft = 2;
        Root.Style.PaddingRight = 2;
        Root.Style.PaddingBottom = 2;

        var inputBar = new UIElement();
        inputBar.Style.Height = 12;
        inputBar.Style.BackgroundColor = new Color(0, 0, 0, 150);
        inputBar.Style.FlexDirection = FlexDirection.Row;
        inputBar.Style.AlignItems = Align.Center;
        inputBar.Style.PaddingLeft = 2;
        Root.AddChild(inputBar);

        var prompt = new Label
        {
            Text = "> ",
            TextColor = Color.GrayE0
        };
        inputBar.AddChild(prompt);

        _textField = new TextField
        {
            Text = prefix,
            MaxLength = 100,
            CursorPosition = prefix.Length,
            SelectionStart = prefix.Length
        };
        _textField.Style.FlexGrow = 1;
        _textField.Style.Height = 12;

        _textField.OnSubmit = SendMessage;

        // Handle history navigation separately since TextField doesn't know about it
        _textField.OnKeyDown += (e) =>
        {
            if (e.KeyCode == Keyboard.KEY_UP)
            {
                if (Keyboard.isKeyDown(Keyboard.KEY_LMENU) || Keyboard.isKeyDown(Keyboard.KEY_RMENU))
                {
                    NavigateHistory(-1);
                    e.Handled = true;
                }
                else
                {
                    chat.ScrollMessages(1);
                    e.Handled = true;
                }
            }
            else if (e.KeyCode == Keyboard.KEY_DOWN)
            {
                if (Keyboard.isKeyDown(Keyboard.KEY_LMENU) || Keyboard.isKeyDown(Keyboard.KEY_RMENU))
                {
                    NavigateHistory(1);
                    e.Handled = true;
                }
                else
                {
                    chat.ScrollMessages(-1);
                    e.Handled = true;
                }
            }
        };

        inputBar.AddChild(_textField);

        _historyIndex = s_history.Count;
        FocusedElement = _textField;

        // Global mouse events for the screen
        Root.OnMouseScroll = (e) =>
        {
            chat.ScrollMessages(e.ScrollDelta > 0 ? 1 : -1);
            e.Handled = true;
        };

        Root.OnMouseDown = (e) =>
        {
            if (e.Button == MouseButton.Left && chat.HoveredItemName != null)
            {
                if (_textField.Text.Length > 0 && !_textField.Text.EndsWith(' '))
                {
                    _textField.Text += " ";
                }

                _textField.Text += chat.HoveredItemName;

                const byte maxLen = 100;
                if (_textField.Text.Length > maxLen)
                {
                    _textField.Text = _textField.Text[..maxLen];
                }
                _textField.CursorPosition = _textField.Text.Length;
                e.Handled = true;
            }
        };
    }

    public override void OnEnter()
    {
        base.OnEnter();
        Keyboard.enableRepeatEvents(true);
        chat.ScrollOffset = 0;
        chat.IsOpen = true;
    }

    public override void Uninit()
    {
        base.Uninit();
        Keyboard.enableRepeatEvents(false);
        chat.IsOpen = false;
        chat.ScrollOffset = 0;
    }

    private void NavigateHistory(int direction)
    {
        if (direction < 0) // Up
        {
            if (_historyIndex > 0)
            {
                _historyIndex--;
                _textField.Text = s_history[_historyIndex];
                _textField.CursorPosition = _textField.Text.Length;
            }
        }
        else // Down
        {
            if (_historyIndex < s_history.Count - 1)
            {
                ++_historyIndex;
                _textField.Text = s_history[_historyIndex];
                _textField.CursorPosition = _textField.Text.Length;
            }
            else if (_historyIndex == s_history.Count - 1)
            {
                _historyIndex = s_history.Count;
                _textField.Text = "";
                _textField.CursorPosition = 0;
            }
        }
    }

    private void SendMessage()
    {
        string msg = _textField.Text.Trim();
        if (msg.Length > 0)
        {
            string sendMsg = ConvertAmpersandToSection(msg);
            player.sendChatMessage(sendMsg);
            s_history.Add(msg); // Store original with & for history navigation
            if (s_history.Count > 100)
            {
                s_history.RemoveAt(0);
            }
        }

        Context.Navigator.Navigate(null);
    }

    private static string ConvertAmpersandToSection(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var sb = new StringBuilder();
        const string colorCodes = "0123456789abcdefklmnor";

        for (int i = 0; i < input.Length; i++)
        {
            if (input[i] == '&' && i + 1 < input.Length)
            {
                char c = char.ToLower(input[i + 1]);
                if (colorCodes.Contains(c))
                {
                    sb.Append('\u00a7');
                    sb.Append(c);
                    i++;
                    continue;
                }
            }

            sb.Append(input[i]);
        }

        return sb.ToString();
    }
}
