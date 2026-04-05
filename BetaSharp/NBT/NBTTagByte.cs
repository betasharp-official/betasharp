namespace BetaSharp.NBT;

internal sealed class NBTTagByte : NBTBase
{
    public sbyte Value { get; set; }

    public NBTTagByte()
    {
    }

    public NBTTagByte(sbyte value)
    {
        Value = value;
    }

    public override void WriteTagContents(Stream output)
    {
        output.WriteByte((byte)Value);
    }

    public override void ReadTagContents(Stream input)
    {
        Value = (sbyte)input.ReadByte();
    }

    public override byte GetTagType()
    {
        return 1;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
