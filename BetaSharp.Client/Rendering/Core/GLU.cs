namespace BetaSharp.Client.Rendering.Core;

public static class GLU
{
    public static void gluPerspective(float fovY, float aspect, float zNear, float zFar)
    {
        float fH = (float)Math.Tan(fovY / 360.0 * Math.PI) * zNear;
        float fW = fH * aspect;
        GLManager.GL.Frustum(-fW, fW, -fH, fH, zNear, zFar);
    }
}
