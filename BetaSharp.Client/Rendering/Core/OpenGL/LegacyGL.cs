using Silk.NET.OpenGL.Legacy;

namespace BetaSharp.Client.Rendering.Core.OpenGL;

public unsafe class LegacyGL : IGL
{
    public GL SilkGL { get; }

    public LegacyGL(GL gl)
    {
        SilkGL = gl;
    }

    public virtual void AlphaFunc(GLEnum func, float refValue)
    {
        SilkGL.AlphaFunc(func, refValue);
    }

    public void AttachShader(uint program, uint shader)
    {
        SilkGL.AttachShader(program, shader);
    }

    public void BindBuffer(GLEnum target, uint buffer)
    {
        SilkGL.BindBuffer(target, buffer);
    }

    public virtual void BindTexture(GLEnum target, uint texture)
    {
        SilkGL.BindTexture(target, texture);
    }

    public void BindVertexArray(uint array)
    {
        SilkGL.BindVertexArray(array);
    }

    public void BlendFunc(GLEnum sfactor, GLEnum dfactor)
    {
        SilkGL.BlendFunc(sfactor, dfactor);
    }

    public void BufferData<T0>(GLEnum target, ReadOnlySpan<T0> data, GLEnum usage) where T0 : unmanaged
    {
        SilkGL.BufferData<T0>(target, data, usage);
    }

    public virtual void BufferData(GLEnum target, nuint size, void* data, GLEnum usage)
    {
        SilkGL.BufferData(target, size, data, usage);
    }

    public virtual void CallList(uint list)
    {
        SilkGL.CallList(list);
    }

    public virtual void CallLists(uint n, GLEnum type, void* lists)
    {
        SilkGL.CallLists(n, type, lists);
    }

    public void Clear(ClearBufferMask mask)
    {
        SilkGL.Clear(mask);
    }

    public void ClearColor(float red, float green, float blue, float alpha)
    {
        SilkGL.ClearColor(red, green, blue, alpha);
    }

    public void ClearDepth(double depth)
    {
        SilkGL.ClearDepth(depth);
    }

    public virtual void Color3(float red, float green, float blue)
    {
        SilkGL.Color3(red, green, blue);
    }

    public virtual void Color3(byte red, byte green, byte blue)
    {
        SilkGL.Color3(red, green, blue);
    }

    public virtual void Color4(float red, float green, float blue, float alpha)
    {
        SilkGL.Color4(red, green, blue, alpha);
    }

    public virtual void ColorMask(bool red, bool green, bool blue, bool alpha)
    {
        SilkGL.ColorMask(red, green, blue, alpha);
    }

    public virtual void ColorMaterial(GLEnum face, GLEnum mode)
    {
        SilkGL.ColorMaterial(face, mode);
    }

    public virtual void ColorPointer(int size, ColorPointerType type, uint stride, void* pointer)
    {
        SilkGL.ColorPointer(size, type, stride, pointer);
    }

    public void CompileShader(uint shader)
    {
        SilkGL.CompileShader(shader);
    }

    public uint CreateProgram()
    {
        return SilkGL.CreateProgram();
    }

    public uint CreateShader(ShaderType type)
    {
        return SilkGL.CreateShader(type);
    }

    public void CullFace(GLEnum mode)
    {
        SilkGL.CullFace(mode);
    }

    public void DeleteBuffer(uint buffer)
    {
        SilkGL.DeleteBuffer(buffer);
    }

    public virtual void DeleteLists(uint list, uint range)
    {
        SilkGL.DeleteLists(list, range);
    }

    public void DeleteProgram(uint program)
    {
        SilkGL.DeleteProgram(program);
    }

    public void DeleteShader(uint shader)
    {
        SilkGL.DeleteShader(shader);
    }

    public void DeleteTexture(uint texture)
    {
        SilkGL.DeleteTexture(texture);
    }

    public void DeleteTextures(uint n, ReadOnlySpan<uint> textures)
    {
        SilkGL.DeleteTextures(n, textures);
    }

    public void DeleteTextures(ReadOnlySpan<uint> textures)
    {
        SilkGL.DeleteTextures(textures);
    }

    public void DeleteVertexArray(uint array)
    {
        SilkGL.DeleteVertexArray(array);
    }

    public void DepthFunc(GLEnum func)
    {
        SilkGL.DepthFunc(func);
    }

    public void DepthMask(bool flag)
    {
        SilkGL.DepthMask(flag);
    }

    public virtual void Disable(EnableCap cap)
    {
        SilkGL.Disable(cap);
    }

    public virtual void Disable(GLEnum cap)
    {
        SilkGL.Disable(cap);
    }

