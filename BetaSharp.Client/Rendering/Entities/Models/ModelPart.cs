using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.OpenGL;
using BetaSharp.Client.Rendering.Core.Textures;

namespace BetaSharp.Client.Rendering.Entities.Models;

public class ModelPart
{
    private PositionTextureVertex[] corners;
    private Quad[] faces;
    private readonly int textureOffsetX;
    private readonly int textureOffsetY;
    public float rotationPointX;
    public float rotationPointY;
    public float rotationPointZ;
    public float rotateAngleX;
    public float rotateAngleY;
    public float rotateAngleZ;
    private bool compiled;
    private uint displayList;
    public bool mirror = false;
    public bool visible = true;
    public bool hidden = false;

    public ModelPart(int textureOffsetX, int textureOffsetY)
    {
        this.textureOffsetX = textureOffsetX;
        this.textureOffsetY = textureOffsetY;
    }

    public void addBox(float x, float y, float z, int width, int height, int depth)
    {
        addBox(x, y, z, width, height, depth, 0.0F);
    }

    public void addBox(float x, float y, float z, int width, int height, int depth, float inflation)
    {
        corners = new PositionTextureVertex[8];
        faces = new Quad[6];

        float minX = x - inflation;
        float minY = y - inflation;
        float minZ = z - inflation;
        float maxX = x + width + inflation;
        float maxY = y + height + inflation;
        float maxZ = z + depth + inflation;

        if (mirror)
        {
            (maxX, minX) = (minX, maxX);
        }


        PositionTextureVertex frontTopLeft = new(minX, minY, minZ, 0.0F, 0.0F);
        PositionTextureVertex frontTopRight = new(maxX, minY, minZ, 0.0F, 8.0F);
        PositionTextureVertex frontBottomRight = new(maxX, maxY, minZ, 8.0F, 8.0F);
        PositionTextureVertex frontBottomLeft = new(minX, maxY, minZ, 8.0F, 0.0F);
        PositionTextureVertex backTopLeft = new(minX, minY, maxZ, 0.0F, 0.0F);
        PositionTextureVertex backTopRight = new(maxX, minY, maxZ, 0.0F, 8.0F);
        PositionTextureVertex backBottomRight = new(maxX, maxY, maxZ, 8.0F, 8.0F);
        PositionTextureVertex backBottomLeft = new(minX, maxY, maxZ, 8.0F, 0.0F);

        corners[0] = frontTopLeft;
        corners[1] = frontTopRight;
        corners[2] = frontBottomRight;
        corners[3] = frontBottomLeft;
        corners[4] = backTopLeft;
        corners[5] = backTopRight;
        corners[6] = backBottomRight;
        corners[7] = backBottomLeft;

        faces[0] = new Quad(
            [backTopRight, frontTopRight, frontBottomRight, backBottomRight],
            this.textureOffsetX + depth + width,
            this.textureOffsetY + depth,
            this.textureOffsetX + depth + width + depth,
            this.textureOffsetY + depth + height);
        faces[1] = new Quad(
            [frontTopLeft, backTopLeft, backBottomLeft, frontBottomLeft],
            this.textureOffsetX,
            this.textureOffsetY + depth,
            this.textureOffsetX + depth,
            this.textureOffsetY + depth + height);
        faces[2] = new Quad(
            [backTopRight, backTopLeft, frontTopLeft, frontTopRight],
            this.textureOffsetX + depth,
            this.textureOffsetY,
            this.textureOffsetX + depth + width,
            this.textureOffsetY + depth);
        faces[3] = new Quad(
            [backBottomRight, backBottomLeft, frontBottomLeft, frontBottomRight],
            this.textureOffsetX + depth + width,
            this.textureOffsetY,
            this.textureOffsetX + depth + width + width,
            this.textureOffsetY + depth);
        faces[4] = new Quad(
            [frontTopRight, frontTopLeft, frontBottomLeft, frontBottomRight],
            this.textureOffsetX + depth,
            this.textureOffsetY + depth,
            this.textureOffsetX + depth + width,
            this.textureOffsetY + depth + height);
        faces[5] = new Quad(
            [backTopLeft, backTopRight, backBottomRight, backBottomLeft],
            this.textureOffsetX + depth + width + depth,
            this.textureOffsetY + depth,
            this.textureOffsetX + depth + width + depth + width,
            this.textureOffsetY + depth + height);

        if (mirror)
        {
            for (int faceIndex = 0; faceIndex < faces.Length; ++faceIndex)
            {
                faces[faceIndex].flipFace();
            }
        }
    }

    public void setRotationPoint(float x, float y, float z)
    {
        rotationPointX = x;
        rotationPointY = y;
        rotationPointZ = z;
    }

