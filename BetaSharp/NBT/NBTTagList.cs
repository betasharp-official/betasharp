namespace BetaSharp.NBT;

public sealed class NBTTagList : NBTBase
{
    private List<NBTBase> list = [];
    private byte type;

    public override void WriteTagContents(Stream output)
    {
        type = list.Count > 0 ? list[0].GetTagType() : (byte)1;

        output.WriteByte(type);
        output.WriteInt(list.Count);

        foreach (var tag in list)
        {
            tag.WriteTagContents(output);
        }
    }

    public override void ReadTagContents(Stream input)
    {
        list = [];
        type = (byte)input.ReadByte();

        var length = input.ReadInt();

        for (var index = 0; index < length; ++index)
        {
            var tag = CreateTagOfType(type);
            tag.ReadTagContents(input);
            list.Add(tag);
        }
    }

    public override byte GetTagType()
    {
        return 9;
    }

    public override string ToString()
    {
        return $"{list.Count} entries of type {GetTagName(type)}";
    }

    public void SetTag(NBTBase value)
    {
        type = value.GetTagType();
        list.Add(value);
    }

    public NBTBase TagAt(int value)
    {
        return list[value];
    }

    public int TagCount()
    {
        return list.Count;
    }
}
