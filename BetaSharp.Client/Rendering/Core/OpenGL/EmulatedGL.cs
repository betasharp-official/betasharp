#pragma warning disable CS0612, CS0618 // Suppress obsolete GLEnum warnings for legacy compat
using System.Numerics;
using System.Runtime.InteropServices;
using BetaSharp.Client.Rendering.Core.VkBackend;
using Silk.NET.OpenGL.Legacy;
using Vuldrid;
using PixelFormat = Silk.NET.OpenGL.Legacy.PixelFormat;
using VkBuffer = Vuldrid.DeviceBuffer;
using VkDevice = Vuldrid.GraphicsDevice;
using VkTexture = Vuldrid.Texture;

namespace BetaSharp.Client.Rendering.Core.OpenGL;

/// <summary>
/// Emulates legacy OpenGL fixed-function pipeline calls on top of Vuldrid (Vulkan).
/// Maintains the same IGL interface for compatibility with the rest of the codebase.
/// </summary>
public unsafe class EmulatedGL : IGL
{
    private readonly VkDevice _device;
    private readonly DisplayListCompiler _displayListCompiler;

    // Matrix stacks
    private readonly MatrixStack _modelViewStack = new();
    private readonly MatrixStack _projectionStack = new();
    private readonly MatrixStack _textureStack = new();
    private MatrixStack _activeStack;

    // Current state
    private Vector4 _currentColor = new(1, 1, 1, 1);
    private Vector3 _currentNormal = new(0, 0, 1);
    private bool _blendEnabled;
    private bool _depthTestEnabled = true;
    private bool _depthWriteEnabled = true;
    private bool _alphaTestEnabled;
    private bool _fogEnabled;
    private bool _lightingEnabled;
    private bool _cullFaceEnabled;
    private bool _colorMaterialEnabled;
    private FaceCullMode _cullMode = FaceCullMode.Back;
    private ComparisonKind _depthFunc = ComparisonKind.LessEqual;
    private GLEnum _srcBlend = GLEnum.One;
    private GLEnum _dstBlend = GLEnum.Zero;
    private float _alphaThreshold = 0.1f;
    private int _shadeModel = 1; // 0 = flat, 1 = smooth
    private Vector4 _clearColor = Vector4.Zero;

    // Fog state
    private int _fogMode; // 0 = linear, 1 = exp
    private float _fogStart;
    private float _fogEnd = 1.0f;
    private float _fogDensity = 1.0f;
    private Vector4 _fogColor = Vector4.Zero;

    // Lighting state
    private Vector3 _light0Dir;
    private Vector3 _light0Diffuse = Vector3.One;
    private Vector3 _light1Dir;
    private Vector3 _light1Diffuse = Vector3.One;
    private Vector3 _ambientLight = new(0.2f, 0.2f, 0.2f);

    // Texture binding
    private readonly Dictionary<uint, (TextureView View, Vuldrid.Sampler Sampler)> _textureRegistry = [];
    private uint _nextTextureId = 1;

    // Buffer binding
    private uint _boundArrayBuffer;
    private readonly Dictionary<uint, VkBuffer> _bufferRegistry = [];
    private uint _nextBufferId = 1;

    // VAO tracking (mostly no-ops in Vuldrid, but we track IDs)
    private uint _nextVaoId = 1;

    // Viewport
    private int _viewportX, _viewportY;
    private uint _viewportW, _viewportH;

    // Uniform buffer (updated before each draw)
    private FixedFunctionUniforms _uniforms;

    // Staging vertex buffer for dynamic draws
    private VkBuffer? _stagingVertexBuffer;
    private uint _stagingVertexBufferSize;

    // Shader program tracking (stubs for code that creates GL shaders — will be no-ops)
    private uint _nextProgramId = 1;
    private uint _nextShaderId = 1;

    // PolygonOffset state
    private float _polygonOffsetFactor;
    private float _polygonOffsetUnits;
    private bool _polygonOffsetEnabled;

    // Color mask
    private bool _colorMaskR = true, _colorMaskG = true, _colorMaskB = true, _colorMaskA = true;

    public FixedFunctionPipeline Pipeline { get; }

    public EmulatedGL(VkDevice device)
    {
        _device = device;
        Pipeline = new FixedFunctionPipeline(device);
        _displayListCompiler = new DisplayListCompiler(this);
        _activeStack = _modelViewStack;
    }

