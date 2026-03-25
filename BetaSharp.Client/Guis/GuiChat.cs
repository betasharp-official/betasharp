using System.Text;
using BetaSharp.Client.Input;
using BetaSharp.Util;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Guis;

public class GuiChat : Screen
{
    private const uint BackgroundColor = 0x80000000;
    private const uint TextColorNormal = 0xE0E0E0;

    protected string _message = "";
    private int _updateCounter = 0;
    private static readonly string s_allowedChars = ChatAllowedCharacters.allowedCharacters;
    private static readonly List<string> s_history = [];
    private int _historyIndex = 0;

    public override bool PausesGame => false;

    public GuiChat(string prefix = "")
    {
        Keyboard.enableRepeatEvents(true);
        IsSubscribedToKeyboard = true;
        _historyIndex = s_history.Count;
        _message = prefix;
    }

    public override void OnGuiClosed()
    {
        Keyboard.enableRepeatEvents(false);
    }

    protected override void OnTick()
    {
        ++_updateCounter;
    }

    protected override void OnKeyInput(KeyboardEventArgs e)
    {
        if (e.Key == Keyboard.KEY_ESCAPE)
        {
            MC.OpenScreen(null);
            return;
        }

        if (e.Key == Keyboard.KEY_RETURN)
        {
            string msg = _message.Trim();
            if (msg.Length > 0)
            {
                string sendMsg = ConvertAmpersandToSection(msg);
                MC.player.sendChatMessage(sendMsg);
                s_history.Add(sendMsg);
                if (s_history.Count > 100)
                {
                    s_history.RemoveAt(0);
                }
            }

            MC.OpenScreen(null);
            _message = "";
            return;
        }

        if (e.Key == Keyboard.KEY_UP)
        {
            if (Keyboard.isKeyDown(Keyboard.KEY_LMENU) || Keyboard.isKeyDown(Keyboard.KEY_RMENU))
            {
                MC.ingameGUI.scrollChat(5);
            }
            else
            {
                if (_historyIndex > 0)
                {
                    --_historyIndex;
                    _message = s_history[_historyIndex];
                }
            }
            return;
        }

        if (e.Key == Keyboard.KEY_DOWN)
        {
            if (Keyboard.isKeyDown(Keyboard.KEY_LMENU) || Keyboard.isKeyDown(Keyboard.KEY_RMENU))
            {
                MC.ingameGUI.scrollChat(-5);
            }
            else
            {
                if (_historyIndex < s_history.Count - 1)
                {
                    ++_historyIndex;
                    _message = s_history[_historyIndex];
                }
                else if (_historyIndex == s_history.Count - 1)
                {
                    _historyIndex = s_history.Count;
                    _message = "";
                }
            }
            return;
        }

        if (e.Key == Keyboard.KEY_BACK)
        {
            if (_message.Length > 0)
            {
                _message = _message.Substring(0, _message.Length - 1);
            }
            return;
        }

        if (s_allowedChars.Contains(e.KeyChar) && _message.Length < 100)
        {
            _message += e.KeyChar;
        }
    }


    protected override void OnRender(RenderEventArgs e)
    {
        Gui.DrawRect(2, EffectiveHeight - 14, EffectiveWidth - 2, EffectiveHeight - 2, BackgroundColor);

        string cursor = (_updateCounter / 6 % 2 == 0) ? "_" : "";
        string textToDraw = "> " + _message + cursor;

        int y = EffectiveHeight - 12;
        int xBase = 4;

        FontRenderer.DrawStringWithShadow(textToDraw, xBase, y, TextColorNormal);
    }

    public override void HandleMouseInput()
    {
        base.HandleMouseInput();
        int wheel = Mouse.getEventDWheel();
        if (wheel != 0)
        {
            MC.ingameGUI.scrollChat(wheel > 0 ? 1 : -1);
        }
    }

    protected override void OnClick(MouseEventArgs e)
    {
        if (e.Button != 0) return;

        if (MC.ingameGUI._hoveredItemName != null)
        {
            if (_message.Length > 0 && !_message.EndsWith(' '))
            {
                _message += " ";
            }

            _message += MC.ingameGUI._hoveredItemName;

            const byte maxLen = 100;
            if (_message.Length > maxLen)
            {
                _message = _message[..maxLen];
            }
        }
    }

    private static string ConvertAmpersandToSection(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var sb = new StringBuilder();
        const string styleCodes = "0123456789abcdefklmnor";

        for (int i = 0; i < input.Length; i++)
        {
            if (input[i] == '&' && i + 1 < input.Length)
            {
                char c = char.ToLower(input[i + 1]);
                if (styleCodes.Contains(c))
                {
                    sb.Append('§');
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
