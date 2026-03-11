using BetaSharp.Client.Input;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering;
using BetaSharp.Blocks;
using BetaSharp.Items;
using BetaSharp.Util.Hit;
using BetaSharp.Entities;
using Silk.NET.GLFW;

namespace BetaSharp.Client.Guis;

public enum ControlIcon
{
    // Controller
    A, B, X, Y,
    LT, RT, LB, RB,
    LS, RS,
    LS_CLICK, RS_CLICK,
    DPAD_UP, DPAD_DOWN, DPAD_LEFT, DPAD_RIGHT,
    START, BACK,
    
    // Mouse
    MOUSE_LEFT, MOUSE_RIGHT, MOUSE_BASE,
    
    // Keyboard
    KEY_BASE
}

public record ActionTip(ControlIcon Icon, string Action, string? KeyName = null);

public static class ControlTooltip
{
    private static readonly List<ActionTip> s_tips = new();

    public static void Clear() => s_tips.Clear();

    public static void Add(ControlIcon icon, string action, string? keyName = null)
    {
        s_tips.Add(new ActionTip(icon, action, keyName));
    }

    public static void Render(BetaSharp game, int screenWidth, int screenHeight, float partialTicks)
    {
        if (game.options.HideGUI) return;

        Clear();
        if (game.currentScreen == null)
        {
            PopulateInGameTips(game);
        }
        else
        {
            PopulateGuiTips(game, game.currentScreen);
        }

        if (s_tips.Count == 0) return;

        int x = 10;
        int y = screenHeight - 20;
        int spacing = 10;

        foreach (var tip in s_tips)
        {
            int iconWidth = DrawIcon(game, tip, x, y);
            x += iconWidth + 4;
            
            game.fontRenderer.DrawStringWithShadow(tip.Action, x, y + 2, Color.White);
            x += game.fontRenderer.GetStringWidth(tip.Action) + spacing;
        }
    }

    private static void PopulateInGameTips(BetaSharp game)
    {
        bool controller = game.isControllerMode;

        // Move
        if (controller) Add(ControlIcon.LS, "Move");
        else Add(ControlIcon.KEY_BASE, "Move", "WASD");

        // Jump
        if (controller) Add(ControlIcon.A, "Jump");
        else Add(ControlIcon.KEY_BASE, "Jump", Keyboard.getKeyName(game.options.KeyBindJump.keyCode));

        // Attack/Mine
        string attackAction = "Mine";
        var hit = game.objectMouseOver;
        if (hit.Type == HitResultType.ENTITY) attackAction = "Attack";
        
        if (controller) Add(ControlIcon.RT, attackAction);
        else Add(ControlIcon.MOUSE_LEFT, attackAction);

        // Use/Interact
        string useAction = "Use";
        ItemStack held = game.player.inventory.getSelectedItem();
        
        if (hit.Type == HitResultType.TILE)
        {
            int blockId = game.world.getBlockId(hit.BlockX, hit.BlockY, hit.BlockZ);
            if (blockId == Block.Chest.id || blockId == Block.Furnace.id || blockId == Block.LitFurnace.id || blockId == Block.CraftingTable.id)
                useAction = "Open";
            else if (blockId == Block.Door.id || blockId == Block.IronDoor.id || blockId == Block.Trapdoor.id)
                useAction = "Open/Close";
            else if (blockId == Block.Lever.id || blockId == Block.Button.id)
                useAction = "Use";
            else if (held != null && held.itemId < 256) // If holding a block
                useAction = "Place";
        }
        else if (hit.Type == HitResultType.ENTITY)
        {
            if (hit.Entity is EntityMinecart || hit.Entity is EntityBoat)
                useAction = "Enter";
            else if (hit.Entity is EntityPig pig && pig.Saddled.Value)
                useAction = "Ride";
        }
        else if (held != null && held.itemId < 256)
        {
            useAction = "Place";
        }

        if (controller) Add(ControlIcon.LT, useAction);
        else Add(ControlIcon.MOUSE_RIGHT, useAction);

        // Inventory
        if (controller) Add(ControlIcon.Y, "Inventory");
        else Add(ControlIcon.KEY_BASE, "Inventory", Keyboard.getKeyName(game.options.KeyBindInventory.keyCode));
        
        // Drop
        if (controller) Add(ControlIcon.B, "Drop");
        else Add(ControlIcon.KEY_BASE, "Drop", Keyboard.getKeyName(game.options.KeyBindDrop.keyCode));
        
        // Sneak (Toggle in BetaSharp/ControllerManager)
        if (controller) Add(ControlIcon.RS_CLICK, "Sneak");
        else Add(ControlIcon.KEY_BASE, "Sneak", Keyboard.getKeyName(game.options.KeyBindSneak.keyCode));
    }