    // ========================================================================
    // Matrix operations
    // ========================================================================

    public void MatrixMode(GLEnum mode)
    {
        _activeStack = mode switch
        {
            GLEnum.Modelview => _modelViewStack,
            GLEnum.Projection => _projectionStack,
            GLEnum.Texture => _textureStack,
            _ => _activeStack,
        };
    }

    public void LoadIdentity() => _activeStack.LoadIdentity();
    public void PushMatrix() => _activeStack.Push();
    public void PopMatrix() => _activeStack.Pop();

    public void Translate(float x, float y, float z)
        => _activeStack.Multiply(Matrix4x4.CreateTranslation(x, y, z));

    public void Rotate(float angle, float x, float y, float z)
    {
        float radians = angle * MathF.PI / 180.0f;
        _activeStack.Multiply(Matrix4x4.CreateFromAxisAngle(new Vector3(x, y, z), radians));
    }

    public void Scale(float x, float y, float z)
        => _activeStack.Multiply(Matrix4x4.CreateScale(x, y, z));

    public void Scale(double x, double y, double z)
        => Scale((float)x, (float)y, (float)z);

    public void Ortho(double left, double right, double bottom, double top, double zNear, double zFar)
    {
        _activeStack.Multiply(Matrix4x4.CreateOrthographicOffCenter(
            (float)left, (float)right, (float)bottom, (float)top, (float)zNear, (float)zFar));
    }

    public void Frustum(double left, double right, double bottom, double top, double zNear, double zFar)
    {
        _activeStack.Multiply(Matrix4x4.CreatePerspectiveOffCenter(
            (float)left, (float)right, (float)bottom, (float)top, (float)zNear, (float)zFar));
    }

    // ========================================================================
    // Color / Normal state
    // ========================================================================

    public int CurrentPackedColor { get; private set; } = unchecked((int)0xFFFFFFFF);

    public void Color3(float r, float g, float b)
    {
        _currentColor = new Vector4(r, g, b, 1.0f);
        UpdateCurrentPackedColor();
    }
    public void Color3(byte r, byte g, byte b) => Color3(r / 255.0f, g / 255.0f, b / 255.0f);
    public void Color4(float r, float g, float b, float a)
    {
        _currentColor = new Vector4(r, g, b, a);
        UpdateCurrentPackedColor();
    }

    private void UpdateCurrentPackedColor()
    {
        int r = (int)(_currentColor.X * 255f);
        int g = (int)(_currentColor.Y * 255f);
        int b = (int)(_currentColor.Z * 255f);
        int a = (int)(_currentColor.W * 255f);
        r = Math.Clamp(r, 0, 255);
        g = Math.Clamp(g, 0, 255);
        b = Math.Clamp(b, 0, 255);
        a = Math.Clamp(a, 0, 255);
        CurrentPackedColor = a << 24 | b << 16 | g << 8 | r;
    }

    public void Normal3(float nx, float ny, float nz) => _currentNormal = new Vector3(nx, ny, nz);

    // ========================================================================
    // Enable / Disable
    // ========================================================================

    public void Enable(GLEnum cap) => SetCap(cap, true);
    public void Disable(GLEnum cap) => SetCap(cap, false);
    public void Disable(EnableCap cap) => SetCap((GLEnum)cap, false);

    private void SetCap(GLEnum cap, bool enabled)
    {
        switch (cap)
        {
            case GLEnum.Texture2D: IsTextureEnabled = enabled; break;
            case GLEnum.Blend: _blendEnabled = enabled; break;
            case GLEnum.DepthTest: _depthTestEnabled = enabled; break;
            case GLEnum.AlphaTest: _alphaTestEnabled = enabled; break;
            case GLEnum.Fog: _fogEnabled = enabled; break;
            case GLEnum.Lighting: _lightingEnabled = enabled; break;
            case GLEnum.CullFace: _cullFaceEnabled = enabled; break;
            case GLEnum.ColorMaterial: _colorMaterialEnabled = enabled; break;
            case GLEnum.PolygonOffsetFill: _polygonOffsetEnabled = enabled; break;
            case GLEnum.Light0: break; // Tracked via lighting enable
            case GLEnum.Light1: break;
            case GLEnum.Multisample: break; // Not supported in our pipeline
            // Ignored caps
            default: break;
        }
    }

