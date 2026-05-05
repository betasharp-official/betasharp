using Silk.NET.GLFW;

namespace BetaSharp.Client.Input;

public class Keyboard
{
    public const int EVENT_SIZE = 4 + 1 + 4 + 8 + 1;

    public const int CHAR_NONE = '\0';
    public const int KEY_NONE = 0x00;

    public const int KEY_ESCAPE = 0x01;
    public const int KEY_1 = 0x02;
    public const int KEY_2 = 0x03;
    public const int KEY_3 = 0x04;
    public const int KEY_4 = 0x05;
    public const int KEY_5 = 0x06;
    public const int KEY_6 = 0x07;
    public const int KEY_7 = 0x08;
    public const int KEY_8 = 0x09;
    public const int KEY_9 = 0x0A;
    public const int KEY_0 = 0x0B;
    public const int KEY_MINUS = 0x0C;
    public const int KEY_EQUALS = 0x0D;
    public const int KEY_BACK = 0x0E;
    public const int KEY_TAB = 0x0F;
    public const int KEY_Q = 0x10;
    public const int KEY_W = 0x11;
    public const int KEY_E = 0x12;
    public const int KEY_R = 0x13;
    public const int KEY_T = 0x14;
    public const int KEY_Y = 0x15;
    public const int KEY_U = 0x16;
    public const int KEY_I = 0x17;
    public const int KEY_O = 0x18;
    public const int KEY_P = 0x19;
    public const int KEY_LBRACKET = 0x1A;
    public const int KEY_RBRACKET = 0x1B;
    public const int KEY_RETURN = 0x1C;
    public const int KEY_LCONTROL = 0x1D;
    public const int KEY_A = 0x1E;
    public const int KEY_S = 0x1F;
    public const int KEY_D = 0x20;
    public const int KEY_F = 0x21;
    public const int KEY_G = 0x22;
    public const int KEY_H = 0x23;
    public const int KEY_J = 0x24;
    public const int KEY_K = 0x25;
    public const int KEY_L = 0x26;
    public const int KEY_SEMICOLON = 0x27;
    public const int KEY_APOSTROPHE = 0x28;
    public const int KEY_GRAVE = 0x29;
    public const int KEY_LSHIFT = 0x2A;
    public const int KEY_BACKSLASH = 0x2B;
    public const int KEY_Z = 0x2C;
    public const int KEY_X = 0x2D;
    public const int KEY_C = 0x2E;
    public const int KEY_V = 0x2F;
    public const int KEY_B = 0x30;
    public const int KEY_N = 0x31;
    public const int KEY_M = 0x32;
    public const int KEY_COMMA = 0x33;
    public const int KEY_PERIOD = 0x34;
    public const int KEY_SLASH = 0x35;
    public const int KEY_RSHIFT = 0x36;
    public const int KEY_MULTIPLY = 0x37;
    public const int KEY_LMENU = 0x38;
    public const int KEY_SPACE = 0x39;
    public const int KEY_CAPITAL = 0x3A;
    public const int KEY_F1 = 0x3B;
    public const int KEY_F2 = 0x3C;
    public const int KEY_F3 = 0x3D;
    public const int KEY_F4 = 0x3E;
    public const int KEY_F5 = 0x3F;
    public const int KEY_F6 = 0x40;
    public const int KEY_F7 = 0x41;
    public const int KEY_F8 = 0x42;
    public const int KEY_F9 = 0x43;
    public const int KEY_F10 = 0x44;
    public const int KEY_NUMLOCK = 0x45;
    public const int KEY_SCROLL = 0x46;
    public const int KEY_NUMPAD7 = 0x47;
    public const int KEY_NUMPAD8 = 0x48;
    public const int KEY_NUMPAD9 = 0x49;
    public const int KEY_SUBTRACT = 0x4A;
    public const int KEY_NUMPAD4 = 0x4B;
    public const int KEY_NUMPAD5 = 0x4C;
    public const int KEY_NUMPAD6 = 0x4D;
    public const int KEY_ADD = 0x4E;
    public const int KEY_NUMPAD1 = 0x4F;
    public const int KEY_NUMPAD2 = 0x50;
    public const int KEY_NUMPAD3 = 0x51;
    public const int KEY_NUMPAD0 = 0x52;
    public const int KEY_DECIMAL = 0x53;
    public const int KEY_F11 = 0x57;
    public const int KEY_F12 = 0x58;
    public const int KEY_F13 = 0x64;
    public const int KEY_F14 = 0x65;
    public const int KEY_F15 = 0x66;
    public const int KEY_F16 = 0x67;
    public const int KEY_F17 = 0x68;
    public const int KEY_F18 = 0x69;
    public const int KEY_KANA = 0x70;
    public const int KEY_F19 = 0x71;
    public const int KEY_CONVERT = 0x79;
    public const int KEY_NOCONVERT = 0x7B;
    public const int KEY_YEN = 0x7D;
    public const int KEY_NUMPADEQUALS = 0x8D;
    public const int KEY_CIRCUMFLEX = 0x90;
    public const int KEY_AT = 0x91;
    public const int KEY_COLON = 0x92;
    public const int KEY_UNDERLINE = 0x93;
    public const int KEY_KANJI = 0x94;
    public const int KEY_STOP = 0x95;
    public const int KEY_AX = 0x96;
    public const int KEY_UNLABELED = 0x97;
    public const int KEY_NUMPADENTER = 0x9C;
    public const int KEY_RCONTROL = 0x9D;
    public const int KEY_SECTION = 0xA7;
    public const int KEY_NUMPADCOMMA = 0xB3;
    public const int KEY_DIVIDE = 0xB5;
    public const int KEY_SYSRQ = 0xB7;
    public const int KEY_RMENU = 0xB8;
    public const int KEY_FUNCTION = 0xC4;
    public const int KEY_PAUSE = 0xC5;
    public const int KEY_HOME = 0xC7;
    public const int KEY_UP = 0xC8;
    public const int KEY_PRIOR = 0xC9;
    public const int KEY_LEFT = 0xCB;
    public const int KEY_RIGHT = 0xCD;
    public const int KEY_END = 0xCF;
    public const int KEY_DOWN = 0xD0;
    public const int KEY_NEXT = 0xD1;
    public const int KEY_INSERT = 0xD2;
    public const int KEY_DELETE = 0xD3;
    public const int KEY_CLEAR = 0xDA;
    public const int KEY_LMETA = 0xDB;
    public const int KEY_LWIN = KEY_LMETA;
    public const int KEY_RMETA = 0xDC;
    public const int KEY_RWIN = KEY_RMETA;
    public const int KEY_APPS = 0xDD;
    public const int KEY_POWER = 0xDE;
    public const int KEY_SLEEP = 0xDF;

