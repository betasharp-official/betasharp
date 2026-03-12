using BetaSharp.Client.Input;
using BetaSharp.Worlds;

namespace BetaSharp.Client.Guis;

public class GuiSelectWorldType : GuiScreen
{
    private const int ButtonDone = 0;
    private const int ButtonCancel = 1;

    private readonly GuiCreateWorld _parent;
    private GuiWorldTypeSlot _slot = null!;
    private int _selectedTypeIndex;
    private WorldType _currentType;

    public int SelectedTypeIndex
    {
        get => _selectedTypeIndex;
        set
        {
            _selectedTypeIndex = value;
            _btnDone.Enabled = true;
        }
    }

    private GuiButton _btnDone = null!;

    public GuiSelectWorldType(GuiCreateWorld parent, WorldType currentType)
    {
        _parent = parent;
        _currentType = currentType;
    }

    public override void InitGui()
    {
        TranslationStorage translations = TranslationStorage.Instance;

        _slot = new GuiWorldTypeSlot(this);
        List<WorldType> types = _slot.GetTypes();
        _selectedTypeIndex = types.IndexOf(_currentType);

        _controlList.Add(_btnDone = new GuiButton(ButtonDone, Width / 2 - 155, Height - 28, 150, 20, "Done"));
        _controlList.Add(new GuiButton(ButtonCancel, Width / 2 + 5, Height - 28, 150, 20, translations.TranslateKey("gui.cancel")));

        _btnDone.Enabled = _selectedTypeIndex >= 0;
    }

    protected override void ActionPerformed(GuiButton button)
    {
        if (button.Enabled)
        {
            switch (button.Id)
            {
                case ButtonDone:
                    DoneClicked();
                    break;
                case ButtonCancel:
                    Game.displayGuiScreen(_parent);
                    break;
                default:
                    _slot.ActionPerformed(button);
                    break;
            }
        }
    }

    public void DoneClicked()
    {
        if (_selectedTypeIndex >= 0)
        {
            WorldType selectedType = _slot.GetTypes()[_selectedTypeIndex];
            _parent.SetWorldType(selectedType);
            Game.displayGuiScreen(_parent);
        }
    }

    protected override void KeyTyped(char eventChar, int eventKey)
    {
        if (eventKey == Keyboard.KEY_ESCAPE)
        {
            Game.displayGuiScreen(_parent);
        }
    }

    public override void Render(int mouseX, int mouseY, float partialTicks)
    {
        _slot.DrawScreen(mouseX, mouseY, partialTicks);
        DrawCenteredString(FontRenderer, "Select World Type", Width / 2, 20, Color.White);
        base.Render(mouseX, mouseY, partialTicks);
    }
}
