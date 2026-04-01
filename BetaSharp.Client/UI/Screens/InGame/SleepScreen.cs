using BetaSharp.Client.Entities;
using BetaSharp.Client.Network;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Layout.Flexbox;
using BetaSharp.Network.Packets.C2SPlay;

namespace BetaSharp.Client.UI.Screens.InGame;

public class SleepScreen(UIContext context, ClientPlayerEntity player) : UIScreen(context)
{
    public override bool PausesGame => false;

    protected override void Init()
    {
        Root.Style.AlignItems = Align.Center;

        Root.Style.JustifyContent = Justify.FlexEnd;
        Root.Style.PaddingBottom = 40;

        TranslationStorage translations = TranslationStorage.Instance;

        Button btnStopSleep = CreateButton();
        btnStopSleep.Text = translations.TranslateKey("multiplayer.stopSleeping");
        btnStopSleep.Style.Width = 200;
        btnStopSleep.OnClick += (_) => SendStopSleepingCommand();

        Root.AddChild(btnStopSleep);
    }

    public override void KeyTyped(int key, char character)
    {
        if (key == Input.Keyboard.KEY_ESCAPE)
        {
            SendStopSleepingCommand();
            Context.Navigator.Navigate(null);
        }
        else
        {
            base.KeyTyped(key, character);
        }
    }

    private void SendStopSleepingCommand()
    {
        if (player is EntityClientPlayerMP playerMP)
        {
            playerMP.sendQueue.AddToSendQueue(ClientCommandC2SPacket.Get(player, 3));
        }
    }
}