    public const int KEYBOARD_SIZE = 256;

    private static bool created;
    private static Glfw glfw;
    private static unsafe WindowHandle* window;

    private static readonly bool[] keyDownBuffer = new bool[KEYBOARD_SIZE];
    private static readonly Queue<KeyEvent> eventQueue = new();
    private static readonly Queue<char> charQueue = new();
    private static KeyEvent current_event = new();
    private static bool repeat_enabled;

    private static Dictionary<Keys, int> keyMap = null!;
    private static string[] keyNames = null!;

    public static unsafe void create(Glfw glfwApi, WindowHandle* windowHandle)
    {
        if (created) return;

        glfw = glfwApi;
        window = windowHandle;

        InitializeKeyMap();

        glfw.SetKeyCallback(window, OnKey);
        glfw.SetCharCallback(window, OnChar);

        keyNames = new string[256];

        var keyboardType = typeof(Keyboard);
        var fields = keyboardType.GetFields(
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Static |
            System.Reflection.BindingFlags.FlattenHierarchy
        );

        foreach (var field in fields)
        {
            if (field.IsLiteral && !field.IsInitOnly && field.Name.StartsWith("KEY_", StringComparison.Ordinal))
            {
                int keyCode = (int)field.GetValue(null)!;
                string keyName = field.Name[4..];

                if (keyCode >= 0 && keyCode < keyNames.Length)
                {
                    keyNames[keyCode] = keyName;
                }
            }
        }

        for (int i = 0; i < keyNames.Length; i++)
        {
            if (keyNames[i] == null)
            {
                keyNames[i] = "UNKNOWN";
            }
        }

        created = true;
    }

