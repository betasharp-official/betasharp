using BetaSharp.Blocks;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using Hexa.NET.ImGui;

namespace BetaSharp.Client.Diagnostics.Windows;

internal sealed class LocalPlayerInfoWindow(DebugWindowContext ctx) : DebugWindow
{
    private static readonly string[] s_cardinalDirections = ["south", "west", "north", "east"];
    private static readonly string[] s_towards = ["positive Z", "negative X", "negative Z", "positive X"];

    public override string Title => "Local Player";

    protected override void OnDraw()
    {
        if (ctx.Player == null || ctx.World == null)
        {
            ImGui.TextDisabled("No player in world.");
            return;
        }

        if (ImGui.CollapsingHeader("Position", ImGuiTreeNodeFlags.DefaultOpen))
        {
            DrawPositionSection();
        }

        if (ImGui.CollapsingHeader("Targeted Block", ImGuiTreeNodeFlags.DefaultOpen))
        {
            DrawTargetedBlockSection();
        }
    }

    private void DrawPositionSection()
    {
        double x = Math.Floor(ctx.Player.X * 1000) / 1000;
        double y = Math.Floor(ctx.Player.Y * 100000) / 100000;
        double z = Math.Floor(ctx.Player.Z * 1000) / 1000;

        int bx = (int)Math.Floor(ctx.Player.X);
        int by = (int)Math.Floor(ctx.Player.Y);
        int bz = (int)Math.Floor(ctx.Player.Z);

        int facingIndex = MathHelper.Floor((double)(ctx.Player.Yaw * 4.0F / 360.0F) + 0.5D) & 3;
        string cardinal = facingIndex is >= 0 and < 4 ? s_cardinalDirections[facingIndex] : "N/A";
        string towards = facingIndex is >= 0 and < 4 ? s_towards[facingIndex] : "N/A";
        string vertical = ctx.Player.Pitch <= -45f ? "up" : ctx.Player.Pitch >= 45f ? "down" : "level";

        float yaw = ctx.Player.Yaw % 360f;
        if (yaw >= 180f) yaw -= 360f;
        if (yaw < -180f) yaw += 360f;
        float pitch = ctx.Player.Pitch;

        string biome = ctx.World.Dimension.BiomeSource.GetBiome(bx, bz).Name;
        int light = ctx.World.Lighting.GetLightLevel(bx, by, bz);

        ImGui.Text($"XYZ:    {x:F3} / {y:F5} / {z:F3}");
        ImGui.Text($"Block:  {bx} {by} {bz}");
        ImGui.Text($"Facing: {cardinal} {vertical} (towards {towards})");
        ImGui.Text($"Yaw / Pitch: {yaw:F1} / {pitch:F1}");
        ImGui.Text($"Biome:  {biome}");
        ImGui.Text($"Light:  {light}");
    }

    private void DrawTargetedBlockSection()
    {
        if (ctx.ObjectMouseOver.Type != HitResultType.TILE)
        {
            ImGui.TextDisabled("Nothing targeted.");
            return;
        }

        int bx = ctx.ObjectMouseOver.BlockX;
        int by = ctx.ObjectMouseOver.BlockY;
        int bz = ctx.ObjectMouseOver.BlockZ;
        int id = ctx.World.Reader.GetBlockId(bx, by, bz);
        int meta = ctx.World.Reader.GetBlockMeta(bx, by, bz);
        Side side = ctx.ObjectMouseOver.Side.ToSide();

        string name = "Unknown";
        if (id == 0)
        {
            name = "Air";
        }
        else if (id > 0 && id < Block.Blocks.Length && Block.Blocks[id] != null)
        {
            Block block = Block.Blocks[id];
            string t = block.translateBlockName();
            name = !string.IsNullOrWhiteSpace(t) ? t : block.getBlockName();
        }

        string sideName = side.ToString();

        GetAdjacentBlockForFaceLight(bx, by, bz, side, out int ax, out int ay, out int az);
        int faceLight = ctx.World.Lighting.GetLightLevel(ax, ay, az);

        ImGui.Text($"{name} ({id}:{meta})");
        ImGui.Text($"XYZ:  {bx} / {by} / {bz}");
        ImGui.Text($"Face: {sideName} (light {faceLight})");
    }

    private static void GetAdjacentBlockForFaceLight(int bx, int by, int bz, Side side, out int ax, out int ay, out int az)
    {
        switch (side)
        {
            case Side.Down:
                ax = bx; ay = by - 1; az = bz;
                break;
            case Side.Up:
                ax = bx; ay = by + 1; az = bz;
                break;
            case Side.North:
                ax = bx; ay = by; az = bz - 1;
                break;
            case Side.South:
                ax = bx; ay = by; az = bz + 1;
                break;
            case Side.West:
                ax = bx - 1; ay = by; az = bz;
                break;
            case Side.East:
                ax = bx + 1; ay = by; az = bz;
                break;
            default:
                ax = bx; ay = by; az = bz;
                break;
        }
    }
}
