using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Sound;

public class SoundPool
{
    public bool IsRandom { get; set; } = true;
    private readonly JavaRandom _rand = new();
    public int LoadedSoundCount => _allLoadedSounds.Count;
    private readonly Dictionary<string, List<SoundPoolEntry>> _weightedSoundSet = [];
    private readonly List<SoundPoolEntry> _allLoadedSounds = [];


    public SoundPoolEntry AddSound(string soundPath, FileInfo fileInfo)
    {
        string soundKey = soundPath;
        int dotIndex = soundKey.IndexOf('.');

        if (dotIndex != -1)
        {
            soundKey = soundKey[..dotIndex];
        }

        if (IsRandom)
        {
            while (soundKey.Length > 0 && char.IsDigit(soundKey[^1]))
            {
                soundKey = soundKey[..^1];
            }
        }

        soundKey = soundKey.Replace('/', '.');

        if (!_weightedSoundSet.TryGetValue(soundKey, out List<SoundPoolEntry>? variations))
        {
            variations = [];
            _weightedSoundSet[soundKey] = variations;
        }

        SoundPoolEntry entry = new(soundPath, new Uri(fileInfo.FullName));

        variations.Add(entry);
        _allLoadedSounds.Add(entry);

        return entry;
    }

    public SoundPoolEntry? GetRandomSoundFromSoundPool(string soundKey)
    {
        if (_weightedSoundSet.TryGetValue(soundKey, out List<SoundPoolEntry>? variations) && variations.Count > 0)
        {
            return variations[_rand.NextInt(variations.Count)];
        }

        return null;
    }

    public SoundPoolEntry? GetRandomSound()
    {
        return _allLoadedSounds.Count == 0 ? null : _allLoadedSounds[_rand.NextInt(_allLoadedSounds.Count)];
    }
}
