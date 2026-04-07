using BetaSharp.Entities;
using BetaSharp.Server.Command;

namespace BetaSharp.Server.Commands;

public class HealCommand : Command.Command
{
    public string Usage => "heal [amount]";
    public string Description => "Heals yourself";
    public string[] Names => ["heal"];

    public void Execute(Command.Command.CommandSource c)
    {
        ServerPlayerEntity? player = c.Server.playerManager.getPlayer(c.SenderName);
        if (player == null)
        {
            c.Output.SendMessage("Could not find your player.");
            return;
        }

        int amount = 20;
        if (c.Args.Length > 0 && int.TryParse(c.Args[0], out int parsed))
        {
            amount = parsed;
        }

        player.heal(amount);
        c.Output.SendMessage($"Healed for {amount} health.");
    }
}
