using System.Runtime.InteropServices;
using BetaSharp.Client.Guis;
using BetaSharp.Util;
using Silk.NET.OpenGL;
using GLEnum = BetaSharp.Client.Rendering.Core.OpenGL.GLEnum;

namespace BetaSharp.Client.Rendering.Core;

[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 32)]
public struct Vertex(float x, float y, float z, float u, float v, int color, int normal)
{
    public float X = x; // 4 bytes
    public float Y = y; // 4 bytes + 4 bytes = 8 bytes
    public float Z = z; // 4 bytes + 8 bytes = 12 bytes
    public float U = u; // 4 bytes + 12 bytes = 16 bytes
    public float V = v; // 4 bytes + 16 bytes = 20 bytes
    public int Color = color; // 4 bytes + 20 bytes = 24 bytes
    public int Normal = normal; // 4 bytes + 20 bytes = 28 bytes
    public int Padding; // 32 bytes total
}

[StructLayout(LayoutKind.Sequential, Size = 16)]
public struct ChunkVertex
{
    public int Color; // 4 bytes
    public short X; // 2 bytes + 4 bytes = 6 bytes
    public short Y; // 2 bytes + 6 bytes = 8 bytes
    public short Z; // 2 bytes + 8 bytes = 10 bytes
    public short U; // 2 bytes + 10 bytes = 12 bytes
    public short V; // 2 bytes + 12 bytes = 14 bytes
    public byte Light; // 1 byte + 14 bytes = 15 bytes
    public byte Padding; // 16 bytes total
}

public static class ChunkVertexHelper
{
    private const float POSITION_SCALE = 32767f / 64f;

    private const float UV_SCALE = 32767f;

    public static ChunkVertex Create(
        int color,
        float x,
        float y,
        float z,
        float u,
        float v,
        float centroidU,
        float centroidV,
        byte skyLight,
        byte blockLight)
    {
        return new ChunkVertex
        {
            Color = color,
            X = FloatToShortPosition(x),
            Y = FloatToShortPosition(y),
            Z = FloatToShortPosition(z),
            U = FloatToShortUVWithInset(u, centroidU),
            V = FloatToShortUVWithInset(v, centroidV),
            Light = PackLight(skyLight, blockLight),
            Padding = 0
        };
    }

    private static short FloatToShortUVWithInset(float uv, float centroid)
    {
        int bias = uv < centroid ? 1 : -1;
        int quantized = (int)System.Math.Round(uv * UV_SCALE) + bias;

        return (short)(quantized & 0x7FFF | Sign(bias) << 15);
    }

    private static int Sign(int x)
    {
        return x < 0 ? 1 : 0;
    }

    public static short FloatToShortPosition(float position)
    {
        return (short)System.Math.Round(position * POSITION_SCALE);
    }

    public static short FloatToShortUV(float uv)
    {
        return (short)(uv * UV_SCALE);
    }

    public static byte PackLight(byte skyLight, byte blockLight)
    {
        skyLight = (byte)(skyLight & 0x0F);
        blockLight = (byte)(blockLight & 0x0F);

        return (byte)(skyLight << 4 | blockLight);
    }

    public static byte GetSkyLight(byte light)
    {
        return (byte)(light >> 4 & 0x0F);
    }

    public static byte GetBlockLight(byte light)
    {
        return (byte)(light & 0x0F);
    }
}

public enum TesselatorCaptureVertexFormat
{
    Default,
    Chunk
}

