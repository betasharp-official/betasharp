using Silk.NET.Maths;

namespace BetaSharp.Client.Rendering.Core;

internal struct PositionTextureVertex
{
    public Vector3D<double> vector3D;
    public float texturePositionX;
    public float texturePositionY;

    public PositionTextureVertex(float x, float y, float z, float u, float v) : this(new Vector3D<double>(x, y, z), u, v)
    {
    }

    public PositionTextureVertex setTexturePosition(float u, float v)
    {
        return new PositionTextureVertex(this, u, v);
    }

    public PositionTextureVertex(PositionTextureVertex source, float u, float v)
    {
        vector3D = source.vector3D;
        texturePositionX = u;
        texturePositionY = v;
    }

    public PositionTextureVertex(Vector3D<double> position, float u, float v)
    {
        vector3D = position;
        texturePositionX = u;
        texturePositionY = v;
    }
}
