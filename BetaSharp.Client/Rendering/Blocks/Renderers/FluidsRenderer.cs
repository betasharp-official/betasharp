using BetaSharp.Blocks;
using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering.Blocks.Renderers;

public class FluidsRenderer : IBlockRenderer
{
    public bool Draw(Block block, in BlockPos pos, ref BlockRenderContext ctx)
    {
        // Base fluid color tint (e.g., biome water color)
        int colorMultiplier = block.GetColorMultiplier(ctx.BlockReader, pos.x, pos.y, pos.z);
        float tintR = (colorMultiplier >> 16 & 255) / 255.0F;
        float tintG = (colorMultiplier >> 8 & 255) / 255.0F;
        float tintB = (colorMultiplier & 255) / 255.0F;

        // Determine which faces are actually visible to the player
        bool isTopVisible = block.IsSideVisible(ctx.BlockReader, pos.x, pos.y + 1, pos.z, Side.Up);
        bool isBottomVisible = block.IsSideVisible(ctx.BlockReader, pos.x, pos.y - 1, pos.z, 0);
        bool[] sideVisible =
        [
            block.IsSideVisible(ctx.BlockReader, pos.x, pos.y, pos.z - 1, Side.North),
            block.IsSideVisible(ctx.BlockReader, pos.x, pos.y, pos.z + 1, Side.South),
            block.IsSideVisible(ctx.BlockReader, pos.x - 1, pos.y, pos.z, Side.West),
            block.IsSideVisible(ctx.BlockReader, pos.x + 1, pos.y, pos.z, Side.East)
        ];

        // Fast exit if completely surrounded
        if (!isTopVisible && !isBottomVisible && !sideVisible[0] && !sideVisible[1] && !sideVisible[2] &&
            !sideVisible[3])
        {
            return false;
        }

        bool hasRendered = false;

        // Directional shading
        const float lightBottom = 0.5F;
        const float lightTop = 1.0F;
        const float lightZ = 0.8F; // North/South
        const float lightX = 0.6F; // East/West

        Material material = block.Material;
        int meta = ctx.BlockReader.GetBlockMeta(pos.x, pos.y, pos.z);

        // Calculate the height of the fluid at each of the 4 corners of this block
        float heightNw = GetFluidVertexHeight(ref ctx, pos.x, pos.y, pos.z, material);
        float heightSw = GetFluidVertexHeight(ref ctx, pos.x, pos.y, pos.z + 1, material);
        float heightSe = GetFluidVertexHeight(ref ctx, pos.x + 1, pos.y, pos.z + 1, material);
        float heightNe = GetFluidVertexHeight(ref ctx, pos.x + 1, pos.y, pos.z, material);

        // TOP FACE (Flowing Surface)
        if (ctx.RenderAllFaces || isTopVisible)
        {
            hasRendered = true;
            int textureId = block.GetTexture(Side.Up, meta);
            float flowAngle = (float)BlockFluid.GetFlowingAngle(ctx.BlockReader, pos.x, pos.y, pos.z, material);

            // If flowing, switch to the flowing texture variant
            if (flowAngle > -999.0F)
            {
                textureId = block.GetTexture(Side.North, meta);
            }

            int texU = (textureId & 15) << 4;
            int texV = textureId & 240;
            float centerU = (texU + 8.0f) / 256.0f;
            float centerV = (texV + 8.0f) / 256.0f;

            // If completely still, use standard flat UVs
            if (flowAngle < -999.0F)
            {
                flowAngle = 0.0F;
            }
            else
            {
                // Shift UV center for flowing animation
                centerU = (texU + 16) / 256.0F;
                centerV = (texV + 16) / 256.0F;
            }

            // Calculate rotational offsets for the UVs to make the texture flow in the correct direction
            float sinAngle = MathHelper.Sin(flowAngle) * 8.0F / 256.0F;
            float cosAngle = MathHelper.Cos(flowAngle) * 8.0F / 256.0F;

            float luminance = block.GetLuminance(ctx.Lighting, pos.x, pos.y, pos.z);
            ctx.Tess.setColorOpaque_F(lightTop * luminance * tintR, lightTop * luminance * tintG,
                lightTop * luminance * tintB);

            // Draw top face with dynamic heights and rotated UVs
            ctx.Tess.addVertexWithUV(pos.x + 0, pos.y + heightNw, pos.z + 0, centerU - cosAngle - sinAngle,
                centerV - cosAngle + sinAngle);
            ctx.Tess.addVertexWithUV(pos.x + 0, pos.y + heightSw, pos.z + 1, centerU - cosAngle + sinAngle,
                centerV + cosAngle + sinAngle);
            ctx.Tess.addVertexWithUV(pos.x + 1, pos.y + heightSe, pos.z + 1, centerU + cosAngle + sinAngle,
                centerV + cosAngle - sinAngle);
            ctx.Tess.addVertexWithUV(pos.x + 1, pos.y + heightNe, pos.z + 0, centerU + cosAngle - sinAngle,
                centerV - cosAngle - sinAngle);
        }

        // BOTTOM FACE
        if (ctx.RenderAllFaces || isBottomVisible)
        {
            float luminance = block.GetLuminance(ctx.Lighting, pos.x, pos.y - 1, pos.z);
            ctx.Tess.setColorOpaque_F(lightBottom * luminance, lightBottom * luminance, lightBottom * luminance);

            // Fluids don't use AO, so pass dummy colors
            FaceColors dummyColors = new FaceColors();
            int tex = block.GetTexture(0);

            // Note: Fluids don't override bounds for the bottom face, so we just pass the default context
            ctx.DrawBottomFace(block, new Vec3D(pos.x, pos.y, pos.z), dummyColors, tex);
            hasRendered = true;
        }

        // SIDE FACES (North, South, West, East)
        for (int side = 0; side < 4; ++side)
        {
            int adjX = pos.x;
            int adjZ = pos.z;

            if (side == 0) adjZ = pos.z - 1; // North
            if (side == 1) adjZ = pos.z + 1; // South
            if (side == 2) adjX = pos.x - 1; // West
            if (side == 3) adjX = pos.x + 1; // East

            int textureId = block.GetTexture((side + 2).ToSide(), meta);
            int texU = (textureId & 15) << 4;
            int texV = textureId & 240;

            if (ctx.RenderAllFaces || sideVisible[side])
            {
                float h1, h2; // Top corner heights for this face
                float x1, x2; // X coordinates
                float z1, z2; // Z coordinates

                if (side == 0) // North
                {
                    h1 = heightNw;
                    h2 = heightNe;
                    x1 = pos.x;
                    x2 = pos.x + 1;
                    z1 = pos.z;
                    z2 = pos.z;
                }
                else if (side == 1) // South
                {
                    h1 = heightSe;
                    h2 = heightSw;
                    x1 = pos.x + 1;
                    x2 = pos.x;
                    z1 = pos.z + 1;
                    z2 = pos.z + 1;
                }
                else if (side == 2) // West
                {
                    h1 = heightSw;
                    h2 = heightNw;
                    x1 = pos.x;
                    x2 = pos.x;
                    z1 = pos.z + 1;
                    z2 = pos.z;
                }
                else // East
                {
                    h1 = heightNe;
                    h2 = heightSe;
                    x1 = pos.x + 1;
                    x2 = pos.x + 1;
                    z1 = pos.z;
                    z2 = pos.z + 1;
                }

                hasRendered = true;

                // Crop the UVs vertically so the texture doesn't stretch on short flowing water blocks
                float minU = (texU + 0) / 256.0F;
                float maxU = (texU + 16 - 0.01f) / 256.0f;
                float minV1 = (texV + (1.0F - h1) * 16.0F) / 256.0F; // UV height match for corner 1
                float minV2 = (texV + (1.0F - h2) * 16.0F) / 256.0F; // UV height match for corner 2
                float maxV = (texV + 16 - 0.01f) / 256.0f;

                float luminance = block.GetLuminance(ctx.Lighting, adjX, pos.y, adjZ);
                float shadow = (side < 2) ? lightZ : lightX;
                luminance *= shadow;

                ctx.Tess.setColorOpaque_F(lightTop * luminance * tintR, lightTop * luminance * tintG,
                    lightTop * luminance * tintB);

                // Draw the side face matching the sloped top corners
                ctx.Tess.addVertexWithUV(x1, pos.y + h1, z1, minU, minV1);
                ctx.Tess.addVertexWithUV(x2, pos.y + h2, z2, maxU, minV2);
                ctx.Tess.addVertexWithUV(x2, pos.y + 0, z2, maxU, maxV);
                ctx.Tess.addVertexWithUV(x1, pos.y + 0, z1, minU, maxV);
            }
        }

        return hasRendered;
    }