    public virtual void DisableClientState(GLEnum array)
    {
        SilkGL.DisableClientState(array);
    }

    public virtual void DrawArrays(GLEnum mode, int first, uint count)
    {
        SilkGL.DrawArrays(mode, first, count);
    }

    public virtual void Enable(EnableCap cap)
    {
        SilkGL.Enable(cap);
    }

    public virtual void Enable(GLEnum cap)
    {
        SilkGL.Enable(cap);
    }

    public virtual void EnableClientState(GLEnum array)
    {
        SilkGL.EnableClientState(array);
    }

    public virtual void EnableVertexAttribArray(uint index)
    {
        SilkGL.EnableVertexAttribArray(index);
    }

    public virtual void EndList()
    {
        SilkGL.EndList();
    }

    public virtual void Fog(GLEnum pname, float param)
    {
        SilkGL.Fog(pname, param);
    }

    public virtual void Fog(GLEnum pname, ReadOnlySpan<float> params_)
    {
        SilkGL.Fog(pname, params_);
    }

    public virtual void Frustum(double left, double right, double bottom, double top, double zNear, double zFar)
    {
        SilkGL.Frustum(left, right, bottom, top, zNear, zFar);
    }

    public uint GenBuffer()
    {
        return SilkGL.GenBuffer();
    }

    public void GenBuffers(uint n, Span<uint> buffers)
    {
        SilkGL.GenBuffers(n, buffers);
    }

    public void GenBuffers(Span<uint> buffers)
    {
        SilkGL.GenBuffers(buffers);
    }

    public virtual uint GenLists(uint range)
    {
        return SilkGL.GenLists(range);
    }

    public uint GenTexture()
    {
        return SilkGL.GenTexture();
    }

    public void GenTextures(Span<uint> textures)
    {
        SilkGL.GenTextures(textures);
    }

    public uint GenVertexArray()
    {
        return SilkGL.GenVertexArray();
    }

    public GLEnum GetError()
    {
        return SilkGL.GetError();
    }

    public virtual void GetFloat(GLEnum pname, Span<float> data)
    {
        SilkGL.GetFloat(pname, data);
    }

    public virtual void GetFloat(GLEnum pname, out float data)
    {
        fixed (float* ptr = &data) { SilkGL.GetFloat(pname, ptr); }
    }

    public virtual void GetFloat(GLEnum pname, float* data)
    {
        SilkGL.GetFloat(pname, data);
    }

    public virtual void GetInteger(GetPName pname, Span<int> data)
    {
        SilkGL.GetInteger(pname, data);
    }

    public void GetProgram(uint program, ProgramPropertyARB pname, out int params_)
    {
        SilkGL.GetProgram(program, pname, out params_);
    }

    public string GetProgramInfoLog(uint program)
    {
        return SilkGL.GetProgramInfoLog(program);
    }

    public void GetShader(uint shader, ShaderParameterName pname, out int params_)
    {
        SilkGL.GetShader(shader, pname, out params_);
    }

    public string GetShaderInfoLog(uint shader)
    {
        return SilkGL.GetShaderInfoLog(shader);
    }

    public int GetUniformLocation(uint program, string name)
    {
        return SilkGL.GetUniformLocation(program, name);
    }

    public virtual bool IsEnabled(EnableCap cap)
    {
        return SilkGL.IsEnabled(cap);
    }

    public bool IsExtensionPresent(string extension)
    {
        return SilkGL.IsExtensionPresent(extension);
    }

    public virtual void Light(GLEnum light, GLEnum pname, float* params_)
    {
        SilkGL.Light(light, pname, params_);
    }

    public virtual void LightModel(GLEnum pname, float* params_)
    {
        SilkGL.LightModel(pname, params_);
    }

    public virtual void LineWidth(float width)
    {
        SilkGL.LineWidth(width);
    }

    public void LinkProgram(uint program)
    {
        SilkGL.LinkProgram(program);
    }

    public virtual void LoadIdentity()
    {
        SilkGL.LoadIdentity();
    }

    public virtual void LogicOp(LogicOp opcode)
    {
        SilkGL.LogicOp(opcode);
    }

    public virtual void MatrixMode(GLEnum mode)
    {
        SilkGL.MatrixMode(mode);
    }

    public virtual void NewList(uint list, GLEnum mode)
    {
        SilkGL.NewList(list, mode);
    }

    public virtual void Normal3(float nx, float ny, float nz)
    {
        SilkGL.Normal3(nx, ny, nz);
    }

    public virtual void NormalPointer(NormalPointerType type, uint stride, void* pointer)
    {
        SilkGL.NormalPointer(type, stride, pointer);
    }

