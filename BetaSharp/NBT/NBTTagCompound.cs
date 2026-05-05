namespace BetaSharp.NBT;

public sealed class NBTTagCompound : NBTBase
{
    public IEnumerable<NBTBase> Values => _dictionary.Values;
    public IReadOnlyDictionary<string, NBTBase> Dictionary => _dictionary;

    private readonly Dictionary<string, NBTBase> _dictionary = [];

    public override void WriteTagContents(Stream output)
    {
        foreach (NBTBase value in _dictionary.Values)
        {
            WriteTag(value, output);
        }

        output.WriteByte(0);
    }

    public override void ReadTagContents(Stream input)
    {
        _dictionary.Clear();

        while (true)
        {
            NBTBase tag = ReadTag(input);

            if (tag.GetTagType() is 0)
            {
                return;
            }

            _dictionary[tag.Key] = tag;
        }
    }

    public override byte GetTagType()
    {
        return 10;
    }

    public void SetTag(string key, NBTBase value)
    {
        value.Key = key;
        _dictionary[key] = value;
    }

    public void SetByte(string key, sbyte value)
    {
        _dictionary[key] = new NBTTagByte(value)
        {
            Key = key
        };
    }

    public void SetShort(string key, short value)
    {
        _dictionary[key] = new NBTTagShort(value)
        {
            Key = key
        };
    }

    public void SetInteger(string key, int value)
    {
        _dictionary[key] = new NBTTagInt(value)
        {
            Key = key
        };
    }

    public void SetLong(string key, long value)
    {
        _dictionary[key] = new NBTTagLong(value)
        {
            Key = key
        };
    }

    public void SetFloat(string key, float value)
    {
        _dictionary[key] = new NBTTagFloat(value)
        {
            Key = key
        };
    }

    public void SetDouble(string key, double value)
    {
        _dictionary[key] = new NBTTagDouble(value)
        {
            Key = key
        };
    }

    public void SetString(string key, string? value)
    {
        _dictionary[key] = new NBTTagString(value)
        {
            Key = key
        };
    }

    public void SetByteArray(string key, byte[] value)
    {
        _dictionary[key] = new NBTTagByteArray(value)
        {
            Key = key
        };
    }

    public void SetCompoundTag(string key, NBTTagCompound value)
    {
        value.Key = key;
        _dictionary[key] = value;
    }

    public void SetBoolean(string key, bool value)
    {
        SetByte(key, (sbyte)(value ? 1 : 0));
    }

    public bool HasKey(string key)
    {
        return _dictionary.ContainsKey(key);
    }

    public sbyte GetByte(string key)
    {
        return !_dictionary.TryGetValue(key, out NBTBase? value) ? (sbyte)0 : ((NBTTagByte)value).Value;
    }

    public short GetShort(string key)
    {
        return !_dictionary.TryGetValue(key, out NBTBase? value) ? (short)0 : ((NBTTagShort)value).Value;
    }

    public int GetInteger(string key)
    {
        return !_dictionary.TryGetValue(key, out NBTBase? value) ? 0 : ((NBTTagInt)value).Value;
    }

    public long GetLong(string key)
    {
        return !_dictionary.TryGetValue(key, out NBTBase? value) ? 0L : ((NBTTagLong)value).Value;
    }

    public float GetFloat(string key)
    {
        return !_dictionary.TryGetValue(key, out NBTBase? value) ? 0.0F : ((NBTTagFloat)value).Value;
    }

    public double GetDouble(string key)
    {
        return !_dictionary.TryGetValue(key, out NBTBase? value) ? 0.0D : ((NBTTagDouble)value).Value;
    }

    public string GetString(string key)
    {
        return !_dictionary.TryGetValue(key, out NBTBase? value) ? string.Empty : ((NBTTagString)value).Value;
    }

    public byte[] GetByteArray(string key)
    {
        return !_dictionary.TryGetValue(key, out NBTBase? value) ? [] : ((NBTTagByteArray)value).Values;
    }

    public NBTTagCompound GetCompoundTag(string key)
    {
        return !_dictionary.TryGetValue(key, out NBTBase? value) ? new NBTTagCompound() : (NBTTagCompound)value;
    }

    public NBTTagList GetTagList(string key)
    {
        return !_dictionary.TryGetValue(key, out NBTBase? value) ? new NBTTagList() : (NBTTagList)value;
    }

    public bool GetBoolean(string key)
    {
        return GetByte(key) != 0;
    }

    public override string ToString()
    {
        return $"{_dictionary.Count} entries";
    }
}