    // ========================================================================
    // Blend, Depth, Alpha
    // ========================================================================

    public void BlendFunc(GLEnum sfactor, GLEnum dfactor)
    {
        _srcBlend = sfactor;
        _dstBlend = dfactor;
    }

    public void DepthFunc(GLEnum func)
    {
        _depthFunc = func switch
        {
            GLEnum.Less => ComparisonKind.Less,
            GLEnum.Lequal => ComparisonKind.LessEqual,
            GLEnum.Greater => ComparisonKind.Greater,
            GLEnum.Gequal => ComparisonKind.GreaterEqual,
            GLEnum.Equal => ComparisonKind.Equal,
            GLEnum.Notequal => ComparisonKind.NotEqual,
            GLEnum.Always => ComparisonKind.Always,
            GLEnum.Never => ComparisonKind.Never,
            _ => ComparisonKind.LessEqual,
        };
    }

    public void DepthMask(bool flag) => _depthWriteEnabled = flag;

    public void AlphaFunc(GLEnum func, float refValue)
    {
        _alphaThreshold = refValue;
    }

    public void ColorMask(bool r, bool g, bool b, bool a)
    {
        _colorMaskR = r; _colorMaskG = g; _colorMaskB = b; _colorMaskA = a;
        // TODO: Implement via pipeline color write mask
    }

    public void CullFace(GLEnum mode)
    {
        _cullMode = mode switch
        {
            GLEnum.Front => FaceCullMode.Front,
            GLEnum.Back => FaceCullMode.Back,
            _ => FaceCullMode.None
        };
    }

    public void ShadeModel(GLEnum mode)
    {
        _shadeModel = mode == GLEnum.Smooth ? 1 : 0;
    }

    public void PolygonOffset(float factor, float units)
    {
        _polygonOffsetFactor = factor;
        _polygonOffsetUnits = units;
    }

    public void LineWidth(float width) { /* Vulkan doesn't support line width > 1 on most GPUs */ }

    // ========================================================================
    // Fog
    // ========================================================================

    public void Fog(GLEnum pname, float param)
    {
        switch (pname)
        {
            case GLEnum.FogMode:
                _fogMode = (int)param == (int)GLEnum.Linear ? 0 : 1;
                break;
            case GLEnum.FogStart: _fogStart = param; break;
            case GLEnum.FogEnd: _fogEnd = param; break;
            case GLEnum.FogDensity: _fogDensity = param; break;
        }
    }

    public void Fog(GLEnum pname, ReadOnlySpan<float> params_)
    {
        if (pname == GLEnum.FogColor && params_.Length >= 4)
        {
            _fogColor = new Vector4(params_[0], params_[1], params_[2], params_[3]);
        }
        else if (params_.Length >= 1)
        {
            Fog(pname, params_[0]);
        }
    }

    // ========================================================================
    // Lighting
    // ========================================================================

    public void Light(GLEnum light, GLEnum pname, float* params_)
    {
        switch (pname)
        {
            case GLEnum.Position:
                {
                    Vector4 pos = new(params_[0], params_[1], params_[2], params_[3]);
                    Vector3 dir = Vector3.Normalize(new Vector3(pos.X, pos.Y, pos.Z));
                    if (light == GLEnum.Light0) _light0Dir = dir;
                    else if (light == GLEnum.Light1) _light1Dir = dir;
                    break;
                }
            case GLEnum.Diffuse:
                {
                    Vector3 diff = new(params_[0], params_[1], params_[2]);
                    if (light == GLEnum.Light0) _light0Diffuse = diff;
                    else if (light == GLEnum.Light1) _light1Diffuse = diff;
                    break;
                }
        }
    }

    public void LightModel(GLEnum pname, float* params_)
    {
        if (pname == GLEnum.LightModelAmbient)
        {
            _ambientLight = new Vector3(params_[0], params_[1], params_[2]);
        }
    }

    public void ColorMaterial(GLEnum face, GLEnum mode) { /* tracked via _colorMaterialEnabled */ }

    // ========================================================================
    // Clear operations
    // ========================================================================

    public void ClearColor(float r, float g, float b, float a)
    {
        _clearColor = new Vector4(r, g, b, a);
    }

    public void ClearDepth(double depth) { /* Handled by Vuldrid clear */ }

