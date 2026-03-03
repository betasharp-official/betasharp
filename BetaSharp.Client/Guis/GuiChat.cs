using System.Text;
using BetaSharp.Client.Input;
using BetaSharp.Util;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Guis;

public class GuiChat : GuiScreen
{
    protected string _message = "";
    private int _updateCounter = 0;
    private static readonly List<string> s_history = [];
    private int _historyIndex = 0;

    public override bool PausesGame => false;

    public GuiChat(string prefix = "")
    {
        Keyboard.OnCharacterTyped += CharTyped;
        _message = prefix;
    }

    public override void InitGui()
    {
        Keyboard.enableRepeatEvents(true);
        _isSubscribedToKeyboard = true;
        _historyIndex = s_history.Count;
    }

    public override void OnGuiClosed()
    {
        Keyboard.enableRepeatEvents(false);
    }

    public override void UpdateScreen()
    {
        ++_updateCounter;
    }

    protected override void KeyTyped(char eventChar, int eventKey)
    {
        if (eventKey == Keyboard.KEY_ESCAPE)
        {
            mc.displayGuiScreen(null);
            return;
        }

        if (eventKey == Keyboard.KEY_RETURN)
        {
            string msg = _message.Trim();
            if (msg.Length > 0)
            {
                string sendMsg = ConvertAmpersandToSection(msg);
                mc.player.sendChatMessage(sendMsg);
                s_history.Add(sendMsg);
                if (s_history.Count > 100)
                {
                    s_history.RemoveAt(0);
                }
            }

            mc.displayGuiScreen(null);
            _message = "";
            return;
        }

        if (eventKey == Keyboard.KEY_UP)
        {
            if (Keyboard.isKeyDown(Keyboard.KEY_LMENU) || Keyboard.isKeyDown(Keyboard.KEY_RMENU))
            {
                if (_historyIndex > 0)
                {
                    --_historyIndex;
                    _message = s_history[_historyIndex];
                }
            }
            else
            {
                mc.ingameGUI.scrollChat(1);
            }
            return;
        }

        if (eventKey == Keyboard.KEY_DOWN)
        {
            if (Keyboard.isKeyDown(Keyboard.KEY_LMENU) || Keyboard.isKeyDown(Keyboard.KEY_RMENU))
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
            else
            {
                mc.ingameGUI.scrollChat(-1);
            }
            return;
        }

        if (eventKey == Keyboard.KEY_BACK)
        {
            if (_message.Length > 0)
            {
                _message = _message.Substring(0, _message.Length - 1);
            }
            return;
        }
    }

    protected override void CharTyped(char eventChar)
    {
        if (ChatAllowedCharacters.IsAllowedCharacter(eventChar) && _message.Length < 100)
        {
            _message += eventChar;
        }
    }


    public override void Render(int mouseX, int mouseY, float partialTicks)
    {
        DrawRect(2, Height - 14, Width - 2, Height - 2, Color.BackgroundBlackAlpha);

        string cursor = (_updateCounter / 6 % 2 == 0) ? "_" : "";
        string textToDraw = "> " + _message + cursor;

        int y = Height - 12;
        int xBase = 4;

        FontRenderer.DrawStringWithShadow(textToDraw, xBase, y, Color.GrayE0);

        base.Render(mouseX, mouseY, partialTicks);
    }

    public override void HandleMouseInput()
    {
        base.HandleMouseInput();
        int wheel = Mouse.getEventDWheel();
        if (wheel != 0)
        {
            mc.ingameGUI.scrollChat(wheel > 0 ? 1 : -1);
        }
    }

    protected override void MouseClicked(int x, int y, int button)
    {
        if (button != 0) return;

        if (mc.ingameGUI._hoveredItemName != null)
        {
            if (_message.Length > 0 && !_message.EndsWith(" "))
            {
                _message += " ";
            }

            _message += mc.ingameGUI._hoveredItemName;

            const byte maxLen = 100;
            if (_message.Length > maxLen)
            {
                _message = _message.Substring(0, maxLen);
            }
            return;
        }

        base.MouseClicked(x, y, button);
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
