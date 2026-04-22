using BetaSharp.Client.Input;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Controls.HUD;
using BetaSharp.Client.UI.Layout;
using BetaSharp.Client.UI.Layout.Flexbox;
using BetaSharp.Client.UI.Rendering;
using Silk.NET.GLFW;
using Silk.NET.Maths;

namespace BetaSharp.Client.UI;

/// <summary>
/// Base class for all game screens.
///
/// SCREEN LAYOUT CONVENTIONS
/// --------------------------
/// Screens use a top-aligned flexbox column (JustifyContent = FlexStart, AlignItems = Center).
/// To match Minecraft's proportional feel across GUI scales, follow these rules:
///
/// 1. TITLE
///    - MarginTop = 20, height ~8px, MarginBottom = 8  →  bottom edge lands at y ≈ 36.
///    - This is the default assumption for AddTitleSpacer().
///
/// 2. TITLE SPACER  (call AddTitleSpacer() right after Root.AddChild(title))
///    - Inserts a dynamic gap so that the content below starts at height/4 from the top.
///    - At small (auto) scales the spacer collapses to 0; at lower GUI scales it grows.
///    - If extra elements exist between the title and the main content (e.g. a tab bar),
///      pass their total height as titleBottomY so the spacer accounts for them.
///    - The main menu passes contentStartOffset = 48 because Minecraft targets height/4 + 48
///      for its first button.
///
/// 3. SCROLLABLE / MAIN CONTENT AREA
///    - Use FlexGrow = 1 so the area fills available space on small screens.
///    - Add MaxHeight to cap the growth and prevent the Done button from drifting to the
///      very bottom on large screens. The standard cap is 200px.
///    - If extra elements sit between the spacer and the content panel (tabs, progress bar,
///      etc.), reduce MaxHeight by their total height so the Done button aligns across screens.
///      Formula: MaxHeight = 200 - (height of elements between spacer and content panel).
///
/// 4. DONE / CLOSE BUTTON
///    - MarginBottom = 20  (matches the 20px bottom margin used in BaseOptionsScreen).
///    - When the content is at its MaxHeight cap, the Done button lands at height/4 + 210
///      (spacer target + MaxHeight 200 + scroll margin 10) on all conforming screens.
///    - When the content is not capped (small screens), Done sits 20px from the screen edge.
/// </summary>
public abstract class UIScreen
{
    protected UIContext Context { get; }
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
    public Func<Button> CreateButton { get; }
    public Func<Slider> CreateSlider { get; }
    protected virtual bool AutoAddTooltipBar => true;

    private bool _isInitialized;

    private Panel? _titleSpacer;
    private float _titleSpacerOffset;

    private Slider? _editingSlider;
    private int _sliderDpadHeldX;
    private int _sliderDpadRepeatTicksRemaining;
    private float _sliderStickAccumulated;
    private const int SliderDpadInitialDelay = 10; // ticks before first repeat
    private const int SliderDpadRepeatInterval = 3; // ticks between subsequent repeats
    private const float SliderStepsPerSecond = 10f; // steps/sec at full stick deflection

    public bool IsEditingSlider => _editingSlider != null;

    private ScaledResolution CurrentScaledResolution
    {
        get
        {
            Vector2D<int> s = Context.InputDisplaySize;
            return new(Context.Options, s.X, s.Y);
        }
    }

    private Vector2D<float> ToScaledCoords(float x, float y, ScaledResolution res)
    {
        Vector2D<int> s = Context.InputDisplaySize;
        return new(x * res.ScaledWidth / s.X, y * res.ScaledHeight / s.Y);
    }

    public UIScreen(UIContext context)
    {
        Context = context;
        Root = new UIElement();
        Root.Style.Width = null;
        Root.Style.Height = null;
        Renderer = new UIRenderer(context.TextRenderer, context.TextureManager, context.Options, context.DisplaySize);

        CreateButton = () => new(context.PlayClickSound);
        CreateSlider = () => new(context.PlayClickSound);
    }

    public void Initialize()
    {
        Keyboard.enableRepeatEvents(true);
        if (!_isInitialized)
        {
            Init();

            if (AutoAddTooltipBar)
            {
                var tooltipBar = new ControlTooltipBar(Context, this);
                tooltipBar.Style.Position = PositionType.Absolute;
                tooltipBar.Style.Bottom = 4;
                tooltipBar.Style.Left = 2;
                tooltipBar.Style.MarginLeft = 16;
                tooltipBar.Style.MarginBottom = 4;
                Root.AddChild(tooltipBar);
            }

            _isInitialized = true;
        }
        OnEnter();
    }

    protected abstract void Init();
    public virtual void OnEnter() { }

