namespace BetaSharp.Client.Rendering.Particles;

public enum PhysicsModel : byte
{
    Standard,   // Uses per-particle gravity scale against a 0.04 engine constant
    Buoyant,    // Uses gravityAccel as upward force
    NoClip,     // No collision; simple velocity integration
    Parametric, // Position determined by time-based function; ignores friction
    BubbleRise, // Upward drift; dies when exiting water material
    RainFall,   // Dies on contact with solid or fluid surfaces
    LavaDrop,   // Hardcoded gravity -0.03; triggers smoke sub-particle spawning
    SnowDrift,  // Hardcoded gravity -0.03; high friction (0.99)
}

public enum ScaleModel : byte
{
    Constant,      // No scale change over lifetime
    GrowToFull,    // Rapid expansion to baseScale (32x speed): min(1, progress * 32)
    ShrinkHalf,    // Parabolic shrink ending at 50%: baseScale * (1 - progress^2 * 0.5)
    ShrinkSquared, // Quadratic shrink ending at zero: baseScale * (1 - progress^2)
    PortalEase,    // Inverted quadratic: 1 - (1 - progress)^2
}

public enum BrightnessModel : byte
{
    WorldBased,   // Sampled from world lighting at the current position
    AlwaysFull,   // Force maximum brightness (1.0)
    FadeFromFull, // Starts at 1.0, lerps toward sampled world light over lifetime
    EaseToFull,   // Starts at world brightness, quartic ease toward 1.0
}

public enum UVModel : byte
{
    Standard16x16, // Full 1/16 tile based on textureIndex
    Jittered4x4,   // Quarter-tile with randomized sub-tile offset
}

public readonly struct ParticleTypeConfig(
    PhysicsModel physics, ScaleModel scale, BrightnessModel brightness, UVModel uv,
    float friction, float groundFriction, float gravityAccel,
    bool stalledSpread, bool animatesTexture)
{
    public PhysicsModel Physics { get; } = physics;
    public ScaleModel Scale { get; } = scale;
    public BrightnessModel Brightness { get; } = brightness;
    public UVModel UV { get; } = uv;
    public float Friction { get; } = friction;
    public float GroundFriction { get; } = groundFriction;
    public float GravityAccel { get; } = gravityAccel;
    // Accelerates horizontal spread when vertical movement is blocked to prevent clumping
    public bool StalledSpread { get; } = stalledSpread;
    // Maps age to an 8-frame sequence in reverse order (7 to 0)
    public bool AnimatesTexture { get; } = animatesTexture;

    public static readonly ParticleTypeConfig[] Configs;

    static ParticleTypeConfig()
    {
        Configs = new ParticleTypeConfig[(int)ParticleType.Count];

        Configs[(int)ParticleType.Smoke] = new(
            PhysicsModel.Buoyant, ScaleModel.GrowToFull, BrightnessModel.WorldBased, UVModel.Standard16x16,
            0.96f, 0.7f, 0.004f, true, true);

        // Scale multiplier of 2.5 is applied during instantiation in the spawner logic
        Configs[(int)ParticleType.LargeSmoke] = new(
            PhysicsModel.Buoyant, ScaleModel.GrowToFull, BrightnessModel.WorldBased, UVModel.Standard16x16,
            0.96f, 0.7f, 0.004f, true, true);

        Configs[(int)ParticleType.Flame] = new(
            PhysicsModel.NoClip, ScaleModel.ShrinkHalf, BrightnessModel.FadeFromFull, UVModel.Standard16x16,
            0.96f, 0.7f, 0f, false, false);

        Configs[(int)ParticleType.Explode] = new(
            PhysicsModel.Buoyant, ScaleModel.Constant, BrightnessModel.WorldBased, UVModel.Standard16x16,
            0.9f, 0.7f, 0.004f, false, true);

        Configs[(int)ParticleType.Reddust] = new(
            PhysicsModel.Standard, ScaleModel.GrowToFull, BrightnessModel.WorldBased, UVModel.Standard16x16,
            0.96f, 0.7f, 0f, true, true);

        Configs[(int)ParticleType.SnowShovel] = new(
            PhysicsModel.SnowDrift, ScaleModel.GrowToFull, BrightnessModel.WorldBased, UVModel.Standard16x16,
            0.99f, 0.7f, -0.03f, false, true);

        Configs[(int)ParticleType.Heart] = new(
            PhysicsModel.Standard, ScaleModel.GrowToFull, BrightnessModel.WorldBased, UVModel.Standard16x16,
            0.86f, 0.7f, 0f, true, false);

        Configs[(int)ParticleType.Note] = new(
            PhysicsModel.Standard, ScaleModel.GrowToFull, BrightnessModel.WorldBased, UVModel.Standard16x16,
            0.66f, 0.7f, 0f, true, false);

        Configs[(int)ParticleType.Portal] = new(
            PhysicsModel.Parametric, ScaleModel.PortalEase, BrightnessModel.EaseToFull, UVModel.Standard16x16,
            0f, 0f, 0f, false, false);

        Configs[(int)ParticleType.Lava] = new(
            PhysicsModel.LavaDrop, ScaleModel.ShrinkSquared, BrightnessModel.AlwaysFull, UVModel.Standard16x16,
            0.999f, 0.7f, -0.03f, false, false);

        Configs[(int)ParticleType.Rain] = new(
            PhysicsModel.RainFall, ScaleModel.Constant, BrightnessModel.WorldBased, UVModel.Standard16x16,
            0.98f, 0.7f, -0.06f, false, false);

        Configs[(int)ParticleType.Splash] = new(
            PhysicsModel.RainFall, ScaleModel.Constant, BrightnessModel.WorldBased, UVModel.Standard16x16,
            0.98f, 0.7f, -0.04f, false, false);

        Configs[(int)ParticleType.Bubble] = new(
            PhysicsModel.BubbleRise, ScaleModel.Constant, BrightnessModel.WorldBased, UVModel.Standard16x16,
            0.85f, 0.7f, 0.002f, false, false);

        // Gravity for Digging and Slime is provided per-particle based on block/material type
        Configs[(int)ParticleType.Digging] = new(
            PhysicsModel.Standard, ScaleModel.Constant, BrightnessModel.WorldBased, UVModel.Jittered4x4,
            0.98f, 0.7f, 0f, false, false);

        Configs[(int)ParticleType.Slime] = new(
            PhysicsModel.Standard, ScaleModel.Constant, BrightnessModel.WorldBased, UVModel.Jittered4x4,
            0.98f, 0.7f, 0f, false, false);

#if DEBUG
        // Prevent any ParticleType from being left uninitialized for whatever reason.
        // Expect a debug build to crash if this situation arises (fail-fast).
        for (int i = 0; i < (int)ParticleType.Count; i++)
        {
            // These are zero-values hence why we're checking for them specifically
            if (Configs[i].Friction == 0 && Configs[i].Physics == PhysicsModel.Standard)
            {
                throw new Exception($"ParticleType {(ParticleType)i} missing static configuration.");
            }
        }
#endif
    }
}
