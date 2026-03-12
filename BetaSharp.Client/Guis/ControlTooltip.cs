using System.Reflection;
using BetaSharp.Blocks;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Util.Hit;

namespace BetaSharp.Client.Guis;

public enum ControlIcon
{
    A, B, X, Y,
    Lt, Rt, Lb, Rb,
    Ls, Rs,
    LsClick, RsClick,
    DPadUp, DPadDown, DPadLeft, DPadRight,
    Start, Back,
    TouchPad
}

public record ActionTip(ControlIcon Icon, string Action);

public static class ControlTooltip
{
    public static string ControllerType = "x360";

    private static readonly List<ActionTip> s_tips = [];
    private static readonly Dictionary<int, bool> s_usabilityCache = [];

    public static void Clear() => s_tips.Clear();

    public static void Add(ControlIcon icon, string action)
    {
        s_tips.Add(new ActionTip(icon, action));
    }

    public static void Render(BetaSharp game, int screenWidth, int screenHeight, float partialTicks)
    {
        if (game.options.HideGUI || !game.isControllerMode) return;

        Clear();
        if (game.currentScreen == null)
        {
            PopulateInGameTips(game);
        }
        else
        {
            PopulateGuiTips(game.currentScreen);
        }

        if (s_tips.Count == 0) return;

        int x = 24;
        int y = screenHeight - 30;
        const int spacing = 10;

        foreach (ActionTip tip in s_tips)
        {
            int iconWidth = DrawIcon(game, tip, x, y);
            x += iconWidth + 4;

            game.fontRenderer.DrawStringWithShadow(tip.Action, x, y + 2, Color.White);
            x += game.fontRenderer.GetStringWidth(tip.Action) + spacing;
        }
    }

    private static void PopulateInGameTips(BetaSharp game)
    {
        Add(ControlIcon.Y, "Inventory");

        string? useAction = null;
        ItemStack held = game.player.inventory.getSelectedItem();
        HitResult hit = game.objectMouseOver;

        if (hit.Type == HitResultType.TILE)
        {
            int blockX = hit.BlockX;
            int blockY = hit.BlockY;
            int blockZ = hit.BlockZ;
            int blockId = game.world.getBlockId(blockX, blockY, blockZ);

            if (blockId == Block.Chest.id || blockId == Block.Furnace.id || blockId == Block.LitFurnace.id || blockId == Block.CraftingTable.id || blockId == Block.Dispenser.id)
                useAction = "Interact";
            else if (blockId == Block.Door.id || blockId == Block.IronDoor.id || blockId == Block.Trapdoor.id)
                useAction = "Open/Close";
            else if (blockId == Block.Lever.id || blockId == Block.Button.id || blockId == Block.Repeater.id || blockId == Block.PoweredRepeater.id)
                useAction = "Use";
            else if (blockId == Block.Bed.id)
                useAction = "Sleep";
            else if (blockId == Block.Cake.id)
                useAction = "Eat";
            else if (blockId == Block.Jukebox.id)
                useAction = "Use";
            else if (IsItemUsable(held))
            {
                useAction = GetItemActionLabel(held);
            }
        }
        else if (hit.Type == HitResultType.ENTITY)
        {
            if (hit.Entity is EntityMinecart || hit.Entity is EntityBoat)
                useAction = "Enter";
            else if (hit.Entity is EntityPig pig && pig.Saddled.Value)
                useAction = "Ride";
            else if (IsItemUsable(held))
            {
                string label = GetItemActionLabel(held);
                if (label != "Place") useAction = label;
            }
        }
        else if (IsItemUsable(held))
        {
            string label = GetItemActionLabel(held);
            if (label != "Place") useAction = label;
        }

        if (useAction != null)
        {
            Add(ControlIcon.Lt, useAction);
        }

        if (hit.Type != HitResultType.MISS)
        {
            string attackAction = "Mine";
            if (hit.Type == HitResultType.ENTITY) attackAction = "Attack";
            Add(ControlIcon.Rt, attackAction);
        }

        if (game.player.inventory.getSelectedItem() != null)
        {
            Add(ControlIcon.B, "Drop");
        }
    }

    private static void PopulateGuiTips(GuiScreen screen)
    {
        Add(ControlIcon.B, "Back");

        screen.GetTooltips(s_tips);
    }

    private static bool IsItemUsable(ItemStack stack)
    {
        if (stack == null) return false;
        if (stack.itemId < 256) return true; // Blocks are always placeable

        if (s_usabilityCache.TryGetValue(stack.itemId, out bool usable))
            return usable;

        Item item = stack.getItem();
        if (item == null) return false;

        Type type = item.GetType();

        // Using a more permissive search to handle internal subclasses
        MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (MethodInfo method in methods)
        {
            if ((method.Name == "use" || method.Name == "useOnBlock") && method.DeclaringType != typeof(Item))
            {
                usable = true;
                break;
            }
        }

        s_usabilityCache[stack.itemId] = usable;
        return usable;
    }

    private static string GetItemActionLabel(ItemStack stack)
    {
        if (stack == null) return "Use";
        if (stack.itemId < 256) return "Place";

        Item item = stack.getItem();
        if (item == null) return "Use";

        string typeName = item.GetType().Name;
        if (typeName.Contains("Food") || typeName.Contains("Soup") || typeName.Contains("MushroomStew")) return "Eat";
        if (typeName.Contains("Egg") || typeName.Contains("Snowball")) return "Throw";
        if (typeName.Contains("Bow")) return "Shoot";

        return "Use";
    }

    private static int DrawIcon(BetaSharp game, ActionTip tip, int x, int y)
    {
        string? assetPath = GetAssetPath(tip.Icon);
        if (assetPath == null) return 0;

        TextureHandle texture = game.textureManager.GetTextureId(assetPath);
        game.textureManager.BindTexture(texture);

        int size = 16;
        float u1 = 0, v1 = 0, u2 = 1, v2 = 1;

        Tessellator tess = Tessellator.instance;
        tess.startDrawingQuads();
        tess.setColorOpaque_I(0xFFFFFF);
        tess.addVertexWithUV(x, y + size, 0, u1, v2);
        tess.addVertexWithUV(x + size, y + size, 0, u2, v2);
        tess.addVertexWithUV(x + size, y, 0, u2, v1);
        tess.addVertexWithUV(x, y, 0, u1, v1);
        tess.draw();

        return size;
    }

    private static string? GetAssetPath(ControlIcon icon)
    {
        string iconName = icon switch
        {
            ControlIcon.A => "down_button",
            ControlIcon.B => "right_button",
            ControlIcon.X => "left_button",
            ControlIcon.Y => "up_button",
            ControlIcon.Lt => "left_trigger",
            ControlIcon.Rt => "right_trigger",
            ControlIcon.Lb => "left_bumper",
            ControlIcon.Rb => "right_bumper",
            ControlIcon.Ls => "left_stick",
            ControlIcon.Rs => "right_stick",
            ControlIcon.LsClick => "left_stick_button",
            ControlIcon.RsClick => "right_stick_button",
            ControlIcon.DPadUp => "dpad_up",
            ControlIcon.DPadDown => "dpad_down",
            ControlIcon.DPadLeft => "dpad_left",
            ControlIcon.DPadRight => "dpad_right",
            ControlIcon.Start => "start_button",
            ControlIcon.Back => "back_button",
            ControlIcon.TouchPad => "touchpad",
            _ => "unknown"
        };

        return $"/gui/controls/{ControllerType}/{iconName}.png";
    }
}
