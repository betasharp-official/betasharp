using Silk.NET.Maths;

namespace BetaSharp.Client.Rendering.Core;

public class MatrixStack
{
    private readonly Stack<Matrix4X4<float>> _stack = new();
    private Matrix4X4<float> _current = Matrix4X4<float>.Identity;

    public Matrix4X4<float> Top => _current;

    public void LoadIdentity()
    {
        _current = Matrix4X4<float>.Identity;
    }

    public void Push()
    {
        _stack.Push(_current);
    }

    public void Pop()
    {
        if (_stack.Count > 0)
        {
            _current = _stack.Pop();
        }
    }

    public void Translate(float x, float y, float z)
    {
        _current = Matrix4X4.CreateTranslation(x, y, z) * _current;
    }

    public void Scale(float x, float y, float z)
    {
        _current = Matrix4X4.CreateScale(x, y, z) * _current;
    }

    public void Rotate(float angleDeg, float x, float y, float z)
    {
        float angleRad = angleDeg * (MathF.PI / 180.0f);
        float len = MathF.Sqrt(x * x + y * y + z * z);

        if (len > 0.0001f)
        {
            x /= len;
            y /= len;
            z /= len;
            _current = Matrix4X4.CreateFromAxisAngle(new Vector3D<float>(x, y, z), angleRad) * _current;
        }
    }

    public void Ortho(double left, double right, double bottom, double top, double zNear, double zFar)
    {
        _current *= Matrix4X4.CreateOrthographicOffCenter((float)left, (float)right, (float)bottom, (float)top,
            (float)zNear, (float)zFar);
    }

    public void Frustum(double left, double right, double bottom, double top, double zNear, double zFar)
    {
        _current *= Matrix4X4.CreatePerspectiveOffCenter((float)left, (float)right, (float)bottom, (float)top,
            (float)zNear, (float)zFar);
    }
}
