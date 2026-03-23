namespace BetaSharp.Client.Guis.Debug;

public class GuiNewDebug : GuiScreen
{
    protected GuiDebugEditor parentScreen;
    private GuiNewDebugSlot? _slot;

    public Type? selectedType;
    public bool right;

    private const int BUTTON_CANCEL = 0;
    private const int BUTTON_SIDE = 1;
    private const int BUTTON_ADD = 2;
    public GuiButton? buttonSide;
    public GuiButton? buttonAdd;

    public GuiNewDebug(GuiDebugEditor parentScreen)
    {
        this.parentScreen = parentScreen;
        selectedType = null;
        right = false;
    }

    private void UpdateSide()
    {
        buttonSide?.DisplayString = "Side: " + (right ? "Right" : "Left");
    }

    public override void InitGui()
    {
        _slot = new GuiNewDebugSlot(this);

        TranslationStorage translations = TranslationStorage.Instance;

        _controlList.Add(buttonSide = new GuiButton(BUTTON_SIDE, Width / 2 - 74, Height - 52, 70, 20, ""));
        UpdateSide();

        _controlList.Add(buttonAdd = new GuiButton(BUTTON_ADD, Width / 2 + 4, Height - 52, 70, 20, "Add"));
        _controlList.Add(new GuiButton(BUTTON_CANCEL, Width / 2 - 74, Height - 28, 148, 20, translations.TranslateKey("gui.cancel")));
        buttonAdd.Enabled = selectedType is not null;
    }

    public override void Render(int mouseX, int mouseY, float partialTicks)
    {
        if (_slot == null)
        {
            return;
        }

        _slot.DrawScreen(mouseX, mouseY, partialTicks);
        DrawCenteredString(FontRenderer, "Add New Component", Width / 2, 20, Color.White);
        base.Render(mouseX, mouseY, partialTicks);
    }

    public void Finish()
    {
        if (selectedType == null) return;
        DebugComponent? comp = (DebugComponent?)Activator.CreateInstance(selectedType);
        if (comp == null) return;
        comp.Right = right;
        parentScreen.components.Add(comp);
        parentScreen.selectedComponent = comp;
        Game.displayGuiScreen(parentScreen);
    }

    protected override void ActionPerformed(GuiButton button)
    {
        if (button.Enabled)
        {
            switch (button.Id)
            {
                case BUTTON_SIDE:
                    right = !right;
                    UpdateSide();
                    break;
                case BUTTON_CANCEL:
                    Game.displayGuiScreen(parentScreen);
                    break;
                case BUTTON_ADD:
                    Finish();
                    break;
                default:
                    if (_slot == null) return;
                    _slot.ActionPerformed(button);
                    break;
            }
        }
    }
}