    private static void InitializeKeyMap()
    {
        keyMap = new Dictionary<Keys, int>
        {
            { Keys.Escape, KEY_ESCAPE },
            { Keys.Number1, KEY_1 },
            { Keys.Number2, KEY_2 },
            { Keys.Number3, KEY_3 },
            { Keys.Number4, KEY_4 },
            { Keys.Number5, KEY_5 },
            { Keys.Number6, KEY_6 },
            { Keys.Number7, KEY_7 },
            { Keys.Number8, KEY_8 },
            { Keys.Number9, KEY_9 },
            { Keys.Number0, KEY_0 },
            { Keys.Minus, KEY_MINUS },
            { Keys.Equal, KEY_EQUALS },
            { Keys.Backspace, KEY_BACK },
            { Keys.Tab, KEY_TAB },
            { Keys.Q, KEY_Q },
            { Keys.W, KEY_W },
            { Keys.E, KEY_E },
            { Keys.R, KEY_R },
            { Keys.T, KEY_T },
            { Keys.Y, KEY_Y },
            { Keys.U, KEY_U },
            { Keys.I, KEY_I },
            { Keys.O, KEY_O },
            { Keys.P, KEY_P },
            { Keys.LeftBracket, KEY_LBRACKET },
            { Keys.RightBracket, KEY_RBRACKET },
            { Keys.Enter, KEY_RETURN },
            { Keys.ControlLeft, KEY_LCONTROL },
            { Keys.A, KEY_A },
            { Keys.S, KEY_S },
            { Keys.D, KEY_D },
            { Keys.F, KEY_F },
            { Keys.G, KEY_G },
            { Keys.H, KEY_H },
            { Keys.J, KEY_J },
            { Keys.K, KEY_K },
            { Keys.L, KEY_L },
            { Keys.Semicolon, KEY_SEMICOLON },
            { Keys.Apostrophe, KEY_APOSTROPHE },
            { Keys.GraveAccent, KEY_GRAVE },
            { Keys.ShiftLeft, KEY_LSHIFT },
            { Keys.BackSlash, KEY_BACKSLASH },
            { Keys.Z, KEY_Z },
            { Keys.X, KEY_X },
            { Keys.C, KEY_C },
            { Keys.V, KEY_V },
            { Keys.B, KEY_B },
            { Keys.N, KEY_N },
            { Keys.M, KEY_M },
            { Keys.Comma, KEY_COMMA },
            { Keys.Period, KEY_PERIOD },
            { Keys.Slash, KEY_SLASH },
            { Keys.ShiftRight, KEY_RSHIFT },
            { Keys.KeypadMultiply, KEY_MULTIPLY },
            { Keys.AltLeft, KEY_LMENU },
            { Keys.Space, KEY_SPACE },
            { Keys.CapsLock, KEY_CAPITAL },
            { Keys.F1, KEY_F1 },
            { Keys.F2, KEY_F2 },
            { Keys.F3, KEY_F3 },
            { Keys.F4, KEY_F4 },
            { Keys.F5, KEY_F5 },
            { Keys.F6, KEY_F6 },
            { Keys.F7, KEY_F7 },
            { Keys.F8, KEY_F8 },
            { Keys.F9, KEY_F9 },
            { Keys.F10, KEY_F10 },
            { Keys.NumLock, KEY_NUMLOCK },
            { Keys.ScrollLock, KEY_SCROLL },
            { Keys.Keypad7, KEY_NUMPAD7 },
            { Keys.Keypad8, KEY_NUMPAD8 },
            { Keys.Keypad9, KEY_NUMPAD9 },
            { Keys.KeypadSubtract, KEY_SUBTRACT },
            { Keys.Keypad4, KEY_NUMPAD4 },
            { Keys.Keypad5, KEY_NUMPAD5 },
            { Keys.Keypad6, KEY_NUMPAD6 },
            { Keys.KeypadAdd, KEY_ADD },
            { Keys.Keypad1, KEY_NUMPAD1 },
            { Keys.Keypad2, KEY_NUMPAD2 },
            { Keys.Keypad3, KEY_NUMPAD3 },
            { Keys.Keypad0, KEY_NUMPAD0 },
            { Keys.KeypadDecimal, KEY_DECIMAL },
            { Keys.F11, KEY_F11 },
            { Keys.F12, KEY_F12 },
            { Keys.KeypadEnter, KEY_NUMPADENTER },
            { Keys.ControlRight, KEY_RCONTROL },
            { Keys.KeypadDivide, KEY_DIVIDE },
            { Keys.AltRight, KEY_RMENU },
            { Keys.Pause, KEY_PAUSE },
            { Keys.Home, KEY_HOME },
            { Keys.Up, KEY_UP },
            { Keys.PageUp, KEY_PRIOR },
            { Keys.Left, KEY_LEFT },
            { Keys.Right, KEY_RIGHT },
            { Keys.End, KEY_END },
            { Keys.Down, KEY_DOWN },
            { Keys.PageDown, KEY_NEXT },
            { Keys.Insert, KEY_INSERT },
            { Keys.Delete, KEY_DELETE },
            { Keys.SuperLeft, KEY_LMETA },
            { Keys.SuperRight, KEY_RMETA },
            { Keys.Menu, KEY_APPS }
        };
    }

