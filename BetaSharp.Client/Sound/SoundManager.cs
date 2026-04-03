using BetaSharp.Client.Options;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using SFML.Audio;
using SFML.System;

namespace BetaSharp.Client.Sound;

public class SoundManager : IDisposable
{
    private readonly SoundPool _soundPoolSounds = new();
    private readonly SoundPool _soundPoolStreaming = new();

    private readonly Dictionary<ResourceLocation, MusicCategory> _musicCategories = [];

    private readonly Dictionary<string, List<SoundBuffer>> _soundBuffers = [];

    private const int MaxChannels = 32;
    private readonly SFML.Audio.Sound[] _soundChannels = new SFML.Audio.Sound[MaxChannels];

    private int _soundSourceSuffix = 0;
    private GameOptions _options;
    private static bool s_started = false;
    private readonly JavaRandom _rand = new();

    private Music _currentMusic = null;
    private Music _currentStreaming = null;

    public int ActiveChannelCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < MaxChannels; i++)
            {
                if (_soundChannels[i] != null && _soundChannels[i].Status == SoundStatus.Playing)
                    count++;
            }
            return count;
        }
    }

    public int LoadedSoundNameCount => _soundBuffers.Count;
    public int LoadedSoundFileCount => _soundPoolSounds.LoadedSoundCount;
    public int LoadedStreamingFileCount => _soundPoolStreaming.LoadedSoundCount;
    public bool IsMusicPlaying => _currentMusic != null && _currentMusic.Status == SoundStatus.Playing;
    public bool IsStreamingPlaying => _currentStreaming != null && _currentStreaming.Status == SoundStatus.Playing;
    public ResourceLocation? ActiveCategory { get; private set; }
    public IReadOnlyDictionary<ResourceLocation, MusicCategory> MusicCategories => _musicCategories;
    public string? CurrentMusicName { get; private set; }
    public string? CurrentStreamingName { get; private set; }

    public void RegisterMusicCategory(ResourceLocation name, int minDelayTicks, int maxDelayTicks)
    {
        _musicCategories[name] = new MusicCategory(name, minDelayTicks, maxDelayTicks);
    }

    public void LoadSoundSettings(GameOptions options)
    {
        _soundPoolStreaming.IsRandom = false;
        _options = options;
        if (!s_started && (options == null || options.SoundVolume != 0.0F || options.MusicVolume != 0.0F))
        {
            TryToSetLibraryAndCodecs();
        }
    }

    private static string SanitizePath(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;

        if (path.StartsWith('/') && path.Length >= 3 && path[2] == ':')
        {
            path = path[1..];
        }

        char separator = System.IO.Path.DirectorySeparatorChar;
        return path.Replace('/', separator).Replace('\\', separator);
    }

    private void TryToSetLibraryAndCodecs()
    {

        float soundVolume = _options.SoundVolume;
        float musicVolume = _options.MusicVolume;
        _options.SoundVolume = 0.0F;
        _options.MusicVolume = 0.0F;
        _options.SaveOptions();

        _options.SoundVolume = soundVolume;
        _options.MusicVolume = musicVolume;
        _options.SaveOptions();

        s_started = true;
    }

    public void OnSoundOptionsChanged()
    {
        if (!s_started && (_options.SoundVolume != 0.0F || _options.MusicVolume != 0.0F))
        {
            TryToSetLibraryAndCodecs();
        }

        if (s_started)
        {
            if (_options.MusicVolume == 0.0F)
            {
                _currentMusic?.Stop();
            }
            else
            {
                _currentMusic?.Volume = _options.MusicVolume * 100.0F;
            }
        }
    }

    public void AddSound(string name, FileInfo file)
    {
        _soundPoolSounds.AddSound(name, file);
        LoadSoundBuffer(name, file);
    }

    public void AddStreaming(string name, FileInfo file) => _soundPoolStreaming.AddSound(name, file);

    public void AddMusic(ResourceLocation category, string name, FileInfo file)
    {
        if (_musicCategories.TryGetValue(category, out MusicCategory? musicCategory))
        {
            musicCategory.Pool.AddSound(name, file);
        }
    }

    private void LoadSoundBuffer(string name, FileInfo file)
    {

        string filepath = SanitizePath(file.FullName);
        string resourceName = name;

        int dotIndex = resourceName.IndexOf('.');
        if (dotIndex >= 0)
        {
            resourceName = resourceName[..dotIndex];
        }

        if (_soundPoolSounds.IsRandom)
        {
            while (resourceName.Length > 0 && char.IsDigit(resourceName[resourceName.Length - 1]))
            {
                resourceName = resourceName[..^1];
            }
        }

        resourceName = resourceName.Replace("/", ".");

        if (!_soundBuffers.TryGetValue(resourceName, out List<SoundBuffer>? value))
        {
            value = [];
            _soundBuffers[resourceName] = value;
        }

        SoundBuffer buffer = new(filepath);
        value.Add(buffer);

    }

    private SoundBuffer getRandomSoundBuffer(string name)
    {
        if (name == null)
        {
            return null;
        }

        if (!_soundBuffers.TryGetValue(name, out List<SoundBuffer>? value) || value.Count == 0)
        {
            return null;
        }

        int index = _rand.NextInt(value.Count);
        return value[index];
    }

    private SFML.Audio.Sound getFreeSoundChannel(SoundBuffer buffer)
    {
        for (int i = 0; i < MaxChannels; i++)
        {
            if (_soundChannels[i] == null)
            {
                _soundChannels[i] = new SFML.Audio.Sound(buffer);
                return _soundChannels[i];
            }

            if (_soundChannels[i].Status == SoundStatus.Stopped)
            {
                _soundChannels[i].SoundBuffer = buffer;
                return _soundChannels[i];
            }
        }

        SFML.Audio.Sound stolen = _soundChannels[0];
        stolen.Stop();
        stolen.SoundBuffer = buffer;
        return stolen;
    }

    public void PlayRandomMusicIfReady(ResourceLocation category)
    {
        if (!s_started || _options.MusicVolume == 0.0F) return;

        if (!_musicCategories.TryGetValue(category, out MusicCategory? musicCategory)) return;

        bool isMusicPlaying = _currentMusic != null && _currentMusic.Status == SoundStatus.Playing;
        bool isStreamingPlaying = _currentStreaming != null && _currentStreaming.Status == SoundStatus.Playing;

        if ((isMusicPlaying || isStreamingPlaying) && ActiveCategory == category) return;

        if (musicCategory.TicksBeforeNext > 0)
        {
            --musicCategory.TicksBeforeNext;
            return;
        }

        SoundPoolEntry? entry = musicCategory.Pool.GetRandomSound();
        if (entry == null) return;

        musicCategory.ResetDelay();

        _currentMusic?.Stop();
        _currentMusic?.Dispose();
        _currentMusic = null;

        string musicName = SanitizePath(entry.SoundUrl.LocalPath);

        _currentMusic = new Music(musicName)
        {
            Volume = _options.MusicVolume * 100.0F,
            IsLooping = false,
            RelativeToListener = true,
            Position = new Vector3f(0, 0, 0)
        };

        _currentMusic.Play();
        ActiveCategory = category;
        CurrentMusicName = entry.SoundName;
    }

    public void StopCurrentMusic()
    {
        _currentMusic?.Stop();
        _currentMusic?.Dispose();
        _currentMusic = null;
        _currentStreaming?.Stop();
        _currentStreaming?.Dispose();
        _currentStreaming = null;
        ActiveCategory = null;
        CurrentMusicName = null;
        CurrentStreamingName = null;
    }

    public void StopMusic(ResourceLocation? category = null)
    {
        if (category == null || ActiveCategory == category)
        {
            StopCurrentMusic();

            if (category != null && _musicCategories.TryGetValue(category, out MusicCategory? musicCategory))
            {
                musicCategory.TicksBeforeNext = 0;
            }
        }
    }

    public void UpdateListener(EntityLiving player, float partialTicks)
    {
        if (!s_started || _options.SoundVolume == 0.0F || player == null) return;


        float yaw = player.prevYaw + (player.yaw - player.prevYaw) * partialTicks;
        double x = player.prevX + (player.x - player.prevX) * (double)partialTicks;
        double y = player.prevY + (player.y - player.prevY) * (double)partialTicks;
        double z = player.prevZ + (player.z - player.prevZ) * (double)partialTicks;

        float lookX = MathHelper.Cos(-yaw * ((float)Math.PI / 180.0F) - (float)Math.PI);
        float lookY = MathHelper.Sin(-yaw * ((float)Math.PI / 180.0F) - (float)Math.PI);

        Listener.Position = new Vector3f((float)x, (float)y, (float)z);
        Listener.Direction = new Vector3f(-lookY, 0.0F, -lookX);
        Listener.UpVector = new Vector3f(0.0F, 1.0F, 0.0F);
    }

    public void PlayStreaming(string? name, float x, float y, float z, float volume, float pitch)
    {
        if (!(s_started && _options.SoundVolume != 0.0F)) return;

        if (_currentStreaming != null && _currentStreaming.Status == SoundStatus.Playing)
        {
            _currentStreaming.Stop();
        }

        if (name == null) return;

        SoundPoolEntry? entry = _soundPoolStreaming.GetRandomSoundFromSoundPool(name);
        if (entry == null || volume <= 0.0F) return;


        if (_currentMusic != null && _currentMusic.Status == SoundStatus.Playing)
        {
            _currentMusic.Stop();
        }

        _currentStreaming?.Dispose();
        _currentStreaming = new Music(SanitizePath(entry.SoundUrl.LocalPath))
        {
            Volume = 0.5F * _options.SoundVolume * 100.0F,
            IsLooping = false,
            RelativeToListener = false,
            Position = new(x, y, z)
        };

        _currentStreaming.Play();
        CurrentStreamingName = entry.SoundName;
    }

    public void PlaySound(string name, float x, float y, float z, float volume, float pitch)
    {
        if (!(s_started && _options.SoundVolume != 0.0F)) return;

        SoundBuffer buffer = getRandomSoundBuffer(name);
        if (buffer == null || volume <= 0.0F) return;


        _soundSourceSuffix = (_soundSourceSuffix + 1) % 256;

        SFML.Audio.Sound sound = getFreeSoundChannel(buffer);

        sound.Position = new Vector3f(x, y, z);
        sound.RelativeToListener = false;

        float minDistance = 16.0F;
        if (volume > 1.0F)
        {
            minDistance *= volume;
        }
        sound.MinDistance = minDistance;
        sound.Attenuation = 2.0F;

        sound.Pitch = pitch;

        float finalVolume = volume;
        if (finalVolume > 1.0F)
        {
            finalVolume = 1.0F;
        }
        sound.Volume = finalVolume * _options.SoundVolume * 100.0F;

        sound.Play();
    }

    public void PlaySoundFX(string name, float volume, float pitch)
    {
        if (!(s_started && _options.SoundVolume != 0.0F)) return;

        SoundBuffer buffer = getRandomSoundBuffer(name);
        if (buffer == null) return;

        _soundSourceSuffix = (_soundSourceSuffix + 1) % 256;

        SFML.Audio.Sound sound = getFreeSoundChannel(buffer);

        sound.RelativeToListener = true;
        sound.Position = new Vector3f(0.0F, 0.0F, 0.0F);

        sound.Pitch = pitch;

        float finalVolume = volume;
        if (finalVolume > 1.0F)
        {
            finalVolume = 1.0F;
        }
        finalVolume *= 0.25F;
        sound.Volume = finalVolume * _options.SoundVolume * 100.0F;

        sound.MinDistance = 1.0f;
        sound.Attenuation = 1.0f;

        sound.Play();
    }

    public void Dispose()
    {
        if (!s_started) return;

        _currentMusic?.Stop();
        _currentMusic?.Dispose();
        _currentMusic = null;
        _currentStreaming?.Stop();
        _currentStreaming?.Dispose();
        _currentStreaming = null;

        for (int i = 0; i < MaxChannels; i++)
        {
            if (_soundChannels[i] != null)
            {
                _soundChannels[i].Stop();
                _soundChannels[i].Dispose();
                _soundChannels[i] = null;
            }
        }

        foreach (List<SoundBuffer> bufferList in _soundBuffers.Values)
        {
            foreach (SoundBuffer buffer in bufferList)
            {
                buffer.Dispose();
            }
        }
        _soundBuffers.Clear();

    }
}
