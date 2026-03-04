using BetaSharp.Client.Rendering.Core;
using Silk.NET.OpenGL.Legacy;

namespace BetaSharp.Client.Guis;

public partial class Control
{
    public event EventHandler<MouseEventArgs>? MousePressed;
    protected virtual void OnMousePress(MouseEventArgs e) { }
    public void DoMousePress(MouseEventArgs e)
    {
        if (Focusable)
        {
            var screen = ParentScreen;
            if (screen != null)
            {
                screen.SetFocus(this); // Will do nothing if already focused
            }
            else
            {
                Focused = true;
            }
        }
        _pressedInside = true;
        OnMousePress(e);
        MousePressed?.Invoke(this, e);
    }

    public event EventHandler<MouseEventArgs>? MouseReleased;
    protected virtual void OnMouseRelease(MouseEventArgs e) { }
    public void DoMouseRelease(MouseEventArgs e)
    {
        OnMouseRelease(e);
        MouseReleased?.Invoke(this, e);

        if (_pressedInside && PointInBounds(e.X, e.Y))
        {
            DoClick(e);
        }

        _pressedInside = false;
    }

    public event EventHandler<MouseEventArgs>? Clicked;
    protected virtual void OnClick(MouseEventArgs e) { }
    public void DoClick(MouseEventArgs e)
    {
        OnClick(e);
        Clicked?.Invoke(this, e);
    }

    public event EventHandler<MouseEventArgs>? MouseMoved;
    protected virtual void OnMouseMove(MouseEventArgs e) { }
    public void DoMouseMove(MouseEventArgs e)
    {
        OnMouseMove(e);
        MouseMoved?.Invoke(this, e);
    }

    public event EventHandler<MouseEventArgs>? MouseDragged;
    protected virtual void OnMouseDrag(MouseEventArgs e) { }
    public void DoMouseDrag(MouseEventArgs e)
    {
        OnMouseDrag(e);
        MouseDragged?.Invoke(this, e);
    }

    public event EventHandler<KeyboardEventArgs>? KeyInput;
    protected virtual void OnKeyInput(KeyboardEventArgs e) { }
    public void DoKeyInput(KeyboardEventArgs e)
    {
        OnKeyInput(e);
        KeyInput?.Invoke(this, e);
    }

    public event EventHandler<RenderEventArgs>? Rendered;
    protected virtual void OnRender(RenderEventArgs e) { }
    public void DoRender(RenderEventArgs e)
    {
        if (!Visible) return;

        OnRender(e);
        Rendered?.Invoke(this, e);

        Point abs = AbsolutePosition;

        bool wasScissorEnabled = GLManager.GL.IsEnabled(EnableCap.ScissorTest);
        Span<int> prevScissor = stackalloc int[4];
        GLManager.GL.GetInteger(GetPName.ScissorBox, prevScissor);

        var mc = Minecraft.INSTANCE;
        ScaledResolution res = new(mc.options, mc.displayWidth, mc.displayHeight);
        int scale = (int)Math.Round(mc.displayWidth / res.ScaledWidthDouble);

        int scissorX = abs.X * scale;
        int scissorY = (mc.displayHeight - (abs.Y + Height) * scale);
        int scissorW = Width * scale;
        int scissorH = Height * scale;

        if (wasScissorEnabled)
        {
            int newRight = Math.Min(scissorX + scissorW, prevScissor[0] + prevScissor[2]);
            int newTop = Math.Min(scissorY + scissorH, prevScissor[1] + prevScissor[3]);
            scissorX = Math.Max(scissorX, prevScissor[0]);
            scissorY = Math.Max(scissorY, prevScissor[1]);
            scissorW = Math.Max(0, newRight - scissorX);
            scissorH = Math.Max(0, newTop - scissorY);
        }

        GLManager.GL.Enable(EnableCap.ScissorTest);
        GLManager.GL.Scissor(scissorX, scissorY, (uint)scissorW, (uint)scissorH);

        GLManager.GL.PushMatrix();
        GLManager.GL.Translate(X, Y, ZLevel);
        foreach (Control child in Children.ToArray())
        {
            child.DoRender(e);
        }
        GLManager.GL.PopMatrix();

        if (wasScissorEnabled)
        {
            GLManager.GL.Scissor(prevScissor[0], prevScissor[1],
                (uint)prevScissor[2], (uint)prevScissor[3]);
        }
        else
        {
            GLManager.GL.Disable(EnableCap.ScissorTest);
        }
    }

    public event EventHandler<FocusEventArgs>? FocusChanged;
    protected virtual void OnFocusChanged(FocusEventArgs e) { }
    public void DoFocusChanged(FocusEventArgs e)
    {
        OnFocusChanged(e);
        FocusChanged?.Invoke(this, e);
    }
    private int _suppressFocusChangedCounter;
    protected bool ShouldSuppressFocusChanged => _suppressFocusChangedCounter > 0;
    protected void SuppressFocusChanged(bool suppress)
    {
        _suppressFocusChangedCounter = Math.Max(_suppressFocusChangedCounter + (suppress ? 1 : -1), 0);
    }

    public event EventHandler<TextEventArgs>? TextChanged;
    protected virtual void OnTextChanged(TextEventArgs e) { }
    public void DoTextChanged(TextEventArgs e)
    {
        OnTextChanged(e);
        TextChanged?.Invoke(this, e);
    }
    private int _suppressTextChangedCounter;
    protected bool ShouldSuppressTextChanged => _suppressTextChangedCounter > 0;
    protected void SuppressTextChanged(bool suppress)
    {
        _suppressTextChangedCounter = Math.Max(_suppressTextChangedCounter + (suppress ? 1 : -1), 0);
    }

    public event EventHandler? Ticked;
    protected virtual void OnTick() { }
    public void DoTick()
    {
        OnTick();
        Ticked?.Invoke(this, EventArgs.Empty);

        foreach (Control child in Children)
        {
            child.DoTick();
        }
    }
}
