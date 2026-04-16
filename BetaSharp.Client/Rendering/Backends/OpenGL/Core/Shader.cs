using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace BetaSharp.Client.Rendering.Core;

public class Shader : IDisposable
{
    private readonly ILogger<Shader> _logger = Log.Instance.For<Shader>();
    private readonly uint _id;
    private readonly Dictionary<string, int> _uniformLocations = [];

    public Shader(string vertexShaderSource, string fragmentShaderSource)
    {
        IGL gl = GLManager.GL;

        uint vertexShader = gl.CreateShader(ShaderType.VertexShader);
        gl.ShaderSource(vertexShader, vertexShaderSource);
        gl.CompileShader(vertexShader);
        CheckShaderCompilation(vertexShader, "Vertex");

        uint fragmentShader = gl.CreateShader(ShaderType.FragmentShader);
        gl.ShaderSource(fragmentShader, fragmentShaderSource);
        gl.CompileShader(fragmentShader);
        CheckShaderCompilation(fragmentShader, "Fragment");

        _id = gl.CreateProgram();
        gl.AttachShader(_id, vertexShader);
        gl.AttachShader(_id, fragmentShader);
        gl.LinkProgram(_id);
        CheckProgramLinking(_id);

        gl.DeleteShader(vertexShader);
        gl.DeleteShader(fragmentShader);
    }

    public void Bind()
    {
        GLManager.GL.UseProgram(_id);
    }

    public void SetUniform3(string name, Vector3D<float> vec)
    {
        int location = GetUniformLocation(name);
        GLManager.GL.Uniform3(location, vec.X, vec.Y, vec.Z);
    }

    public void SetUniform1(string name, float value)
    {
        int location = GetUniformLocation(name);
        GLManager.GL.Uniform1(location, value);
    }

    public void SetUniform1(string name, int value)
    {
        int location = GetUniformLocation(name);
        GLManager.GL.Uniform1(location, value);
    }

    public void SetUniform2(string name, float x, float y)
    {
        int location = GetUniformLocation(name);
        GLManager.GL.Uniform2(location, x, y);
    }

    public void SetUniformMatrix4(string name, Matrix4X4<float> matrix)
    {
        int location = GetUniformLocation(name);
        unsafe
        {
            GLManager.GL.UniformMatrix4(location, 1, false, (float*)&matrix);
        }
    }

    public void SetUniformMatrix4(string name, float[] matrix)
    {
        int location = GetUniformLocation(name);
        unsafe
        {
            fixed (float* mat = matrix)
            {
                GLManager.GL.UniformMatrix4(location, 1, false, mat);
            }
        }
    }

    public void SetUniform4(string name, Vector4D<float> vec)
    {
        int location = GetUniformLocation(name);
        GLManager.GL.Uniform4(location, vec.X, vec.Y, vec.Z, vec.W);
    }

    private int GetUniformLocation(string name)
    {
        if (_uniformLocations.TryGetValue(name, out int location))
        {
            return location;
        }

        location = GLManager.GL.GetUniformLocation(_id, name);
        _uniformLocations[name] = location;

        if (location == -1)
        {
            _logger.LogWarning($"Warning: Uniform '{name}' not found in shader");
        }

        return location;
    }

    private static void CheckShaderCompilation(uint shader, string type)
    {
        IGL gl = GLManager.GL;
        gl.GetShader(shader, ShaderParameterName.CompileStatus, out int success);
        if (success == 0)
        {
            string infoLog = gl.GetShaderInfoLog(shader);
            throw new Exception($"{type} shader compilation failed:\n{infoLog}");
        }
    }

    private static void CheckProgramLinking(uint program)
    {
        IGL gl = GLManager.GL;
        gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out int success);
        if (success == 0)
        {
            string infoLog = gl.GetProgramInfoLog(program);
            throw new Exception($"Shader program linking failed:\n{infoLog}");
        }
    }

    public void Dispose()
    {
        GLManager.GL.DeleteProgram(_id);
        GC.SuppressFinalize(this);
    }
}
