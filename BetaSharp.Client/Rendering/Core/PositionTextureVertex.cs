using Silk.NET.Maths;

namespace BetaSharp.Client.Rendering.Core;

internal struct PositionTextureVertex
{
    public Vector3D<double> vector3D;
    public float texturePositionX;
    public float texturePositionY;

    public PositionTextureVertex(float var1, float var2, float var3, float var4, float var5) : this(
        new Vector3D<double>(var1, var2, var3), var4, var5)
    {
    }

    public readonly PositionTextureVertex setTexturePosition(float var1, float var2)
    {
        return new PositionTextureVertex(this, var1, var2);
    }

    public PositionTextureVertex(PositionTextureVertex var1, float var2, float var3)
    {
        vector3D = var1.vector3D;
        texturePositionX = var2;
        texturePositionY = var3;
    }

    public PositionTextureVertex(Vector3D<double> var1, float var2, float var3)
    {
        vector3D = var1;
        texturePositionX = var2;
        texturePositionY = var3;
    }
}
