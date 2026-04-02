namespace BetaSharp.Client.Sound;

public class SoundPoolEntry
{
    public string SoundName;
    public Uri SoundUrl;

    public SoundPoolEntry(string soundName, Uri soundUrl)
    {
        SoundName = soundName;
        SoundUrl = soundUrl;
    }
}
