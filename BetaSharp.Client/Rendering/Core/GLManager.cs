using BetaSharp.Client.Rendering.Core.OpenGL;
using Silk.NET.OpenGL.Legacy;

namespace BetaSharp.Client.Rendering.Core;

public class GLManager
{
    private static IGL _originalGL;
    public static IGL GL { get; private set; }

    public static void Init(GL silkGl)
    {
        _originalGL = new EmulatedGL(silkGl);
        ResetGL();
    }
    public static void ResetGL() => GL = _originalGL;

    public static void SetGL(IGL newGL) => GL = newGL;
}
