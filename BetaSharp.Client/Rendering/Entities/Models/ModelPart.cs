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

    public ModelPart(int var1, int var2)
    {
        textureOffsetX = var1;
        textureOffsetY = var2;
    }

    public void addBox(float var1, float var2, float var3, int var4, int var5, int var6)
    {
        addBox(var1, var2, var3, var4, var5, var6, 0.0F);
    }

    public void addBox(float var1, float var2, float var3, int var4, int var5, int var6, float var7)
    {
        corners = new PositionTextureVertex[8];
        faces = new Quad[6];
        float var8 = var1 + var4;
        float var9 = var2 + var5;
        float var10 = var3 + var6;
        var1 -= var7;
        var2 -= var7;
        var3 -= var7;
        var8 += var7;
        var9 += var7;
        var10 += var7;
        if (mirror)
        {
            (var8, var1) = (var1, var8);
        }

        PositionTextureVertex var20 = new(var1, var2, var3, 0.0F, 0.0F);
        PositionTextureVertex var12 = new(var8, var2, var3, 0.0F, 8.0F);
        PositionTextureVertex var13 = new(var8, var9, var3, 8.0F, 8.0F);
        PositionTextureVertex var14 = new(var1, var9, var3, 8.0F, 0.0F);
        PositionTextureVertex var15 = new(var1, var2, var10, 0.0F, 0.0F);
        PositionTextureVertex var16 = new(var8, var2, var10, 0.0F, 8.0F);
        PositionTextureVertex var17 = new(var8, var9, var10, 8.0F, 8.0F);
        PositionTextureVertex var18 = new(var1, var9, var10, 8.0F, 0.0F);
        corners[0] = var20;
        corners[1] = var12;
        corners[2] = var13;
        corners[3] = var14;
        corners[4] = var15;
        corners[5] = var16;
        corners[6] = var17;
        corners[7] = var18;
        faces[0] = new Quad([var16, var12, var13, var17], textureOffsetX + var6 + var4, textureOffsetY + var6, textureOffsetX + var6 + var4 + var6, textureOffsetY + var6 + var5);
        faces[1] = new Quad([var20, var15, var18, var14], textureOffsetX + 0, textureOffsetY + var6, textureOffsetX + var6, textureOffsetY + var6 + var5);
        faces[2] = new Quad([var16, var15, var20, var12], textureOffsetX + var6, textureOffsetY + 0, textureOffsetX + var6 + var4, textureOffsetY + var6);
        faces[3] = new Quad([var13, var14, var18, var17], textureOffsetX + var6 + var4, textureOffsetY + 0, textureOffsetX + var6 + var4 + var4, textureOffsetY + var6);
        faces[4] = new Quad([var12, var20, var14, var13], textureOffsetX + var6, textureOffsetY + var6, textureOffsetX + var6 + var4, textureOffsetY + var6 + var5);
        faces[5] = new Quad([var15, var16, var17, var18], textureOffsetX + var6 + var4 + var6, textureOffsetY + var6, textureOffsetX + var6 + var4 + var6 + var4, textureOffsetY + var6 + var5);
        if (mirror)
        {
            for (int var19 = 0; var19 < faces.Length; ++var19)
            {
                faces[var19].flipFace();
            }
        }

    }

    public void setRotationPoint(float var1, float var2, float var3)
    {
        rotationPointX = var1;
        rotationPointY = var2;
        rotationPointZ = var3;
    }

    public void render(float var1)
    {
        if (!hidden)
        {
            if (visible)
            {
                if (!compiled)
                {
                    compileDisplayList(var1);
                }

                if (rotateAngleX == 0.0F && rotateAngleY == 0.0F && rotateAngleZ == 0.0F)
                {
                    if (rotationPointX == 0.0F && rotationPointY == 0.0F && rotationPointZ == 0.0F)
                    {
                        GLManager.GL.CallList(displayList);
                    }
                    else
                    {
                        GLManager.GL.Translate(rotationPointX * var1, rotationPointY * var1, rotationPointZ * var1);
                        GLManager.GL.CallList(displayList);
                        GLManager.GL.Translate(-rotationPointX * var1, -rotationPointY * var1, -rotationPointZ * var1);
                    }
                }
                else
                {
                    GLManager.GL.PushMatrix();
                    GLManager.GL.Translate(rotationPointX * var1, rotationPointY * var1, rotationPointZ * var1);
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

    public void renderWithRotation(float var1)
    {
        if (!hidden)
        {
            if (visible)
            {
                if (!compiled)
                {
                    compileDisplayList(var1);
                }

                GLManager.GL.PushMatrix();
                GLManager.GL.Translate(rotationPointX * var1, rotationPointY * var1, rotationPointZ * var1);
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

    public void transform(float var1)
    {
        if (!hidden)
        {
            if (visible)
            {
                if (!compiled)
                {
                    compileDisplayList(var1);
                }

                if (rotateAngleX == 0.0F && rotateAngleY == 0.0F && rotateAngleZ == 0.0F)
                {
                    if (rotationPointX != 0.0F || rotationPointY != 0.0F || rotationPointZ != 0.0F)
                    {
                        GLManager.GL.Translate(rotationPointX * var1, rotationPointY * var1, rotationPointZ * var1);
                    }
                }
                else
                {
                    GLManager.GL.Translate(rotationPointX * var1, rotationPointY * var1, rotationPointZ * var1);
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

    private void compileDisplayList(float var1)
    {
        displayList = (uint)GLAllocation.generateDisplayLists(1);
        GLManager.GL.NewList(displayList, GLEnum.Compile);
        Tessellator var2 = Tessellator.instance;

        for (int var3 = 0; var3 < faces.Length; ++var3)
        {
            faces[var3].draw(var2, var1);
        }

        GLManager.GL.EndList();
        compiled = true;
    }
}
