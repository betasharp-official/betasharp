using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace BetaSharp.Client.Rendering.Core.OpenGL;

public class FixedFunctionShader
{
    private readonly GL _gl;
    private readonly int _uModelView;
    private readonly int _uProjection;
    private readonly int _uTextureMatrix;
    private readonly int _uUseTexture;
    private readonly int _uTexture0;
    private readonly int _uAlphaThreshold;
    private readonly int _uShadeModel;

    private readonly int _uEnableLighting;
    private readonly int _uLight0Dir;
    private readonly int _uLight0Diffuse;
    private readonly int _uLight1Dir;
    private readonly int _uLight1Diffuse;
    private readonly int _uAmbientLight;
    private readonly int _uNormalMatrix;

    private readonly int _uEnableFog;
    private readonly int _uFogMode; // 0=linear, 1=exp
    private readonly int _uFogColor;
    private readonly int _uFogStart;
    private readonly int _uFogEnd;
    private readonly int _uFogDensity;

    public uint Program { get; }

    private const string VertexShaderSource = @"
#version 330 core
layout (location = 0) in vec3 a_Position;
layout (location = 1) in vec4 a_Color;
layout (location = 2) in vec2 a_TexCoord;
layout (location = 3) in vec3 a_Normal;

uniform mat4 u_ModelView;
uniform mat4 u_Projection;
uniform mat4 u_TextureMatrix;
uniform mat3 u_NormalMatrix;
uniform int u_EnableLighting;
uniform vec3 u_Light0Dir;
uniform vec3 u_Light0Diffuse;
uniform vec3 u_Light1Dir;
uniform vec3 u_Light1Diffuse;
uniform vec3 u_AmbientLight;

flat out vec4 v_ColorFlat;
out vec4 v_ColorSmooth;
out vec2 v_TexCoord;
out float v_FogDist;

void main()
{
    vec4 tex = u_TextureMatrix * vec4(a_TexCoord, 0.0, 1.0);
    v_TexCoord = tex.xy;

    vec4 viewPos = u_ModelView * vec4(a_Position, 1.0);
    gl_Position = u_Projection * viewPos;
    v_FogDist = length(viewPos.xyz);

    vec4 finalColor;
    if (u_EnableLighting != 0)
    {
        vec3 normal = normalize(u_NormalMatrix * a_Normal);
        float diff0 = max(dot(normal, u_Light0Dir), 0.0);
        float diff1 = max(dot(normal, u_Light1Dir), 0.0);
        vec3 lighting = u_AmbientLight
                      + diff0 * u_Light0Diffuse
                      + diff1 * u_Light1Diffuse;
        finalColor = vec4(clamp(a_Color.rgb * lighting, 0.0, 1.0), a_Color.a);
    }
    else
    {
        finalColor = a_Color;
    }
    
    v_ColorFlat = finalColor;
    v_ColorSmooth = finalColor;
}
";

    private const string FragmentShaderSource = @"
#version 330 core
flat in vec4 v_ColorFlat;
in vec4 v_ColorSmooth;
in vec2 v_TexCoord;
in float v_FogDist;

uniform sampler2D u_Texture0;
uniform int u_UseTexture;
uniform float u_AlphaThreshold;
uniform int u_ShadeModel;

uniform int u_EnableFog;
uniform int u_FogMode;      // 0=linear, 1=exp
uniform vec4 u_FogColor;
uniform float u_FogStart;
uniform float u_FogEnd;
uniform float u_FogDensity;

out vec4 FragColor;

void main()
{
    vec4 finalColor = u_ShadeModel == 1 ? v_ColorSmooth : v_ColorFlat;
    vec4 texColor = vec4(1.0);
    if (u_UseTexture != 0)
    {
        texColor = texture(u_Texture0, v_TexCoord);
    }
    FragColor = finalColor * texColor;

    if (FragColor.a < u_AlphaThreshold)
        discard;

    if (u_EnableFog != 0)
    {
        float fogFactor;
        if (u_FogMode == 0)
        {
            // Linear
            fogFactor = clamp((u_FogEnd - v_FogDist) / (u_FogEnd - u_FogStart), 0.0, 1.0);
        }
        else
        {
            // Exponential
            fogFactor = clamp(exp(-u_FogDensity * v_FogDist), 0.0, 1.0);
        }
        FragColor = mix(u_FogColor, FragColor, fogFactor);
    }
}";

