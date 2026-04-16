using BetaSharp.Blocks;
using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Client.Rendering.Particles;

public static class ParticleUpdater
{
    // Queues new particles spawned during update to avoid mutating the active buffer while iterating
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

        // Localize arrays
        double[] x = buf.X;
        double[] y = buf.Y;
        double[] z = buf.Z;
        double[] px = buf.PrevX;
        double[] py = buf.PrevY;
        double[] pz = buf.PrevZ;
        double[] vx = buf.VelX;
        double[] vy = buf.VelY;
        double[] vz = buf.VelZ;
        short[] age = buf.Age;
        short[] maxAge = buf.MaxAge;

        Array.Copy(x, px, count);
        Array.Copy(y, py, count);
        Array.Copy(z, pz, count);

        Random rng = Random.Shared;

        for (int i = 0; i < count; i++)
        {
            if (++age[i] >= maxAge[i])
            {
                buf.Dead[i] = true;
                continue;
            }

            ref readonly ParticleTypeConfig config = ref ParticleTypeConfig.Configs[(int)buf.Type[i]];

            if (config.AnimatesTexture)
            {
                buf.TextureIndex[i] = 7 - (age[i] * 8 / maxAge[i]);
            }

            switch (config.Physics)
            {
                case PhysicsModel.Standard:
                    vy[i] -= 0.04 * buf.Gravity[i];
                    ParticlePhysics.MoveWithCollision(buf, i, world);
                    break;

                case PhysicsModel.Buoyant:
                    vy[i] += config.GravityAccel;
                    ParticlePhysics.MoveWithCollision(buf, i, world);
                    break;

                case PhysicsModel.NoClip:
                    x[i] += vx[i];
                    y[i] += vy[i];
                    z[i] += vz[i];
                    break;

                case PhysicsModel.Parametric:
                    TickPortal(buf, i);
                    continue;

                case PhysicsModel.BubbleRise:
                    vy[i] += config.GravityAccel;
                    ParticlePhysics.MoveWithCollision(buf, i, world);
                    if (world.Reader.GetMaterial(
                            MathHelper.Floor(x[i]),
                            MathHelper.Floor(y[i]),
                            MathHelper.Floor(z[i])) != Material.Water)
                        buf.Dead[i] = true;
                    break;

                case PhysicsModel.RainFall:
                    vy[i] += config.GravityAccel;
                    ParticlePhysics.MoveWithCollision(buf, i, world);
                    TickRain(buf, i, world, rng);
                    break;

                case PhysicsModel.LavaDrop:
                    TickLava(buf, i, world, deferredSmoke, config, rng);
                    break;

                case PhysicsModel.SnowDrift:
                    vy[i] += config.GravityAccel;
                    ParticlePhysics.MoveWithCollision(buf, i, world);
                    break;
            }

            if (config.StalledSpread && y[i] == py[i])
            {
                vx[i] *= 1.1;
                vz[i] *= 1.1;
            }

            vx[i] *= config.Friction;
            vy[i] *= config.Friction;
            vz[i] *= config.Friction;

            if (buf.OnGround[i])
            {
                vx[i] *= config.GroundFriction;
                vz[i] *= config.GroundFriction;
            }
        }

        for (int i = buf.Count - 1; i >= 0; i--)
        {
            if (buf.Dead[i]) buf.SwapRemove(i);
        }
    }

    private static void TickPortal(ParticleBuffer buf, int i)
    {
        float progress = (float)buf.Age[i] / buf.MaxAge[i];
        float factor = 1.0f - (-progress + progress * progress * 2.0f);

        buf.X[i] = buf.SpawnX[i] + buf.VelX[i] * factor;
        buf.Y[i] = buf.SpawnY[i] + buf.VelY[i] * factor + (1.0f - progress);
        buf.Z[i] = buf.SpawnZ[i] + buf.VelZ[i] * factor;
    }

    private static void TickRain(ParticleBuffer buf, int i, IWorldContext world, Random rng)
    {
        if (buf.OnGround[i])
        {
            if (rng.NextDouble() < 0.5) buf.Dead[i] = true;
            return; // No need to check fluid height if we are already on solid ground
        }

        int fx = MathHelper.Floor(buf.X[i]);
        int fy = MathHelper.Floor(buf.Y[i]);
        int fz = MathHelper.Floor(buf.Z[i]);

        Material mat = world.Reader.GetMaterial(fx, fy, fz);
        if (mat.IsFluid || mat.IsSolid)
        {
            double surfaceY = fy + 1 - BlockFluid.getFluidHeightFromMeta(world.Reader.GetBlockMeta(fx, fy, fz));
            if (buf.Y[i] < surfaceY) buf.Dead[i] = true;
        }
    }

    private static void TickLava(ParticleBuffer buf, int i, IWorldContext world, List<DeferredSmoke> deferred,
        in ParticleTypeConfig config, Random rng)
    {
        if (rng.NextDouble() > (float)buf.Age[i] / buf.MaxAge[i])
        {
            deferred.Add(new DeferredSmoke
            {
                X = buf.X[i],
                Y = buf.Y[i],
                Z = buf.Z[i],
                VelX = buf.VelX[i],
                VelY = buf.VelY[i],
                VelZ = buf.VelZ[i]
            });
        }

        buf.VelY[i] += config.GravityAccel;
        ParticlePhysics.MoveWithCollision(buf, i, world);
    }
}