    public virtual void Ortho(double left, double right, double bottom, double top, double zNear, double zFar)
    {
        SilkGL.Ortho(left, right, bottom, top, zNear, zFar);
    }

    public void PixelStore(PixelStoreParameter pname, int param)
    {
        SilkGL.PixelStore(pname, param);
    }

    public void PolygonOffset(float factor, float units)
    {
        SilkGL.PolygonOffset(factor, units);
    }

    public virtual void PopMatrix()
    {
        SilkGL.PopMatrix();
    }

    public virtual void PushMatrix()
    {
        SilkGL.PushMatrix();
    }

    public void ReadPixels(int x, int y, uint width, uint height, PixelFormat format, PixelType type, void* pixels)
    {
        SilkGL.ReadPixels(x, y, width, height, format, type, pixels);
    }

    public virtual void Rotate(float angle, float x, float y, float z)
    {
        SilkGL.Rotate(angle, x, y, z);
    }

    public virtual void Scale(float x, float y, float z)
    {
        SilkGL.Scale(x, y, z);
    }

    public virtual void Scale(double x, double y, double z)
    {
        SilkGL.Scale(x, y, z);
    }

    public virtual void Scissor(int x, int y, uint width, uint height)
    {
        SilkGL.Scissor(x, y, width, height);
    }

    public virtual void ShadeModel(GLEnum mode)
    {
        SilkGL.ShadeModel(mode);
    }

    public void ShaderSource(uint shader, string string_)
    {
        SilkGL.ShaderSource(shader, string_);
    }

    public virtual void TexCoordPointer(int size, GLEnum type, uint stride, void* pointer)
    {
        SilkGL.TexCoordPointer(size, type, stride, pointer);
    }

    public void TexImage2D(TextureTarget target, int level, InternalFormat internalformat, uint width, uint height, int border, PixelFormat format, PixelType type, void* pixels)
    {
        SilkGL.TexImage2D(target, level, internalformat, width, height, border, format, type, pixels);
    }

    public void TexImage2D(GLEnum target, int level, int internalformat, uint width, uint height, int border, GLEnum format, GLEnum type, void* pixels)
    {
        SilkGL.TexImage2D(target, level, internalformat, width, height, border, format, type, pixels);
    }

    public void TexParameter(TextureTarget target, TextureParameterName pname, int param)
    {
        SilkGL.TexParameter(target, pname, param);
    }

    public void TexParameter(GLEnum target, GLEnum pname, int param)
    {
        SilkGL.TexParameter(target, pname, param);
    }

    public void TexParameter(GLEnum target, GLEnum pname, float param)
    {
        SilkGL.TexParameter(target, pname, param);
    }

    public void TexSubImage2D(GLEnum target, int level, int xoffset, int yoffset, uint width, uint height, GLEnum format, GLEnum type, void* pixels)
    {
        SilkGL.TexSubImage2D(target, level, xoffset, yoffset, width, height, format, type, pixels);
    }

    public virtual void Translate(float x, float y, float z)
    {
        SilkGL.Translate(x, y, z);
    }

    public void Uniform1(int location, int v0)
    {
        SilkGL.Uniform1(location, v0);
    }

    public void Uniform1(int location, float v0)
    {
        SilkGL.Uniform1(location, v0);
    }

    public void Uniform2(int location, float v0, float v1)
    {
        SilkGL.Uniform2(location, v0, v1);
    }

    public void Uniform3(int location, float v0, float v1, float v2)
    {
        SilkGL.Uniform3(location, v0, v1, v2);
    }

    public void Uniform4(int location, float v0, float v1, float v2, float v3)
    {
        SilkGL.Uniform4(location, v0, v1, v2, v3);
    }

    public void UniformMatrix4(int location, uint count, bool transpose, float* value)
    {
        SilkGL.UniformMatrix4(location, count, transpose, value);
    }

    public virtual void UseProgram(uint program)
    {
        SilkGL.UseProgram(program);
    }

    public virtual void VertexAttribIPointer(uint index, int size, GLEnum type, uint stride, void* pointer)
    {
        SilkGL.VertexAttribIPointer(index, size, type, stride, pointer);
    }

    public virtual void VertexAttribPointer(uint index, int size, GLEnum type, bool normalized, uint stride, void* pointer)
    {
        SilkGL.VertexAttribPointer(index, size, type, normalized, stride, pointer);
    }

    public virtual void VertexPointer(int size, GLEnum type, uint stride, void* pointer)
    {
        SilkGL.VertexPointer(size, type, stride, pointer);
    }

    public void Viewport(int x, int y, uint width, uint height)
    {
        SilkGL.Viewport(x, y, width, height);
    }
}