public class Tessellator
{
    private static readonly bool convertQuadsToTriangles = true;
    private readonly int[] rawBuffer;
    private int vertexCount;
    private double textureU;
    private double textureV;
    private int color;
    private bool hasColor;
    private bool hasTexture;
    private bool hasNormals;
    private byte skyLight;
    private byte blockLight;
    private bool hasLight;
    private int rawBufferIndex;
    private int addedVertices;
    private bool isColorDisabled;
    private int drawMode;
    private double xOffset;
    private double yOffset;
    private double zOffset;
    private int normal;
    public static readonly Tessellator instance = new(2097152);
    public bool IsDrawing { get; private set; }
    private readonly uint[] _vboIds;
    private int vboIndex;
    private readonly int vboCount = 10;
    private readonly int bufferSize;
    private float uvCentroidU;
    private float uvCentroidV;
    private bool isCaptureMode;
    private PooledList<Vertex> capturedVertices;
    private PooledList<ChunkVertex> capturedChunkVertices;
    private int[] scratchBuffer;
    private int scratchBufferIndex;
    private TesselatorCaptureVertexFormat vertexFormat;

    private unsafe Tessellator(int var1)
    {
        bufferSize = var1;
        rawBuffer = new int[var1];
        _vboIds = new uint[vboCount];
        GLManager.GL.GenBuffers((uint)vboCount, _vboIds);
    }

    public Tessellator()
    {
    }

    public void startCapture(TesselatorCaptureVertexFormat format)
    {
        if (format == TesselatorCaptureVertexFormat.Chunk && IsDrawing)
        {
            throw new InvalidOperationException("Chunk vertex format is only supported in capture mode!");
        }

        vertexFormat = format;
        isCaptureMode = true;

        capturedVertices = null;
        capturedChunkVertices = null;

        if (format == TesselatorCaptureVertexFormat.Default)
        {
            capturedVertices = new();
        }
        else
        {
            capturedChunkVertices = new();
        }

        scratchBuffer = new int[32];
        scratchBufferIndex = 0;
        uvCentroidU = 0f;
        uvCentroidV = 0f;
    }

    public PooledList<Vertex> endCaptureVertices()
    {
        if (!isCaptureMode || vertexFormat != TesselatorCaptureVertexFormat.Default)
        {
            throw new InvalidOperationException("Not capturing default vertices!");
        }

        isCaptureMode = false;
        var result = capturedVertices;
        CleanupCapture();
        return result;
    }

    public PooledList<ChunkVertex> endCaptureChunkVertices()
    {
        if (!isCaptureMode || vertexFormat != TesselatorCaptureVertexFormat.Chunk)
        {
            throw new InvalidOperationException("Not capturing chunk vertices!");
        }

        isCaptureMode = false;
        var result = capturedChunkVertices;
        CleanupCapture();
        return result;
    }

    private void CleanupCapture()
    {
        capturedVertices = null;
        capturedChunkVertices = null;
        scratchBuffer = null;
        scratchBufferIndex = 0;
    }

    public void begin()
    {
        scratchBufferIndex = 0;
        vertexCount = 0;
        hasTexture = false;
        hasColor = false;
        hasNormals = false;
    }

    public unsafe void draw()
    {
        if (!IsDrawing)
        {
            throw new InvalidOperationException("Not tesselating!");
        }
        else
        {
            IsDrawing = false;

            if (isCaptureMode)
            {
                scratchBufferIndex = 0;
                return;
            }

            if (vertexCount > 0)
            {
                vboIndex = (vboIndex + 1) % vboCount;
                GLManager.GL.BindBuffer(GLEnum.ArrayBuffer, _vboIds[vboIndex]);

                fixed (int* ptr = rawBuffer)
                {
                    GLManager.GL.BufferData(GLEnum.ArrayBuffer, (nuint)(rawBufferIndex * 4), ptr, GLEnum.StreamDraw);
                }

                if (hasTexture)
                {
                    GLManager.GL.TexCoordPointer(2, GLEnum.Float, 32, (void*)12);
                    GLManager.GL.EnableClientState(GLEnum.TextureCoordArray);
                }
                if (hasColor)
                {
                    GLManager.GL.ColorPointer(4, ColorPointerType.UnsignedByte, 32, (void*)20);
                    GLManager.GL.EnableClientState(GLEnum.ColorArray);
                }
                if (hasNormals)
                {
                    GLManager.GL.NormalPointer(NormalPointerType.Byte, 32, (void*)24);
                    GLManager.GL.EnableClientState(GLEnum.NormalArray);
                }

                GLManager.GL.VertexPointer(3, GLEnum.Float, 32, (void*)0);

                GLManager.GL.EnableClientState(GLEnum.VertexArray);
                if (drawMode == 7 && convertQuadsToTriangles)
                {
                    GLManager.GL.DrawArrays(GLEnum.Triangles, 0, (uint)vertexCount);
                }
                else
                {
                    GLManager.GL.DrawArrays((GLEnum)drawMode, 0, (uint)vertexCount);
                }

                GLManager.GL.DisableClientState(GLEnum.VertexArray);
                if (hasTexture)
                {
                    GLManager.GL.DisableClientState(GLEnum.TextureCoordArray);
                }

                if (hasColor)
                {
                    GLManager.GL.DisableClientState(GLEnum.ColorArray);
                }

                if (hasNormals)
                {
                    GLManager.GL.DisableClientState(GLEnum.NormalArray);
                }
            }

            reset();
        }
    }

