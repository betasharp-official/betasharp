using System.Globalization;
using BetaSharp.Entities;
using BetaSharp.Server.Command;

namespace BetaSharp.Server.Commands;

public class TeleportCommand : ICommand
{
    public string Usage => "tp <x> <y> <z> / <p1> <p2>";
    public string Description => "Teleport";
    public string[] Names => ["tp", "teleport"];

    public void Execute(ICommand.CommandContext c)
    {
        if (c.Args.Length == 3)
        {
            ServerPlayerEntity? sender = c.Server.playerManager.getPlayer(c.SenderName);
            if (sender == null)
            {
                c.Output.SendMessage("Could not find your player.");
                return;
            }

            if (float.TryParse(c.Args[0], CultureInfo.InvariantCulture, out float x) &&
                float.TryParse(c.Args[1], CultureInfo.InvariantCulture, out float y) &&
                float.TryParse(c.Args[2], CultureInfo.InvariantCulture, out float z))
            {
                sender.networkHandler.teleport(x, y, z, sender.yaw, sender.pitch);
                c.Output.SendMessage($"Teleported to {x}, {y}, {z}");
            }
            else
            {
                c.Output.SendMessage("Invalid coordinates. Usage: tp <x> <y> <z>");
            }
            return;
        }

        if (c.Args.Length == 2)
        {
            ServerPlayerEntity? source = c.Server.playerManager.getPlayer(c.Args[0]);
            ServerPlayerEntity? target = c.Server.playerManager.getPlayer(c.Args[1]);

            if (source == null)
            {
                c.Output.SendMessage("Can't find user " + c.Args[0] + ". No tp.");
            }
            else if (target == null)
            {
                c.Output.SendMessage("Can't find user " + c.Args[1] + ". No tp.");
            }
            else if (source.dimensionId != target.dimensionId)
            {
                c.Output.SendMessage("User " + c.Args[0] + " and " + c.Args[1] + " are in different dimensions. No tp.");
            }
            else
            {
                source.networkHandler.teleport(target.x, target.y, target.z, target.yaw, target.pitch);
                c.LogOp("Teleporting " + c.Args[0] + " to " + c.Args[1] + ".");
            }
            return;
        }

        c.Output.SendMessage("Usage: tp <x> <y> <z>  or  tp <player1> <player2>");
    }
}