    /// <summary>
    /// Call in Init() right after adding the title/header. Inserts a spacer that grows
    /// proportionally so the content below starts at roughly <c>height/4 + contentStartOffset</c>.
    /// </summary>
    /// <param name="titleBottomY">Approximate Y of the header's bottom edge (padding + header height + margin).</param>
    /// <param name="contentStartOffset">Added to height/4 as the target Y for the content. Default 0.</param>
    protected void AddTitleSpacer(float titleBottomY = 36f, float contentStartOffset = 0f)
    {
        _titleSpacer = new Panel();
        _titleSpacerOffset = titleBottomY - contentStartOffset;
        Root.AddChild(_titleSpacer);
    }

    protected virtual void PreLayout(float scaledWidth, float scaledHeight)
    {
        _titleSpacer?.Style.Height = Math.Max(0f, scaledHeight / 4f - _titleSpacerOffset);
    }

    public virtual void Uninit()
    {
        Keyboard.enableRepeatEvents(false);
    }

    public void HandleInput()
    {
        while (Mouse.next())
        {
            if (Mouse.getEventDX() != 0 || Mouse.getEventDY() != 0 || Mouse.getEventButton() != -1)
            {
                Context.ControllerState.IsControllerMode = false;
                Mouse.setCursorVisible(true);
            }
            HandleMouseInput();
        }
        while (Keyboard.Next())
        {
            Context.ControllerState.IsControllerMode = false;

            if (ImGuiInput.CapturingKeyboard)
            {
                continue;
            }

            HandleKeyboardInput();
        }
        ControllerManager.UpdateUI(this);
        HandleControllerScroll();
        if (_editingSlider != null) HandleSliderEditTick();
    }

    private void HandleControllerScroll()
    {
        if (!Context.ControllerState.IsControllerMode) return;

        float ry = Controller.RightStickY;
        if (ry == 0f) return;

        ScaledResolution res = CurrentScaledResolution;
        Vector2D<float> scaled = ToScaledCoords(Context.VirtualCursor.X, Context.VirtualCursor.Y, res);

        UIElement? current = Root.HitTest(scaled.X, scaled.Y);
        while (current != null)
        {
            if (current is ScrollView sv && sv.Enabled && sv.MaxScrollY > 0)
            {
                sv.ScrollBy(ry * 300f / Context.Timer.ticksPerSecond);
                break;
            }
            current = current.Parent;
        }
    }

