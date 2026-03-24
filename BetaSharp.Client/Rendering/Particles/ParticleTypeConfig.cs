namespace BetaSharp.Client.Rendering.Particles;

public enum PhysicsModel : byte
{
    Standard,   // gravity down via particleGravity, friction, ground friction
    Buoyant,    // gravity up (velocityY += upwardForce), friction, stalled-spread
    NoClip,     // no collision, just integrate velocity
    Parametric, // position from spawn + progress curve (Portal)
    BubbleRise, // upward drift, dies outside water
    RainFall,   // gravity via particleGravity, dies on ground/fluid, age counts down
    LavaDrop,   // gravity down -0.03, spawns smoke sub-particles
    SnowDrift,  // gravity down -0.03, animated texture, friction 0.99
}

public enum ScaleModel : byte
{
    Constant,        // no scale change
    GrowToFull,      // min(1, (age+partial)/maxAge * 32) * baseScale
    ShrinkHalf,    // baseScale * (1 - p^2 * 0.5), shrinks to ~50% (Flame)
    ShrinkSquared, // baseScale * (1 - p^2), shrinks to zero (Lava)
    PortalEase,      // 1 - (1-progress)^2  (Portal)
}

public enum BrightnessModel : byte
{
    WorldBased,   // getBrightnessAtEyes from world lighting
    AlwaysFull,   // 1.0
    FadeFromFull, // starts at 1.0, lerps toward world brightness (Flame)
    EaseToFull,   // starts at world brightness, quartic ease toward 1.0 (Portal)
}

public enum UVModel : byte
{
    Standard16x16, // full 1/16 tile from textureIndex
    Jittered4x4,   // quarter-tile with jitter offset (Digging, Slime)
}