    public void Clear(ClearBufferMask mask)
    {
        CommandList cl = GLManager.CommandList;
        if ((mask & ClearBufferMask.ColorBufferBit) != 0)
        {
            cl.ClearColorTarget(0, new global::Vuldrid.RgbaFloat(_clearColor.X, _clearColor.Y, _clearColor.Z, _clearColor.W));
        }
        if ((mask & ClearBufferMask.DepthBufferBit) != 0)
        {
            cl.ClearDepthStencil(1.0f);
        }
    }

    // ========================================================================
    // Viewport
    // ========================================================================

    public void Viewport(int x, int y, uint width, uint height)
    {
        _viewportX = x; _viewportY = y;
        _viewportW = width; _viewportH = height;
        CommandList cl = GLManager.CommandList;
        cl.SetViewport(0, new global::Vuldrid.Viewport(x, y, width, height, 0, 1));
        cl.SetScissorRect(0, (uint)x, (uint)y, width, height);
    }

    // ========================================================================
    // Texture management
    // ========================================================================

    public uint GenTexture() => _nextTextureId++;
    public void GenTextures(Span<uint> textures)
    {
        for (int i = 0; i < textures.Length; i++)
            textures[i] = _nextTextureId++;
    }

    public void BindTexture(GLEnum target, uint texture)
    {
        BoundTextureId = texture;
    }

    public void DeleteTexture(uint texture)
    {
        if (_textureRegistry.Remove(texture, out (TextureView View, Vuldrid.Sampler Sampler) entry))
        {
            entry.View.Dispose();
        }
    }

    public void DeleteTextures(uint n, ReadOnlySpan<uint> textures)
    {
        for (int i = 0; i < (int)n && i < textures.Length; i++)
            DeleteTexture(textures[i]);
    }

    public void DeleteTextures(ReadOnlySpan<uint> textures)
    {
        for (int i = 0; i < textures.Length; i++)
            DeleteTexture(textures[i]);
    }

    public void TexParameter(TextureTarget target, TextureParameterName pname, int param) { /* Handled via Sampler in Vuldrid */ }
    public void TexParameter(GLEnum target, GLEnum pname, int param) { /* Handled via Sampler in Vuldrid */ }
    public void TexParameter(GLEnum target, GLEnum pname, float param) { /* Handled via Sampler in Vuldrid */ }
    public void PixelStore(PixelStoreParameter pname, int param) { /* Not needed for Vuldrid */ }

    public void TexImage2D(TextureTarget target, int level, InternalFormat internalformat,
        uint width, uint height, int border, PixelFormat format, PixelType type, void* pixels)
    {
        TexImage2DImpl(level, width, height, pixels);
    }

    public void TexImage2D(GLEnum target, int level, int internalformat,
        uint width, uint height, int border, GLEnum format, GLEnum type, void* pixels)
    {
        TexImage2DImpl(level, width, height, pixels);
    }

    private void TexImage2DImpl(int level, uint width, uint height, void* pixels)
    {
        if (BoundTextureId == 0) return;

        // Create or recreate the texture for this ID
        if (_textureRegistry.TryGetValue(BoundTextureId, out (TextureView View, Vuldrid.Sampler Sampler) existing))
        {
            existing.View.Dispose();
        }

        uint mipLevels = (uint)(level + 1);
        // If level 0, calculate max mip levels for future uploads
        if (level == 0)
        {
            mipLevels = 1 + (uint)Math.Floor(Math.Log2(Math.Max(width, height)));
        }

        // Only create new texture on level 0
        VkTexture tex;
        if (level == 0)
        {
            tex = _device.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
                width, height, mipLevels, 1,
                global::Vuldrid.PixelFormat.R8_G8_B8_A8_UNorm,
                global::Vuldrid.TextureUsage.Sampled));

            if (pixels != null)
            {
                uint size = width * height * 4;
                _device.UpdateTexture(tex, (IntPtr)pixels, size, 0, 0, 0, width, height, 1, (uint)level, 0);
            }

            TextureView view = _device.ResourceFactory.CreateTextureView(tex);
            Vuldrid.Sampler sampler = Pipeline.DefaultSampler; // Use default sampler; filter is set separately

