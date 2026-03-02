using BetaSharp.Client.Input;
using BetaSharp.Stats;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Storage;
using java.text;
using java.util;

namespace BetaSharp.Client.Guis;

public class GuiSelectWorld : GuiScreen
{
    private const int BUTTON_CANCEL = 0;
    private const int BUTTON_SELECT = 1;
    private const int BUTTON_DELETE = 2;
    private const int BUTTON_CREATE = 3;
    private const int BUTTON_RENAME = 6;

    private readonly DateFormat dateFormatter = new SimpleDateFormat();
    protected GuiScreen parentScreen;
    protected string screenTitle = "Select world";
    private bool selected;
    private int selectedWorld;
    private List<WorldSaveInfo> saveList;
    private GuiWorldSlot worldSlotContainer;
    private string worldNameHeader;
    private string unsupportedFormatMessage;
    private bool deleting;
    private GuiButton buttonRename;
    private GuiButton buttonSelect;
    private GuiButton buttonDelete;

    public GuiSelectWorld(GuiScreen parentScreen)
    {
        this.parentScreen = parentScreen;
    }

    public override void InitGui()
    {
        TranslationStorage translations = TranslationStorage.Instance;
        screenTitle = translations.TranslateKey("selectWorld.title");
        worldNameHeader = translations.TranslateKey("selectWorld.world");
        unsupportedFormatMessage = "Unsupported Format!";
        loadSaves();
        worldSlotContainer = new GuiWorldSlot(this);
        worldSlotContainer.RegisterScrollButtons(_controlList, 4, 5);
        initButtons();
    }

    private void loadSaves()
    {
        IWorldStorageSource worldStorage = mc.getSaveLoader();
        saveList = worldStorage.GetAll();
        saveList.Sort();
        selectedWorld = -1;
    }

    protected string getSaveFileName(int worldIndex)
    {
        return saveList[worldIndex].FileName;
    }

    protected string getSaveName(int worldIndex)
    {
        string worldName = saveList[worldIndex].DisplayName;
        if (worldName == null || string.IsNullOrEmpty(worldName))
        {
            TranslationStorage translations = TranslationStorage.Instance;
            worldName = translations.TranslateKey("selectWorld.world") + " " + (worldIndex + 1);
        }

        return worldName;
    }

    public void initButtons()
    {
        TranslationStorage translations = TranslationStorage.Instance;
        _controlList.Add(buttonSelect = new GuiButton(BUTTON_SELECT, Width / 2 - 154, Height - 52, 150, 20, translations.TranslateKey("selectWorld.select")));
        _controlList.Add(buttonRename = new GuiButton(BUTTON_RENAME, Width / 2 - 154, Height - 28, 70, 20, translations.TranslateKey("selectWorld.rename")));
        _controlList.Add(buttonDelete = new GuiButton(BUTTON_DELETE, Width / 2 - 74, Height - 28, 70, 20, translations.TranslateKey("selectWorld.delete")));
        _controlList.Add(new GuiButton(BUTTON_CREATE, Width / 2 + 4, Height - 52, 150, 20, translations.TranslateKey("selectWorld.create")));
        _controlList.Add(new GuiButton(BUTTON_CANCEL, Width / 2 + 4, Height - 28, 150, 20, translations.TranslateKey("gui.cancel")));
        buttonSelect.Enabled = false;
        buttonRename.Enabled = false;
        buttonDelete.Enabled = false;
    }

    private void deleteWorld(int worldIndex)
    {
        string worldName = getSaveName(worldIndex);
        if (worldName != null)
        {
            deleting = true;
            TranslationStorage translations = TranslationStorage.Instance;
            string deleteQuestion = translations.TranslateKey("selectWorld.deleteQuestion");
            string deleteWarning = "'" + worldName + "' " + translations.TranslateKey("selectWorld.deleteWarning");
            string deleteButtonText = translations.TranslateKey("selectWorld.deleteButton");
            string cancelButtonText = translations.TranslateKey("gui.cancel");
            GuiYesNo confirmDialog = new(this, deleteQuestion, deleteWarning, deleteButtonText, cancelButtonText, worldIndex);
            mc.displayGuiScreen(confirmDialog);
        }
    }

    protected override void ActionPerformed(GuiButton button)
    {
        if (button.Enabled)
        {
            switch (button.Id)
            {
                case BUTTON_DELETE:
                    deleteWorld(selectedWorld);
                    break;
                case BUTTON_SELECT:
                    selectWorld(selectedWorld);
                    break;
                case BUTTON_CREATE:
                    mc.displayGuiScreen(new GuiCreateWorld(this));
                    break;
                case BUTTON_RENAME:
                    mc.displayGuiScreen(new GuiRenameWorld(this, getSaveFileName(selectedWorld)));
                    break;
                case BUTTON_CANCEL:
                    mc.displayGuiScreen(parentScreen);
                    break;
                default:
                    worldSlotContainer.ActionPerformed(button);
                    break;
            }
        }
    }

    public void selectWorld(int worldIndex)
    {
        if (!selected)
        {
            selected = true;
            mc.statFileWriter.ReadStat(Stats.Stats.LoadWorldStat, 1);
            mc.playerController = new PlayerControllerSP(mc);
            string worldFileName = getSaveFileName(worldIndex);
            worldFileName ??= "World" + worldIndex;

            mc.startWorld(worldFileName, getSaveName(worldIndex), 0L);
        }
    }

    public override void DeleteWorld(bool confirmed, int worldIndex)
    {
        if (deleting)
        {
            deleting = false;
            if (confirmed)
            {
                performDelete(worldIndex);
            }

            mc.displayGuiScreen(this);
        }

    }

    private void performDelete(int worldIndex)
    {
        IWorldStorageSource worldStorage = mc.getSaveLoader();
        worldStorage.Flush();
        worldStorage.Delete(getSaveFileName(worldIndex));
        loadSaves();
    }

    public override void Render(int mouseX, int mouseY, float partialTicks)
    {
        worldSlotContainer.DrawScreen(mouseX, mouseY, partialTicks);
        DrawCenteredString(FontRenderer, screenTitle, Width / 2, 20, Color.White);
        base.Render(mouseX, mouseY, partialTicks);
    }

    public static List<WorldSaveInfo> GetSize(GuiSelectWorld screen)
    {
        return screen.saveList;
    }

    public static int onElementSelected(GuiSelectWorld screen, int worldIndex)
    {
        return screen.selectedWorld = worldIndex;
    }

    public static int getSelectedWorld(GuiSelectWorld screen)
    {
        return screen.selectedWorld;
    }

    public static GuiButton getSelectButton(GuiSelectWorld screen)
    {
        return screen.buttonSelect;
    }

    public static GuiButton getRenameButton(GuiSelectWorld screen)
    {
        return screen.buttonRename;
    }

    public static GuiButton getDeleteButton(GuiSelectWorld screen)
    {
        return screen.buttonDelete;
    }

    public static string getWorldNameHeader(GuiSelectWorld screen)
    {
        return screen.worldNameHeader;
    }

    public static DateFormat getDateFormatter(GuiSelectWorld screen)
    {
        return screen.dateFormatter;
    }

    public static string getUnsupportedFormatMessage(GuiSelectWorld screen)
    {
        return screen.unsupportedFormatMessage;
    }
}