    private static void PopulateGuiTips(BetaSharp game, GuiScreen screen)
    {
        bool controller = game.isControllerMode;

        // Back
        if (controller) Add(ControlIcon.B, "Back");
        else Add(ControlIcon.KEY_BASE, "Back", "ESC");

        // Select (Generic)
        if (controller) Add(ControlIcon.A, "Select");
        else Add(ControlIcon.MOUSE_LEFT, "Select");
        
        // Let screens add their own
        var extraTips = screen.GetTooltips(controller);
        if (extraTips != null)
        {
            foreach (var tip in extraTips) s_tips.Add(tip);
        }
    }

    private static int DrawIcon(BetaSharp game, ActionTip tip, int x, int y)
    {
        string assetPath = GetAssetPath(tip.Icon);
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

        if (tip.Icon == ControlIcon.KEY_BASE && tip.KeyName != null)
        {
            string name = tip.KeyName;
            if (name.Length > 1) name = name[..1]; // Simple truncate for now
            int textX = x + (size - game.fontRenderer.GetStringWidth(name)) / 2;
            game.fontRenderer.DrawString(name, textX, y + 4, Color.Gray40);
        }

        return size;
    }

    private static string? GetAssetPath(ControlIcon icon)
    {
        return icon switch
        {
            ControlIcon.A => "/gui/controls/down_button.png",
            ControlIcon.B => "/gui/controls/right_button.png",
            ControlIcon.X => "/gui/controls/left_button.png",
            ControlIcon.Y => "/gui/controls/up_button.png",
            ControlIcon.LT => "/gui/controls/left_trigger.png",
            ControlIcon.RT => "/gui/controls/right_trigger.png",
            ControlIcon.LB => "/gui/controls/left_bumper.png",
            ControlIcon.RB => "/gui/controls/right_bumper.png",
            ControlIcon.LS => "/gui/controls/left_stick.png",
            ControlIcon.RS => "/gui/controls/right_stick.png",
            ControlIcon.LS_CLICK => "/gui/controls/left_stick_button.png",
            ControlIcon.RS_CLICK => "/gui/controls/right_stick_button.png",
            ControlIcon.DPAD_UP => "/gui/controls/dpad_up.png",
            ControlIcon.DPAD_DOWN => "/gui/controls/dpad_down.png",
            ControlIcon.DPAD_LEFT => "/gui/controls/dpad_left.png",
            ControlIcon.DPAD_RIGHT => "/gui/controls/dpad_right.png",
            ControlIcon.START => "/gui/controls/start_button.png",
            ControlIcon.BACK => "/gui/controls/back_button.png",
            ControlIcon.MOUSE_LEFT => "/gui/controls/mouse_left.png",
            ControlIcon.MOUSE_RIGHT => "/gui/controls/mouse_right.png",
            ControlIcon.MOUSE_BASE => "/gui/controls/mouse_base.png",
            ControlIcon.KEY_BASE => "/gui/controls/key_base.png",
            _ => null
        };
    }
}
