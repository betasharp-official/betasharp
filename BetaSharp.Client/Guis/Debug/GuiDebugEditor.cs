namespace BetaSharp.Client.Guis.Debug;

public class GuiDebugEditor : GuiScreen
{
    protected GuiScreen parentScreen;
    private GuiDebugSlot? _slot;

    public List<DebugComponent> components;
    public DebugComponent? selectedComponent;

    private const int BUTTON_CANCEL = 0;
    private const int BUTTON_CHANGE = 1;
    private const int BUTTON_DELETE = 2;
    private const int BUTTON_CREATE = 3;
    private const int BUTTON_RESET = 4;
    private const int BUTTON_SAVE = 6;
    public GuiButton? buttonChange;
    public GuiButton? buttonDelete;

    public GuiDebugEditor(BetaSharp game, GuiScreen parentScreen)
    {
        this.parentScreen = parentScreen;
        components = [];
        selectedComponent = null;

        foreach (DebugComponent component in game.componentsStorage.Overlay.Components)
        {
            components.Add(component.Duplicate());
        }
    }

    public override void InitGui()
    {
        _slot = new GuiDebugSlot(this);

        TranslationStorage translations = TranslationStorage.Instance;

        _controlList.Add(buttonChange = new GuiButton(BUTTON_CHANGE, Width / 2 - 74, Height - 28, 70, 20, "Change Side..."));
        _controlList.Add(buttonDelete = new GuiButton(BUTTON_DELETE, Width / 2 - 154, Height - 28, 70, 20, translations.TranslateKey("selectWorld.delete")));
        _controlList.Add(new GuiButton(BUTTON_CREATE, Width / 2 - 154, Height - 52, 150, 20, "Add new..."));
        _controlList.Add(new GuiButton(BUTTON_SAVE, Width / 2 + 4, Height - 52, 150, 20, "Save"));
        _controlList.Add(new GuiButton(BUTTON_CANCEL, Width / 2 + 4, Height - 28, 70, 20, translations.TranslateKey("gui.cancel")));
        _controlList.Add(new GuiButton(BUTTON_RESET, Width / 2 + 84, Height - 28, 70, 20, "Defaults"));
        buttonChange.Enabled = selectedComponent is not null;
        buttonDelete.Enabled = buttonChange.Enabled;
    }

    public override void Render(int mouseX, int mouseY, float partialTicks)
    {
        if (_slot == null)
        {
            return;
        }

        _slot.DrawScreen(mouseX, mouseY, partialTicks);
        DrawCenteredString(FontRenderer, "Edit Debug Overlay", Width / 2, 20, Color.White);
        base.Render(mouseX, mouseY, partialTicks);
    }

    protected override void ActionPerformed(GuiButton button)
    {
        if (button.Enabled)
        {
            switch (button.Id)
            {
                case BUTTON_DELETE:
                    if (selectedComponent != null)
                    {
                        components.Remove(selectedComponent);
                        selectedComponent = null;
                    }

                    break;
                case BUTTON_CHANGE:
                    if (selectedComponent == null) return;
                    selectedComponent.Right = !selectedComponent.Right;
                    break;
                case BUTTON_CANCEL:
                    Game.displayGuiScreen(parentScreen);
                    break;
                case BUTTON_RESET:
                    components.Clear();
                    DebugComponentsStorage.DefaultComponents(components);
                    break;
                case BUTTON_SAVE:
                    Game.componentsStorage.Overlay.Components.Clear();
                    foreach (DebugComponent comp in components)
                    {
                        Game.componentsStorage.Overlay.Components.Add(comp.Duplicate());
                    }
                    Game.componentsStorage.SaveComponents();

                    Game.displayGuiScreen(parentScreen);
                    break;
                case BUTTON_CREATE:
                    Game.displayGuiScreen(new GuiNewDebug(this));
                    break;
                default:
                    _slot?.ActionPerformed(button);
                    break;
            }
        }
    }
}
