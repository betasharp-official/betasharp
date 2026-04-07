using BetaSharp.Entities;
using BetaSharp.Network.Packets.S2CPlay;
using BetaSharp.Registries;
using BetaSharp.Server.Command;
using Brigadier.NET.Builder;
using Brigadier.NET.Context;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Server.Commands;

public class GameModeCommand : Command.Command
{
    private static readonly ILogger s_logger = Log.Instance.For(nameof(GameModeCommand));

    // ReSharper disable once StringLiteralTypo
    public override string Usage => "gamemode <player> <gamemode>";
    public override string Description => "Sets player gamemode";

    // ReSharper disable once StringLiteralTypo
    public override string[] Names => ["gamemode", "gm"];

    public override LiteralArgumentBuilder<CommandSource> Register(LiteralArgumentBuilder<CommandSource> argBuilder) =>
        argBuilder
            .Executes(ShowGamemode)
            .Then(Literal("list").Executes(ListCommands))
            .Then(ArgumentString("gamemode").Executes(SetSendersGm))
            .Then(ArgumentString("player").Then(ArgumentString("gamemode").Executes(SetTargetGm)));

    private static int ListCommands(CommandContext<CommandSource> context)
    {
        var registry = context.Source.Server.RegistryAccess.GetOrThrow(RegistryKeys.GameModes);
        foreach (var key in registry.Keys)
        {
            context.Source.Output.SendMessage(key.ToString());
        }

        return 1;
    }

    private static int SetSendersGm(CommandContext<CommandSource> context)
    {
        SetGameMode(context.Source.Server.playerManager.getPlayer(context.Source.SenderName)!, context.GetArgument<string>("gamemode"), context.Source);
        return 1;
    }

    private static int SetTargetGm(CommandContext<CommandSource> context)
    {
        var p = context.Source.Server.playerManager.getPlayer(context.GetArgument<string>("player"));
        if (p == null) context.Source.Output.SendMessage("Player not found.");
        else SetGameMode(context.Source.Server.playerManager.getPlayer(context.Source.SenderName)!, context.GetArgument<string>("gamemode"), context.Source);
        return 1;
    }

    private static int ShowGamemode(CommandContext<CommandSource> context)
    {
        var p = context.Source.Server.playerManager.getPlayer(context.Source.SenderName)!;
        context.Source.Output.SendMessage(p.GameMode.Name);
        return 1;
    }

    private static void SetGameMode(ServerPlayerEntity p, string arg, CommandSource c)
    {
        if (c.Server.RegistryAccess.GetOrThrow(RegistryKeys.GameModes).AsAssetLoader().TryGetByPrefix(arg, out var gameMode))
        {
            SetGameMode(p, gameMode, c);
            return;
        }

        c.Output.SendMessage("Gamemode not found.");
    }

    private static void SetGameMode(ServerPlayerEntity p, GameMode gameMode, CommandSource c)
    {
        p.networkHandler.sendPacket(PlayerGameModeUpdateS2CPacket.Get(gameMode));
        p.GameMode = gameMode;
        string s = $"{p.name} game mode set to {gameMode.Name}.";
        s_logger.LogInformation(s);
        c.Output.SendMessage(s);
    }
}
