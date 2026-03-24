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
    ShrinkQuadratic, // baseScale * (1 - progress^2 * 0.5)  (Flame)
    ShrinkLinear,    // baseScale * (1 - progress^2)  (Lava)
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

public readonly struct ParticleTypeConfig
{
    public readonly PhysicsModel Physics;
    public readonly ScaleModel Scale;
    public readonly BrightnessModel Brightness;
    public readonly UVModel UV;
    public readonly byte FXLayer;
    public readonly float Friction;
    public readonly float GroundFriction;
    public readonly float GravityAccel;    // added to velocityY each tick
    public readonly bool NoClip;
    public readonly bool StalledSpread;    // velocityX/Z *= 1.1 when y==prevY
    public readonly bool AnimatesTexture;  // textureIndex = 7 - age*8/maxAge

    public ParticleTypeConfig(
        PhysicsModel physics, ScaleModel scale, BrightnessModel brightness, UVModel uv,
        byte fxLayer, float friction, float groundFriction, float gravityAccel,
        bool noClip, bool stalledSpread, bool animatesTexture)
    {
        Physics = physics;
        Scale = scale;
        Brightness = brightness;
        UV = uv;
        FXLayer = fxLayer;
        Friction = friction;
        GroundFriction = groundFriction;
        GravityAccel = gravityAccel;
        NoClip = noClip;
        StalledSpread = stalledSpread;
        AnimatesTexture = animatesTexture;
    }

    public static readonly ParticleTypeConfig[] Configs;

    static ParticleTypeConfig()
    {
        Configs = new ParticleTypeConfig[(int)ParticleType.Count];

        // Smoke: buoyant +0.004, friction 0.96, animated tex, stalled-spread, GrowToFull scale
        Configs[(int)ParticleType.Smoke] = new(
            PhysicsModel.Buoyant, ScaleModel.GrowToFull, BrightnessModel.WorldBased, UVModel.Standard16x16,
            0, 0.96f, 0.7f, 0.004f, false, true, true);

        // LargeSmoke: same as Smoke but spawned with scale multiplier 2.5
        Configs[(int)ParticleType.LargeSmoke] = new(
            PhysicsModel.Buoyant, ScaleModel.GrowToFull, BrightnessModel.WorldBased, UVModel.Standard16x16,
            0, 0.96f, 0.7f, 0.004f, false, true, true);

        // Flame: noClip, friction 0.96, ShrinkQuadratic scale, FadeToFull brightness
        Configs[(int)ParticleType.Flame] = new(
            PhysicsModel.NoClip, ScaleModel.ShrinkQuadratic, BrightnessModel.FadeFromFull, UVModel.Standard16x16,
            0, 0.96f, 0.7f, 0f, true, false, false);

        // Explode: buoyant +0.004, friction 0.9, animated tex, Constant scale
        Configs[(int)ParticleType.Explode] = new(
            PhysicsModel.Buoyant, ScaleModel.Constant, BrightnessModel.WorldBased, UVModel.Standard16x16,
            0, 0.9f, 0.7f, 0.004f, false, false, true);

        // Reddust: like Smoke - buoyant (no explicit upward, just friction 0.96), animated, stalled-spread, GrowToFull
        // Note: Reddust has no gravity (uses default EntityFX.tick pattern with no velocityY +=)
        // Actually looking at code: it calls move() with standard EntityFX gravity via base tick... no, it overrides tick()
        // Its tick: no gravity add, just move + friction 0.96 + stalled-spread + animated
        Configs[(int)ParticleType.Reddust] = new(
            PhysicsModel.Standard, ScaleModel.GrowToFull, BrightnessModel.WorldBased, UVModel.Standard16x16,
            0, 0.96f, 0.7f, 0f, false, true, true);

        // SnowShovel: gravity -0.03, friction 0.99, animated tex, GrowToFull
        Configs[(int)ParticleType.SnowShovel] = new(
            PhysicsModel.SnowDrift, ScaleModel.GrowToFull, BrightnessModel.WorldBased, UVModel.Standard16x16,
            0, 0.99f, 0.7f, -0.03f, false, false, true);

        // Heart: no gravity in tick (just move + friction), stalled-spread, friction 0.86, GrowToFull
        Configs[(int)ParticleType.Heart] = new(
            PhysicsModel.Standard, ScaleModel.GrowToFull, BrightnessModel.WorldBased, UVModel.Standard16x16,
            0, 0.86f, 0.7f, 0f, false, true, false);

        // Note: no gravity, friction 0.66, stalled-spread, GrowToFull
        Configs[(int)ParticleType.Note] = new(
            PhysicsModel.Standard, ScaleModel.GrowToFull, BrightnessModel.WorldBased, UVModel.Standard16x16,
            0, 0.66f, 0.7f, 0f, false, true, false);

        // Portal: parametric position, PortalEase scale, EaseToFull brightness, noClip
        Configs[(int)ParticleType.Portal] = new(
            PhysicsModel.Parametric, ScaleModel.PortalEase, BrightnessModel.EaseToFull, UVModel.Standard16x16,
            0, 0f, 0f, 0f, true, false, false);

        // Lava: gravity -0.03, friction 0.999, ShrinkLinear scale, AlwaysFull brightness, spawns smoke
        Configs[(int)ParticleType.Lava] = new(
            PhysicsModel.LavaDrop, ScaleModel.ShrinkLinear, BrightnessModel.AlwaysFull, UVModel.Standard16x16,
            0, 0.999f, 0.7f, -0.03f, false, false, false);

        // Rain: gravity via particleGravity (0.06), friction 0.98, Constant scale, dies on ground/fluid
        Configs[(int)ParticleType.Rain] = new(
            PhysicsModel.RainFall, ScaleModel.Constant, BrightnessModel.WorldBased, UVModel.Standard16x16,
            0, 0.98f, 0.7f, -0.06f, false, false, false);

        // Splash: like Rain but gravity 0.04
        Configs[(int)ParticleType.Splash] = new(
            PhysicsModel.RainFall, ScaleModel.Constant, BrightnessModel.WorldBased, UVModel.Standard16x16,
            0, 0.98f, 0.7f, -0.04f, false, false, false);

        // Bubble: upward drift +0.002, friction 0.85, dies outside water
        Configs[(int)ParticleType.Bubble] = new(
            PhysicsModel.BubbleRise, ScaleModel.Constant, BrightnessModel.WorldBased, UVModel.Standard16x16,
            0, 0.85f, 0.7f, 0.002f, false, false, false);

        // Digging: standard physics with block gravity, Constant scale, Jittered UV, layer 1
        Configs[(int)ParticleType.Digging] = new(
            PhysicsModel.Standard, ScaleModel.Constant, BrightnessModel.WorldBased, UVModel.Jittered4x4,
            1, 0.98f, 0.7f, 0f, false, false, false);

        // Slime: standard physics with snow block gravity, Constant scale, Jittered UV, layer 2
        Configs[(int)ParticleType.Slime] = new(
            PhysicsModel.Standard, ScaleModel.Constant, BrightnessModel.WorldBased, UVModel.Jittered4x4,
            2, 0.98f, 0.7f, 0f, false, false, false);
    }
}
