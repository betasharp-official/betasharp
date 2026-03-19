using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Dimensions;
using BetaSharp.Worlds.Generation.Biomes;
using Silk.NET.Maths;

namespace BetaSharp.Worlds.Core.Systems;

public class EnvironmentManager
{
    private readonly Dimension _dimension;
    private readonly WorldProperties _props;
    private readonly JavaRandom _random;
    private readonly WorldReader _view;
    private readonly long _worldTimeMask = 0xFFFFFFL;

    public EnvironmentManager(WorldProperties props, Dimension dimension, WorldReader view, JavaRandom random)
    {
        _props = props;
        _dimension = dimension;
        _random = random;
        _view = view;
    }

    public float PrevRainingStrength { get; private set; }
    public float RainingStrength { get; private set; }
    public float PrevThunderingStrength { get; private set; }
    public float ThunderingStrength { get; private set; }

    public int TicksSinceLightning { get; set; }
    public int LightningTicksLeft { get; set; }

    public int AmbientDarkness { get; set; }
    public bool IsRaining => GetRainGradient(1.0F) > 0.2D;
    public event Action<bool>? OnRainingStateChanged;

    public void PrepareWeather()
    {
        if (!_props.IsRaining) return;

        RainingStrength = 1.0F;
        if (_props.IsThundering)
        {
            ThunderingStrength = 1.0F;
        }
    }

    public void UpdateWeatherCycles()
    {
        if (_dimension.HasCeiling)
        {
            return;
        }

        bool wasRaining = IsRaining;

        //WorldProperties props = _properties;
        //JavaRandom random = _random;

        if (TicksSinceLightning > 0)
        {
            --TicksSinceLightning;
        }

        int thunderTime = _props.ThunderTime;
        if (thunderTime <= 0)
        {
            _props.ThunderTime = _props.IsThundering ? _random.NextInt(12000) + 3600 : _random.NextInt(168000) + 12000;
        }
        else
        {
            --thunderTime;
            _props.ThunderTime = thunderTime;
            if (thunderTime <= 0)
            {
                _props.IsThundering = !_props.IsThundering;
            }
        }

        int rainTime = _props.RainTime;
        if (rainTime <= 0)
        {
            _props.RainTime = _props.IsRaining ? _random.NextInt(12000) + 12000 : _random.NextInt(168000) + 12000;
        }
        else
        {
            --rainTime;
            _props.RainTime = rainTime;
            if (rainTime <= 0)
            {
                _props.IsRaining = !_props.IsRaining;
            }
        }

        PrevRainingStrength = RainingStrength;
        RainingStrength += _props.IsRaining ? 0.01F : -0.01F;
        RainingStrength = Math.Clamp(RainingStrength, 0.0F, 1.0F);

        PrevThunderingStrength = ThunderingStrength;
        ThunderingStrength += _props.IsThundering ? 0.01F : -0.01F;
        ThunderingStrength = Math.Clamp(ThunderingStrength, 0.0F, 1.0F);

        if (wasRaining != IsRaining)
        {
            OnRainingStateChanged?.Invoke(IsRaining);
        }
    }

    public void ClearWeather()
    {
        WorldProperties props = _props;
        props.RainTime = 0;
        props.IsRaining = false;
        props.ThunderTime = 0;
        props.IsThundering = false;
    }

    public float GetTime(float delta) => _dimension.GetTimeOfDay(_props.WorldTime, delta);

    public int GetAmbientDarkness(float delta)
    {
        float timeOfDay = GetTime(delta);
        float sunIntensity = 1.0F - (MathHelper.Cos(timeOfDay * (float)Math.PI * 2.0F) * 2.0F + 0.5F);
        sunIntensity = Math.Clamp(sunIntensity, 0.0F, 1.0F);

        float lightLevel = 1.0F - sunIntensity;
        lightLevel = (float)(lightLevel * (1.0D - GetRainGradient(delta) * 5.0F / 16.0D));
        lightLevel = (float)(lightLevel * (1.0D - GetThunderGradient(delta) * 5.0F / 16.0D));

        return (int)((1.0F - lightLevel) * 11.0F);
    }

    public void UpdateSkyBrightness()
    {
        int darkness = GetAmbientDarkness(1.0F);
        if (darkness != AmbientDarkness)
        {
            AmbientDarkness = darkness;
        }
    }

    public float GetThunderGradient(float delta) => (PrevThunderingStrength + (ThunderingStrength - PrevThunderingStrength) * delta) * GetRainGradient(delta);
    public float GetRainGradient(float delta) => PrevRainingStrength + (RainingStrength - PrevRainingStrength) * delta;
    public void SetRainGradient(float rainGradient) => PrevRainingStrength = RainingStrength = rainGradient;
    public void SetThunderGradient(float thunderGradient) => PrevThunderingStrength = ThunderingStrength = thunderGradient;

    public bool IsThundering() => GetThunderGradient(1.0F) > 0.9D;

    public bool IsRainingAt(int x, int y, int z)
    {
        if (!IsRaining || y < _view.GetTopSolidBlockY(x, z))
        {
            return false;
        }

        Biome biome = _dimension.BiomeSource.GetBiome(x, z);
        return !biome.GetEnableSnow() && biome.CanSpawnLightningBolt();
    }

