using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Silk.NET.OpenGL;

namespace BetaSharp.Client.Rendering.Core.OpenGL;

internal class GLErrorHandler
{
    private readonly ILogger _logger = Log.Instance.For<GLErrorHandler>();
    private readonly DebugProc _debugProcCallback;

    public unsafe GLErrorHandler()
    {
        GL gl = GL.GetApi(Display.getWindow());

        _debugProcCallback = DebugCallback;

        gl.Enable(EnableCap.DebugOutput);
        gl.Enable(EnableCap.DebugOutputSynchronous);
        gl.DebugMessageCallback(_debugProcCallback, (void*)0);
        gl.DebugMessageControl(
            DebugSource.DontCare,
            DebugType.DontCare,
            DebugSeverity.DontCare,
            0, (uint*)0, true);
    }

    private void DebugCallback(
        Silk.NET.OpenGL.GLEnum source,
        Silk.NET.OpenGL.GLEnum type,
        int id,
        Silk.NET.OpenGL.GLEnum severity,
        int length,
        nint message,
        nint _)
    {
        if (severity == Silk.NET.OpenGL.GLEnum.DebugSeverityNotification) return;

        string msg = Marshal.PtrToStringAnsi(message, length) ?? "(null)";

        LogLevel logLevel = type == Silk.NET.OpenGL.GLEnum.DebugTypeError
            ? LogLevel.Error
            : LogLevel.Warning;

        _logger.Log(logLevel,
            "[GL] [{Severity}] [{Source}] [{Type}] (id={Id}): {Message}",
            severity, source, type, id, msg);

        Debugger.Break();
    }
}
