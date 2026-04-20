using BetaSharp.NBT;
using BetaSharp.Network.Packets;
using BetaSharp.Network.Packets.Play;

namespace BetaSharp.Blocks.Entities;

/// <summary>
///     Block entity for a sign, storing the text on the sign and whether it is editable.
/// </summary>
public class BlockEntitySign : BlockEntity
{
    public override BlockEntityType Type => Sign;

    public bool Editable { get; set; } = true;

    /// <summary>
    ///     The text on the sign, with a maximum of 15 characters per line and 4 lines total.
    /// </summary>
    public string[] Texts { get; set; } = ["", "", "", ""];

    public int CurrentRow { get; set; } = -1;

    public override void WriteNbt(NBTTagCompound nbt)
    {
        base.WriteNbt(nbt);
        nbt.SetString("Text1", Texts[0]);
        nbt.SetString("Text2", Texts[1]);
        nbt.SetString("Text3", Texts[2]);
        nbt.SetString("Text4", Texts[3]);
    }

    public override void ReadNbt(NBTTagCompound nbt)
    {
        Editable = false;
        base.ReadNbt(nbt);

        for (int line = 0; line < 4; ++line)
        {
            Texts[line] = nbt.GetString("Text" + (line + 1));
            if (Texts[line].Length > 15)
            {
                Texts[line] = Texts[line].Substring(0, 15);
            }
        }
    }

    public override Packet CreateUpdatePacket()
    {
        string[] lines = new string[4];

        for (int lineIndex = 0; lineIndex < 4; lineIndex++)
        {
            lines[lineIndex] = Texts[lineIndex];
        }

        return UpdateSignPacket.Get(X, Y, Z, lines);
    }
}
