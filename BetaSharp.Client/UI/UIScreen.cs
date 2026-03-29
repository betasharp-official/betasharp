using BetaSharp.Client.Input;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Layout;
using BetaSharp.Client.UI.Rendering;
using Silk.NET.GLFW;

namespace BetaSharp.Client.UI;

public abstract class UIScreen
{
    public BetaSharp Game { get; internal set; }
    public UIElement Root { get; private set; }
    public UIRenderer Renderer { get; private set; }

    private UIElement? _hoveredElement;

    public UIElement? FocusedElement
    {
        get;
        set
        {
            if (field != value)
            {
                field?.IsFocused = false;
                field = value;
                field?.IsFocused = true;
            }
        }
    }
    public UIElement? DraggingElement { get; set; }
    public float MouseX { get; protected set; }
    public float MouseY { get; protected set; }
    public virtual bool PausesGame => true;
    public virtual bool AllowUserInput => false;

    private bool _isInitialized = false;

    private Slider? _editingSlider;
    private int _sliderDpadHeldX;
    private int _sliderDpadRepeatTicksRemaining;
    private float _sliderStickAccumulated;
    private const int SliderDpadInitialDelay = 10; // ticks before first repeat
    private const int SliderDpadRepeatInterval = 3; // ticks between subsequent repeats
    private const float SliderStepsPerSecond = 10f; // steps/sec at full stick deflection

    public bool IsEditingSlider => _editingSlider != null;

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

    public void HandleInput()
    {
        while (Mouse.next()) { HandleMouseInput(); }
        while (Keyboard.Next()) { HandleKeyboardInput(); }
        ControllerManager.UpdateGui(this);
        HandleControllerScroll();
        if (_editingSlider != null) HandleSliderEditTick();
    }

    private void HandleControllerScroll()
    {
        if (!Game.isControllerMode) return;

        float ry = Controller.RightStickY;
        if (ry == 0f) return;

        ScaledResolution res = new(Game.options, Game.displayWidth, Game.displayHeight);
        float scaledX = Game.VirtualCursor.X * res.ScaledWidth / Game.displayWidth;
        float scaledY = Game.VirtualCursor.Y * res.ScaledHeight / Game.displayHeight;

        UIElement? current = Root.HitTest(scaledX, scaledY);
        while (current != null)
        {
            if (current is ScrollView sv && sv.Enabled && sv.MaxScrollY > 0)
            {
                sv.ScrollBy(ry * 300f / Game.Timer.ticksPerSecond);
                break;
            }
            current = current.Parent;
        }
    }

    private void HandleSliderEditTick()
    {
        // Exit if A is no longer held
        if (!Controller.IsButtonDown(GamepadButton.A))
        {
            _editingSlider = null;
            _sliderDpadHeldX = 0;
            _sliderStickAccumulated = 0f;
            return;
        }

        float step = _editingSlider!.Step;

        // Left stick: accumulate fractional steps so we always move in whole units
        float lx = Controller.LeftStickX;
        if (lx != 0f)
        {
            _sliderStickAccumulated += lx * SliderStepsPerSecond / Game.Timer.ticksPerSecond;
            while (_sliderStickAccumulated >= 1f) { _editingSlider.AdjustValue(step); _sliderStickAccumulated -= 1f; }
            while (_sliderStickAccumulated <= -1f) { _editingSlider.AdjustValue(-step); _sliderStickAccumulated += 1f; }
        }
        else
        {
            _sliderStickAccumulated = 0f;
        }

        // DPad: one step per press with hold-repeat
        bool dpadLeft = Controller.IsButtonDown(GamepadButton.DPadLeft);
        bool dpadRight = Controller.IsButtonDown(GamepadButton.DPadRight);
        int dpadX = dpadRight ? 1 : dpadLeft ? -1 : 0;

        if (dpadX != _sliderDpadHeldX)
        {
            _sliderDpadHeldX = dpadX;
            _sliderDpadRepeatTicksRemaining = SliderDpadInitialDelay;
            if (dpadX != 0)
                _editingSlider.AdjustValue(dpadX * step);
        }
        else if (dpadX != 0)
        {
            _sliderDpadRepeatTicksRemaining--;
            if (_sliderDpadRepeatTicksRemaining <= 0)
            {
                _editingSlider.AdjustValue(dpadX * step);
                _sliderDpadRepeatTicksRemaining = SliderDpadRepeatInterval;
            }
        }
    }