    private void reset()
    {
        vertexCount = 0;
        rawBufferIndex = 0;
        addedVertices = 0;
    }

    public void startDrawingQuads()
    {
        startDrawing(7);
    }

    public void startDrawing(int var1)
    {
        if (IsDrawing)
        {
            throw new InvalidOperationException("Already tesselating!");
        }
        else
        {
            IsDrawing = true;
            reset();
            drawMode = var1;
            hasNormals = false;
            hasColor = false;
            hasTexture = false;
            isColorDisabled = false;
        }
    }

    public void setTextureUV(double u, double v)
    {
        hasTexture = true;
        textureU = u;
        textureV = v;
    }

    public void setColorOpaque_F(float red, float green, float blue)
    {
        setColorOpaque((int)(red * 255.0F), (int)(green * 255.0F), (int)(blue * 255.0F));
    }

    public void setColorRGBA_F(float red, float green, float blue, float alpha)
    {
        setColorRGBA((int)(red * 255.0F), (int)(green * 255.0F), (int)(blue * 255.0F), (int)(alpha * 255.0F));
    }

    public void setColorOpaque(int red, int green, int blue)
    {
        setColorRGBA(red, green, blue, 255);
    }

    public void setColorRGBA(int red, int green, int blue, int alpha)
    {
        if (!isColorDisabled)
        {
            if (red > 255)
            {
                red = 255;
            }

            if (green > 255)
            {
                green = 255;
            }

            if (blue > 255)
            {
                blue = 255;
            }

            if (alpha > 255)
            {
                alpha = 255;
            }

            if (red < 0)
            {
                red = 0;
            }

            if (green < 0)
            {
                green = 0;
            }

            if (blue < 0)
            {
                blue = 0;
            }

            if (alpha < 0)
            {
                alpha = 0;
            }

            hasColor = true;
            if (BitConverter.IsLittleEndian)
            {
                color = alpha << 24 | blue << 16 | green << 8 | red;
            }
            else
            {
                color = red << 24 | green << 16 | blue << 8 | alpha;
            }

        }
    }

    public void setColorOpaque(Color c)
    {
        if (isColorDisabled) return;
        if (BitConverter.IsLittleEndian)
        {
            color = (int)c | 255;
        }
        else
        {
            color = ((int)c << 8) | 255;
        }
        hasColor = true;
    }

    public void setColorRGBA(Color c)
    {
        if (isColorDisabled) return;
        if (BitConverter.IsLittleEndian)
        {
            color = (int)c;
        }
        else
        {
            int v = (int)c;
            color = (v << 8) | (v >> 24);
        }
        hasColor = true;
    }

    public void addVertexWithUV(double x, double y, double z, double u, double v)
    {
        setTextureUV(u, v);
        addVertex(x, y, z);
    }