    private static readonly Dictionary<char, char> ShiftMap = new() {
        { '1', '!' }, { '2', '@' }, { '3', '#' }, { '4', '$' }, { '5', '%' },
        { '6', '^' }, { '7', '&' }, { '8', '*' }, { '9', '(' }, { '0', ')' },
        { '`', '~' }, { '-', '_' }, { '=', '+' }, { '[', '{' }, { ']', '}' },
        { '\\', '|' }, { ';', ':' }, { '\'', '"' }, { ',', '<' }, { '.', '>' }, { '/', '?' }
    };

    private static char ShiftUp(char c)
    {
        if (char.IsLetter(c)) return char.ToUpper(c);
        if (ShiftMap.TryGetValue(c, out char up)) return up;
        return c;
    }

    public static event Action<Keys, InputAction, KeyModifiers>? OnGlfwKey;

    private static unsafe void OnKey(WindowHandle* window, Keys key, int scancode, InputAction action,
        KeyModifiers mods)
    {
        if (!created) return;

        OnGlfwKey?.Invoke(key, action, mods);

        if (!keyMap.TryGetValue(key, out int lwjglKey)) lwjglKey = KEY_NONE;

        bool pressed = action == InputAction.Press || action == InputAction.Repeat;
        bool isRepeat = action == InputAction.Repeat;

        if (lwjglKey > 0 && lwjglKey < KEYBOARD_SIZE)
        {
            keyDownBuffer[lwjglKey] = pressed;
        }

        eventQueue.Enqueue(new KeyEvent
        {
            Key = lwjglKey,
            Character = CHAR_NONE,
            State = pressed,
            Repeat = isRepeat,
            Nanos = GetNanos()
        });
    }

    private static unsafe void OnChar(WindowHandle* _, uint codepoint)
    {
        if (!created) return;

        if (codepoint <= char.MaxValue)
        {
            char character = (char)codepoint;
            if (!char.IsSurrogate(character))
            {
                charQueue.Enqueue(character);
                OnCharacterTyped?.Invoke(character);
            }
        }
    }

    public static event Action<char>? OnCharacterTyped;

    public static bool Next()
    {
        if (!created) throw new InvalidOperationException("Keyboard must be created before you can read events");

        while (eventQueue.Count > 0)
        {
            KeyEvent evt = eventQueue.Dequeue();

            if (evt.Repeat && !repeat_enabled)
                continue;

            if (evt.State && evt.Character == CHAR_NONE && charQueue.Count > 0)
            {
                evt.Character = charQueue.Dequeue();
            }

            current_event = evt;
            return true;
        }

        return false;
    }

    public static bool getEventKeyState() => current_event.State;
    public static int getEventKey() => current_event.Key;
    public static char getEventCharacter() => (char)current_event.Character;
    public static long getEventNanoseconds() => current_event.Nanos;
    public static bool isRepeatEvent() => current_event.Repeat;

    public static bool isKeyDown(int key)
    {
        if (!created) throw new InvalidOperationException("Keyboard must be created before you can poll it");
        if (key >= KEYBOARD_SIZE || key < 0) return false;
        return keyDownBuffer[key];
    }

    public static void enableRepeatEvents(bool enable)
    {
        repeat_enabled = enable;
    }

    public static void Flush()
    {
        eventQueue.Clear();
        charQueue.Clear();
    }

    public static bool areRepeatEventsEnabled() => repeat_enabled;

    public static bool isCreated() => created;

    public static void destroy()
    {
        if (!created) return;
        created = false;
        eventQueue.Clear();
        charQueue.Clear();
    }

    private static long GetNanos()
    {
        return DateTime.UtcNow.Ticks * 100;
    }

    public static string getKeyName(int keyCode)
    {
        if (keyCode >= 0 && keyCode < keyNames.Length)
        {
            return keyNames[keyCode];
        }

        return "UNKNOWN";
    }

    private struct KeyEvent
    {
        public int Character;
        public int Key;
        public bool State;
        public long Nanos;
        public bool Repeat;
    }
}
