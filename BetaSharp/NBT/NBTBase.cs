namespace BetaSharp.NBT;

public abstract class NBTBase
{
    public string Key { get; set; } = string.Empty;

    public abstract void WriteTagContents(Stream output);

    public abstract void ReadTagContents(Stream input);

    public abstract byte GetTagType();

    public static NBTBase ReadTag(Stream input)
    {
        var identifier = input.ReadByte();

        if (identifier is 0)
        {
            return new NBTTagEnd();
        }

        var tag = CreateTagOfType(identifier);

        tag.Key = input.ReadString();
        tag.ReadTagContents(input);

        return tag;
    }

    public static void WriteTag(NBTBase tag, Stream output)
    {
        output.WriteByte(tag.GetTagType());

        if (tag.GetTagType() is 0)
        {
            return;
        }

        output.WriteString(tag.Key);
        tag.WriteTagContents(output);
    }

    public static NBTBase CreateTagOfType(int identifier)
    {
        return identifier switch
        {
            0 => new NBTTagEnd(),
            1 => new NBTTagByte(),
            2 => new NBTTagShort(),
            3 => new NBTTagInt(),
            4 => new NBTTagLong(),
            5 => new NBTTagFloat(),
            6 => new NBTTagDouble(),
            7 => new NBTTagByteArray(),
            8 => new NBTTagString(),
            9 => new NBTTagList(),
            10 => new NBTTagCompound(),
            _ => throw new ArgumentOutOfRangeException(nameof(identifier), identifier, "Unknown NBT identifier")
        };
    }

    public static string GetTagName(int identifier)
    {
        return identifier switch
        {
            0 => "TAG_End",
            1 => "TAG_Byte",
            2 => "TAG_Short",
            3 => "TAG_Int",
            4 => "TAG_Long",
            5 => "TAG_Float",
            6 => "TAG_Double",
            7 => "TAG_Byte_Array",
            8 => "TAG_String",
            9 => "TAG_List",
            10 => "TAG_Compound",
            _ => throw new ArgumentOutOfRangeException(nameof(identifier), identifier, "Unknown NBT identifier")
        };
    }
}
