using BetaSharp.Client.Guis.Controls;
using BetaSharp.Client.Input;
using BetaSharp.Client.Network;
using BetaSharp.Network.Packets.C2SPlay;

namespace BetaSharp.Client.Guis;

public class GuiSleepMP : GuiChat
{
    public GuiSleepMP()
    {
        Keyboard.enableRepeatEvents(true);
        TranslationStorage translations = TranslationStorage.Instance;
        Button stopSleepingButton = new(EffectiveWidth / 2 - 100, EffectiveHeight - 40, translations.TranslateKey("multiplayer.stopSleeping"));
        stopSleepingButton.Clicked += (_, _) => sendStopSleepingCommand();
        AddChild(stopSleepingButton);
    }

    public override void OnGuiClosed()
    {
        Keyboard.enableRepeatEvents(false);
    }

    protected override void OnKeyInput(KeyboardEventArgs e)
    {
        if (e.Key == Keyboard.KEY_ESCAPE)
        {
            sendStopSleepingCommand();
        }
        else if (e.Key == Keyboard.KEY_RETURN)
        {
            string trimmed = _message.Trim();
            if (trimmed.Length > 0)
            {
                MC.player.sendChatMessage(trimmed);
            }

            _message = "";
        }
    }

    private void sendStopSleepingCommand()
    {
        if (MC.player is EntityClientPlayerMP)
        {
            ClientNetworkHandler sendQueue = ((EntityClientPlayerMP)MC.player).sendQueue;
            sendQueue.addToSendQueue(new ClientCommandC2SPacket(MC.player, 3));
        }
    }
}
