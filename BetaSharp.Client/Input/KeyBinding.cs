using Silk.NET.GLFW;

namespace BetaSharp.Client.Input;

public class KeyBinding
{
    public string keyDescription;
    public int scanCode;
    public Keys defaultLogicalKey;

    public KeyBinding(string desc, Keys logicalDefault)
    {
        keyDescription = desc;
        defaultLogicalKey = logicalDefault;
        scanCode = Keyboard.KEY_NONE;
    }
}
