using BetaSharp.Client.Guis;
using BetaSharp.Client.Input;
using BetaSharp.Client.UI.Layout;
using BetaSharp.Client.UI.Rendering;

namespace BetaSharp.Client.UI;

public abstract class UIScreen
{
    public BetaSharp Game { get; internal set; }
    public UIElement Root { get; private set; }
    public UIRenderer Renderer { get; private set; }

    private UIElement? _hoveredElement;
    private UIElement? _focusedElement;
    public UIElement? FocusedElement
    {
        get => _focusedElement;
        set
        {
            if (_focusedElement != value)
            {
                if (_focusedElement != null) _focusedElement.IsFocused = false;
                _focusedElement = value;
                if (_focusedElement != null) _focusedElement.IsFocused = true;
            }
        }
    }
    public UIElement? DraggingElement { get; set; }
    public float MouseX { get; protected set; }
    public float MouseY { get; protected set; }
    public virtual bool PausesGame => true;
    public virtual bool AllowUserInput => false;

    private bool _isInitialized = false;

    public UIScreen(BetaSharp game)
    {
        Game = game;
        Root = new UIElement();
        Root.Style.Width = null; // Auto fill screen
        Root.Style.Height = null;
        Renderer = new UIRenderer(game.fontRenderer, game.textureManager);
    }

    public void Initialize()
    {
        Keyboard.enableRepeatEvents(true);
        if (!_isInitialized)
        {
            Init();
            _isInitialized = true;
        }
        OnEnter();
    }

    protected abstract void Init();
    public virtual void OnEnter() { }

    public virtual void Uninit()
    {
        Keyboard.enableRepeatEvents(false);
    }

    public virtual void UpdateScreen() { }

    public void HandleInput()
    {
        while (Mouse.next()) { HandleMouseInput(); }
        while (Keyboard.Next()) { HandleKeyboardInput(); }
        ControllerManager.UpdateGui(this);
    }

    public virtual bool HandleDPadNavigation(int dpadX, int dpadY, ref float cursorX, ref float cursorY) => false;

    public virtual void GetTooltips(List<ActionTip> tips) { }

    public virtual void Update(float partialTicks)
    {
        Root.Update(partialTicks);
    }

    public virtual void Render(int mouseX, int mouseY, float partialTicks)
    {
        ScaledResolution res = new(Game.options, Game.displayWidth, Game.displayHeight);

        Root.Style.Width = res.ScaledWidth;
        Root.Style.Height = res.ScaledHeight;

        FlexLayout.ApplyLayout(Root, res.ScaledWidth, res.ScaledHeight);

        float scaledMouseX = mouseX;
        float scaledMouseY = mouseY;
        MouseX = scaledMouseX;
        MouseY = scaledMouseY;

        UpdateHovers(scaledMouseX, scaledMouseY);

        Renderer.Begin();
        Root.Render(Renderer);
        Renderer.End();
    }

    private void UpdateHovers(float mouseX, float mouseY)
    {
        UIElement? newHovered = Root.HitTest(mouseX, mouseY);

        if (newHovered != _hoveredElement)
        {
            if (_hoveredElement != null)
            {
                _hoveredElement.IsHovered = false;
                _hoveredElement.OnMouseLeave?.Invoke(new UIMouseEvent { Target = _hoveredElement, MouseX = (int)mouseX, MouseY = (int)mouseY });
            }

            _hoveredElement = newHovered;

            if (_hoveredElement != null && _hoveredElement.Enabled)
            {
                _hoveredElement.IsHovered = true;
                _hoveredElement.OnMouseEnter?.Invoke(new UIMouseEvent { Target = _hoveredElement, MouseX = (int)mouseX, MouseY = (int)mouseY });
            }
        }
    }

    public void HandleMouseInput()
    {
        ScaledResolution res = new(Game.options, Game.displayWidth, Game.displayHeight);
        float scaledX = Mouse.getEventX() * res.ScaledWidth / (float)Game.displayWidth;
        float scaledY = res.ScaledHeight - Mouse.getEventY() * res.ScaledHeight / (float)Game.displayHeight - 1f;

        if (Mouse.getEventButtonState())
        {
            int rawButton = Mouse.getEventButton();
            MouseButton button = Enum.IsDefined(typeof(MouseButton), rawButton) ? (MouseButton)rawButton : MouseButton.Unknown;
            UIElement? target = Root.HitTest(scaledX, scaledY);

            FocusedElement = target;

            if (target != null && target.Enabled)
            {
                var evt = new UIMouseEvent { Target = target, MouseX = (int)scaledX, MouseY = (int)scaledY, Button = button };
                target.OnMouseDown?.Invoke(evt);
                DraggingElement = target;

                if (button == MouseButton.Left)
                {
                    target.OnClick?.Invoke(evt);
                }
            }
            else if (target != null)
            {
                DraggingElement = null; // Don't drag if disabled
            }
        }
        else
        {
            int rawButton = Mouse.getEventButton();
            if (rawButton != -1) // -1 means moved, not button up
            {
                MouseButton button = Enum.IsDefined(typeof(MouseButton), rawButton) ? (MouseButton)rawButton : MouseButton.Unknown;

                UIElement? target = Root.HitTest(scaledX, scaledY);
                if (target != null && target.Enabled)
                {
                    var evt = new UIMouseEvent { Target = target, MouseX = (int)scaledX, MouseY = (int)scaledY, Button = button };
                    target.OnMouseUp?.Invoke(evt);
                }

                DraggingElement = null; // Snap dragging off when button released
            }
            else if (DraggingElement != null)
            {
                var moveEvt = new UIMouseEvent { Target = DraggingElement, MouseX = (int)scaledX, MouseY = (int)scaledY, Button = MouseButton.Unknown };
                DraggingElement.OnMouseMove?.Invoke(moveEvt);
            }
        }

        int dWheel = Mouse.getEventDWheel();
        if (dWheel != 0)
        {
            UIElement? target = Root.HitTest(scaledX, scaledY);
            if (target != null)
            {
                var scrollEvt = new UIMouseEvent { Target = target, MouseX = (int)scaledX, MouseY = (int)scaledY, ScrollDelta = dWheel };
                UIElement? current = target;
                while (current != null)
                {
                    if (current.Enabled)
                    {
                        current.OnMouseScroll?.Invoke(scrollEvt);
                        if (scrollEvt.Handled) break;
                    }
                    current = current.Parent;
                }
            }
        }
    }

    public void HandleKeyboardInput()
    {
        if (Keyboard.getEventKeyState())
        {
            if (FocusedElement != null && FocusedElement.Enabled)
            {
                var evt = new UIKeyEvent
                {
                    Target = FocusedElement,
                    KeyCode = Keyboard.getEventKey(),
                    KeyChar = Keyboard.getEventCharacter(),
                    IsDown = true
                };

                FocusedElement.OnKeyDown?.Invoke(evt);
            }

            KeyTyped(Keyboard.getEventKey(), Keyboard.getEventCharacter());
        }
    }

    public virtual void KeyTyped(int key, char character)
    {
        if (key == Keyboard.KEY_ESCAPE || key == Keyboard.KEY_NONE)
        {
            Uninit();
            Game.displayGuiScreen(null); // Default close behavior
        }
    }

    public virtual void HandleControllerInput()
    {
    }
}
