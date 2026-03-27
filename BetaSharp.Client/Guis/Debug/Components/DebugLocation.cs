using System.ComponentModel;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Generation.Biomes;

namespace BetaSharp.Client.Guis.Debug.Components;

[DisplayName("Location")]
[Description("Shows info about your location.")]
public class DebugLocation : DebugComponent
{
    private static readonly string[] s_cardinalDirections = ["south", "west", "north", "east"];
    private static readonly string[] s_towards = ["positive Z", "negative X", "negative Z", "positive X"];

    public DebugLocation() { }

    public override void Draw(DebugContext ctx)
    {
        double x = Math.Floor(ctx.Game.player.x * 1000) / 1000;
        double y = Math.Floor(ctx.Game.player.y * 100000) / 100000;
        double z = Math.Floor(ctx.Game.player.z * 1000) / 1000;

        int bx = (int)Math.Floor(ctx.Game.player.x);
        int by = (int)Math.Floor(ctx.Game.player.y);
        int bz = (int)Math.Floor(ctx.Game.player.z);

        int facingIndex = MathHelper.Floor((double)(ctx.Game.player.yaw * 4.0F / 360.0F) + 0.5D) & 3;
        string cardinalDirection = GetCardinalDirection(facingIndex);
        string verticalLookDirection = GetVerticalLookDirection(ctx.Game.player.pitch);
        string towards = GetTowards(facingIndex);

        double yaw = Math.Floor(WrapYaw(ctx.Game.player.yaw) * 10) / 10;
        double pitch = Math.Floor(ctx.Game.player.pitch * 10) / 10;

        Biome biome = ctx.Game.world.Dimension.BiomeSource.GetBiome(bx, bz);
        int light = ctx.Game.world.Lighting.GetLightLevel(bx, by, bz);

        ctx.String("XYZ: " + x + " / " + y + " / " + z);
        ctx.String("Block: " + bx + " " + by + " " + bz);
        ctx.String("Facing: " + cardinalDirection + " " + verticalLookDirection + " (" +
            towards + ") (" + yaw + " / " + pitch + ")");
        ctx.String("Biome: " + biome.Name);
        ctx.String("Light: " + light);
    }

    private static string GetTowards(int facingIndex)
    {
        return facingIndex >= 0 && facingIndex < s_towards.Length
            ? "Towards " + s_towards[facingIndex]
            : "Towards N/A";
    }
    private static string GetCardinalDirection(int facingIndex)
    {
        return facingIndex >= 0 && facingIndex < s_cardinalDirections.Length
            ? s_cardinalDirections[facingIndex]
            : "N/A";
    }

    private static string GetVerticalLookDirection(float pitch)
    {
        if (pitch <= -45.0F)
        {
            return "up";
        }

        if (pitch >= 45.0F)
        {
            return "down";
        }

        return "level";
    }

    private static float WrapYaw(float yaw)
    {
        yaw %= 360f;

        if (yaw >= 180f) yaw -= 360f;
        if (yaw < -180f) yaw += 360f;

        return yaw;
    }

    public override DebugComponent Duplicate()
    {
        return new DebugLocation()
        {
            Right = Right
        };
    }
}
