using BetaSharp.Blocks;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using ImGuiNET;

namespace BetaSharp.Client.Diagnostics.Windows;

internal sealed class LocalPlayerInfoWindow(BetaSharp game) : DebugWindow
{
    private static readonly string[] s_cardinalDirections = ["south", "west", "north", "east"];
    private static readonly string[] s_towards = ["positive Z", "negative X", "negative Z", "positive X"];
    private static readonly string[] s_blockSides = ["Down", "Up", "North", "South", "West", "East"];

    public override string Title => "Local Player";

    protected override void OnDraw()
    {
        if (game.Player == null || game.World == null)
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
        double x = Math.Floor(game.Player.x * 1000) / 1000;
        double y = Math.Floor(game.Player.y * 100000) / 100000;
        double z = Math.Floor(game.Player.z * 1000) / 1000;

        int bx = (int)Math.Floor(game.Player.x);
        int by = (int)Math.Floor(game.Player.y);
        int bz = (int)Math.Floor(game.Player.z);

        int facingIndex = MathHelper.Floor((double)(game.Player.yaw * 4.0F / 360.0F) + 0.5D) & 3;
        string cardinal = facingIndex is >= 0 and < 4 ? s_cardinalDirections[facingIndex] : "N/A";
        string towards = facingIndex is >= 0 and < 4 ? s_towards[facingIndex] : "N/A";
        string vertical = game.Player.pitch <= -45f ? "up" : game.Player.pitch >= 45f ? "down" : "level";

        float yaw = game.Player.yaw % 360f;
        if (yaw >= 180f) yaw -= 360f;
        if (yaw < -180f) yaw += 360f;
        float pitch = game.Player.pitch;

        string biome = game.World.Dimension.BiomeSource.GetBiome(bx, bz).Name;
        int light = game.World.Lighting.GetLightLevel(bx, by, bz);

        ImGui.Text($"XYZ:    {x:F3} / {y:F5} / {z:F3}");
        ImGui.Text($"Block:  {bx} {by} {bz}");
        ImGui.Text($"Facing: {cardinal} {vertical} (towards {towards})");
        ImGui.Text($"Yaw / Pitch: {yaw:F1} / {pitch:F1}");
        ImGui.Text($"Biome:  {biome}");
        ImGui.Text($"Light:  {light}");
    }

    private void DrawTargetedBlockSection()
    {
        if (game.ObjectMouseOver.Type != HitResultType.TILE)
        {
            ImGui.TextDisabled("Nothing targeted.");
            return;
        }

        int bx = game.ObjectMouseOver.BlockX;
        int by = game.ObjectMouseOver.BlockY;
        int bz = game.ObjectMouseOver.BlockZ;
        int id = game.World.Reader.GetBlockId(bx, by, bz);
        int meta = game.World.Reader.GetBlockMeta(bx, by, bz);
        int side = game.ObjectMouseOver.Side;

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

        string sideName = side is >= 0 and < 6 ? s_blockSides[side] : side.ToString();

        ImGui.Text($"{name} ({id}:{meta})");
        ImGui.Text($"XYZ:  {bx} / {by} / {bz}");
        ImGui.Text($"Face: {sideName}");
    }
}
