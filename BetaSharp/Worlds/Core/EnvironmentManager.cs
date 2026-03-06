using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Generation.Biomes;
using Silk.NET.Maths;

namespace BetaSharp.Worlds.Core;

public class EnvironmentManager
{
    private readonly World _world;

    private readonly long _worldTimeMask = 0xFFFFFFL;
    private bool _allPlayersSleeping;

    public EnvironmentManager(World world) => _world = world;

    public float PrevRainingStrength { get; private set; }
    public float RainingStrength { get; private set; }
    public float PrevThunderingStrength { get; private set; }
    public float ThunderingStrength { get; private set; }

    public int TicksSinceLightning { get; set; }
    public int LightningTicksLeft { get; set; }

    public int AmbientDarkness { get; private set; }
    public bool IsRaining => GetRainGradient(1.0F) > 0.2D;

    public event Action<bool> OnRainingStateChanged;

    public void PrepareWeather()
    {
        WorldProperties props = _world.Properties;
        if (props.IsRaining)
        {
            RainingStrength = 1.0F;
            if (props.IsThundering)
            {
                ThunderingStrength = 1.0F;
            }
        }
    }

    public void UpdateWeatherCycles()
    {
        if (_world.Dimension.HasCeiling)
        {
            return;
        }

        bool wasRaining = IsRaining;

        WorldProperties props = _world.Properties;
        JavaRandom random = _world.random;

        if (TicksSinceLightning > 0)
        {
            --TicksSinceLightning;
        }

        int thunderTime = props.ThunderTime;
        if (thunderTime <= 0)
        {
            props.ThunderTime = props.IsThundering ? random.NextInt(12000) + 3600 : random.NextInt(168000) + 12000;
        }
        else
        {
            --thunderTime;
            props.ThunderTime = thunderTime;
            if (thunderTime <= 0)
            {
                props.IsThundering = !props.IsThundering;
            }
        }

        int rainTime = props.RainTime;
        if (rainTime <= 0)
        {
            props.RainTime = props.IsRaining ? random.NextInt(12000) + 12000 : random.NextInt(168000) + 12000;
        }
        else
        {
            --rainTime;
            props.RainTime = rainTime;
            if (rainTime <= 0)
            {
                props.IsRaining = !props.IsRaining;
            }
        }

        PrevRainingStrength = RainingStrength;
        RainingStrength += props.IsRaining ? 0.01F : -0.01F;
        RainingStrength = Math.Clamp(RainingStrength, 0.0F, 1.0F);

        PrevThunderingStrength = ThunderingStrength;
        ThunderingStrength += props.IsThundering ? 0.01F : -0.01F;
        ThunderingStrength = Math.Clamp(ThunderingStrength, 0.0F, 1.0F);

        if (wasRaining != IsRaining)
        {
            OnRainingStateChanged?.Invoke(IsRaining);
        }
    }

    public void ClearWeather()
    {
        WorldProperties props = _world.Properties;
        props.RainTime = 0;
        props.IsRaining = false;
        props.ThunderTime = 0;
        props.IsThundering = false;
    }

    public float GetTime(float delta) => _world.Dimension.GetTimeOfDay(_world.Properties.WorldTime, delta);

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
        if (!IsRaining || !_world.hasSkyLight(x, y, z) || _world.getTopSolidBlockY(x, z) > y)
        {
            return false;
        }

        Biome biome = _world.getBiomeSource().GetBiome(x, z);
        return !biome.GetEnableSnow() && biome.CanSpawnLightningBolt();
    }

    public void UpdateSleepingPlayers() => _allPlayersSleeping = _world.Entities.Players.Count > 0 && _world.Entities.Players.All(p => p.isSleeping());

    public bool CanSkipNight()
    {
        if (!_allPlayersSleeping || _world.IsRemote)
        {
            return false;
        }

        return _world.Entities.Players.All(player => player.isPlayerFullyAsleep());
    }

    public void AfterSkipNight()
    {
        _allPlayersSleeping = false;
        foreach (EntityPlayer player in _world.Entities.Players.Where(p => p.isSleeping()))
        {
            player.wakeUp(false, false, true);
        }

        ClearWeather();
    }

    public Vector3D<double> GetCloudColor(float partialTicks)
    {
        float timeOfDay = _world.getTime(partialTicks);

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
        float timeOfDay = _world.getTime(partialTicks);

        float sunIntensity = MathHelper.Cos(timeOfDay * (float)Math.PI * 2.0F) * 2.0F + 0.5F;
        sunIntensity = Math.Clamp(sunIntensity, 0.0F, 1.0F);

        int blockX = MathHelper.Floor(entity.x);
        int blockZ = MathHelper.Floor(entity.z);
        float temperature = (float)_world.getBiomeSource().GetTemperature(blockX, blockZ);
        int biomeSkyColorInt = _world.getBiomeSource().GetBiome(blockX, blockZ).GetSkyColorByTemp(temperature);

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

        if (LightningTicksLeft > 0)
        {
            float lightningFactor = LightningTicksLeft - partialTicks;
            if (lightningFactor > 1.0F)
            {
                lightningFactor = 1.0F;
            }

            lightningFactor *= 0.45F;

            red = red * (1.0F - lightningFactor) + 0.8F * lightningFactor;
            green = green * (1.0F - lightningFactor) + 0.8F * lightningFactor;
            blue = blue * (1.0F - lightningFactor) + 1.0F * lightningFactor;
        }

        return new Vector3D<double>(red, green, blue);
    }
}
