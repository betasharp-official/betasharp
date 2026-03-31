using BetaSharp.Blocks.Entities;
using BetaSharp.Client.Guis;
using BetaSharp.Client.Input;
using BetaSharp.Client.UI.Controls;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Layout.Flexbox;
using BetaSharp.Util;

namespace BetaSharp.Client.UI.Screens.InGame;

public class SignEditScreen(UIContext context, BlockEntitySign sign, Action? editCompleted) : UIScreen(context)
{
    private readonly BlockEntitySign _sign = sign;
    private int _editLine = 0;
    private int _updateCounter = 0;

    protected override void Init()
    {
        Root.Style.AlignItems = Align.Center;
        Root.Style.JustifyContent = Justify.Center;

        Root.AddChild(new Background(BackgroundType.World));

        Label title = new() { Text = "Edit sign message:", TextColor = Color.White };
        title.Style.MarginBottom = 10;
        Root.AddChild(title);

        SignPreview preview = new() { Sign = _sign };
        preview.Style.Width = 300;
        preview.Style.Height = 150;
        preview.Style.MarginBottom = 40;
        Root.AddChild(preview);

        Button btnDone = CreateButton();
        btnDone.Text = "Done";
        btnDone.Style.Width = 200;
        btnDone.OnClick += (_) => CloseAndSave();
        Root.AddChild(btnDone);
    }

    public override void Update(float partialTicks)
    {
        base.Update(partialTicks);
        _updateCounter++;
        if (_updateCounter / 6 % 2 == 0)
        {
            _sign.CurrentRow = _editLine;
        }
        else
        {
            _sign.CurrentRow = -1;
        }
    }

    public override void KeyTyped(int key, char character)
    {
        if (key == Keyboard.KEY_UP)
        {
            _editLine = (_editLine - 1) & 3;
        }
        else if (key == Keyboard.KEY_DOWN || key == Keyboard.KEY_RETURN)
        {
            _editLine = (_editLine + 1) & 3;
        }
        else if (key == Keyboard.KEY_BACK)
        {
            if (_sign.Texts[_editLine].Length > 0)
            {
                _sign.Texts[_editLine] = _sign.Texts[_editLine].Substring(0, _sign.Texts[_editLine].Length - 1);
            }
        }
        else if (key == Keyboard.KEY_ESCAPE)
        {
            CloseAndSave();
        }
        else if (ChatAllowedCharacters.IsAllowedCharacter(character) && _sign.Texts[_editLine].Length < 15)
        {
            _sign.Texts[_editLine] += character;
        }
        else
        {
            base.KeyTyped(key, character);
        }
    }

    private void CloseAndSave()
    {
        _sign.CurrentRow = -1; // Reset cursor
        _sign.markDirty();
        Context.Navigator.Navigate(null);
    }

    public override void Uninit()
    {
        editCompleted?.Invoke();
    }
}
