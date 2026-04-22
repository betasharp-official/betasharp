using System.Reflection;
using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Util.Hit;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Client.UI;

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

public record InGameTipContext(HitResult ObjectMouseOver, IBlockReader WorldReader, ItemStack HeldItem);

public static class ControlTooltip
{
    public static ControllerType ControllerType = ControllerType.XboxOne;

    private static readonly Dictionary<int, bool> s_usabilityCache = [];

    internal static void PopulateInGameTips(InGameTipContext context, List<ActionTip> tips)
    {
        tips.Add(new ActionTip(ControlIcon.Y, "Inventory"));

        string? useAction = null;
        ItemStack held = context.HeldItem;
        HitResult hit = context.ObjectMouseOver;

        if (hit.Type == HitResultType.TILE)
        {
            int blockX = hit.BlockX;
            int blockY = hit.BlockY;
            int blockZ = hit.BlockZ;
            int blockId = context.WorldReader.GetBlockId(blockX, blockY, blockZ);

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
            tips.Add(new ActionTip(ControlIcon.Lt, useAction));

        if (hit.Type != HitResultType.MISS)
        {
            string attackAction = hit.Type == HitResultType.ENTITY ? "Attack" : "Mine";
            tips.Add(new ActionTip(ControlIcon.Rt, attackAction));
        }

        if (held != null)
            tips.Add(new ActionTip(ControlIcon.B, "Drop"));
    }

    internal static void PopulateGuiTips(UIScreen screen, List<ActionTip> tips)
    {
        tips.Add(new ActionTip(ControlIcon.B, "Back"));

        screen.GetTooltips(tips);

        if (tips.All(t => t.Icon != ControlIcon.A) && screen.HasInteractiveElementUnderCursor())
            tips.Add(new ActionTip(ControlIcon.A, "Select"));
    }

    internal static string? GetAssetPath(ControlIcon icon)
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

        return $"/gui/controls/{ControllerType.Key}/{iconName}.png";
    }

    private static bool IsItemUsable(ItemStack stack)
    {
        if (stack == null) return false;
        if (stack.ItemId < 256) return true;

        if (s_usabilityCache.TryGetValue(stack.ItemId, out bool usable))
            return usable;

        Item item = stack.getItem();
        if (item == null) return false;

        Type type = item.GetType();
        MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (MethodInfo method in methods)
        {
            if ((method.Name == "use" || method.Name == "useOnBlock") && method.DeclaringType != typeof(Item))
            {
                usable = true;
                break;
            }
        }

        s_usabilityCache[stack.ItemId] = usable;
        return usable;
    }

    private static string GetItemActionLabel(ItemStack stack)
    {
        if (stack == null) return "Use";
        if (stack.ItemId < 256) return "Place";

        Item item = stack.getItem();
        if (item == null) return "Use";

        string typeName = item.GetType().Name;
        if (typeName.Contains("Food") || typeName.Contains("Soup") || typeName.Contains("MushroomStew")) return "Eat";
        if (typeName.Contains("Egg") || typeName.Contains("Snowball")) return "Throw";
        if (typeName.Contains("Bow")) return "Shoot";

        return "Use";
    }
}