    private void HandleSliderEditTick()
    {
        if (!Controller.IsButtonDown(GamepadButton.A))
        {
            CancelSliderEdit();
            return;
        }

        float step = _editingSlider!.Step;

        // Left stick: accumulate fractional steps so we always move in whole units
        float lx = Controller.LeftStickX;
        if (lx != 0f)
        {
            _sliderStickAccumulated += lx * SliderStepsPerSecond / Context.Timer.ticksPerSecond;
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

    private void CancelSliderEdit()
    {
        _editingSlider = null;
        _sliderDpadHeldX = 0;
        _sliderStickAccumulated = 0f;
    }

    public virtual bool HandleDPadNavigation(int dpadX, int dpadY, ref float cursorX, ref float cursorY)
    {
        ScaledResolution res = CurrentScaledResolution;
        Vector2D<float> scaledCursor = ToScaledCoords(cursorX, cursorY, res);

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

            float dx = cx - scaledCursor.X;
            float dy = cy - scaledCursor.Y;

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

                float dx = cx - scaledCursor.X;
                float dy = cy - scaledCursor.Y;

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
        Vector2D<int> inputSize = Context.InputDisplaySize;
        cursorX = bestCx * inputSize.X / res.ScaledWidth;
        cursorY = bestCy * inputSize.Y / res.ScaledHeight;
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

        // Reject elements clipped by an ancestor ScrollView
        UIElement? ancestor = element.Parent;
        while (ancestor != null)
        {
            if (ancestor is ScrollView scrollAncestor)
            {
                if (cx < scrollAncestor.ScreenX || cx > scrollAncestor.ScreenX + scrollAncestor.ComputedWidth ||
                    cy < scrollAncestor.ScreenY || cy > scrollAncestor.ScreenY + scrollAncestor.ComputedHeight)
                    return;
            }
            ancestor = ancestor.Parent;
        }

        result.Add(element);
    }

    public bool HasInteractiveElementUnderCursor()
    {
        UIElement? el = _hoveredElement;
        return el != null && el.Enabled && el is not ScrollView && (el.OnClick != null || el.OnMouseDown != null);
    }

    protected UIElement? GetElementUnderVirtualCursor()
    {
        ScaledResolution res = CurrentScaledResolution;
        Vector2D<float> scaled = ToScaledCoords(Context.VirtualCursor.X, Context.VirtualCursor.Y, res);
        return Root.HitTest(scaled.X, scaled.Y);
    }

    public virtual void GetTooltips(List<ActionTip> tips) { }

    public virtual void Update(float partialTicks)
    {
        Root.Update(partialTicks);
    }

    public virtual void Render(int mouseX, int mouseY, float partialTicks)
    {
        ScaledResolution res = CurrentScaledResolution;

        Root.Style.Width = res.ScaledWidth;
        Root.Style.Height = res.ScaledHeight;

        PreLayout(res.ScaledWidth, res.ScaledHeight);

        FlexLayout.LayoutContext layoutContext = new()
        {
            Root = Root,
            AvailableWidth = res.ScaledWidth,
            AvailableHeight = res.ScaledHeight,
            MeasureString = (s) => Renderer.TextRenderer.GetStringWidth(s)
        };

        FlexLayout.ApplyLayout(layoutContext);

        MouseX = mouseX;
        MouseY = mouseY;

        UpdateHovers(mouseX, mouseY);

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
        Vector2D<int> inputSize = Context.InputDisplaySize;
        ScaledResolution res = new(Context.Options, inputSize.X, inputSize.Y);
        Vector2D<int> offset = Context.MouseOffset;
        float scaledX = (Mouse.getEventX() - offset.X) * res.ScaledWidth / (float)inputSize.X;
        float scaledY = res.ScaledHeight - (Mouse.getEventY() - offset.Y) * res.ScaledHeight / (float)inputSize.Y - 1f;

        if (Mouse.getEventButtonState())
        {
            HandleMouseButtonDown(scaledX, scaledY);
        }
        else
        {
            HandleMouseButtonUpOrMove(scaledX, scaledY);
        }

        HandleMouseScroll(scaledX, scaledY);
    }

    private void HandleMouseButtonDown(float scaledX, float scaledY)
    {
        MouseButton button = ParseMouseButton(Mouse.getEventButton());
        UIElement? target = Root.HitTest(scaledX, scaledY);

        FocusedElement = target;

        if (target != null && target.Enabled)
        {
            var evt = new UIMouseEvent { Target = target, MouseX = (int)scaledX, MouseY = (int)scaledY, Button = button };
            target.OnMouseDown?.Invoke(evt);
            DraggingElement = target;

            if (button == MouseButton.Left)
                target.OnClick?.Invoke(evt);
        }
        else
        {
            DraggingElement = null; // Don't drag if disabled
        }
    }

    private void HandleMouseButtonUpOrMove(float scaledX, float scaledY)
    {
        int rawButton = Mouse.getEventButton();
        if (rawButton != -1) // -1 means moved, not button up
        {
            MouseButton button = ParseMouseButton(rawButton);
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

    private void HandleMouseScroll(float scaledX, float scaledY)
    {
        int dWheel = Mouse.getEventDWheel();
        if (dWheel == 0) return;

        UIElement? target = Root.HitTest(scaledX, scaledY);
        if (target == null) return;

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

    private static MouseButton ParseMouseButton(int rawButton) =>
        Enum.IsDefined(typeof(MouseButton), rawButton) ? (MouseButton)rawButton : MouseButton.Unknown;

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
                if (evt.Handled) return;
            }

            KeyTyped(Keyboard.getEventKey(), Keyboard.getEventCharacter());
        }
    }

    public virtual void KeyTyped(int key, char character)
    {
        if (key == Keyboard.KEY_ESCAPE || key == Keyboard.KEY_NONE)
        {
            Uninit();
            Context.Navigator.Navigate(null);
        }
    }

    public virtual void HandleControllerInput()
    {
        var button = (GamepadButton)Controller.GetEventButton();
        bool isDown = Controller.GetEventButtonState();

        if (button == GamepadButton.A && isDown)
        {
            ScaledResolution res = CurrentScaledResolution;
            Vector2D<float> scaled = ToScaledCoords(Context.VirtualCursor.X, Context.VirtualCursor.Y, res);

            UIElement? target = Root.HitTest(scaled.X, scaled.Y);

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
                var evt = new UIMouseEvent { Target = target, MouseX = (int)scaled.X, MouseY = (int)scaled.Y, Button = MouseButton.Left };
                target.OnMouseDown?.Invoke(evt);
                target.OnClick?.Invoke(evt);
            }
        }
        else if (button == GamepadButton.B && isDown)
        {
            if (_editingSlider != null)
                CancelSliderEdit();
            else
                KeyTyped(Keyboard.KEY_ESCAPE, '\0');
        }
    }
}