    public void addVertex(double x, double y, double z)
    {
        if (isCaptureMode)
        {
            scratchBuffer[scratchBufferIndex + 0] = BitConverter.SingleToInt32Bits((float)(x + xOffset));
            scratchBuffer[scratchBufferIndex + 1] = BitConverter.SingleToInt32Bits((float)(y + yOffset));
            scratchBuffer[scratchBufferIndex + 2] = BitConverter.SingleToInt32Bits((float)(z + zOffset));

            if (hasTexture)
            {
                scratchBuffer[scratchBufferIndex + 3] = BitConverter.SingleToInt32Bits((float)textureU);
                scratchBuffer[scratchBufferIndex + 4] = BitConverter.SingleToInt32Bits((float)textureV);
            }
            else if (vertexFormat == TesselatorCaptureVertexFormat.Chunk)
            {
                throw new InvalidOperationException("ChunkVertex requires texture coordinates!");
            }

            if (hasColor)
            {
                scratchBuffer[scratchBufferIndex + 5] = color;
            }

            if (hasNormals)
            {
                scratchBuffer[scratchBufferIndex + 6] = normal;
            }

            if (hasLight)
            {
                scratchBuffer[scratchBufferIndex + 7] =
                    ChunkVertexHelper.PackLight(skyLight, blockLight);
            }

            scratchBufferIndex += 8;

            if (drawMode == 7 && convertQuadsToTriangles && scratchBufferIndex == 32)
            {
                uvCentroidU = 0f;
                uvCentroidV = 0f;
                for (int i = 0; i < 4; i++)
                {
                    int idx = i * 8;
                    uvCentroidU += BitConverter.Int32BitsToSingle(scratchBuffer[idx + 3]);
                    uvCentroidV += BitConverter.Int32BitsToSingle(scratchBuffer[idx + 4]);
                }
                uvCentroidU *= 0.25f;
                uvCentroidV *= 0.25f;

                EmitVertexFromScratch(0);
                EmitVertexFromScratch(8);
                EmitVertexFromScratch(16);

                EmitVertexFromScratch(16);
                EmitVertexFromScratch(24);
                EmitVertexFromScratch(0);

                scratchBufferIndex = 0;
            }

            return;
        }
        else
        {
            ++addedVertices;
            if (drawMode == 7 && convertQuadsToTriangles && addedVertices % 4 == 0)
            {
                for (int var7 = 0; var7 < 2; ++var7)
                {
                    int var8 = 8 * (3 - var7);
                    if (hasTexture)
                    {
                        rawBuffer[rawBufferIndex + 3] = rawBuffer[rawBufferIndex - var8 + 3];
                        rawBuffer[rawBufferIndex + 4] = rawBuffer[rawBufferIndex - var8 + 4];
                    }

                    if (hasColor)
                    {
                        rawBuffer[rawBufferIndex + 5] = rawBuffer[rawBufferIndex - var8 + 5];
                    }

                    rawBuffer[rawBufferIndex + 0] = rawBuffer[rawBufferIndex - var8 + 0];
                    rawBuffer[rawBufferIndex + 1] = rawBuffer[rawBufferIndex - var8 + 1];
                    rawBuffer[rawBufferIndex + 2] = rawBuffer[rawBufferIndex - var8 + 2];
                    ++vertexCount;
                    rawBufferIndex += 8;
                }
            }

            if (hasTexture)
            {
                rawBuffer[rawBufferIndex + 3] = BitConverter.SingleToInt32Bits((float)textureU);
                rawBuffer[rawBufferIndex + 4] = BitConverter.SingleToInt32Bits((float)textureV);
            }

            if (hasColor)
            {
                rawBuffer[rawBufferIndex + 5] = color;
            }

            if (hasNormals)
            {
                rawBuffer[rawBufferIndex + 6] = normal;
            }

            rawBuffer[rawBufferIndex + 0] = BitConverter.SingleToInt32Bits((float)(x + xOffset));
            rawBuffer[rawBufferIndex + 1] = BitConverter.SingleToInt32Bits((float)(y + yOffset));
            rawBuffer[rawBufferIndex + 2] = BitConverter.SingleToInt32Bits((float)(z + zOffset));
            rawBufferIndex += 8;
            ++vertexCount;

            if (vertexCount % 4 == 0 && rawBufferIndex >= bufferSize - 32)
            {
                draw();
                IsDrawing = true;
            }
        }
    }