    public void SkipNightAndClearWeather()
    {
        long nextWorldTime = _props.WorldTime + 24000L;
        _props.WorldTime = nextWorldTime - nextWorldTime % 24000L;
        ClearWeather();
    }

    public Vector3D<double> GetCloudColor(float partialTicks)
    {
        float timeOfDay = _dimension.GetTimeOfDay(_props.WorldTime, partialTicks);

        float sunIntensity = MathHelper.Cos(timeOfDay * (float)Math.PI * 2.0F) * 2.0F + 0.5F;
        sunIntensity = Math.Clamp(sunIntensity, 0.0F, 1.0F);

        float red = ((_worldTimeMask >> 16) & 255L) / 255.0F;
        float green = ((_worldTimeMask >> 8) & 255L) / 255.0F;
        float blue = (_worldTimeMask & 255L) / 255.0F;

        float rainStrength = GetRainGradient(partialTicks);
        if (rainStrength > 0.0F)
        {
            float grayscaleLuminance = (red * 0.3F + green * 0.59F + blue * 0.11F) * 0.6F;
            float rainFactor = 1.0F - rainStrength * 0.95F;

            red = red * rainFactor + grayscaleLuminance * (1.0F - rainFactor);
            green = green * rainFactor + grayscaleLuminance * (1.0F - rainFactor);
            blue = blue * rainFactor + grayscaleLuminance * (1.0F - rainFactor);
        }

        red *= sunIntensity * 0.9F + 0.1F;
        green *= sunIntensity * 0.9F + 0.1F;
        blue *= sunIntensity * 0.85F + 0.15F;

        float thunderStrength = GetThunderGradient(partialTicks);
        if (thunderStrength > 0.0F)
        {
            float grayscaleLuminance = (red * 0.3F + green * 0.59F + blue * 0.11F) * 0.2F;
            float thunderFactor = 1.0F - thunderStrength * 0.95F;

            red = red * thunderFactor + grayscaleLuminance * (1.0F - thunderFactor);
            green = green * thunderFactor + grayscaleLuminance * (1.0F - thunderFactor);
            blue = blue * thunderFactor + grayscaleLuminance * (1.0F - thunderFactor);
        }

        return new Vector3D<double>(red, green, blue);
    }

    public Vector3D<double> GetSkyColor(Entity entity, float partialTicks)
    {
        float timeOfDay = _dimension.GetTimeOfDay(_props.WorldTime, partialTicks);

        float sunIntensity = MathHelper.Cos(timeOfDay * (float)Math.PI * 2.0F) * 2.0F + 0.5F;
        sunIntensity = Math.Clamp(sunIntensity, 0.0F, 1.0F);

        int blockX = MathHelper.Floor(entity.x);
        int blockZ = MathHelper.Floor(entity.z);
        float temperature = (float)_dimension.BiomeSource.GetTemperature(blockX, blockZ);
        int biomeSkyColorInt = _dimension.BiomeSource.GetBiome(blockX, blockZ).GetSkyColorByTemp(temperature);

        float red = ((biomeSkyColorInt >> 16) & 255) / 255.0F;
        float green = ((biomeSkyColorInt >> 8) & 255) / 255.0F;
        float blue = (biomeSkyColorInt & 255) / 255.0F;

        red *= sunIntensity;
        green *= sunIntensity;
        blue *= sunIntensity;

        float rainStrength = GetRainGradient(partialTicks);
        if (rainStrength > 0.0F)
        {
            float grayscaleLuminance = (red * 0.3F + green * 0.59F + blue * 0.11F) * 0.6F;
            float rainFactor = 1.0F - rainStrength * (12.0F / 16.0F);

            red = red * rainFactor + grayscaleLuminance * (1.0F - rainFactor);
            green = green * rainFactor + grayscaleLuminance * (1.0F - rainFactor);
            blue = blue * rainFactor + grayscaleLuminance * (1.0F - rainFactor);
        }

        float thunderStrength = GetThunderGradient(partialTicks);
        if (thunderStrength > 0.0F)
        {
            float grayscaleLuminance = (red * 0.3F + green * 0.59F + blue * 0.11F) * 0.2F;
            float thunderFactor = 1.0F - thunderStrength * (12.0F / 16.0F);

            red = red * thunderFactor + grayscaleLuminance * (1.0F - thunderFactor);
            green = green * thunderFactor + grayscaleLuminance * (1.0F - thunderFactor);
            blue = blue * thunderFactor + grayscaleLuminance * (1.0F - thunderFactor);
        }

        if (LightningTicksLeft <= 0) return new Vector3D<double>(red, green, blue);


        float lightningFactor = LightningTicksLeft - partialTicks;
        if (lightningFactor > 1.0F)
        {
            lightningFactor = 1.0F;
        }

        lightningFactor *= 0.45F;

        red = red * (1.0F - lightningFactor) + 0.8F * lightningFactor;
        green = green * (1.0F - lightningFactor) + 0.8F * lightningFactor;
        blue = blue * (1.0F - lightningFactor) + 1.0F * lightningFactor;

        return new Vector3D<double>(red, green, blue);
    }

    public bool CanMonsterSpawn() => AmbientDarkness < 4;
}