            _textureRegistry[BoundTextureId] = (view, sampler);
        }
        else
        {
            // Mip level upload to existing texture
            if (_textureRegistry.TryGetValue(BoundTextureId, out (TextureView View, Vuldrid.Sampler Sampler) entry) && pixels != null)
            {
                tex = entry.View.Target;
                uint mipWidth = Math.Max(1, tex.Width >> level);
                uint mipHeight = Math.Max(1, tex.Height >> level);
                uint size = mipWidth * mipHeight * 4;
                _device.UpdateTexture(tex, (IntPtr)pixels, size, 0, 0, 0, mipWidth, mipHeight, 1, (uint)level, 0);
            }
        }
    }

    public void TexSubImage2D(GLEnum target, int level, int xoffset, int yoffset, uint width, uint height, GLEnum format, GLEnum type, void* pixels)
    {
        if (BoundTextureId == 0 || pixels == null) return;
        if (!_textureRegistry.TryGetValue(BoundTextureId, out (TextureView View, Vuldrid.Sampler Sampler) entry)) return;

        VkTexture tex = entry.View.Target;
        uint size = width * height * 4;
        _device.UpdateTexture(tex, (IntPtr)pixels, size, (uint)xoffset, (uint)yoffset, 0, width, height, 1, (uint)level, 0);
    }

    /// <summary>
    /// Register an externally created Vuldrid TextureView + Sampler for a texture ID.
    /// Used by GLTexture when it creates textures directly.
    /// </summary>
    public void RegisterTexture(uint id, global::Vuldrid.TextureView view, global::Vuldrid.Sampler sampler)
    {
        _textureRegistry[id] = (view, sampler);
    }

    public bool TryGetTexture(uint id, out global::Vuldrid.TextureView? view, out global::Vuldrid.Sampler? sampler)
    {
        if (_textureRegistry.TryGetValue(id, out (TextureView View, Vuldrid.Sampler Sampler) entry))
        {
            view = entry.View;
            sampler = entry.Sampler;
            return true;
        }
        view = null;
        sampler = null;
        return false;
    }

    // ========================================================================
    // Buffer management
    // ========================================================================

    public uint GenBuffer() => _nextBufferId++;
    public void GenBuffers(uint n, Span<uint> buffers)
    {
        for (int i = 0; i < (int)n; i++) buffers[i] = _nextBufferId++;
    }
    public void GenBuffers(Span<uint> buffers)
    {
        for (int i = 0; i < buffers.Length; i++) buffers[i] = _nextBufferId++;
    }

    public void BindBuffer(GLEnum target, uint buffer) => _boundArrayBuffer = buffer;

    public void BufferData<T0>(GLEnum target, ReadOnlySpan<T0> data, GLEnum usage) where T0 : unmanaged
    {
        if (_boundArrayBuffer == 0) return;

        uint size = (uint)(data.Length * sizeof(T0));
        if (_bufferRegistry.TryGetValue(_boundArrayBuffer, out VkBuffer? existing))
        {
            if (existing.SizeInBytes < size)
            {
                existing.Dispose();
                _bufferRegistry.Remove(_boundArrayBuffer);
            }
            else
            {
                fixed (T0* ptr = data)
                {
                    _device.UpdateBuffer(existing, 0, (IntPtr)ptr, size);
                }
                return;
            }
        }

        VkBuffer buffer = _device.ResourceFactory.CreateBuffer(new BufferDescription(
            size, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        fixed (T0* ptr = data)
        {
            _device.UpdateBuffer(buffer, 0, (IntPtr)ptr, size);
        }
        _bufferRegistry[_boundArrayBuffer] = buffer;
    }

    public void BufferData(GLEnum target, nuint size, void* data, GLEnum usage)
    {
        if (_boundArrayBuffer == 0) return;

        if (_bufferRegistry.TryGetValue(_boundArrayBuffer, out VkBuffer? existing))
        {
            existing.Dispose();
            _bufferRegistry.Remove(_boundArrayBuffer);
        }

        VkBuffer buffer = _device.ResourceFactory.CreateBuffer(new BufferDescription(
            (uint)size, BufferUsage.VertexBuffer | BufferUsage.Dynamic));

        if (data != null)
        {
            _device.UpdateBuffer(buffer, 0, (IntPtr)data, (uint)size);
        }
        _bufferRegistry[_boundArrayBuffer] = buffer;
    }

    public void DeleteBuffer(uint buffer)
    {
        if (_bufferRegistry.Remove(buffer, out VkBuffer? existing))
        {
            existing.Dispose();
        }
    }

    // ========================================================================
    // VAO (mostly stubs — Vuldrid doesn't use VAOs)
    // ========================================================================

    public uint GenVertexArray() => _nextVaoId++;
    public void BindVertexArray(uint array) { /* No-op in Vuldrid */ }
    public void DeleteVertexArray(uint array) { /* No-op */ }
    public void EnableVertexAttribArray(uint index) { /* No-op — handled by pipeline */ }
    public void VertexAttribPointer(uint index, int size, GLEnum type, bool normalized, uint stride, void* pointer) { }
    public void VertexAttribIPointer(uint index, int size, GLEnum type, uint stride, void* pointer) { }

    // ========================================================================
    // Legacy vertex pointers (used by Tessellator)
    // ========================================================================

    public void VertexPointer(int size, GLEnum type, uint stride, void* pointer) { /* State tracked by Tessellator */ }
    public void ColorPointer(int size, ColorPointerType type, uint stride, void* pointer) { /* State tracked by Tessellator */ }
    public void TexCoordPointer(int size, GLEnum type, uint stride, void* pointer) { /* State tracked by Tessellator */ }
    public void NormalPointer(NormalPointerType type, uint stride, void* pointer) { /* State tracked by Tessellator */ }
    public void EnableClientState(GLEnum array) { /* No-op */ }
    public void DisableClientState(GLEnum array) { /* No-op */ }

    // ========================================================================
    // Draw calls
    // ========================================================================

    public void DrawArrays(GLEnum mode, int first, uint count)
    {
        if (count == 0) return;

        // Check if we're recording a display list
        if (_displayListCompiler.IsRecording)
        {
            // Display list recording will handle this
            return;
        }

        FlushState();

        PrimitiveTopology topology = FixedFunctionPipeline.GLEnumToTopology(mode);
        PipelineStateKey stateKey = BuildPipelineStateKey(topology);
        Pipeline pipeline = Pipeline.GetOrCreatePipeline(stateKey);

        CommandList cl = GLManager.CommandList;
        cl.SetPipeline(pipeline);
        cl.SetGraphicsResourceSet(0, Pipeline.UniformResourceSet);

        // Set texture resource set
        if (IsTextureEnabled && BoundTextureId != 0 && _textureRegistry.TryGetValue(BoundTextureId, out (TextureView View, Vuldrid.Sampler Sampler) texEntry))
        {
            ResourceSet texRS = Pipeline.GetOrCreateTextureResourceSet(texEntry.View, texEntry.Sampler);
            cl.SetGraphicsResourceSet(1, texRS);
        }
        else
        {
            cl.SetGraphicsResourceSet(1, Pipeline.GetDefaultTextureResourceSet());
        }

        // Bind vertex buffer
        if (_boundArrayBuffer != 0 && _bufferRegistry.TryGetValue(_boundArrayBuffer, out VkBuffer? vbo))
        {
            cl.SetVertexBuffer(0, vbo);
        }

        cl.Draw(count, 1, (uint)first, 0);
    }

    /// <summary>
    /// Draw with an externally-provided vertex buffer (used by Tessellator and DisplayListCompiler).
    /// </summary>
    public void DrawWithBuffer(GLEnum mode, VkBuffer vertexBuffer, uint vertexCount, uint firstVertex = 0)
    {
        if (vertexCount == 0) return;

        FlushState();

        PrimitiveTopology topology = FixedFunctionPipeline.GLEnumToTopology(mode);
        PipelineStateKey stateKey = BuildPipelineStateKey(topology);
        Pipeline pipeline = Pipeline.GetOrCreatePipeline(stateKey);

        CommandList cl = GLManager.CommandList;
        cl.SetPipeline(pipeline);
        cl.SetGraphicsResourceSet(0, Pipeline.UniformResourceSet);

        if (IsTextureEnabled && BoundTextureId != 0 && _textureRegistry.TryGetValue(BoundTextureId, out (TextureView View, Vuldrid.Sampler Sampler) texEntry))
        {
            ResourceSet texRS = Pipeline.GetOrCreateTextureResourceSet(texEntry.View, texEntry.Sampler);
            cl.SetGraphicsResourceSet(1, texRS);
        }
        else
        {
            cl.SetGraphicsResourceSet(1, Pipeline.GetDefaultTextureResourceSet());
        }

        cl.SetVertexBuffer(0, vertexBuffer);
        cl.Draw(vertexCount, 1, firstVertex, 0);
    }

    /// <summary>
    /// Upload raw vertex data to a staging buffer and draw immediately.
    /// Used for immediate-mode style rendering (Tessellator).
    /// </summary>
    public void DrawImmediate(GLEnum mode, ReadOnlySpan<byte> vertexData, uint vertexCount)
    {
        if (vertexCount == 0 || vertexData.Length == 0) return;

        uint requiredSize = (uint)vertexData.Length;

        // Resize staging buffer if needed
        if (_stagingVertexBuffer == null || _stagingVertexBufferSize < requiredSize)
        {
            _stagingVertexBuffer?.Dispose();
            _stagingVertexBufferSize = Math.Max(requiredSize, 65536); // At least 64KB
            _stagingVertexBuffer = _device.ResourceFactory.CreateBuffer(new BufferDescription(
                _stagingVertexBufferSize,
                BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        }

        fixed (byte* ptr = vertexData)
        {
            GLManager.CommandList.UpdateBuffer(_stagingVertexBuffer, 0, (IntPtr)ptr, requiredSize);
        }

        DrawWithBuffer(mode, _stagingVertexBuffer, vertexCount);
    }

    private void FlushState()
    {
        // Update uniform buffer with current state
        _uniforms.ModelView = _modelViewStack.Current;
        _uniforms.Projection = _projectionStack.Current;
        _uniforms.TextureMatrix = _textureStack.Current;

        // Compute normal matrix (inverse transpose of upper-left 3x3 of modelview)
        Matrix4x4.Invert(_modelViewStack.Current, out Matrix4x4 invMV);
        Matrix4x4 normalMat = Matrix4x4.Transpose(invMV);
        _uniforms.NormalMatrixRow0 = new Vector4(normalMat.M11, normalMat.M12, normalMat.M13, 0);
        _uniforms.NormalMatrixRow1 = new Vector4(normalMat.M21, normalMat.M22, normalMat.M23, 0);
        _uniforms.NormalMatrixRow2 = new Vector4(normalMat.M31, normalMat.M32, normalMat.M33, 0);

        _uniforms.Light0Dir = _light0Dir;
        _uniforms.Light0Diffuse = _light0Diffuse;
        _uniforms.Light1Dir = _light1Dir;
        _uniforms.Light1Diffuse = _light1Diffuse;
        _uniforms.AmbientLight = _ambientLight;
        _uniforms.EnableLighting = _lightingEnabled ? 1 : 0;
        _uniforms.AlphaThreshold = _alphaTestEnabled ? _alphaThreshold : 0.0f;
        _uniforms.UseTexture = IsTextureEnabled ? 1 : 0;
        _uniforms.ShadeModel = _shadeModel;
        _uniforms.EnableFog = _fogEnabled ? 1 : 0;
        _uniforms.FogMode = _fogMode;
        _uniforms.FogStart = _fogStart;
        _uniforms.FogEnd = _fogEnd;
        _uniforms.FogDensity = _fogDensity;
        _uniforms.FogColor = _fogColor;

        Pipeline.UpdateUniforms(ref _uniforms);
    }

    private PipelineStateKey BuildPipelineStateKey(PrimitiveTopology topology)
    {
        return new PipelineStateKey
        {
            Topology = topology,
            BlendEnabled = _blendEnabled,
            SrcBlend = FixedFunctionPipeline.GLEnumToBlendFactor(_srcBlend),
            DstBlend = FixedFunctionPipeline.GLEnumToBlendFactor(_dstBlend),
            DepthTestEnabled = _depthTestEnabled,
            DepthWriteEnabled = _depthWriteEnabled,
            DepthComparison = _depthFunc,
            CullEnabled = _cullFaceEnabled,
            CullMode = _cullMode,
            HasVertexColor = true,
            HasTexCoord = true,
            HasNormal = true,
        };
    }

    // ========================================================================
    // Display lists
    // ========================================================================

    public uint GenLists(uint range) => _displayListCompiler.AllocateLists(range);
    public void NewList(uint list, GLEnum mode) => _displayListCompiler.BeginList(list);
    public void EndList() => _displayListCompiler.EndList();
    public void CallList(uint list) => _displayListCompiler.Execute(list);
    public void DeleteLists(uint list, uint range) => _displayListCompiler.DeleteLists(list, range);

    public void CallLists(uint n, GLEnum type, void* lists)
    {
        // Used for rendering string characters via display lists
        if (type == GLEnum.Int || type == GLEnum.UnsignedInt)
        {
            int* intLists = (int*)lists;
            for (uint i = 0; i < n; i++)
            {
                _displayListCompiler.Execute((uint)intLists[i]);
            }
        }
        else if (type == GLEnum.UnsignedByte)
        {
            byte* byteLists = (byte*)lists;
            for (uint i = 0; i < n; i++)
            {
                _displayListCompiler.Execute(byteLists[i]);
            }
        }
    }

    // ========================================================================
    // Shader program stubs (keep IGL compatibility for code that creates GL shaders)
    // These are no-ops since we use Vuldrid pipelines instead.
    // ========================================================================

    public uint CreateProgram() => _nextProgramId++;
    public uint CreateShader(ShaderType type) => _nextShaderId++;
    public void ShaderSource(uint shader, string string_) { }
    public void CompileShader(uint shader) { }
    public void AttachShader(uint program, uint shader) { }
    public void LinkProgram(uint program) { }
    public void UseProgram(uint program) { }
    public void DeleteProgram(uint program) { }
    public void DeleteShader(uint shader) { }
    public void GetShader(uint shader, ShaderParameterName pname, out int params_) { params_ = 1; }
    public string GetShaderInfoLog(uint shader) => "";
    public void GetProgram(uint program, ProgramPropertyARB pname, out int params_) { params_ = 1; }
    public string GetProgramInfoLog(uint program) => "";
    public int GetUniformLocation(uint program, string name) => -1;
    public void Uniform1(int location, int v0) { }
    public void Uniform1(int location, float v0) { }
    public void Uniform2(int location, float v0, float v1) { }
    public void Uniform3(int location, float v0, float v1, float v2) { }
    public void Uniform4(int location, float v0, float v1, float v2, float v3) { }
    public void UniformMatrix4(int location, uint count, bool transpose, float* value) { }

    // ========================================================================
    // Query / Misc
    // ========================================================================

    public GLEnum GetError() => GLEnum.NoError;
    public bool IsExtensionPresent(string extension) => false;

    public void GetFloat(GLEnum pname, Span<float> data)
    {
        if (pname == GLEnum.ModelviewMatrix && data.Length >= 16)
        {
            Matrix4x4 m = _modelViewStack.Current;
            MemoryMarshal.Cast<Matrix4x4, float>(new ReadOnlySpan<Matrix4x4>(in m)).CopyTo(data);
        }
        else if (pname == GLEnum.ProjectionMatrix && data.Length >= 16)
        {
            Matrix4x4 m = _projectionStack.Current;
            MemoryMarshal.Cast<Matrix4x4, float>(new ReadOnlySpan<Matrix4x4>(in m)).CopyTo(data);
        }
    }

    public void GetFloat(GLEnum pname, out float data)
    {
        data = 0;
    }

    public void GetFloat(GLEnum pname, float* data)
    {
        Span<float> span = new(data, 16);
        GetFloat(pname, span);
    }

    public void ReadPixels(int x, int y, uint width, uint height, PixelFormat format, PixelType type, void* pixels)
    {
        // TODO: Implement via staging texture readback if needed
    }

    // ========================================================================
    // State getters (for internal use)
    // ========================================================================

    public Vector4 CurrentColor => _currentColor;
    public Vector3 CurrentNormal => _currentNormal;
    public bool IsTextureEnabled { get; private set; }
    public uint BoundTextureId { get; private set; }
    public Matrix4x4 ModelViewMatrix => _modelViewStack.Current;
    public Matrix4x4 ProjectionMatrix => _projectionStack.Current;
    public Matrix4x4 TextureMatrix => _textureStack.Current;
}

/// <summary>
/// Simple matrix stack implementing push/pop/multiply operations.
/// </summary>
public class MatrixStack
{
    private readonly Stack<Matrix4x4> _stack = new();
    public Matrix4x4 Current { get; set; } = Matrix4x4.Identity;

    public void Push() => _stack.Push(Current);

    public void Pop()
    {
        if (_stack.Count > 0)
            Current = _stack.Pop();
    }

    public void LoadIdentity() => Current = Matrix4x4.Identity;

    public void Multiply(Matrix4x4 m)
    {
        Current = m * Current;
    }
}
