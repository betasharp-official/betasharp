using BetaSharp.Blocks;
using BetaSharp.Blocks.Entities;
using BetaSharp.Client.Guis.Controls;
using BetaSharp.Client.Input;
using BetaSharp.Client.Rendering.Blocks.Entities;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Network.Packets.Play;
using BetaSharp.Util;

namespace BetaSharp.Client.Guis;

public class GuiEditSign : Screen
{
    private const int MaxLineLength = 15;

    private readonly BlockEntitySign _entitySign;
    private int _updateCounter;
    private int _editLine;
    private static readonly string s_allowedCharacters = ChatAllowedCharacters.allowedCharacters;

    public GuiEditSign(BlockEntitySign sign)
    {
        _entitySign = sign;
        Text = "Edit sign message:";
        DisplayTitle = true;
        Keyboard.enableRepeatEvents(true);
        Button doneButton = new(EffectiveWidth / 2 - 100, EffectiveHeight / 4 + 120, "Done");
        doneButton.Clicked += (_, _) =>
        {
            _entitySign.markDirty();
            MC.OpenScreen(null);
        };
        AddChild(doneButton);
    }

    public override void OnGuiClosed()
    {
        Keyboard.enableRepeatEvents(false);
        if (MC?.world?.isRemote ?? false)
        {
            MC.getSendQueue().addToSendQueue(new UpdateSignPacket(_entitySign.X, _entitySign.Y, _entitySign.Z, _entitySign.Texts));
        }
    }

    protected override void OnTick()
    {
        ++_updateCounter;
    }

    protected override void OnKeyInput(KeyboardEventArgs e)
    {
        switch (e.Key)
        {
            case Keyboard.KEY_UP:
                _editLine = _editLine - 1 & 3;
                return;
            case Keyboard.KEY_DOWN or Keyboard.KEY_RETURN:
                _editLine = _editLine + 1 & 3;
                return;
            case Keyboard.KEY_BACK:
                {
                    if (_entitySign.Texts[_editLine].Length > 0)
                    {
                        _entitySign.Texts[_editLine] = _entitySign.Texts[_editLine][..(_entitySign.Texts[_editLine].Length - 1)];
                    }
                    return;
                }
            case Keyboard.KEY_ESCAPE:
                _entitySign.markDirty();
                MC?.OpenScreen(null);
                return;
        }

        if (s_allowedCharacters.Contains(e.KeyChar) && _entitySign.Texts[_editLine].Length < MaxLineLength)
        {
            _entitySign.Texts[_editLine] += e.KeyChar;
        }
    }

    protected override void OnRender(RenderEventArgs e)
    {
        DrawDefaultBackground();

        GLManager.GL.PushMatrix();
        GLManager.GL.Translate(EffectiveWidth / 2f, 0.0F, 50.0F);
        float scale = 93.75F;
        GLManager.GL.Scale(-scale, -scale, -scale);
        GLManager.GL.Rotate(180.0F, 0.0F, 1.0F, 0.0F);

        Block signBlock = _entitySign.getBlock();
        if (signBlock == Block.Sign)
        {
            float rotation = _entitySign.getPushedBlockData() * 360 / 16.0F;
            GLManager.GL.Rotate(rotation, 0.0F, 1.0F, 0.0F);
        }
        else
        {
            int rotationIndex = _entitySign.getPushedBlockData();
            float angle = 0.0F;
            if (rotationIndex == 2) angle = 180.0F;
            if (rotationIndex == 4) angle = 90.0F;
            if (rotationIndex == 5) angle = -90.0F;

            GLManager.GL.Rotate(angle, 0.0F, 1.0F, 0.0F);
        }

        GLManager.GL.Translate(0.0F, -1.0625F, 0.0F);

        if (_updateCounter / 6 % 2 == 0)
        {
            _entitySign.CurrentRow = _editLine;
        }

        BlockEntityRenderer.Instance.RenderTileEntityAt(_entitySign, -0.5D, -0.75D, -0.5D, 0.0F);
        _entitySign.CurrentRow = -1;
        GLManager.GL.PopMatrix();
    }
}
