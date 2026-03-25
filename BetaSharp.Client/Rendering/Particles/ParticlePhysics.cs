using BetaSharp.Blocks;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Client.Rendering.Particles;

public static class ParticlePhysics
{
    public static void MoveWithCollision(
        ParticleBuffer buf, int i, IWorldContext world)
    {
        double nx = buf.X[i] + buf.VelX[i];
        double ny = buf.Y[i] + buf.VelY[i];
        double nz = buf.Z[i] + buf.VelZ[i];

        int bx = MathHelper.Floor(nx);
        int by = MathHelper.Floor(ny);
        int bz = MathHelper.Floor(nz);

        int blockId = world.Reader.GetBlockId(bx, by, bz);
        bool solid = blockId > 0 && Block.Blocks[blockId] != null &&
                     Block.Blocks[blockId].Material.BlocksMovement;

        if (!solid)
        {
            buf.X[i] = nx;
            buf.Y[i] = ny;
            buf.Z[i] = nz;
            buf.OnGround[i] = false;
        }
        else
        {
            // Try each axis independently using the already-computed target block coords
            // Y axis
            int yBlockId = world.Reader.GetBlockId(MathHelper.Floor(buf.X[i]), by, MathHelper.Floor(buf.Z[i]));
            bool ySolid = yBlockId > 0 && Block.Blocks[yBlockId] != null &&
                          Block.Blocks[yBlockId].Material.BlocksMovement;
            if (!ySolid)
            {
                buf.Y[i] += buf.VelY[i];
                buf.OnGround[i] = false;
            }
            else
            {
                buf.OnGround[i] = buf.VelY[i] < 0;
                buf.VelY[i] = 0;
            }

            // X axis
            int xBlockId = world.Reader.GetBlockId(bx, MathHelper.Floor(buf.Y[i]), MathHelper.Floor(buf.Z[i]));
            bool xSolid = xBlockId > 0 && Block.Blocks[xBlockId] != null &&
                          Block.Blocks[xBlockId].Material.BlocksMovement;
            if (!xSolid)
            {
                buf.X[i] += buf.VelX[i];
            }
            else
            {
                buf.VelX[i] = 0;
            }

            // Z axis
            int zBlockId = world.Reader.GetBlockId(MathHelper.Floor(buf.X[i]), MathHelper.Floor(buf.Y[i]), bz);
            bool zSolid = zBlockId > 0 && Block.Blocks[zBlockId] != null &&
                          Block.Blocks[zBlockId].Material.BlocksMovement;
            if (!zSolid)
            {
                buf.Z[i] += buf.VelZ[i];
            }
            else
            {
                buf.VelZ[i] = 0;
            }
        }
    }
}