    public virtual bool HandleDPadNavigation(int dpadX, int dpadY, ref float cursorX, ref float cursorY)
    {
        ScaledResolution res = new(Game.options, Game.displayWidth, Game.displayHeight);
        float scaledCursorX = cursorX * res.ScaledWidth / Game.displayWidth;
        float scaledCursorY = cursorY * res.ScaledHeight / Game.displayHeight;

        // While editing a slider, DPad is handled by HandleSliderEditTick — block navigation
        if (_editingSlider != null) return true;

        List<UIElement> candidates = [];
        CollectNavigable(Root, candidates, res.ScaledWidth, res.ScaledHeight);

        if (candidates.Count == 0) return false;

        UIElement? best = null;
        float bestDistSq = float.MaxValue;

        // First pass: nearest element within a 45 cone of the direction
        foreach (UIElement element in candidates)
        {
            float cx = element.ScreenX + element.ComputedWidth / 2f;
            float cy = element.ScreenY + element.ComputedHeight / 2f;

            float dx = cx - scaledCursorX;
            float dy = cy - scaledCursorY;

            float primary = dpadX != 0 ? dx * dpadX : dy * dpadY;
            float perp = dpadX != 0 ? Math.Abs(dy) : Math.Abs(dx);

            if (primary <= 1f) continue;        // not in this direction
            if (perp >= primary) continue;      // outside 45° cone

            float distSq = dx * dx + dy * dy;
            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                best = element;
            }
        }

        // Second pass: if nothing in cone, take nearest element in the half-plane
        if (best == null)
        {
            foreach (UIElement element in candidates)
            {
                float cx = element.ScreenX + element.ComputedWidth / 2f;
                float cy = element.ScreenY + element.ComputedHeight / 2f;

                float dx = cx - scaledCursorX;
                float dy = cy - scaledCursorY;

                float primary = dpadX != 0 ? dx * dpadX : dy * dpadY;
                if (primary <= 1f) continue;

                float distSq = dx * dx + dy * dy;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    best = element;
                }
            }
        }

        if (best == null) return false;

        float bestCx = best.ScreenX + best.ComputedWidth / 2f;
        float bestCy = best.ScreenY + best.ComputedHeight / 2f;
        cursorX = bestCx * Game.displayWidth / res.ScaledWidth;
        cursorY = bestCy * Game.displayHeight / res.ScaledHeight;
        return true;
    }

    private static void CollectNavigable(UIElement element, List<UIElement> result, float screenW, float screenH)
    {
        if (!element.Visible || !element.IsHitTestVisible) return;

        if (element is ScrollView sv)
        {
            CollectNavigable(sv.ContentContainer, result, screenW, screenH);
        }

        foreach (UIElement child in element.Children)
        {
            CollectNavigable(child, result, screenW, screenH);
        }

        if (!element.Enabled) return;
        if (element is ScrollView) return;
        if (element.OnClick == null && element.OnMouseDown == null) return;
        if (element.ComputedWidth <= 0 || element.ComputedHeight <= 0) return;

        // Only include elements whose center is within the visible screen
        float cx = element.ScreenX + element.ComputedWidth / 2f;
        float cy = element.ScreenY + element.ComputedHeight / 2f;
        if (cx < 0 || cx > screenW || cy < 0 || cy > screenH) return;

        result.Add(element);
    }

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
        var button = (GamepadButton)Controller.GetEventButton();
        bool isDown = Controller.GetEventButtonState();

        if (button == GamepadButton.A && isDown)
        {
            ScaledResolution res = new(Game.options, Game.displayWidth, Game.displayHeight);
            float scaledX = Game.VirtualCursor.X * res.ScaledWidth / Game.displayWidth;
            float scaledY = Game.VirtualCursor.Y * res.ScaledHeight / Game.displayHeight;

            UIElement? target = Root.HitTest(scaledX, scaledY);

            // Holding A on a slider enters slider-edit mode instead of clicking
            if (target is Slider slider && slider.Enabled)
            {
                _editingSlider = slider;
                _sliderDpadHeldX = 0;
                return;
            }

            FocusedElement = target;
            if (target != null && target.Enabled)
            {
                var evt = new UIMouseEvent { Target = target, MouseX = (int)scaledX, MouseY = (int)scaledY, Button = MouseButton.Left };
                target.OnMouseDown?.Invoke(evt);
                target.OnClick?.Invoke(evt);
            }
        }
        else if (button == GamepadButton.B && isDown)
        {
            if (_editingSlider != null)
            {
                _editingSlider = null;
                _sliderDpadHeldX = 0;
                _sliderStickAccumulated = 0f;
            }
            else
            {
                KeyTyped(Keyboard.KEY_ESCAPE, '\0');
            }
        }
    }
}
