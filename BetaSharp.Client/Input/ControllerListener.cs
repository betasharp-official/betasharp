using Silk.NET.GLFW;

namespace BetaSharp.Client.Input;

public class ControllerListener
{
    private Action<GamepadButton>? _callback;
    private readonly bool[] _snapshot = new bool[15];

    public bool IsListening { get; private set; }

    public void StartListening(Action<GamepadButton> callback)
    {
        _callback = callback;
        IsListening = true;

        // Take initial snapshot to ignore buttons already held down
        for (int i = 0; i < _snapshot.Length; i++)
        {
            _snapshot[i] = Controller.IsButtonDown((GamepadButton)i);
        }
    }

    public void StopListening()
    {
        IsListening = false;
        _callback = null;
    }

    public void Update()
    {
        if (!IsListening) return;

        for (int i = 0; i < _snapshot.Length; i++)
        {
            bool isDown = Controller.IsButtonDown((GamepadButton)i);

            // Check for new press
            if (isDown && !_snapshot[i])
            {
                GamepadButton pressed = (GamepadButton)i;

                // Allow callback to handle or ignore specific buttons
                IsListening = false;
                _callback?.Invoke(pressed);
                _callback = null;
                return;
            }

            // Update snapshot for released buttons
            if (!isDown)
            {
                _snapshot[i] = false;
            }
        }
    }
}