public readonly struct ParticleTypeConfig(
    PhysicsModel physics, ScaleModel scale, BrightnessModel brightness, UVModel uv,
    float friction, float groundFriction, float gravityAccel,
    bool stalledSpread, bool animatesTexture)
{
    public readonly PhysicsModel Physics = physics;
    public readonly ScaleModel Scale = scale;
    public readonly BrightnessModel Brightness = brightness;
    public readonly UVModel UV = uv;
    public readonly float Friction = friction;
    public readonly float GroundFriction = groundFriction;
    public readonly float GravityAccel = gravityAccel;    // added to velocityY each tick
    public readonly bool StalledSpread = stalledSpread;    // velocityX/Z *= 1.1 when y==prevY
    public readonly bool AnimatesTexture = animatesTexture;  // textureIndex = 7 - age*8/maxAge

    public static readonly ParticleTypeConfig[] Configs;

    static ParticleTypeConfig()
    {
        Configs = new ParticleTypeConfig[(int)ParticleType.Count];

        // Smoke: buoyant +0.004, friction 0.96, animated tex, stalled-spread, GrowToFull scale
        Configs[(int)ParticleType.Smoke] = new(
            PhysicsModel.Buoyant, ScaleModel.GrowToFull, BrightnessModel.WorldBased, UVModel.Standard16x16,
            0.96f, 0.7f, 0.004f, true, true);

        // LargeSmoke: same as Smoke but spawned with scale multiplier 2.5
        Configs[(int)ParticleType.LargeSmoke] = new(
            PhysicsModel.Buoyant, ScaleModel.GrowToFull, BrightnessModel.WorldBased, UVModel.Standard16x16,
            0.96f, 0.7f, 0.004f, true, true);

        // Flame: noClip, friction 0.96, ShrinkHalf scale, FadeFromFull brightness
        Configs[(int)ParticleType.Flame] = new(
            PhysicsModel.NoClip, ScaleModel.ShrinkHalf, BrightnessModel.FadeFromFull, UVModel.Standard16x16,
            0.96f, 0.7f, 0f, false, false);

        // Explode: buoyant +0.004, friction 0.9, animated tex, Constant scale
        Configs[(int)ParticleType.Explode] = new(
            PhysicsModel.Buoyant, ScaleModel.Constant, BrightnessModel.WorldBased, UVModel.Standard16x16,
            0.9f, 0.7f, 0.004f, false, true);

        // Reddust: standard physics, no gravity, friction 0.96, animated, stalled-spread, GrowToFull
        Configs[(int)ParticleType.Reddust] = new(
            PhysicsModel.Standard, ScaleModel.GrowToFull, BrightnessModel.WorldBased, UVModel.Standard16x16,
            0.96f, 0.7f, 0f, true, true);

        // SnowShovel: gravity -0.03, friction 0.99, animated tex, GrowToFull
        Configs[(int)ParticleType.SnowShovel] = new(
            PhysicsModel.SnowDrift, ScaleModel.GrowToFull, BrightnessModel.WorldBased, UVModel.Standard16x16,
            0.99f, 0.7f, -0.03f, false, true);

        // Heart: no gravity, stalled-spread, friction 0.86, GrowToFull
        Configs[(int)ParticleType.Heart] = new(
            PhysicsModel.Standard, ScaleModel.GrowToFull, BrightnessModel.WorldBased, UVModel.Standard16x16,
            0.86f, 0.7f, 0f, true, false);

        // Note: no gravity, friction 0.66, stalled-spread, GrowToFull
        Configs[(int)ParticleType.Note] = new(
            PhysicsModel.Standard, ScaleModel.GrowToFull, BrightnessModel.WorldBased, UVModel.Standard16x16,
            0.66f, 0.7f, 0f, true, false);

        // Portal: parametric position, PortalEase scale, EaseToFull brightness
        Configs[(int)ParticleType.Portal] = new(
            PhysicsModel.Parametric, ScaleModel.PortalEase, BrightnessModel.EaseToFull, UVModel.Standard16x16,
            0f, 0f, 0f, false, false);

        // Lava: gravity -0.03, friction 0.999, ShrinkSquared scale, AlwaysFull brightness, spawns smoke
        Configs[(int)ParticleType.Lava] = new(
            PhysicsModel.LavaDrop, ScaleModel.ShrinkSquared, BrightnessModel.AlwaysFull, UVModel.Standard16x16,
            0.999f, 0.7f, -0.03f, false, false);

        // Rain: gravity -0.06, friction 0.98, Constant scale, dies on ground/fluid
        Configs[(int)ParticleType.Rain] = new(
            PhysicsModel.RainFall, ScaleModel.Constant, BrightnessModel.WorldBased, UVModel.Standard16x16,
            0.98f, 0.7f, -0.06f, false, false);

        // Splash: like Rain but gravity -0.04
        Configs[(int)ParticleType.Splash] = new(
            PhysicsModel.RainFall, ScaleModel.Constant, BrightnessModel.WorldBased, UVModel.Standard16x16,
            0.98f, 0.7f, -0.04f, false, false);

        // Bubble: upward drift +0.002, friction 0.85, dies outside water
        Configs[(int)ParticleType.Bubble] = new(
            PhysicsModel.BubbleRise, ScaleModel.Constant, BrightnessModel.WorldBased, UVModel.Standard16x16,
            0.85f, 0.7f, 0.002f, false, false);

        // Digging: standard physics with block gravity, Constant scale, Jittered UV
        Configs[(int)ParticleType.Digging] = new(
            PhysicsModel.Standard, ScaleModel.Constant, BrightnessModel.WorldBased, UVModel.Jittered4x4,
            0.98f, 0.7f, 0f, false, false);

        // Slime: standard physics with snow block gravity, Constant scale, Jittered UV
        Configs[(int)ParticleType.Slime] = new(
            PhysicsModel.Standard, ScaleModel.Constant, BrightnessModel.WorldBased, UVModel.Jittered4x4,
            0.98f, 0.7f, 0f, false, false);
    }
}
