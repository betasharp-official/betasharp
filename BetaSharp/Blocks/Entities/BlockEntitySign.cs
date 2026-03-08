using BetaSharp.NBT;
using BetaSharp.Network.Packets;
using BetaSharp.Network.Packets.Play;

namespace BetaSharp.Blocks.Entities;

public class BlockEntitySign : BlockEntity
{
    public string[] Texts { get; set; } = ["", "", "", ""];
    public int CurrentRow { get; set; } = -1;
    private bool _editable = true;

    public override void writeNbt(NBTTagCompound nbt)
    {
        base.writeNbt(nbt);
        nbt.SetString("Text1", Texts[0]);
        nbt.SetString("Text2", Texts[1]);
        nbt.SetString("Text3", Texts[2]);
        nbt.SetString("Text4", Texts[3]);
    }

    public override void readNbt(NBTTagCompound nbt)
    {
        _editable = false;
        base.readNbt(nbt);

        for (int line = 0; line < 4; ++line)
        {
            Texts[line] = nbt.GetString("Text" + (line + 1));
            if (Texts[line].Length > 15)
            {
                Texts[line] = Texts[line].Substring(0, 15);
            }
        }

    }

    public override Packet createUpdatePacket()
    {
        string[] lines = new string[4];

        for (int lineIndex = 0; lineIndex < 4; lineIndex++)
        {
            lines[lineIndex] = Texts[lineIndex];
        }

        return UpdateSignPacket.Get(X, Y, Z, lines);
    }

    public bool IsEditable()
    {
        return _editable;
    }

    public void SetEditable(bool editable)
    {
        _editable = editable;
    }
}
