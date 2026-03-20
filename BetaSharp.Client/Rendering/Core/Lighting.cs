using BetaSharp.Client.Rendering.Core.OpenGL;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering.Core;

public unsafe class Lighting
{
    private static readonly float[] s_buffer = new float[4];

    public static void turnOff()
    {
        GLManager.GL.Disable(GLEnum.Lighting);
        GLManager.GL.Disable(GLEnum.Light0);
        GLManager.GL.Disable(GLEnum.Light1);
        GLManager.GL.Disable(GLEnum.ColorMaterial);
    }

    public static void turnOn()
    {
        GLManager.GL.Enable(GLEnum.Lighting);
        GLManager.GL.Enable(GLEnum.Light0);
        GLManager.GL.Enable(GLEnum.Light1);
        GLManager.GL.Enable(GLEnum.ColorMaterial);
        GLManager.GL.ColorMaterial(GLEnum.FrontAndBack, GLEnum.AmbientAndDiffuse);
        float ambientBrightness = 0.4F;
        float diffuseBrightness = 0.6F;
        float specularIntensity = 0.0F;
        Vec3D lightDir = new Vec3D((double)0.2F, 1.0D, (double)-0.7F).normalize();
        fixed (float* buf = s_buffer)
        {
            GLManager.GL.Light(GLEnum.Light0, GLEnum.Position, getBuffer(buf, lightDir.x, lightDir.y, lightDir.z, 0.0D));
            GLManager.GL.Light(GLEnum.Light0, GLEnum.Diffuse, getBuffer(buf, diffuseBrightness, diffuseBrightness, diffuseBrightness, 1.0F));
            GLManager.GL.Light(GLEnum.Light0, GLEnum.Ambient, getBuffer(buf, 0.0F, 0.0F, 0.0F, 1.0F));
            GLManager.GL.Light(GLEnum.Light0, GLEnum.Specular, getBuffer(buf, specularIntensity, specularIntensity, specularIntensity, 1.0F));
            lightDir = new Vec3D((double)-0.2F, 1.0D, (double)0.7F).normalize();
            GLManager.GL.Light(GLEnum.Light1, GLEnum.Position, getBuffer(buf, lightDir.x, lightDir.y, lightDir.z, 0.0D));
            GLManager.GL.Light(GLEnum.Light1, GLEnum.Diffuse, getBuffer(buf, diffuseBrightness, diffuseBrightness, diffuseBrightness, 1.0F));
            GLManager.GL.Light(GLEnum.Light1, GLEnum.Ambient, getBuffer(buf, 0.0F, 0.0F, 0.0F, 1.0F));
            GLManager.GL.Light(GLEnum.Light1, GLEnum.Specular, getBuffer(buf, specularIntensity, specularIntensity, specularIntensity, 1.0F));
            GLManager.GL.ShadeModel(GLEnum.Flat);
            GLManager.GL.LightModel(GLEnum.LightModelAmbient, getBuffer(buf, ambientBrightness, ambientBrightness, ambientBrightness, 1.0F));
        }
    }

    private static float* getBuffer(float* buffer, double x, double y, double z, double w)
    {
        return getBuffer(buffer, (float)x, (float)y, (float)z, (float)w);
    }

    private static float* getBuffer(float* buffer, float x, float y, float z, float w)
    {
        buffer[0] = x;
        buffer[1] = y;
        buffer[2] = z;
        buffer[3] = w;
        return buffer;
    }
}
