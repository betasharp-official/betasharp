using BetaSharp.Client.Input;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Items;
using BetaSharp.Client.Rendering.Core.OpenGL;
using BetaSharp.Blocks;
using BetaSharp.Worlds.Gen.Flat;
using BetaSharp.Items;

namespace BetaSharp.Client.Guis;

public class GuiFlatPresets : GuiScreen
{
    private readonly GuiCreateFlatWorld _parent;
    private GuiSlotFlatPresets? _presetsSlot;
    private GuiButton? _btnSelect;

    public GuiFlatPresets(GuiCreateFlatWorld parent)
    {
        _parent = parent;
    }

    public override void InitGui()
    {
        _controlList.Clear();
        Keyboard.enableRepeatEvents(true);

        TextField = new GuiTextField(this, FontRenderer, 50, 40, Width - 100, 20, _parent.GeneratorOptions);
        TextField.SetMaxStringLength(1230);

        _presetsSlot = new GuiSlotFlatPresets(this);

        _controlList.Add(_btnSelect = new GuiButton(0, Width / 2 - 155, Height - 28, 150, 20, "Select Preset"));
        _controlList.Add(new GuiButton(1, Width / 2 + 5, Height - 28, 150, 20, "Cancel"));

        UpdateSelectButtonStatus();
    }

    public void UpdateSelectButtonStatus()
    {
        _btnSelect!.Enabled = _presetsSlot!.SelectedIndex >= 0 || TextField.GetText().Length > 1;
    }

    protected override void ActionPerformed(GuiButton button)
    {
        if (button.Id == 0) // Select
        {
            SelectPreset();
        }
        else if (button.Id == 1) // Cancel
        {
            Game.displayGuiScreen(_parent);
        }
    }

    public void SelectPreset()
    {
        _parent.GeneratorOptions = TextField.GetText();
        Game.displayGuiScreen(_parent);
    }

    protected override void KeyTyped(char eventChar, int eventKey)
    {
        if (TextField.IsFocused)
        {
            TextField.textboxKeyTyped(eventChar, eventKey);
            UpdateSelectButtonStatus();
        }
        else
        {
            base.KeyTyped(eventChar, eventKey);
        }
    }

    protected override void MouseClicked(int x, int y, int button)
    {
        TextField.MouseClicked(x, y, button);
        base.MouseClicked(x, y, button);
    }

    public override void OnGuiClosed()
    {
        Keyboard.enableRepeatEvents(false);
    }

    public override void Render(int mouseX, int mouseY, float partialTicks)
    {
        DrawDefaultBackground();
        _presetsSlot!.DrawScreen(mouseX, mouseY, partialTicks);
        DrawCenteredString(FontRenderer, "Select a Preset", Width / 2, 8, Color.White);
        DrawString(FontRenderer, "Share your preset with a friend!", 50, 30, Color.GrayA0);
        DrawString(FontRenderer, "Preset List", 50, 70, Color.GrayA0);
        TextField.DrawTextBox();
        base.Render(mouseX, mouseY, partialTicks);
    }

    public class PresetItem(string Name, string Value, int IconBlockId = -1)
    {
        public string Name { get; } = Name;
        public string Value { get; } = Value;
        public int IconBlockId { get; } = IconBlockId != -1 ? IconBlockId : GetIconBlockIdFromValue(Value);
        public int IconBlockMeta { get; } = GetIconBlockMetaFromValue(Value);

        private static int GetIconBlockIdFromValue(string value)
        {
            var info = FlatGeneratorInfo.CreateFromString(value);
            return info.FlatLayers.Count > 0 ? info.FlatLayers[^1].FillBlock : Block.GrassBlock.id;
        }

        private static int GetIconBlockMetaFromValue(string value)
        {
            var info = FlatGeneratorInfo.CreateFromString(value);
            return info.FlatLayers.Count > 0 ? info.FlatLayers[^1].FillBlockMeta : 0;
        }
    }

