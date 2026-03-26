using System.Runtime.CompilerServices;
using BetaSharp.Blocks;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Client.Rendering.Blocks;

public ref struct BlockRenderContext
{
    public readonly IBlockReader BlockReader;
    public readonly ILightProvider Lighting;
    public readonly Tessellator Tess;

    public int OverrideTexture;
    public readonly bool RenderAllFaces;
    public bool FlipTexture;
    public Box? OverrideBounds;
    public bool EnableAo = true;
    public int AoBlendMode = 0;

    // UV Rotations
    public int UvRotateTop;
    public int UvRotateBottom;
    public int UvRotateNorth;
    public int UvRotateSouth;
    public int UvRotateEast;
    public int UvRotateWest;

    // Custom flag for Pistons (Expanded/Short arm)
    public bool CustomFlag;

    public BlockRenderContext(
        IBlockReader blockReader, Tessellator tess,
        ILightProvider lighting,
        int overrideTexture = -1, bool renderAllFaces = false,
        bool flipTexture = false, Box? bounds = null,
        int uvTop = 0, int uvBottom = 0,
        int uvNorth = 0, int uvSouth = 0,
        int uvEast = 0, int uvWest = 0,
        bool customFlag = false, bool enableAo = true,
        int aoBlendMode = 0)
    {
        BlockReader = blockReader;
        Tess = tess;
        Lighting =  lighting;

        OverrideTexture = overrideTexture;
        RenderAllFaces = renderAllFaces;
        FlipTexture = flipTexture;
        OverrideBounds = bounds;

        UvRotateTop = uvTop;
        UvRotateBottom = uvBottom;
        UvRotateNorth = uvNorth;
        UvRotateSouth = uvSouth;
        UvRotateEast = uvEast;
        UvRotateWest = uvWest;

        AoBlendMode = aoBlendMode;
        EnableAo = enableAo;

        CustomFlag = customFlag;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Clamp(float value) => value < 0f ? 0f : (value > 1f ? 1f : value);

    internal readonly void DrawBottomFace(Block block, in Vec3D pos, in FaceColors colors, int textureId, bool flipped = false)
    {
        Box bb = OverrideBounds ?? block.BoundingBox;
        int texU = (textureId & 15) << 4;
        int texV = textureId & 240;

        float bbMinX = (float)bb.MinX;
        float bbMaxX = (float)bb.MaxX;
        float bbMinZ = (float)bb.MinZ;
        float bbMaxZ = (float)bb.MaxZ;

        float bMinX = Clamp(bbMinX);
        float bMaxX = Clamp(bbMaxX);
        float bMinZ = Clamp(bbMinZ);
        float bMaxZ = Clamp(bbMaxZ);

        CalculateUv(bMinX, bMaxZ, UvRotateBottom, texU, texV, out float u0, out float v0);
        CalculateUv(bMinX, bMinZ, UvRotateBottom, texU, texV, out float u1, out float v1);
        CalculateUv(bMaxX, bMinZ, UvRotateBottom, texU, texV, out float u2, out float v2);
        CalculateUv(bMaxX, bMaxZ, UvRotateBottom, texU, texV, out float u3, out float v3);

        float pX = (float)pos.x;
        float pY = (float)pos.y;
        float pZ = (float)pos.z;

        float minX = pX + bbMinX;
        float maxX = pX + bbMaxX;
        float minY = pY + (float)bb.MinY;
        float minZ = pZ + bbMinZ;
        float maxZ = pZ + bbMaxZ;

        if (EnableAo)
        {
            if (flipped)
            {
                Tess.setColorOpaque_F(colors.RedBottomLeft, colors.GreenBottomLeft, colors.BlueBottomLeft);
                Tess.addVertexWithUV(minX, minY, minZ, u1, v1);
                Tess.setColorOpaque_F(colors.RedBottomRight, colors.GreenBottomRight, colors.BlueBottomRight);
                Tess.addVertexWithUV(maxX, minY, minZ, u2, v2);
                Tess.setColorOpaque_F(colors.RedTopRight, colors.GreenTopRight, colors.BlueTopRight);
                Tess.addVertexWithUV(maxX, minY, maxZ, u3, v3);
                Tess.setColorOpaque_F(colors.RedTopLeft, colors.GreenTopLeft, colors.BlueTopLeft);
                Tess.addVertexWithUV(minX, minY, maxZ, u0, v0);
            }
            else
            {
                Tess.setColorOpaque_F(colors.RedTopLeft, colors.GreenTopLeft, colors.BlueTopLeft);
                Tess.addVertexWithUV(minX, minY, maxZ, u0, v0);
                Tess.setColorOpaque_F(colors.RedBottomLeft, colors.GreenBottomLeft, colors.BlueBottomLeft);
                Tess.addVertexWithUV(minX, minY, minZ, u1, v1);
                Tess.setColorOpaque_F(colors.RedBottomRight, colors.GreenBottomRight, colors.BlueBottomRight);
                Tess.addVertexWithUV(maxX, minY, minZ, u2, v2);
                Tess.setColorOpaque_F(colors.RedTopRight, colors.GreenTopRight, colors.BlueTopRight);
                Tess.addVertexWithUV(maxX, minY, maxZ, u3, v3);
            }
        }
        else
        {
            Tess.addVertexWithUV(minX, minY, maxZ, u0, v0);
            Tess.addVertexWithUV(minX, minY, minZ, u1, v1);
            Tess.addVertexWithUV(maxX, minY, minZ, u2, v2);
            Tess.addVertexWithUV(maxX, minY, maxZ, u3, v3);
        }
    }

    internal readonly void DrawTopFace(Block block, in Vec3D pos, in FaceColors colors, int textureId, bool flipped = false)
    {
        Box bb = OverrideBounds ?? block.BoundingBox;
        int texU = (textureId & 15) << 4;
        int texV = textureId & 240;

        float bbMinX = (float)bb.MinX;
        float bbMaxX = (float)bb.MaxX;
        float bbMinZ = (float)bb.MinZ;
        float bbMaxZ = (float)bb.MaxZ;

        float bMinX = Clamp(bbMinX);
        float bMaxX = Clamp(bbMaxX);
        float bMinZ = Clamp(bbMinZ);
        float bMaxZ = Clamp(bbMaxZ);

        CalculateUv(bMaxX, bMaxZ, UvRotateTop, texU, texV, out float u0, out float v0);
        CalculateUv(bMaxX, bMinZ, UvRotateTop, texU, texV, out float u1, out float v1);
        CalculateUv(bMinX, bMinZ, UvRotateTop, texU, texV, out float u2, out float v2);
        CalculateUv(bMinX, bMaxZ, UvRotateTop, texU, texV, out float u3, out float v3);

        float pX = (float)pos.x;
        float pY = (float)pos.y;
        float pZ = (float)pos.z;

        float minX = pX + bbMinX;
        float maxX = pX + bbMaxX;
        float maxY = pY + (float)bb.MaxY;
        float minZ = pZ + bbMinZ;
        float maxZ = pZ + bbMaxZ;

        if (EnableAo)
        {
            if (flipped)
            {
                Tess.setColorOpaque_F(colors.RedBottomLeft, colors.GreenBottomLeft, colors.BlueBottomLeft);
                Tess.addVertexWithUV(maxX, maxY, minZ, u1, v1);
                Tess.setColorOpaque_F(colors.RedBottomRight, colors.GreenBottomRight, colors.BlueBottomRight);
                Tess.addVertexWithUV(minX, maxY, minZ, u2, v2);
                Tess.setColorOpaque_F(colors.RedTopRight, colors.GreenTopRight, colors.BlueTopRight);
                Tess.addVertexWithUV(minX, maxY, maxZ, u3, v3);
                Tess.setColorOpaque_F(colors.RedTopLeft, colors.GreenTopLeft, colors.BlueTopLeft);
                Tess.addVertexWithUV(maxX, maxY, maxZ, u0, v0);
            }
            else
            {
                Tess.setColorOpaque_F(colors.RedTopLeft, colors.GreenTopLeft, colors.BlueTopLeft);
                Tess.addVertexWithUV(maxX, maxY, maxZ, u0, v0);
                Tess.setColorOpaque_F(colors.RedBottomLeft, colors.GreenBottomLeft, colors.BlueBottomLeft);
                Tess.addVertexWithUV(maxX, maxY, minZ, u1, v1);
                Tess.setColorOpaque_F(colors.RedBottomRight, colors.GreenBottomRight, colors.BlueBottomRight);
                Tess.addVertexWithUV(minX, maxY, minZ, u2, v2);
                Tess.setColorOpaque_F(colors.RedTopRight, colors.GreenTopRight, colors.BlueTopRight);
                Tess.addVertexWithUV(minX, maxY, maxZ, u3, v3);
            }
        }
        else
        {
            Tess.addVertexWithUV(maxX, maxY, maxZ, u0, v0);
            Tess.addVertexWithUV(maxX, maxY, minZ, u1, v1);
            Tess.addVertexWithUV(minX, maxY, minZ, u2, v2);
            Tess.addVertexWithUV(minX, maxY, maxZ, u3, v3);
        }
    }

    internal readonly void DrawNorthFace(Block block, in Vec3D pos, in FaceColors colors, int textureId, bool flipped = false)
    {
        Box bb = OverrideBounds ?? block.BoundingBox;
        int texU = (textureId & 15) << 4;
        int texV = textureId & 240;

        float bbMinY = (float)bb.MinY;
        float bbMaxY = (float)bb.MaxY;
        float bbMinZ = (float)bb.MinZ;
        float bbMaxZ = (float)bb.MaxZ;

        CalculateUv(bbMinZ, 1.0f - bbMaxY, UvRotateNorth, texU, texV, out float uTl, out float vTl);
        CalculateUv(bbMinZ, 1.0f - bbMinY, UvRotateNorth, texU, texV, out float uBl, out float vBl);
        CalculateUv(bbMaxZ, 1.0f - bbMinY, UvRotateNorth, texU, texV, out float uBr, out float vBr);
        CalculateUv(bbMaxZ, 1.0f - bbMaxY, UvRotateNorth, texU, texV, out float uTr, out float vTr);

        float pX = (float)pos.x;
        float pY = (float)pos.y;
        float pZ = (float)pos.z;

        float minX = pX + (float)bb.MinX;
        float minY = pY + bbMinY;
        float maxY = pY + bbMaxY;
        float minZ = pZ + bbMinZ;
        float maxZ = pZ + bbMaxZ;

        if (EnableAo)
        {
            if (flipped)
            {
                Tess.setColorOpaque_F(colors.RedBottomLeft, colors.GreenBottomLeft, colors.BlueBottomLeft);
                Tess.addVertexWithUV(minX, minY, minZ, uBl, vBl);
                Tess.setColorOpaque_F(colors.RedBottomRight, colors.GreenBottomRight, colors.BlueBottomRight);
                Tess.addVertexWithUV(minX, minY, maxZ, uBr, vBr);
                Tess.setColorOpaque_F(colors.RedTopRight, colors.GreenTopRight, colors.BlueTopRight);
                Tess.addVertexWithUV(minX, maxY, maxZ, uTr, vTr);
                Tess.setColorOpaque_F(colors.RedTopLeft, colors.GreenTopLeft, colors.BlueTopLeft);
                Tess.addVertexWithUV(minX, maxY, minZ, uTl, vTl);
            }
            else
            {
                Tess.setColorOpaque_F(colors.RedTopLeft, colors.GreenTopLeft, colors.BlueTopLeft);
                Tess.addVertexWithUV(minX, maxY, minZ, uTl, vTl);
                Tess.setColorOpaque_F(colors.RedBottomLeft, colors.GreenBottomLeft, colors.BlueBottomLeft);
                Tess.addVertexWithUV(minX, minY, minZ, uBl, vBl);
                Tess.setColorOpaque_F(colors.RedBottomRight, colors.GreenBottomRight, colors.BlueBottomRight);
                Tess.addVertexWithUV(minX, minY, maxZ, uBr, vBr);
                Tess.setColorOpaque_F(colors.RedTopRight, colors.GreenTopRight, colors.BlueTopRight);
                Tess.addVertexWithUV(minX, maxY, maxZ, uTr, vTr);
            }
        }
        else
        {
            Tess.addVertexWithUV(minX, maxY, minZ, uTl, vTl);
            Tess.addVertexWithUV(minX, minY, minZ, uBl, vBl);
            Tess.addVertexWithUV(minX, minY, maxZ, uBr, vBr);
            Tess.addVertexWithUV(minX, maxY, maxZ, uTr, vTr);
        }
    }

    internal readonly void DrawSouthFace(Block block, in Vec3D pos, in FaceColors colors, int textureId, bool flipped = false)
    {
        Box bb = OverrideBounds ?? block.BoundingBox;
        int texU = (textureId & 15) << 4;
        int texV = textureId & 240;

        float bbMinY = (float)bb.MinY;
        float bbMaxY = (float)bb.MaxY;
        float bbMinZ = (float)bb.MinZ;
        float bbMaxZ = (float)bb.MaxZ;

        float bMinY = Clamp(bbMinY);
        float bMaxY = Clamp(bbMaxY);
        float bMinZ = Clamp(bbMinZ);
        float bMaxZ = Clamp(bbMaxZ);

        CalculateUv(1.0f - bMaxZ, 1.0f - bMaxY, UvRotateSouth, texU, texV, out float uTl, out float vTl);
        CalculateUv(1.0f - bMaxZ, 1.0f - bMinY, UvRotateSouth, texU, texV, out float uBl, out float vBl);
        CalculateUv(1.0f - bMinZ, 1.0f - bMinY, UvRotateSouth, texU, texV, out float uBr, out float vBr);
        CalculateUv(1.0f - bMinZ, 1.0f - bMaxY, UvRotateSouth, texU, texV, out float uTr, out float vTr);

        float pX = (float)pos.x;
        float pY = (float)pos.y;
        float pZ = (float)pos.z;

        float posX = pX + (float)bb.MaxX;
        float minY = pY + bbMinY;
        float maxY = pY + bbMaxY;
        float minZ = pZ + bbMinZ;
        float maxZ = pZ + bbMaxZ;

        if (EnableAo)
        {
            if (flipped)
            {
                Tess.setColorOpaque_F(colors.RedBottomLeft, colors.GreenBottomLeft, colors.BlueBottomLeft);
                Tess.addVertexWithUV(posX, minY, maxZ, uBl, vBl);
                Tess.setColorOpaque_F(colors.RedBottomRight, colors.GreenBottomRight, colors.BlueBottomRight);
                Tess.addVertexWithUV(posX, minY, minZ, uBr, vBr);
                Tess.setColorOpaque_F(colors.RedTopRight, colors.GreenTopRight, colors.BlueTopRight);
                Tess.addVertexWithUV(posX, maxY, minZ, uTr, vTr);
                Tess.setColorOpaque_F(colors.RedTopLeft, colors.GreenTopLeft, colors.BlueTopLeft);
                Tess.addVertexWithUV(posX, maxY, maxZ, uTl, vTl);
            }
            else
            {
                Tess.setColorOpaque_F(colors.RedTopLeft, colors.GreenTopLeft, colors.BlueTopLeft);
                Tess.addVertexWithUV(posX, maxY, maxZ, uTl, vTl);
                Tess.setColorOpaque_F(colors.RedBottomLeft, colors.GreenBottomLeft, colors.BlueBottomLeft);
                Tess.addVertexWithUV(posX, minY, maxZ, uBl, vBl);
                Tess.setColorOpaque_F(colors.RedBottomRight, colors.GreenBottomRight, colors.BlueBottomRight);
                Tess.addVertexWithUV(posX, minY, minZ, uBr, vBr);
                Tess.setColorOpaque_F(colors.RedTopRight, colors.GreenTopRight, colors.BlueTopRight);
                Tess.addVertexWithUV(posX, maxY, minZ, uTr, vTr);
            }
        }
        else
        {
            Tess.addVertexWithUV(posX, maxY, maxZ, uTl, vTl);
            Tess.addVertexWithUV(posX, minY, maxZ, uBl, vBl);
            Tess.addVertexWithUV(posX, minY, minZ, uBr, vBr);
            Tess.addVertexWithUV(posX, maxY, minZ, uTr, vTr);
        }
    }

    internal readonly void DrawEastFace(Block block, in Vec3D pos, in FaceColors colors, int textureId, bool flipped = false)
    {
        Box bb = OverrideBounds ?? block.BoundingBox;
        int texU = (textureId & 15) << 4;
        int texV = textureId & 240;

        float bbMinX = (float)bb.MinX;
        float bbMaxX = (float)bb.MaxX;
        float bbMinY = (float)bb.MinY;
        float bbMaxY = (float)bb.MaxY;

        float bMinX = Clamp(bbMinX);
        float bMaxX = Clamp(bbMaxX);
        float bMinY = Clamp(bbMinY);
        float bMaxY = Clamp(bbMaxY);

        CalculateUv(1.0f - bMaxX, 1.0f - bMaxY, UvRotateEast, texU, texV, out float uTl, out float vTl);
        CalculateUv(1.0f - bMaxX, 1.0f - bMinY, UvRotateEast, texU, texV, out float uBl, out float vBl);
        CalculateUv(1.0f - bMinX, 1.0f - bMinY, UvRotateEast, texU, texV, out float uBr, out float vBr);
        CalculateUv(1.0f - bMinX, 1.0f - bMaxY, UvRotateEast, texU, texV, out float uTr, out float vTr);

        float pX = (float)pos.x;
        float pY = (float)pos.y;
        float pZ = (float)pos.z;

        float minX = pX + bbMinX;
        float maxX = pX + bbMaxX;
        float minY = pY + bbMinY;
        float maxY = pY + bbMaxY;
        float minZ = pZ + (float)bb.MinZ;

        if (EnableAo)
        {
            if (flipped)
            {
                Tess.setColorOpaque_F(colors.RedBottomLeft, colors.GreenBottomLeft, colors.BlueBottomLeft);
                Tess.addVertexWithUV(maxX, minY, minZ, uBl, vBl);
                Tess.setColorOpaque_F(colors.RedBottomRight, colors.GreenBottomRight, colors.BlueBottomRight);
                Tess.addVertexWithUV(minX, minY, minZ, uBr, vBr);
                Tess.setColorOpaque_F(colors.RedTopRight, colors.GreenTopRight, colors.BlueTopRight);
                Tess.addVertexWithUV(minX, maxY, minZ, uTr, vTr);
                Tess.setColorOpaque_F(colors.RedTopLeft, colors.GreenTopLeft, colors.BlueTopLeft);
                Tess.addVertexWithUV(maxX, maxY, minZ, uTl, vTl);
            }
            else
            {
                Tess.setColorOpaque_F(colors.RedTopLeft, colors.GreenTopLeft, colors.BlueTopLeft);
                Tess.addVertexWithUV(maxX, maxY, minZ, uTl, vTl);
                Tess.setColorOpaque_F(colors.RedBottomLeft, colors.GreenBottomLeft, colors.BlueBottomLeft);
                Tess.addVertexWithUV(maxX, minY, minZ, uBl, vBl);
                Tess.setColorOpaque_F(colors.RedBottomRight, colors.GreenBottomRight, colors.BlueBottomRight);
                Tess.addVertexWithUV(minX, minY, minZ, uBr, vBr);
                Tess.setColorOpaque_F(colors.RedTopRight, colors.GreenTopRight, colors.BlueTopRight);
                Tess.addVertexWithUV(minX, maxY, minZ, uTr, vTr);
            }
        }
        else
        {
            Tess.addVertexWithUV(maxX, maxY, minZ, uTl, vTl);
            Tess.addVertexWithUV(maxX, minY, minZ, uBl, vBl);
            Tess.addVertexWithUV(minX, minY, minZ, uBr, vBr);
            Tess.addVertexWithUV(minX, maxY, minZ, uTr, vTr);
        }
    }

    internal readonly void DrawWestFace(Block block, in Vec3D pos, in FaceColors colors, int textureId, bool flipped = false)
    {
        Box bb = OverrideBounds ?? block.BoundingBox;
        int texU = (textureId & 15) << 4;
        int texV = textureId & 240;

        float bbMinX = (float)bb.MinX;
        float bbMaxX = (float)bb.MaxX;
        float bbMinY = (float)bb.MinY;
        float bbMaxY = (float)bb.MaxY;

        float bMinX = Clamp(bbMinX);
        float bMaxX = Clamp(bbMaxX);
        float bMinY = Clamp(bbMinY);
        float bMaxY = Clamp(bbMaxY);

        CalculateUv(bMinX, 1.0f - bMaxY, UvRotateWest, texU, texV, out float uTl, out float vTl);
        CalculateUv(bMinX, 1.0f - bMinY, UvRotateWest, texU, texV, out float uBl, out float vBl);
        CalculateUv(bMaxX, 1.0f - bMinY, UvRotateWest, texU, texV, out float uBr, out float vBr);
        CalculateUv(bMaxX, 1.0f - bMaxY, UvRotateWest, texU, texV, out float uTr, out float vTr);

        float pX = (float)pos.x;
        float pY = (float)pos.y;
        float pZ = (float)pos.z;

        float minX = pX + bbMinX;
        float maxX = pX + bbMaxX;
        float minY = pY + bbMinY;
        float maxY = pY + bbMaxY;
        float maxZ = pZ + (float)bb.MaxZ;

        if (EnableAo)
        {
            if (flipped)
            {
                Tess.setColorOpaque_F(colors.RedBottomLeft, colors.GreenBottomLeft, colors.BlueBottomLeft);
                Tess.addVertexWithUV(minX, minY, maxZ, uBl, vBl);
                Tess.setColorOpaque_F(colors.RedBottomRight, colors.GreenBottomRight, colors.BlueBottomRight);
                Tess.addVertexWithUV(maxX, minY, maxZ, uBr, vBr);
                Tess.setColorOpaque_F(colors.RedTopRight, colors.GreenTopRight, colors.BlueTopRight);
                Tess.addVertexWithUV(maxX, maxY, maxZ, uTr, vTr);
                Tess.setColorOpaque_F(colors.RedTopLeft, colors.GreenTopLeft, colors.BlueTopLeft);
                Tess.addVertexWithUV(minX, maxY, maxZ, uTl, vTl);
            }
            else
            {
                Tess.setColorOpaque_F(colors.RedTopLeft, colors.GreenTopLeft, colors.BlueTopLeft);
                Tess.addVertexWithUV(minX, maxY, maxZ, uTl, vTl);
                Tess.setColorOpaque_F(colors.RedBottomLeft, colors.GreenBottomLeft, colors.BlueBottomLeft);
                Tess.addVertexWithUV(minX, minY, maxZ, uBl, vBl);
                Tess.setColorOpaque_F(colors.RedBottomRight, colors.GreenBottomRight, colors.BlueBottomRight);
                Tess.addVertexWithUV(maxX, minY, maxZ, uBr, vBr);
                Tess.setColorOpaque_F(colors.RedTopRight, colors.GreenTopRight, colors.BlueTopRight);
                Tess.addVertexWithUV(maxX, maxY, maxZ, uTr, vTr);
            }
        }
        else
        {
            Tess.addVertexWithUV(minX, maxY, maxZ, uTl, vTl);
            Tess.addVertexWithUV(minX, minY, maxZ, uBl, vBl);
            Tess.addVertexWithUV(maxX, minY, maxZ, uBr, vBr);
            Tess.addVertexWithUV(maxX, maxY, maxZ, uTr, vTr);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly bool IsOpaque(int x, int y, int z) => !Block.BlocksAllowVision[BlockReader.GetBlockId(x, y, z)];

    internal readonly bool DrawBlock(in Block block, in BlockPos pos)
    {
        bool hasRendered = false;
        Box bounds = OverrideBounds ?? block.BoundingBox;

        int colorMultiplier = block.getColorMultiplier(BlockReader, pos.x, pos.y, pos.z);
        float r = (colorMultiplier >> 16 & 255) * 0.0039215686F;
        float g = (colorMultiplier >> 8 & 255) * 0.0039215686F;
        float b = (colorMultiplier & 255) * 0.0039215686F;

        bool hasOverrideTex = OverrideTexture >= 0;
        bool tintBottom = true, tintTop = true, tintEast = true, tintWest = true, tintNorth = true, tintSouth = true;

        if (block.textureId == 3 || hasOverrideTex)
        {
            tintBottom = tintEast = tintWest = tintNorth = tintSouth = false;
        }

        float v0, v1, v2, v3;
        bool ao = AoBlendMode > 0;
        Vec3D vecPos = new(pos.x, pos.y, pos.z); // Allocate struct once

        // BOTTOM FACE (Y - 1)
        if (RenderAllFaces || bounds.MinY > 0.0F || block.isSideVisible(BlockReader, pos.x, pos.y - 1, pos.z, 0))
        {
            float lYn = block.getLuminance(Lighting, pos.x, pos.y - 1, pos.z);
            if (!ao) v0 = v1 = v2 = v3 = lYn;
            else
            {
                float n = block.getLuminance(Lighting, pos.x, pos.y - 1, pos.z - 1);
                float s = block.getLuminance(Lighting, pos.x, pos.y - 1, pos.z + 1);
                float w = block.getLuminance(Lighting, pos.x - 1, pos.y - 1, pos.z);
                float e = block.getLuminance(Lighting, pos.x + 1, pos.y - 1, pos.z);

                float nw = (IsOpaque(pos.x - 1, pos.y - 1, pos.z) && IsOpaque(pos.x, pos.y - 1, pos.z - 1)) ? w : block.getLuminance(Lighting, pos.x - 1, pos.y - 1, pos.z - 1);
                float sw = (IsOpaque(pos.x - 1, pos.y - 1, pos.z) && IsOpaque(pos.x, pos.y - 1, pos.z + 1)) ? w : block.getLuminance(Lighting, pos.x - 1, pos.y - 1, pos.z + 1);
                float ne = (IsOpaque(pos.x + 1, pos.y - 1, pos.z) && IsOpaque(pos.x, pos.y - 1, pos.z - 1)) ? e : block.getLuminance(Lighting, pos.x + 1, pos.y - 1, pos.z - 1);
                float se = (IsOpaque(pos.x + 1, pos.y - 1, pos.z) && IsOpaque(pos.x, pos.y - 1, pos.z + 1)) ? e : block.getLuminance(Lighting, pos.x + 1, pos.y - 1, pos.z + 1);

                v0 = (sw + w + s + lYn) * 0.25F;
                v1 = (w + nw + lYn + n) * 0.25F;
                v2 = (lYn + n + e + ne) * 0.25F;
                v3 = (s + lYn + se + e) * 0.25F;
            }

            var colors = FaceColors.AssignVertexColors(v0, v1, v2, v3, r, g, b, 0.5F, tintBottom);
            int textureId = hasOverrideTex ? OverrideTexture : block.getTextureId(BlockReader, pos.x, pos.y, pos.z, 0);

            DrawBottomFace(block, in vecPos, colors, textureId, ao && (v0 + v2 > v1 + v3));

            hasRendered = true;
        }

        // TOP FACE (Y + 1)
        if (RenderAllFaces || bounds.MaxY < 1.0F || block.isSideVisible(BlockReader, pos.x, pos.y + 1, pos.z, 1))
        {
            float lYp = block.getLuminance(Lighting, pos.x, pos.y + 1, pos.z);
            if (!ao) v0 = v1 = v2 = v3 = lYp;
            else
            {
                float n = block.getLuminance(Lighting, pos.x, pos.y + 1, pos.z - 1);
                float s = block.getLuminance(Lighting, pos.x, pos.y + 1, pos.z + 1);
                float w = block.getLuminance(Lighting, pos.x - 1, pos.y + 1, pos.z);
                float e = block.getLuminance(Lighting, pos.x + 1, pos.y + 1, pos.z);

                float nw = (IsOpaque(pos.x - 1, pos.y + 1, pos.z) && IsOpaque(pos.x, pos.y + 1, pos.z - 1)) ? w : block.getLuminance(Lighting, pos.x - 1, pos.y + 1, pos.z - 1);
                float sw = (IsOpaque(pos.x - 1, pos.y + 1, pos.z) && IsOpaque(pos.x, pos.y + 1, pos.z + 1)) ? w : block.getLuminance(Lighting, pos.x - 1, pos.y + 1, pos.z + 1);
                float ne = (IsOpaque(pos.x + 1, pos.y + 1, pos.z) && IsOpaque(pos.x, pos.y + 1, pos.z - 1)) ? e : block.getLuminance(Lighting, pos.x + 1, pos.y + 1, pos.z - 1);
                float se = (IsOpaque(pos.x + 1, pos.y + 1, pos.z) && IsOpaque(pos.x, pos.y + 1, pos.z + 1)) ? e : block.getLuminance(Lighting, pos.x + 1, pos.y + 1, pos.z + 1);

                v0 = (s + lYp + se + e) * 0.25F;
                v1 = (lYp + n + e + ne) * 0.25F;
                v2 = (w + nw + lYp + n) * 0.25F;
                v3 = (sw + w + s + lYp) * 0.25F;
            }

            var colors = FaceColors.AssignVertexColors(v0, v1, v2, v3, r, g, b, 1.0F, tintTop);
            int textureId = hasOverrideTex ? OverrideTexture : block.getTextureId(BlockReader, pos.x, pos.y, pos.z, 1);

            DrawTopFace(block, in vecPos, colors, textureId, ao && (v0 + v2 > v1 + v3));

            hasRendered = true;
        }

        // EAST FACE (Z - 1)
        if (RenderAllFaces || bounds.MinZ > 0.0F || block.isSideVisible(BlockReader, pos.x, pos.y, pos.z - 1, 2))
        {
            float lZn = block.getLuminance(Lighting, pos.x, pos.y, pos.z - 1);
            if (!ao) v0 = v1 = v2 = v3 = lZn;
            else
            {
                float u = block.getLuminance(Lighting, pos.x, pos.y + 1, pos.z - 1);
                float d = block.getLuminance(Lighting, pos.x, pos.y - 1, pos.z - 1);
                float w = block.getLuminance(Lighting, pos.x - 1, pos.y, pos.z - 1);
                float e = block.getLuminance(Lighting, pos.x + 1, pos.y, pos.z - 1);

                float uw = (IsOpaque(pos.x - 1, pos.y, pos.z - 1) && IsOpaque(pos.x, pos.y + 1, pos.z - 1)) ? w : block.getLuminance(Lighting, pos.x - 1, pos.y + 1, pos.z - 1);
                float dw = (IsOpaque(pos.x - 1, pos.y, pos.z - 1) && IsOpaque(pos.x, pos.y - 1, pos.z - 1)) ? w : block.getLuminance(Lighting, pos.x - 1, pos.y - 1, pos.z - 1);
                float ue = (IsOpaque(pos.x + 1, pos.y, pos.z - 1) && IsOpaque(pos.x, pos.y + 1, pos.z - 1)) ? e : block.getLuminance(Lighting, pos.x + 1, pos.y + 1, pos.z - 1);
                float de = (IsOpaque(pos.x + 1, pos.y, pos.z - 1) && IsOpaque(pos.x, pos.y - 1, pos.z - 1)) ? e : block.getLuminance(Lighting, pos.x + 1, pos.y - 1, pos.z - 1);

                v0 = (w + uw + lZn + u) * 0.25F;
                v1 = (lZn + u + e + ue) * 0.25F;
                v2 = (d + lZn + de + e) * 0.25F;
                v3 = (dw + w + d + lZn) * 0.25F;
            }

            int textureId = hasOverrideTex ? OverrideTexture : block.getTextureId(BlockReader, pos.x, pos.y, pos.z, 2);
            var colors = FaceColors.AssignVertexColors(v1, v2, v3, v0, r, g, b, 0.8F, tintEast);
            bool flipped = ao && (v1 + v3 > v2 + v0);

            DrawEastFace(block, in vecPos, colors, textureId, flipped);

            if (textureId == GrassRenderConstants.GrassSideTextureId && !hasOverrideTex)
            {
                var overlayColors = FaceColors.AssignVertexColors(v1, v2, v3, v0, r, g, b, 0.8F, true);
                DrawEastFace(block, in vecPos, overlayColors, GrassRenderConstants.GrassSideOverlayTextureId, flipped);
            }

            hasRendered = true;
        }

        // WEST FACE (Z + 1)
        if (RenderAllFaces || bounds.MaxZ < 1.0F || block.isSideVisible(BlockReader, pos.x, pos.y, pos.z + 1, 3))
        {
            float lZp = block.getLuminance(Lighting, pos.x, pos.y, pos.z + 1);
            if (!ao) v0 = v1 = v2 = v3 = lZp;
            else
            {
                float u = block.getLuminance(Lighting, pos.x, pos.y + 1, pos.z + 1);
                float d = block.getLuminance(Lighting, pos.x, pos.y - 1, pos.z + 1);
                float w = block.getLuminance(Lighting, pos.x - 1, pos.y, pos.z + 1);
                float e = block.getLuminance(Lighting, pos.x + 1, pos.y, pos.z + 1);

                float uw = (IsOpaque(pos.x - 1, pos.y, pos.z + 1) && IsOpaque(pos.x, pos.y + 1, pos.z + 1)) ? w : block.getLuminance(Lighting, pos.x - 1, pos.y + 1, pos.z + 1);
                float dw = (IsOpaque(pos.x - 1, pos.y, pos.z + 1) && IsOpaque(pos.x, pos.y - 1, pos.z + 1)) ? w : block.getLuminance(Lighting, pos.x - 1, pos.y - 1, pos.z + 1);
                float ue = (IsOpaque(pos.x + 1, pos.y, pos.z + 1) && IsOpaque(pos.x, pos.y + 1, pos.z + 1)) ? e : block.getLuminance(Lighting, pos.x + 1, pos.y + 1, pos.z + 1);
                float de = (IsOpaque(pos.x + 1, pos.y, pos.z + 1) && IsOpaque(pos.x, pos.y - 1, pos.z + 1)) ? e : block.getLuminance(Lighting, pos.x + 1, pos.y - 1, pos.z + 1);

                v0 = (w + uw + lZp + u) * 0.25F;
                v1 = (dw + w + d + lZp) * 0.25F;
                v2 = (d + lZp + de + e) * 0.25F;
                v3 = (lZp + u + e + ue) * 0.25F;
            }

            int textureId = hasOverrideTex ? OverrideTexture : block.getTextureId(BlockReader, pos.x, pos.y, pos.z, 3);
            var colors = FaceColors.AssignVertexColors(v0, v1, v2, v3, r, g, b, 0.8F, tintWest);
            bool flipped = ao && (v0 + v2 > v1 + v3);

            DrawWestFace(block, in vecPos, colors, textureId, flipped);

            if (textureId == GrassRenderConstants.GrassSideTextureId && !hasOverrideTex)
            {
                var overlayColors = FaceColors.AssignVertexColors(v0, v1, v2, v3, r, g, b, 0.8F, true);
                DrawWestFace(block, in vecPos, overlayColors, GrassRenderConstants.GrassSideOverlayTextureId, flipped);
            }

            hasRendered = true;
        }

        // NORTH FACE (X - 1)
        if (RenderAllFaces || bounds.MinX > 0.0F || block.isSideVisible(BlockReader, pos.x - 1, pos.y, pos.z, 4))
        {
            float lXn = block.getLuminance(Lighting, pos.x - 1, pos.y, pos.z);
            if (!ao) v0 = v1 = v2 = v3 = lXn;
            else
            {
                float u = block.getLuminance(Lighting, pos.x - 1, pos.y + 1, pos.z);
                float d = block.getLuminance(Lighting, pos.x - 1, pos.y - 1, pos.z);
                float n = block.getLuminance(Lighting, pos.x - 1, pos.y, pos.z - 1);
                float s = block.getLuminance(Lighting, pos.x - 1, pos.y, pos.z + 1);

                float un = (IsOpaque(pos.x - 1, pos.y, pos.z - 1) && IsOpaque(pos.x - 1, pos.y + 1, pos.z)) ? n : block.getLuminance(Lighting, pos.x - 1, pos.y + 1, pos.z - 1);
                float dn = (IsOpaque(pos.x - 1, pos.y, pos.z - 1) && IsOpaque(pos.x - 1, pos.y - 1, pos.z)) ? n : block.getLuminance(Lighting, pos.x - 1, pos.y - 1, pos.z - 1);
                float us = (IsOpaque(pos.x - 1, pos.y, pos.z + 1) && IsOpaque(pos.x - 1, pos.y + 1, pos.z)) ? s : block.getLuminance(Lighting, pos.x - 1, pos.y + 1, pos.z + 1);
                float ds = (IsOpaque(pos.x - 1, pos.y, pos.z + 1) && IsOpaque(pos.x - 1, pos.y - 1, pos.z)) ? s : block.getLuminance(Lighting, pos.x - 1, pos.y - 1, pos.z + 1);

                v0 = (u + us + lXn + s) * 0.25F;
                v1 = (u + un + n + lXn) * 0.25F;
                v2 = (n + lXn + dn + d) * 0.25F;
                v3 = (d + ds + lXn + s) * 0.25F;
            }

            int textureId = hasOverrideTex ? OverrideTexture : block.getTextureId(BlockReader, pos.x, pos.y, pos.z, 4);
            var colors = FaceColors.AssignVertexColors(v1, v2, v3, v0, r, g, b, 0.6F, tintNorth);
            bool flipped = ao && (v1 + v3 > v2 + v0);

            DrawNorthFace(block, in vecPos, colors, textureId, flipped);

            if (textureId == GrassRenderConstants.GrassSideTextureId && !hasOverrideTex)
            {
                var overlayColors = FaceColors.AssignVertexColors(v1, v2, v3, v0, r, g, b, 0.6F, true);
                DrawNorthFace(block, in vecPos, overlayColors, GrassRenderConstants.GrassSideOverlayTextureId, flipped);
            }

            hasRendered = true;
        }

        // SOUTH FACE (X + 1)
        if (RenderAllFaces || bounds.MaxX < 1.0F || block.isSideVisible(BlockReader, pos.x + 1, pos.y, pos.z, 5))
        {
            float lXp = block.getLuminance(Lighting, pos.x + 1, pos.y, pos.z);
            if (!ao) v0 = v1 = v2 = v3 = lXp;
            else
            {
                float u = block.getLuminance(Lighting, pos.x + 1, pos.y + 1, pos.z);
                float d = block.getLuminance(Lighting, pos.x + 1, pos.y - 1, pos.z);
                float n = block.getLuminance(Lighting, pos.x + 1, pos.y, pos.z - 1);
                float s = block.getLuminance(Lighting, pos.x + 1, pos.y, pos.z + 1);

                float un = (IsOpaque(pos.x + 1, pos.y, pos.z - 1) && IsOpaque(pos.x + 1, pos.y + 1, pos.z)) ? n : block.getLuminance(Lighting, pos.x + 1, pos.y + 1, pos.z - 1);
                float dn = (IsOpaque(pos.x + 1, pos.y, pos.z - 1) && IsOpaque(pos.x + 1, pos.y - 1, pos.z)) ? n : block.getLuminance(Lighting, pos.x + 1, pos.y - 1, pos.z - 1);
                float us = (IsOpaque(pos.x + 1, pos.y, pos.z + 1) && IsOpaque(pos.x + 1, pos.y + 1, pos.z)) ? s : block.getLuminance(Lighting, pos.x + 1, pos.y + 1, pos.z + 1);
                float ds = (IsOpaque(pos.x + 1, pos.y, pos.z + 1) && IsOpaque(pos.x + 1, pos.y - 1, pos.z)) ? s : block.getLuminance(Lighting, pos.x + 1, pos.y - 1, pos.z + 1);

                v0 = (d + ds + lXp + s) * 0.25F;
                v1 = (n + lXp + dn + d) * 0.25F;
                v2 = (u + un + n + lXp) * 0.25F;
                v3 = (u + us + lXp + s) * 0.25F;
            }

            int textureId = hasOverrideTex ? OverrideTexture : block.getTextureId(BlockReader, pos.x, pos.y, pos.z, 5);
            var colors = FaceColors.AssignVertexColors(v3, v0, v1, v2, r, g, b, 0.6F, tintSouth);
            bool flipped = ao && (v3 + v1 > v0 + v2);

            DrawSouthFace(block, in vecPos, colors, textureId, flipped);

            if (textureId == GrassRenderConstants.GrassSideTextureId && !hasOverrideTex)
            {
                var overlayColors = FaceColors.AssignVertexColors(v3, v0, v1, v2, r, g, b, 0.6F, true);
                DrawSouthFace(block, in vecPos, overlayColors, GrassRenderConstants.GrassSideOverlayTextureId, flipped);
            }

            hasRendered = true;
        }

        return hasRendered;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal readonly void DrawTorch(in Block block, in Vec3D pos, float tiltX, float tiltZ)
    {
        const float texScale = 1.0f / 256.0f;
        const float radius = 1.0f / 16.0f;
        const float height = 10.0f / 16.0f;
        const float tipOffsetBase = 1.0f - height;

        const float topMinUOffset = 7.0f * texScale;
        const float topMaxUOffset = 9.0f * texScale;
        const float topMinVOffset = 6.0f * texScale;
        const float topMaxVOffset = 8.0f * texScale;

        int textureId = OverrideTexture >= 0 ? OverrideTexture : block.getTexture(0);

        int texU = (textureId & 15) << 4;
        int texV = textureId & 240;

        float minU = texU * texScale;
        float maxU = (texU + 15.99f) * texScale;
        float minV = texV * texScale;
        float maxV = (texV + 15.99f) * texScale;

        float topMinU = minU + topMinUOffset;
        float topMinV = minV + topMinVOffset;
        float topMaxU = minU + topMaxUOffset;
        float topMaxV = minV + topMaxVOffset;

        float pX = (float)pos.x;
        float pY = (float)pos.y;
        float pZ = (float)pos.z;

        float centerX = pX + 0.5f;
        float centerZ = pZ + 0.5f;
        float leftX = pX;
        float rightX = pX + 1.0f;
        float frontZ = pZ;
        float backZ = pZ + 1.0f;

        float yBot = pY;
        float yTop = pY + 1.0f;
        float yTip = pY + height;

        float cXmin = centerX - radius;
        float cXmax = centerX + radius;
        float cZmin = centerZ - radius;
        float cZmax = centerZ + radius;

        float tLeftX = leftX + tiltX;
        float tRightX = rightX + tiltX;
        float tFrontZ = frontZ + tiltZ;
        float tBackZ = backZ + tiltZ;

        float cXminT = cXmin + tiltX;
        float cXmaxT = cXmax + tiltX;
        float cZminT = cZmin + tiltZ;
        float cZmaxT = cZmax + tiltZ;

        float tipX = centerX + tiltX * tipOffsetBase;
        float tipZ = centerZ + tiltZ * tipOffsetBase;

        Tess.setColorOpaque_F(1.0f, 1.0f, 1.0f);

        // TOP FACE
        Tess.addVertexWithUV(tipX - radius, yTip, tipZ - radius, topMinU, topMinV);
        Tess.addVertexWithUV(tipX - radius, yTip, tipZ + radius, topMinU, topMaxV);
        Tess.addVertexWithUV(tipX + radius, yTip, tipZ + radius, topMaxU, topMaxV);
        Tess.addVertexWithUV(tipX + radius, yTip, tipZ - radius, topMaxU, topMinV);

        // West Face
        Tess.addVertexWithUV(cXmin, yTop, frontZ, minU, minV);
        Tess.addVertexWithUV(cXminT, yBot, tFrontZ, minU, maxV);
        Tess.addVertexWithUV(cXminT, yBot, tBackZ, maxU, maxV);
        Tess.addVertexWithUV(cXmin, yTop, backZ, maxU, minV);

        // East Face
        Tess.addVertexWithUV(cXmax, yTop, backZ, minU, minV);
        Tess.addVertexWithUV(cXmaxT, yBot, tBackZ, minU, maxV);
        Tess.addVertexWithUV(cXmaxT, yBot, tFrontZ, maxU, maxV);
        Tess.addVertexWithUV(cXmax, yTop, frontZ, maxU, minV);

        // North Face
        Tess.addVertexWithUV(leftX, yTop, cZmax, minU, minV);
        Tess.addVertexWithUV(tLeftX, yBot, cZmaxT, minU, maxV);
        Tess.addVertexWithUV(tRightX, yBot, cZmaxT, maxU, maxV);
        Tess.addVertexWithUV(rightX, yTop, cZmax, maxU, minV);

        // South Face
        Tess.addVertexWithUV(rightX, yTop, cZmin, minU, minV);
        Tess.addVertexWithUV(tRightX, yBot, cZminT, minU, maxV);
        Tess.addVertexWithUV(tLeftX, yBot, cZminT, maxU, maxV);
        Tess.addVertexWithUV(leftX, yTop, cZmin, maxU, minV);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly void CalculateUv(float h, float v, int rotation, int texU, int texV, out float u, out float outV)
    {
        //  0 and no flip are the most common states
        if (rotation == 0 && !FlipTexture)
        {
            u = texU * 0.00390625f + h * 0.0625f;
            outV = texV * 0.00390625f + v * 0.0625f;
            return;
        }

        float fU, fV;

        // Stripped down switch (pure assignment, no inline math)
        switch (rotation)
        {
            case 1:
                fU = v;
                fV = 1.0f - h;
                break;
            case 2:
                fU = 1.0f - h;
                fV = 1.0f - v;
                break;
            case 3:
                fU = 1.0f - v;
                fV = h;
                break;
            case 4:
                fU = 1.0f - h;
                fV = v;
                break;
            case 5:
                fU = v;
                fV = h;
                break;
            case 6:
                fU = h;
                fV = 1.0f - v;
                break;
            case 7:
                fU = 1.0f - v;
                fV = 1.0f - h;
                break;
            default:
                fU = h;
                fV = v;
                break;
        }

        fU = FlipTexture ? 1.0f - fU : fU;

        u = texU * 0.00390625f + fU * 0.0625f;
        outV = texV * 0.00390625f + fV * 0.0625f;
    }
}
