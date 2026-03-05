using BetaSharp.Client.Rendering.Core.OpenGL;
using Silk.NET.OpenGL;

namespace BetaSharp.Client.Rendering.Core;

public class GLManager
{
    public static IGL GL { get; private set; }

    public static void Init(GL silkGl)
    {
        GL = new EmulatedGL(silkGl);
    }
}
