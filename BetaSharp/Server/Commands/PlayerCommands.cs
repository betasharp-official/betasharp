using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Server.Internal;

namespace BetaSharp.Server.Commands;

internal static class PlayerCommands
{
    public static void Kill(BetaSharpServer server, string senderName, string[] args, CommandOutput output)
    {
        ServerPlayerEntity player = server.playerManager.getPlayer(senderName);
        if (player == null) { output.SendMessage("Could not find your player."); return; }

        player.damage(null, 1000);
    }

    public static void Heal(BetaSharpServer server, string senderName, string[] args, CommandOutput output)
    {
        ServerPlayerEntity player = server.playerManager.getPlayer(senderName);
        if (player == null) { output.SendMessage("Could not find your player."); return; }

        int amount = 20;
        if (args.Length > 0 && int.TryParse(args[0], out int parsed))
        {
            amount = parsed;
        }

        player.heal(amount);
        output.SendMessage($"Healed for {amount} health.");
    }

    public static void Clear(BetaSharpServer server, string senderName, string[] args, CommandOutput output)
    {
        ServerPlayerEntity player = server.playerManager.getPlayer(senderName);
        if (player == null) { output.SendMessage("Could not find your player."); return; }

        ItemStack[] inventory = player.inventory.main;
        for (int i = 0; i < inventory.Length; i++)
        {
            inventory[i] = null;
        }

        output.SendMessage("Inventory cleared.");
    }

    public static void Teleport(BetaSharpServer server, string senderName, string[] args, CommandOutput output)
    {
        if (args.Length == 3)
        {
            ServerPlayerEntity sender = server.playerManager.getPlayer(senderName);
            if (sender == null) { output.SendMessage("Could not find your player."); return; }

            if (float.TryParse(args[0], out float x) &&
                float.TryParse(args[1], out float y) &&
                float.TryParse(args[2], out float z))
            {
                sender.networkHandler.teleport(x, y, z, sender.yaw, sender.pitch);
                output.SendMessage($"Teleported to {x}, {y}, {z}");
            }
            else
            {
                output.SendMessage("Invalid coordinates. Usage: tp <x> <y> <z>");
            }
            return;
        }

        if (args.Length == 2)
        {
            ServerPlayerEntity source = server.playerManager.getPlayer(args[0]);
            ServerPlayerEntity target = server.playerManager.getPlayer(args[1]);

            if (source == null)
            {
                output.SendMessage("Can't find user " + args[0] + ". No tp.");
            }
            else if (target == null)
            {
                output.SendMessage("Can't find user " + args[1] + ". No tp.");
            }
            else if (source.dimensionId != target.dimensionId)
            {
                output.SendMessage("User " + args[0] + " and " + args[1] + " are in different dimensions. No tp.");
            }
            else
            {
                source.networkHandler.teleport(target.x, target.y, target.z, target.yaw, target.pitch);
                AdminCommands.LogCommand(server, senderName, "Teleporting " + args[0] + " to " + args[1] + ".");
            }
            return;
        }

        output.SendMessage("Usage: tp <x> <y> <z>  or  tp <player1> <player2>");
    }

    public static void MoveToDimension(BetaSharpServer server, string senderName, string[] args, CommandOutput output)
    {
        if (args.Length < 1)
        {
            output.SendMessage("Usage: /tpdim <dimension id> [player]");
            return;
        }

        if (!int.TryParse(args[0], out int dim))
        {
            output.SendMessage("Invalid dimension ID.");
            return;
        }

        if (dim != 0 && dim != -1)
        {
            output.SendMessage("Dimension " + dim + " does not exist.");
            return;
        }

        ServerPlayerEntity targetPlayer;
        if (args.Length >= 2)
        {
            targetPlayer = server.playerManager.getPlayer(args[1]);
            if (targetPlayer == null)
            {
                output.SendMessage("Player " + args[1] + " not found.");
                return;
            }
        }
        else
        {
            targetPlayer = server.playerManager.getPlayer(senderName);
            if (targetPlayer == null)
            {
                output.SendMessage("Could not find your player.");
                return;
            }
        }

        if (targetPlayer.dimensionId == dim)
        {
            output.SendMessage("Player is already in dimension " + dim);
            return;
        }

        server.playerManager.sendPlayerToDimension(targetPlayer, dim);
        output.SendMessage("Teleported " + targetPlayer.name + " to dimension " + dim);
    }
}
