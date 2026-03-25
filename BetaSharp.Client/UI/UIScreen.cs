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
    public UIElement? FocusedElement { get; set; }
    public UIElement? DraggingElement { get; set; }

    public UIScreen(BetaSharp game)
    {
        Game = game;
        Root = new UIElement();
        Root.Style.Width = null; // Auto fill screen
        Root.Style.Height = null;
        Renderer = new UIRenderer(game.fontRenderer, game.textureManager);

        Init();
    }

    protected abstract void Init();
    public virtual void Uninit() { }

    public void Update(float partialTicks)
    {
        Root.Update(partialTicks);
    }

    public void Render(int mouseX, int mouseY, float partialTicks)
    {
        ScaledResolution res = new(Game.options, Game.displayWidth, Game.displayHeight);

        Root.Style.Width = (float)res.ScaledWidthDouble;
        Root.Style.Height = (float)res.ScaledHeightDouble;

        FlexLayout.ApplyLayout(Root, (float)res.ScaledWidthDouble, (float)res.ScaledHeightDouble);

        float scaledMouseX = mouseX;
        float scaledMouseY = mouseY;

        UpdateHovers(scaledMouseX, scaledMouseY);

        Renderer.Begin();
        Root.Render(Renderer);
        UIRenderer.End();
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

            if (_hoveredElement != null)
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

            if (FocusedElement != target)
            {
                if (FocusedElement != null) FocusedElement.IsFocused = false;
                FocusedElement = target;
                if (FocusedElement != null) FocusedElement.IsFocused = true;
            }

            if (target != null)
            {
                var evt = new UIMouseEvent { Target = target, MouseX = (int)scaledX, MouseY = (int)scaledY, Button = button };
                target.OnMouseDown?.Invoke(evt);
                DraggingElement = target;

                if (button == MouseButton.Left)
                {
                    target.OnClick?.Invoke(evt);
                }
            }
        }
        else
        {
            int rawButton = Mouse.getEventButton();
            if (rawButton != -1) // -1 means moved, not button up
            {
                MouseButton button = Enum.IsDefined(typeof(MouseButton), rawButton) ? (MouseButton)rawButton : MouseButton.Unknown;

                UIElement? target = Root.HitTest(scaledX, scaledY);
                if (target != null)
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
                    current.OnMouseScroll?.Invoke(scrollEvt);
                    if (scrollEvt.Handled) break;
                    current = current.Parent;
                }
            }
        }
    }

    public void HandleKeyboardInput()
    {
        if (Keyboard.getEventKeyState())
        {
            if (FocusedElement != null)
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
        }
    }
}