    // Passed ctx.World explicitly into this helper method
    private float GetFluidVertexHeight(ref BlockRenderContext ctx, int x, int y, int z, Material material)
    {
        int totalWeight = 0;
        float totalDepth = 0.0F;

        // Iterate through the 2x2 grid sharing this vertex: (x, z), (x-1, z), (x, z-1), (x-1, z-1)
        for (int i = 0; i < 4; ++i)
        {
            int checkX = x - (i & 1);
            int checkZ = z - (i >> 1 & 1);

            // If there is fluid directly above any of the 4 blocks, the corner must be completely full (height 1.0)
            if (ctx.BlockReader.GetMaterial(checkX, y + 1, checkZ) == material)
            {
                return 1.0F;
            }

            Material neighborMaterial = ctx.BlockReader.GetMaterial(checkX, y, checkZ);

            if (neighborMaterial != material)
            {
                // If the neighbor is air or a non-solid block, it contributes "full depth" (pulls the water level down to 0)
                if (!neighborMaterial.IsSolid)
                {
                    ++totalDepth;
                    ++totalWeight;
                }
            }
            else
            {
                int neighborMeta = ctx.BlockReader.GetBlockMeta(checkX, y, checkZ);
                float fluidDepth = BlockFluid.GetFluidHeightFromMeta(neighborMeta);

                // Meta >= 8 (falling fluid) or Meta == 0 (source block)
                if (neighborMeta >= 8 || neighborMeta == 0)
                {
                    // Source blocks and falling columns get 10x the "weight" in the average,
                    // heavily anchoring the fluid corner to their height.
                    totalDepth += fluidDepth * 10.0F;
                    totalWeight += 10;
                }

                totalDepth += fluidDepth;
                ++totalWeight;
            }
        }

        // Depth is measured from the top down. Subtract from 1.0 to get height from bottom up.
        return 1.0F - totalDepth / totalWeight;
    }
}