    public void render(float scale)
    {
        if (!hidden)
        {
            if (visible)
            {
                if (!compiled)
                {
                    compileDisplayList(scale);
                }

                if (rotateAngleX == 0.0F && rotateAngleY == 0.0F && rotateAngleZ == 0.0F)
                {
                    if (rotationPointX == 0.0F && rotationPointY == 0.0F && rotationPointZ == 0.0F)
                    {
                        GLManager.GL.CallList(displayList);
                    }
                    else
                    {
                        GLManager.GL.Translate(rotationPointX * scale, rotationPointY * scale, rotationPointZ * scale);
                        GLManager.GL.CallList(displayList);
                        GLManager.GL.Translate(-rotationPointX * scale, -rotationPointY * scale, -rotationPointZ * scale);
                    }
                }
                else
                {
                    GLManager.GL.PushMatrix();
                    GLManager.GL.Translate(rotationPointX * scale, rotationPointY * scale, rotationPointZ * scale);
                    if (rotateAngleZ != 0.0F)
                    {
                        GLManager.GL.Rotate(rotateAngleZ * (180.0F / (float)Math.PI), 0.0F, 0.0F, 1.0F);
                    }

                    if (rotateAngleY != 0.0F)
                    {
                        GLManager.GL.Rotate(rotateAngleY * (180.0F / (float)Math.PI), 0.0F, 1.0F, 0.0F);
                    }

                    if (rotateAngleX != 0.0F)
                    {
                        GLManager.GL.Rotate(rotateAngleX * (180.0F / (float)Math.PI), 1.0F, 0.0F, 0.0F);
                    }

                    GLManager.GL.CallList(displayList);
                    GLManager.GL.PopMatrix();
                }
            }
        }
    }

    public void renderWithRotation(float scale)
    {
        if (!hidden)
        {
            if (visible)
            {
                if (!compiled)
                {
                    compileDisplayList(scale);
                }

                GLManager.GL.PushMatrix();
                GLManager.GL.Translate(rotationPointX * scale, rotationPointY * scale, rotationPointZ * scale);
                if (rotateAngleY != 0.0F)
                {
                    GLManager.GL.Rotate(rotateAngleY * (180.0F / (float)Math.PI), 0.0F, 1.0F, 0.0F);
                }

                if (rotateAngleX != 0.0F)
                {
                    GLManager.GL.Rotate(rotateAngleX * (180.0F / (float)Math.PI), 1.0F, 0.0F, 0.0F);
                }

                if (rotateAngleZ != 0.0F)
                {
                    GLManager.GL.Rotate(rotateAngleZ * (180.0F / (float)Math.PI), 0.0F, 0.0F, 1.0F);
                }

                GLManager.GL.CallList(displayList);
                GLManager.GL.PopMatrix();
            }
        }
    }

    public void transform(float scale)
    {
        if (!hidden)
        {
            if (visible)
            {
                if (!compiled)
                {
                    compileDisplayList(scale);
                }

                if (rotateAngleX == 0.0F && rotateAngleY == 0.0F && rotateAngleZ == 0.0F)
                {
                    if (rotationPointX != 0.0F || rotationPointY != 0.0F || rotationPointZ != 0.0F)
                    {
                        GLManager.GL.Translate(rotationPointX * scale, rotationPointY * scale, rotationPointZ * scale);
                    }
                }
                else
                {
                    GLManager.GL.Translate(rotationPointX * scale, rotationPointY * scale, rotationPointZ * scale);
                    if (rotateAngleZ != 0.0F)
                    {
                        GLManager.GL.Rotate(rotateAngleZ * (180.0F / (float)Math.PI), 0.0F, 0.0F, 1.0F);
                    }

                    if (rotateAngleY != 0.0F)
                    {
                        GLManager.GL.Rotate(rotateAngleY * (180.0F / (float)Math.PI), 0.0F, 1.0F, 0.0F);
                    }

                    if (rotateAngleX != 0.0F)
                    {
                        GLManager.GL.Rotate(rotateAngleX * (180.0F / (float)Math.PI), 1.0F, 0.0F, 0.0F);
                    }
                }
            }
        }
    }

    private void compileDisplayList(float scale)
    {
        displayList = (uint)GLAllocation.generateDisplayLists(1);
        GLManager.GL.NewList(displayList, GLEnum.Compile);
        Tessellator tessellator = Tessellator.instance;

        for (int faceIndex = 0; faceIndex < faces.Length; ++faceIndex)
        {
            faces[faceIndex].draw(tessellator, scale);
        }

        GLManager.GL.EndList();
        compiled = true;
    }
}