    internal static List<PresetItem> PresetList { get; } =
    [
        new PresetItem("Classic Flat", "2;7,2x3,2;1;village"),
        new PresetItem("Tunnelers' Dream", "2;7,230x1,5x3,2;1;biome_1,dungeon,decoration,stronghold,mineshaft", Block.Stone.id),
        new PresetItem("Water World", "2;7,5x1,5x3,5x12,90x9;1;village,biome_1"),
        new PresetItem("Overworld", "2;7,59x1,3x3,2;1;village,biome_1,decoration,stronghold,mineshaft,dungeon,lake,lava_lake", Block.DeadBush.id),
        new PresetItem("Snowy Kingdom", "2;7,59x1,3x3,2,78;1;village,biome_1"),
        new PresetItem("Bottomless Pit", "2;2x4,3x3,2;1;village,biome_1", Item.Feather.id),
        new PresetItem("Desert", "2;7,3x1,52x24,8x12;1;village,biome_1,decoration,stronghold,mineshaft,dungeon"),
        new PresetItem("Redstone Ready", "2;7,3x1,52x24;1;", Item.Redstone.id)
    ];

    internal GuiTextField TextField { get; private set; }
}

public class GuiSlotFlatPresets : GuiSlot
{
    private static readonly ItemRenderer s_itemRenderer = new();
    private readonly GuiFlatPresets _parent;
    public int SelectedIndex { get; set; } = -1;

    public GuiSlotFlatPresets(GuiFlatPresets parent) : base(parent.Game, parent.Width, parent.Height, 80, parent.Height - 32, 24)
    {
        _parent = parent;
    }

    public override int GetSize() => GuiFlatPresets.PresetList.Count;

    protected override void ElementClicked(int index, bool doubleClick)
    {
        SelectedIndex = index;
        _parent.UpdateSelectButtonStatus();
        _parent.TextField.SetText(GuiFlatPresets.PresetList[index].Value);

        if (doubleClick)
        {
            _parent.SelectPreset();
        }
    }

    protected override bool IsSelected(int slotIndex) => slotIndex == SelectedIndex;

    protected override void DrawBackground() { }

    protected override void DrawSlot(int index, int x, int y, int height, Tessellator tess)
    {
        GuiFlatPresets.PresetItem preset = GuiFlatPresets.PresetList[index];

        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
        _parent.Game.textureManager.BindTexture(_parent.Game.textureManager.GetTextureId("/gui/slot.png"));
        _parent.DrawTexturedModalRect(x, y, 0, 0, 18, 18, 128, 128);

        if (preset.IconBlockId < 256)
        {
            Block block = Block.Blocks[preset.IconBlockId];
            if (block != null)
            {
                GLManager.GL.PushMatrix();
                GLManager.GL.Rotate(120.0F, 1.0F, 0.0F, 0.0F);
                Lighting.turnOn();
                GLManager.GL.PopMatrix();

                int textureId = block.getTexture(1);
                s_itemRenderer.drawItemIntoGui(_parent.FontRenderer, _parent.Game.textureManager, preset.IconBlockId, preset.IconBlockMeta, textureId, x + 1, y + 1);

                Lighting.turnOff();
                GLManager.GL.Enable(GLEnum.DepthTest);
            }
        }
        else
        {
            Item item = Item.ITEMS[preset.IconBlockId];
            if (item != null)
            {
                int textureId = item.getTextureId(preset.IconBlockMeta);
                s_itemRenderer.drawItemIntoGui(_parent.FontRenderer, _parent.Game.textureManager, preset.IconBlockId, preset.IconBlockMeta, textureId, x + 1, y + 1);
            }
        }

        GLManager.GL.Disable(GLEnum.Lighting);

        Gui.DrawString(_parent.FontRenderer, preset.Name, x + 22, y + 1, Color.White);
    }
}
