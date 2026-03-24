using BetaSharp.Blocks;
using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Client.Rendering.Particles;

public static class ParticleUpdater
{
    public struct DeferredSmoke
    {
        public double X, Y, Z, VelX, VelY, VelZ;
    }

    public static void Update(ParticleBuffer buf, IWorldContext world, List<DeferredSmoke> deferredSmoke)
    {
        int count = buf.Count;
        if (count == 0)
        {
            return;
        }

        // Save prev positions (memcpy)
        Array.Copy(buf.X, buf.PrevX, count);
        Array.Copy(buf.Y, buf.PrevY, count);
        Array.Copy(buf.Z, buf.PrevZ, count);

        for (int i = 0; i < count; i++)
        {
            // Age
            buf.Age[i]++;
            if (buf.Age[i] >= buf.MaxAge[i])
            {
                buf.Dead[i] = true;
                continue;
            }

            ref readonly ParticleTypeConfig config = ref ParticleTypeConfig.Configs[(int)buf.Type[i]];

            // Animated texture
            if (config.AnimatesTexture)
            {
                buf.TextureIndex[i] = 7 - buf.Age[i] * 8 / buf.MaxAge[i];
            }

            // Physics per model
            switch (config.Physics)
            {
                case PhysicsModel.Standard:
                    // Gravity uses per-particle gravity for Digging (block-specific) and Slime
                    buf.VelY[i] -= 0.04 * buf.Gravity[i];
                    ParticlePhysics.MoveWithCollision(buf, i, world);
                    break;

                case PhysicsModel.Buoyant:
                    buf.VelY[i] += config.GravityAccel;
                    ParticlePhysics.MoveWithCollision(buf, i, world);
                    break;

                case PhysicsModel.NoClip:
                    buf.X[i] += buf.VelX[i];
                    buf.Y[i] += buf.VelY[i];
                    buf.Z[i] += buf.VelZ[i];
                    break;

                case PhysicsModel.Parametric:
                    TickPortal(buf, i);
                    continue; // Portal handles its own position, skip friction

                case PhysicsModel.BubbleRise:
                    buf.VelY[i] += config.GravityAccel;
                    ParticlePhysics.MoveWithCollision(buf, i, world);
                    if (world.Reader.GetMaterial(
                            MathHelper.Floor(buf.X[i]),
                            MathHelper.Floor(buf.Y[i]),
                            MathHelper.Floor(buf.Z[i])) != Material.Water)
                    {
                        buf.Dead[i] = true;
                    }
                    break;

                case PhysicsModel.RainFall:
                    buf.VelY[i] += config.GravityAccel;
                    ParticlePhysics.MoveWithCollision(buf, i, world);
                    TickRain(buf, i, world);
                    break;

                case PhysicsModel.LavaDrop:
                    TickLava(buf, i, world, deferredSmoke);
                    break;

                case PhysicsModel.SnowDrift:
                    buf.VelY[i] += config.GravityAccel;
                    ParticlePhysics.MoveWithCollision(buf, i, world);
                    break;
            }

            // Stalled spread (smoke, reddust, heart, note)
            if (config.StalledSpread && buf.Y[i] == buf.PrevY[i])
            {
                buf.VelX[i] *= 1.1;
                buf.VelZ[i] *= 1.1;
            }

            // Friction
            buf.VelX[i] *= config.Friction;
            buf.VelY[i] *= config.Friction;
            buf.VelZ[i] *= config.Friction;

            // Ground friction
            if (buf.OnGround[i])
            {
                buf.VelX[i] *= config.GroundFriction;
                buf.VelZ[i] *= config.GroundFriction;
            }
        }

        // Swap-and-pop dead particles (iterate backwards)
        for (int i = buf.Count - 1; i >= 0; i--)
        {
            if (buf.Dead[i])
            {
                buf.SwapRemove(i);
            }
        }
    }

    private static void TickPortal(ParticleBuffer buf, int i)
    {
        float progress = (float)buf.Age[i] / buf.MaxAge[i];
        float factor = -progress + progress * progress * 2.0f;
        factor = 1.0f - factor;
        buf.X[i] = buf.SpawnX[i] + buf.VelX[i] * factor;
        buf.Y[i] = buf.SpawnY[i] + buf.VelY[i] * factor + (1.0f - progress);
        buf.Z[i] = buf.SpawnZ[i] + buf.VelZ[i] * factor;
    }

    private static void TickRain(ParticleBuffer buf, int i, IWorldContext world)
    {
        if (buf.OnGround[i])
        {
            if (Random.Shared.NextDouble() < 0.5)
            {
                buf.Dead[i] = true;
            }
        }

        int fx = MathHelper.Floor(buf.X[i]);
        int fy = MathHelper.Floor(buf.Y[i]);
        int fz = MathHelper.Floor(buf.Z[i]);
        Material mat = world.Reader.GetMaterial(fx, fy, fz);
        if (mat.IsFluid || mat.IsSolid)
        {
            double height = (float)(fy + 1) - BlockFluid.getFluidHeightFromMeta(
                world.Reader.GetBlockMeta(fx, fy, fz));
            if (buf.Y[i] < height)
            {
                buf.Dead[i] = true;
            }
        }
    }

    private static void TickLava(ParticleBuffer buf, int i, IWorldContext world, List<DeferredSmoke> deferred)
    {
        float lifeProgress = (float)buf.Age[i] / buf.MaxAge[i];
        if ((float)Random.Shared.NextDouble() > lifeProgress)
        {
            deferred.Add(new DeferredSmoke
            {
                X = buf.X[i], Y = buf.Y[i], Z = buf.Z[i],
                VelX = buf.VelX[i], VelY = buf.VelY[i], VelZ = buf.VelZ[i]
            });
        }

        buf.VelY[i] -= 0.03;
        ParticlePhysics.MoveWithCollision(buf, i, world);
    }
}
