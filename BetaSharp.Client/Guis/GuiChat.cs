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
    private int _cursorPosition = 0;

    public override bool PausesGame => false;

    public GuiChat(string prefix = "")
    {
        Keyboard.OnCharacterTyped += CharTyped;
        _message = prefix;
        _cursorPosition = prefix.Length;
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
            Game.displayGuiScreen(null);
            return;
        }

        if (eventKey == Keyboard.KEY_RETURN)
        {
            string msg = _message.Trim();
            if (msg.Length > 0)
            {
                string sendMsg = ConvertAmpersandToSection(msg);
                Game.player.sendChatMessage(sendMsg);
                s_history.Add(sendMsg);
                if (s_history.Count > 100)
                {
                    s_history.RemoveAt(0);
                }
            }

            Game.displayGuiScreen(null);
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
                    _cursorPosition = _message.Length;
                }
            }
            else
            {
                Game.ingameGUI.ScrollChat(1);
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
                    _cursorPosition = _message.Length;
                }
                else if (_historyIndex == s_history.Count - 1)
                {
                    _historyIndex = s_history.Count;
                    _message = "";
                    _cursorPosition = 0;
                }
            }
            else
            {
                Game.ingameGUI.ScrollChat(-1);
            }
            return;
        }

        switch (eventKey)
        {
            case Keyboard.KEY_LEFT:
                if (_cursorPosition > 0)
                    _cursorPosition--;
                return;
            case Keyboard.KEY_RIGHT:
                    if (_cursorPosition < _message.Length)
                        _cursorPosition++;
                    return;
            case Keyboard.KEY_HOME:
                _cursorPosition = 0;
                return;
            case Keyboard.KEY_END:
                _cursorPosition = _message.Length;
                return;
            case Keyboard.KEY_BACK:
                    if (_cursorPosition <= 0)
                        return;
                    _message = _message.Remove(_cursorPosition - 1, 1);
                    _cursorPosition--;
                    return;
            case Keyboard.KEY_DELETE:
                    if (_cursorPosition < _message.Length)
                    {
                        _message = _message.Remove(_cursorPosition, 1);
                    }
                    return;
        }
    }

    protected override void CharTyped(char eventChar)
    {
        if (!ChatAllowedCharacters.IsAllowedCharacter(eventChar) || _message.Length >= 100)
            return;

        _message = _message.Insert(_cursorPosition, eventChar.ToString());
        _cursorPosition++;
    }


    public override void Render(int mouseX, int mouseY, float partialTicks)
    {
        DrawRect(2, Height - 14, Width - 2, Height - 2, Color.BackgroundBlackAlpha);

        const string asciiBrokenPipe = "\u00A6"; // Broken pipe character '¦'
        const string prompt = "> ";
        const int xBase = 4;
        int y = Height - 12;

        string beforeCursor = _message[.._cursorPosition];
        string afterCursor = _message[_cursorPosition..];

        // Determine the cursor character based on position
        bool isAtEnd = _cursorPosition >= _message.Length;
        string cursor = (_updateCounter / 6 % 2 == 0) ? ((isAtEnd) ? "_" : ":") : (isAtEnd ? "" : asciiBrokenPipe);
        int cursorWidth = FontRenderer.GetStringWidth(cursor);
        int negPadding = 0;

        // Draw the prompt and text before the cursor
        FontRenderer.DrawStringWithShadow(prompt, xBase, y, Color.GrayE0);
        int currentX = xBase + FontRenderer.GetStringWidth(prompt);

        if (beforeCursor.Length > 0)
            FontRenderer.DrawStringWithShadow(beforeCursor, currentX, y, Color.GrayE0);

        currentX += FontRenderer.GetStringWidth(beforeCursor);

        // Draw cursor - use colon width for both cursor characters when between characters
        // Calculate negative padding to center the cursor when the char is broken pipe (has width of normal char).
        if (cursor.Length > 0 && cursor == asciiBrokenPipe)
                negPadding += (FontRenderer.GetStringWidth(asciiBrokenPipe) - FontRenderer.GetStringWidth(":"));

        FontRenderer.DrawStringWithShadow(cursor, currentX - (negPadding / 2), y, Color.GrayE0);

        // Draw text after cursor
        if (afterCursor.Length > 0)
            FontRenderer.DrawStringWithShadow(afterCursor, currentX + cursorWidth - negPadding, y, Color.GrayE0);

        base.Render(mouseX, mouseY, partialTicks);
    }

    public override void HandleMouseInput()
    {
        base.HandleMouseInput();
        int wheel = Mouse.getEventDWheel();
        if (wheel != 0)
        {
            Game.ingameGUI.ScrollChat(wheel > 0 ? 1 : -1);
        }
    }

    protected override void MouseClicked(int x, int y, int button)
    {
        if (button != 0) return;

        if (Game.ingameGUI.HoveredItemName != null)
        {
            if (_message.Length > 0 && !_message.EndsWith(" "))
            {
                _message += " ";
            }

            _message += Game.ingameGUI.HoveredItemName;

            const byte maxLen = 100;
            if (_message.Length > maxLen)
            {
                _message = _message.Substring(0, maxLen);
            }
            _cursorPosition = _message.Length;
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
