using System.Collections.Generic;
using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Rendering;
using System;

namespace BetaSharp.Client.UI.Controls;

public class ChatOverlay : UIElement
{
    private readonly BetaSharp _game;
    private readonly List<ChatLine> _messages = [];
    private string? _recordPlaying;
    private int _recordPlayingTimer;
    public int ScrollOffset { get; set; }
    public string? HoveredItemName { get; set; }

    public ChatOverlay(BetaSharp game)
    {
        _game = game;
        Style.Width = 320;
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
        _recordPlayingTimer = 120; // 6 seconds at 20 ticks
    }

    public void ScrollMessages(int amount)
    {
        ScrollOffset += amount;
        if (ScrollOffset < 0) ScrollOffset = 0;
        if (ScrollOffset > _messages.Count - 10) ScrollOffset = Math.Max(0, _messages.Count - 10);
    }

    public override void Update(float partialTicks)
    {
        base.Update(partialTicks);
        foreach (var msg in _messages) msg.UpdateCounter++;
        if (_recordPlayingTimer > 0) _recordPlayingTimer--;
    }

    public override void Render(UIRenderer renderer)
    {
        // Render chat messages
        int yOffset = 0;
        for (int i = 0; i < _messages.Count && i < 10; i++)
        {
            var msg = _messages[i];
            if (msg.UpdateCounter < 200)
            {
                float alpha = Math.Clamp(1.0f - msg.UpdateCounter / 200.0f, 0, 1);
                renderer.DrawRect(0, yOffset - 9, 320, 9, new Color(0, 0, 0, (byte)(100 * alpha)));
                renderer.DrawText(msg.Message, 0, yOffset - 9, new Color(255, 255, 255, (byte)(255 * alpha)));
                yOffset -= 9;
            }
        }

        // Render record playing
        if (_recordPlayingTimer > 0 && _recordPlaying != null)
        {
            renderer.DrawCenteredText(_recordPlaying, 160, -40, Color.White, shadow: true);
        }

        base.Render(renderer);
    }

    private class ChatLine(string message)
    {
        public string Message = message;
        public int UpdateCounter = 0;
    }
}