    public FixedFunctionShader(GL gl)
    {
        _gl = gl;

        uint vertexShader = CompileShader(ShaderType.VertexShader, VertexShaderSource);
        uint fragmentShader = CompileShader(ShaderType.FragmentShader, FragmentShaderSource);

        Program = _gl.CreateProgram();
        _gl.AttachShader(Program, vertexShader);
        _gl.AttachShader(Program, fragmentShader);
        _gl.LinkProgram(Program);

        _gl.GetProgram(Program, Silk.NET.OpenGL.GLEnum.LinkStatus, out int status);
        if (status == 0)
        {
            string infoLog = _gl.GetProgramInfoLog(Program);
            throw new Exception($"Program linking failed: {infoLog}");
        }

        _gl.DetachShader(Program, vertexShader);
        _gl.DetachShader(Program, fragmentShader);
        _gl.DeleteShader(vertexShader);
        _gl.DeleteShader(fragmentShader);

        _uModelView = _gl.GetUniformLocation(Program, "u_ModelView");
        _uProjection = _gl.GetUniformLocation(Program, "u_Projection");
        _uTextureMatrix = _gl.GetUniformLocation(Program, "u_TextureMatrix");
        _uUseTexture = _gl.GetUniformLocation(Program, "u_UseTexture");
        _uTexture0 = _gl.GetUniformLocation(Program, "u_Texture0");
        _uAlphaThreshold = _gl.GetUniformLocation(Program, "u_AlphaThreshold");
        _uShadeModel = _gl.GetUniformLocation(Program, "u_ShadeModel");
        _uEnableLighting = _gl.GetUniformLocation(Program, "u_EnableLighting");
        _uLight0Dir = _gl.GetUniformLocation(Program, "u_Light0Dir");
        _uLight0Diffuse = _gl.GetUniformLocation(Program, "u_Light0Diffuse");
        _uLight1Dir = _gl.GetUniformLocation(Program, "u_Light1Dir");
        _uLight1Diffuse = _gl.GetUniformLocation(Program, "u_Light1Diffuse");
        _uAmbientLight = _gl.GetUniformLocation(Program, "u_AmbientLight");
        _uNormalMatrix = _gl.GetUniformLocation(Program, "u_NormalMatrix");
        _uEnableFog = _gl.GetUniformLocation(Program, "u_EnableFog");
        _uFogMode = _gl.GetUniformLocation(Program, "u_FogMode");
        _uFogColor = _gl.GetUniformLocation(Program, "u_FogColor");
        _uFogStart = _gl.GetUniformLocation(Program, "u_FogStart");
        _uFogEnd = _gl.GetUniformLocation(Program, "u_FogEnd");
        _uFogDensity = _gl.GetUniformLocation(Program, "u_FogDensity");
    }

    private uint CompileShader(ShaderType type, string source)
    {
        uint shader = _gl.CreateShader(type);
        _gl.ShaderSource(shader, source);
        _gl.CompileShader(shader);

        _gl.GetShader(shader, ShaderParameterName.CompileStatus, out int status);
        if (status == 0)
        {
            string infoLog = _gl.GetShaderInfoLog(shader);
            throw new Exception($"Error compiling {type}: {infoLog}");
        }

        return shader;
    }

    public void Use() => _gl.UseProgram(Program);

    public unsafe void SetModelView(Matrix4X4<float> matrix) =>
        _gl.UniformMatrix4(_uModelView, 1, false, (float*)&matrix);

    public unsafe void SetProjection(Matrix4X4<float> matrix) =>
        _gl.UniformMatrix4(_uProjection, 1, false, (float*)&matrix);

    public unsafe void SetTextureMatrix(Matrix4X4<float> matrix) =>
        _gl.UniformMatrix4(_uTextureMatrix, 1, false, (float*)&matrix);

    public void SetUseTexture(bool useTexture) =>
        _gl.Uniform1(_uUseTexture, useTexture ? 1 : 0);

    public void SetTexture0(int unit) =>
        _gl.Uniform1(_uTexture0, unit);

    public void SetAlphaThreshold(float threshold) =>
        _gl.Uniform1(_uAlphaThreshold, threshold);

    public void SetShadeModel(int mode) =>
        _gl.Uniform1(_uShadeModel, mode);

    public void SetEnableLighting(bool enable) =>
        _gl.Uniform1(_uEnableLighting, enable ? 1 : 0);

    public void SetLight0(float dirX, float dirY, float dirZ, float diffR, float diffG, float diffB)
    {
        _gl.Uniform3(_uLight0Dir, dirX, dirY, dirZ);
        _gl.Uniform3(_uLight0Diffuse, diffR, diffG, diffB);
    }

    public void SetLight1(float dirX, float dirY, float dirZ, float diffR, float diffG, float diffB)
    {
        _gl.Uniform3(_uLight1Dir, dirX, dirY, dirZ);
        _gl.Uniform3(_uLight1Diffuse, diffR, diffG, diffB);
    }

    public void SetAmbientLight(float r, float g, float b) =>
        _gl.Uniform3(_uAmbientLight, r, g, b);

    public unsafe void SetNormalMatrix(Matrix3X3<float> matrix) =>
        _gl.UniformMatrix3(_uNormalMatrix, 1, false, (float*)&matrix);

    public void SetEnableFog(bool enable) =>
        _gl.Uniform1(_uEnableFog, enable ? 1 : 0);

    public void SetFogMode(int mode) =>
        _gl.Uniform1(_uFogMode, mode);

    public void SetFogColor(float r, float g, float b, float a) =>
        _gl.Uniform4(_uFogColor, r, g, b, a);

    public void SetFogStart(float start) =>
        _gl.Uniform1(_uFogStart, start);

    public void SetFogEnd(float end) =>
        _gl.Uniform1(_uFogEnd, end);

    public void SetFogDensity(float density) =>
        _gl.Uniform1(_uFogDensity, density);
}
