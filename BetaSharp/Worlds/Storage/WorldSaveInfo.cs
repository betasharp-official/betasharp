namespace BetaSharp.Worlds.Storage;

public class WorldSaveInfo : IComparable<WorldSaveInfo>
{
    public WorldSaveInfo(string fileName, string displayName, long lastPlayed, long size, bool isUnsupported)
    {
        FileName = fileName;
        DisplayName = displayName;
        LastPlayed = lastPlayed;
        Size = size;
        IsUnsupported = isUnsupported;
    }

    public string FileName { get; }
    public string DisplayName { get; }
    public long LastPlayed { get; }
    public long Size { get; }
    public bool IsUnsupported { get; }

    public int CompareTo(WorldSaveInfo? other)
    {
        if (other is null)
        {
            return 1;
        }

        int timeComparison = other.LastPlayed.CompareTo(LastPlayed);
        if (timeComparison != 0)
        {
            return timeComparison;
        }

        return string.Compare(FileName, other.FileName, StringComparison.Ordinal);
    }
}