    private void EmitVertexFromScratch(int baseIndex)
    {
        float x = BitConverter.Int32BitsToSingle(scratchBuffer[baseIndex + 0]);
        float y = BitConverter.Int32BitsToSingle(scratchBuffer[baseIndex + 1]);
        float z = BitConverter.Int32BitsToSingle(scratchBuffer[baseIndex + 2]);

        if (vertexFormat == TesselatorCaptureVertexFormat.Chunk)
        {
            int col = hasColor ? scratchBuffer[baseIndex + 5] : unchecked((int)0xFFFFFFFF);
            byte light = hasLight ? (byte)scratchBuffer[baseIndex + 7] : (byte)0;

            float u = BitConverter.Int32BitsToSingle(scratchBuffer[baseIndex + 3]);
            float v = BitConverter.Int32BitsToSingle(scratchBuffer[baseIndex + 4]);

            capturedChunkVertices.Add(
                ChunkVertexHelper.Create(
                    col,
                    x, y, z,
                    u, v,
                    uvCentroidU, uvCentroidV,
                    ChunkVertexHelper.GetSkyLight(light),
                    ChunkVertexHelper.GetBlockLight(light)
                )
            );
        }
        else
        {
            float u = hasTexture ? BitConverter.Int32BitsToSingle(scratchBuffer[baseIndex + 3]) : 0f;
            float v = hasTexture ? BitConverter.Int32BitsToSingle(scratchBuffer[baseIndex + 4]) : 0f;
            int col = hasColor ? scratchBuffer[baseIndex + 5] : 0;
            int norm = hasNormals ? scratchBuffer[baseIndex + 6] : 0;

            capturedVertices.Add(new Vertex(x, y, z, u, v, col, norm));
        }
    }


    public void setColorOpaque_I(int color)
    {
        int red = color >> 16 & 255;
        int green = color >> 8 & 255;
        int blue = color & 255;
        setColorOpaque(red, green, blue);
    }

    public void setColorRGBA_I(int color, int alpha)
    {
        int red = color >> 16 & 255;
        int green = color >> 8 & 255;
        int blue = color & 255;
        setColorRGBA(red, green, blue, alpha);
    }

    public void disableColor()
    {
        isColorDisabled = true;
    }

    public void setNormal(float var1, float var2, float var3)
    {
        hasNormals = true;
        byte var4 = (byte)(int)(var1 * 128.0F);
        byte var5 = (byte)(int)(var2 * 127.0F);
        byte var6 = (byte)(int)(var3 * 127.0F);
        normal = var4 | var5 << 8 | var6 << 16;
    }

    public void setSkyLight(byte value)
    {
        skyLight = (byte)(value & 0x0F);
        hasLight = true;
    }

    public void setBlockLight(byte value)
    {
        blockLight = (byte)(value & 0x0F);
        hasLight = true;
    }

    public void setLight(byte sky, byte block)
    {
        skyLight = (byte)(sky & 0x0F);
        blockLight = (byte)(block & 0x0F);
        hasLight = true;
    }

    public void setTranslationD(double var1, double var3, double var5)
    {
        xOffset = var1;
        yOffset = var3;
        zOffset = var5;
    }

    public void setTranslationF(float var1, float var2, float var3)
    {
        xOffset += var1;
        yOffset += var2;
        zOffset += var3;
    }
}
