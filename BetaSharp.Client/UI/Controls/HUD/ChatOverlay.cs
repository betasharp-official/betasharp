using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Rendering;

namespace BetaSharp.Client.UI.Controls.HUD;

public class ChatOverlay : UIElement
{
    private const int LineHeight = 9;
    private const int MaxHistoryLines = 20;
    private const int ChatWidth = 320;

    private readonly List<ChatLine> _messages = [];
    private string? _recordPlaying;
    private int _recordPlayingTimer;
    public int ScrollOffset { get; set; }
    public string? HoveredItemName { get; set; }
    public bool IsOpen { get; set; }

    public ChatOverlay()
    {
        Style.Width = ChatWidth;
        Style.Height = null; // Auto wrap
    }

    public void AddMessage(string message)
    {
        _messages.Insert(0, new ChatLine(message));
        if (_messages.Count > 100) _messages.RemoveAt(_messages.Count - 1);
    }

    public void ClearMessages() => _messages.Clear();

    public void SetRecordPlaying(string recordName)
    {
        _recordPlaying = "Now playing: " + recordName;
        _recordPlayingTimer = 120; // 6 seconds
    }

    public void ScrollMessages(int amount)
    {
        ScrollOffset += amount;
        if (ScrollOffset < 0) ScrollOffset = 0;
        int maxScroll = Math.Max(0, _messages.Count - MaxHistoryLines);
        if (ScrollOffset > maxScroll) ScrollOffset = maxScroll;
    }

    public override void Update(float partialTicks)
    {
        base.Update(partialTicks);
        foreach (ChatLine msg in _messages) msg.UpdateCounter++;
        if (_recordPlayingTimer > 0) _recordPlayingTimer--;
    }

    public override void Render(UIRenderer renderer)
    {
        if (IsOpen)
        {
            RenderHistory(renderer);
        }
        else
        {
            RenderFading(renderer);
        }

        // Render record playing
        if (_recordPlayingTimer > 0 && _recordPlaying != null)
        {
            renderer.DrawCenteredText(_recordPlaying, 160, -40, Color.White, shadow: true);
        }

        base.Render(renderer);
    }

    private void RenderFading(UIRenderer renderer)
    {
        int yOffset = 0;
        for (int i = 0; i < _messages.Count && i < 10; i++)
        {
            ChatLine msg = _messages[i];
            if (msg.UpdateCounter < 200)
            {
                float progress = msg.UpdateCounter / 200.0f;
                float alpha = Math.Clamp((1.0f - progress) * 10.0f, 0, 1);
                alpha *= alpha; // Non-linear fade out

                renderer.DrawRect(0, yOffset - LineHeight, ChatWidth, LineHeight, new Color(0, 0, 0, (byte)(100 * alpha)));
                renderer.DrawText(msg.Message, 0, yOffset - LineHeight, new Color(255, 255, 255, (byte)(255 * alpha)));
                yOffset -= LineHeight;
            }
        }
    }

    private void RenderHistory(UIRenderer renderer)
    {
        int visibleCount = Math.Min(MaxHistoryLines, _messages.Count - ScrollOffset);
        if (visibleCount <= 0) return;

        // Render messages bottom-up
        int yOffset = 0;
        for (int i = ScrollOffset; i < ScrollOffset + visibleCount; i++)
        {
            renderer.DrawRect(0, yOffset - LineHeight, ChatWidth, LineHeight, new Color(0, 0, 0, 100));
            renderer.DrawText(_messages[i].Message, 0, yOffset - LineHeight, Color.White);
            yOffset -= LineHeight;
        }

        // Scroll indicator when not at the bottom
        if (ScrollOffset > 0)
        {
            renderer.DrawText("^  ^  ^", ChatWidth / 2 - 20, yOffset - LineHeight, new Color(255, 255, 255, 180), shadow: false);
        }
    }

    private class ChatLine(string message)
    {
        public string Message = message;
        public int UpdateCounter = 0;
    }
}
