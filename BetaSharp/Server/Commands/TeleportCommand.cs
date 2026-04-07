using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using Brigadier.NET.Builder;
using Brigadier.NET.Context;

namespace BetaSharp.Server.Commands;

public class TeleportCommand : Command.Command
{
    public override string Usage => "tp <x> <y> <z> / <p1> <p2>";
    public override string Description => "Teleport";
    public override string[] Names => ["tp", "teleport"];

    public override LiteralArgumentBuilder<CommandSource> Register(LiteralArgumentBuilder<CommandSource> argBuilder) =>
        argBuilder
            .Then(ArgumentPlayer("player")
                .Then(ArgumentPlayer("target")
                    .Executes(TpTwoArgs)))
            .Then(ArgumentPos("position")
                .Executes(TpPos))
            .Then(ArgumentPlayer("player")
                .Then(ArgumentPos("position")
                    .Executes(TpPlayerPos)));

    // tp <p1> <p2>
    private static int TpTwoArgs(CommandContext<CommandSource> context)
    {
        EntityPlayer a1 = context.GetArgument<EntityPlayer>("player");
        EntityPlayer a2 = context.GetArgument<EntityPlayer>("target");

        if (a1.dimensionId != a2.dimensionId)
        {
            context.Source.Output.SendMessage("User " + a1.name + " and " + a2.name + " are in different dimensions. No tp.");
        }
        else
        {
            if (a1 is not ServerPlayerEntity e)
            {
                throw new Exception("Not a ServerPlayerEntity");
            }

            e.NetworkHandler.teleport(a2.x, a2.y, a2.z, e.yaw, e.pitch);
            context.Source.LogOp("Teleporting " + a1.name + " to " + a2.name + ".");
        }

        return 1;
    }

    // tp <x> <y> <z>
    private static int TpPos(CommandContext<CommandSource> context)
    {
        Vec3D pos = context.GetArgument<Vec3D>("position");

        ServerPlayerEntity? sender = context.Source.Server.playerManager.getPlayer(context.Source.SenderName);
        if (sender == null)
        {
            context.Source.Output.SendMessage("Could not find your player.");
            return 1;
        }

        sender.NetworkHandler.teleport(pos.x, pos.y, pos.z, sender.yaw, sender.pitch);
        context.Source.Output.SendMessage($"Teleported to {pos}");

        return 1;
    }

    private static int TpPlayerPos(CommandContext<CommandSource> context)
    {
        EntityPlayer a1 = context.GetArgument<EntityPlayer>("player");
        Vec3D pos = context.GetArgument<Vec3D>("position");

        if (a1 is not ServerPlayerEntity e)
        {
            throw new Exception("Not a ServerPlayerEntity");
        }

        e.NetworkHandler.teleport(pos.x, pos.y, pos.z, e.yaw, e.pitch);
        context.Source.Output.SendMessage($"Teleported to {pos}");

        return 1;
    }
}
