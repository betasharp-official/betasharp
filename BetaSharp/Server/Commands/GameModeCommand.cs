using BetaSharp.Entities;
using BetaSharp.Network.Packets.S2CPlay;
using BetaSharp.Registries;
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
            .Then(ArgumentPlayer("player").Then(ArgumentString("gamemode").Executes(SetTargetGm)));

    private static int ListCommands(CommandContext<CommandSource> context)
    {
        IReadableRegistry<GameMode> registry = context.Source.Server.RegistryAccess.GetOrThrow(RegistryKeys.GameModes);
        foreach (ResourceLocation key in registry.Keys)
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
        ServerPlayerEntity p = context.GetArgument<ServerPlayerEntity>("player");
        SetGameMode(p, context.GetArgument<string>("gamemode"), context.Source);
        return 1;
    }

    private static int ShowGamemode(CommandContext<CommandSource> context)
    {
        ServerPlayerEntity p = context.Source.Server.playerManager.getPlayer(context.Source.SenderName)!;
        context.Source.Output.SendMessage(p.GameMode.Name);
        return 1;
    }

    private static void SetGameMode(ServerPlayerEntity p, string arg, CommandSource c)
    {
        if (c.Server.RegistryAccess.GetOrThrow(RegistryKeys.GameModes).AsAssetLoader().TryGetHolderByPrefix(arg, out Holder<GameMode>? holder))
        {
            SetGameMode(p, holder, c);
            return;
        }

        c.Output.SendMessage("Gamemode not found.");
    }

    private static void SetGameMode(ServerPlayerEntity p, Holder<GameMode> holder, CommandSource c)
    {
        p.GameModeHolder = holder;
        p.NetworkHandler.SendPacket(PlayerGameModeUpdateS2CPacket.Get(holder.Value));
        string s = $"{p.name} game mode set to {holder.Value.Name}.";
        s_logger.LogInformation(s);
        c.Output.SendMessage(s);
    }
}
