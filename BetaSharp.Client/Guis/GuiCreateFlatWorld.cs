using BetaSharp.Blocks;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.OpenGL;
using BetaSharp.Client.Rendering.Items;
using BetaSharp.Worlds.Gen.Flat;

namespace BetaSharp.Client.Guis;

public class GuiCreateFlatWorld(GuiCreateWorld parent, string generatorOptions) : GuiScreen
{
    private GuiSlotFlatLayers? _layersSlot;
    private GuiButton? _btnRemoveLayer;

    public string GeneratorOptions
    {
        get => GeneratorInfo.ToString();
        set
        {
            GeneratorInfo = FlatGeneratorInfo.CreateFromString(value);
        }
    }

    public override void InitGui()
    {
        _controlList.Clear();
        _controlList.Add(new GuiButton(0, Width / 2 - 155, Height - 28, 150, 20, "Done"));
        _controlList.Add(new GuiButton(3, Width / 2 + 5, Height - 28, 150, 20, "Cancel"));
        _controlList.Add(_btnRemoveLayer = new GuiButton(2, Width / 2 - 155, Height - 52, 150, 20, "Remove Layer"));
        _controlList.Add(new GuiButton(1, Width / 2 + 5, Height - 52, 150, 20, "Presets"));
        _btnRemoveLayer.Enabled = false;

        _layersSlot = new GuiSlotFlatLayers(this);
    }

    protected override void ActionPerformed(GuiButton button)
    {
        switch (button.Id)
        {
            case 0: // Done
                parent.GeneratorOptions = GeneratorInfo.ToString();
                Game.displayGuiScreen(parent);
                break;
            case 1: // Presets
                Game.displayGuiScreen(new GuiFlatPresets(this));
                break;
            case 2: // Remove Layer
                if (_layersSlot!.SelectedLayerIndex >= 0 && _layersSlot.SelectedLayerIndex < GeneratorInfo.FlatLayers.Count)
                {
                    GeneratorInfo.FlatLayers.RemoveAt(GeneratorInfo.FlatLayers.Count - _layersSlot.SelectedLayerIndex - 1);
                    GeneratorInfo.UpdateLayerHeights();
                    _layersSlot.SelectedLayerIndex = Math.Min(_layersSlot.SelectedLayerIndex, GeneratorInfo.FlatLayers.Count - 1);
                }
                break;
            case 3: // Cancel
                Game.displayGuiScreen(parent);
                break;
        }
        _btnRemoveLayer!.Enabled = _layersSlot!.SelectedLayerIndex >= 0;
    }

    public void OnLayerSelected(int index)
    {
        _btnRemoveLayer!.Enabled = index >= 0;
    }

    public override void Render(int mouseX, int mouseY, float partialTicks)
    {
        DrawDefaultBackground();
        _layersSlot!.DrawScreen(mouseX, mouseY, partialTicks);
        DrawCenteredString(FontRenderer, "Customize Superflat World", Width / 2, 8, Color.White);
        base.Render(mouseX, mouseY, partialTicks);
    }

    internal FlatGeneratorInfo GeneratorInfo { get; private set; } = FlatGeneratorInfo.CreateFromString(generatorOptions);
}

public class GuiSlotFlatLayers(GuiCreateFlatWorld parent) : GuiSlot(parent.Game, parent.Width, parent.Height, 32, parent.Height - 64, 24)
{
    private static readonly ItemRenderer s_itemRenderer = new();
    public int SelectedLayerIndex { get; set; } = -1;

    public override int GetSize()
    {
        return parent.GeneratorInfo.FlatLayers.Count;
    }

    protected override void ElementClicked(int index, bool doubleClick)
    {
        SelectedLayerIndex = index;
        parent.OnLayerSelected(index);
    }

    protected override bool IsSelected(int slotIndex)
    {
        return slotIndex == SelectedLayerIndex;
    }

    protected override void DrawBackground()
    {
    }

    protected override void DrawSlot(int index, int x, int y, int height, Tessellator tess)
    {
        int count = parent.GeneratorInfo.FlatLayers.Count;
        FlatLayerInfo layer = parent.GeneratorInfo.FlatLayers[count - index - 1];

        Block block = Block.Blocks[layer.FillBlock];
        string blockName = block?.translateBlockName() ?? "Unknown";

        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
        parent.Game.textureManager.GetTextureId("/gui/slot.png").Bind();
        parent.DrawTexturedModalRect(x, y, 0, 0, 18, 18, 128, 128);

        if (block != null)
        {
            GLManager.GL.PushMatrix();
            GLManager.GL.Rotate(120.0F, 1.0F, 0.0F, 0.0F);
            Lighting.turnOn();
            GLManager.GL.PopMatrix();

            int textureId = block.getTexture(1);
            s_itemRenderer.drawItemIntoGui(parent.FontRenderer, parent.Game.textureManager, layer.FillBlock, layer.FillBlockMeta, textureId, x + 1, y + 1);

            Lighting.turnOff();
            GLManager.GL.Disable(GLEnum.Lighting);
            GLManager.GL.Enable(GLEnum.DepthTest);
        }

        string info = "Height: " + layer.LayerCount;

        Gui.DrawString(parent.FontRenderer, blockName, x + 22, y + 1, Color.White);
        Gui.DrawString(parent.FontRenderer, info, x + 22, y + 12, Color.Gray80);
    }
}
