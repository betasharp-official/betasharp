using BetaSharp.Blocks;
using BetaSharp.Blocks.Entities;
using BetaSharp.Client.Input;
using BetaSharp.Client.Rendering.Blocks.Entities;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Network.Packets.Play;
using BetaSharp.Util;

namespace BetaSharp.Client.Guis;

public class GuiEditSign : GuiScreen
{
    private const string ScreenTitle = "Edit sign message:";
    private const int ButtonDoneId = 0;
    private const int MaxLineLength = 15;

    private readonly BlockEntitySign _entitySign;
    private int _updateCounter;
    private int _editLine = 0;

    public GuiEditSign(BlockEntitySign sign)
    {
        _entitySign = sign;
    }

    public override void InitGui()
    {
        _controlList.Clear();
        Keyboard.enableRepeatEvents(true);
        _controlList.Add(new GuiButton(ButtonDoneId, Width / 2 - 100, Height / 4 + 120, "Done"));
    }

    public override void OnGuiClosed()
    {
        Keyboard.enableRepeatEvents(false);
        if (mc?.world?.isRemote ?? false)
        {
            mc.getSendQueue().addToSendQueue(new UpdateSignPacket(_entitySign.X, _entitySign.Y, _entitySign.Z, _entitySign.Texts));
        }
    }

    public override void UpdateScreen()
    {
        ++_updateCounter;
    }

    protected override void ActionPerformed(GuiButton button)
    {
        if (button.Enabled && button.Id == ButtonDoneId)
        {
            _entitySign.markDirty();
            mc?.displayGuiScreen(null);
        }
    }

    protected override void KeyTyped(char eventChar, int eventKey)
    {
        if (eventKey == Keyboard.KEY_UP)
        {
            _editLine = _editLine - 1 & 3;
            return;
        }

        if (eventKey == Keyboard.KEY_DOWN || eventKey == Keyboard.KEY_RETURN)
        {
            _editLine = _editLine + 1 & 3;
            return;
        }

        if (eventKey == Keyboard.KEY_BACK)
        {
            if (_entitySign.Texts[_editLine].Length > 0)
            {
                _entitySign.Texts[_editLine] = _entitySign.Texts[_editLine].Substring(0, _entitySign.Texts[_editLine].Length - 1);
            }
            return;
        }

        if (eventKey == Keyboard.KEY_ESCAPE)
        {
            _entitySign.markDirty();
            mc?.displayGuiScreen(null);
            return;
        }

        if (ChatAllowedCharacters.IsAllowedCharacter(eventChar) && _entitySign.Texts[_editLine].Length < MaxLineLength)
        {
            _entitySign.Texts[_editLine] += eventChar;
        }
    }

    public override void Render(int mouseX, int mouseY, float partialTicks)
    {
        DrawDefaultBackground();
        if (FontRenderer != null)
        {
            DrawCenteredString(FontRenderer, ScreenTitle, Width / 2, 40, Color.White);
        }

        GLManager.GL.PushMatrix();
        GLManager.GL.Translate(Width / 2, 0.0F, 50.0F);
        float scale = 93.75F;
        GLManager.GL.Scale(-scale, -scale, -scale);
        GLManager.GL.Rotate(180.0F, 0.0F, 1.0F, 0.0F);

        Block signBlock = _entitySign.getBlock();
        if (signBlock == Block.Sign)
        {
            float rotation = _entitySign.getPushedBlockData() * 360 / 16.0F;
            GLManager.GL.Rotate(rotation, 0.0F, 1.0F, 0.0F);
            GLManager.GL.Translate(0.0F, -1.0625F, 0.0F);
        }
        else
        {
            int rotationIndex = _entitySign.getPushedBlockData();
            float angle = 0.0F;
            if (rotationIndex == 2) angle = 180.0F;
            if (rotationIndex == 4) angle = 90.0F;
            if (rotationIndex == 5) angle = -90.0F;

            GLManager.GL.Rotate(angle, 0.0F, 1.0F, 0.0F);
            GLManager.GL.Translate(0.0F, -1.0625F, 0.0F);
        }

        if (_updateCounter / 6 % 2 == 0)
        {
            _entitySign.CurrentRow = _editLine;
        }

        BlockEntityRenderer.Instance.RenderTileEntityAt(_entitySign, -0.5D, -0.75D, -0.5D, 0.0F);
        _entitySign.CurrentRow = -1;
        GLManager.GL.PopMatrix();

        base.Render(mouseX, mouseY, partialTicks);
    }
}
