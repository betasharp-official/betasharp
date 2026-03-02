using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL.Legacy;
using Vuldrid;
using Sampler = Vuldrid.Sampler;

namespace BetaSharp.Client.Rendering.Core.VkBackend;

/// <summary>
/// Must be kept in sync with fixed_function.vert/frag uniform block.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct FixedFunctionUniforms
{
    public Matrix4x4 ModelView;       // 64 bytes, offset 0
    public Matrix4x4 Projection;     // 64 bytes, offset 64
    public Matrix4x4 TextureMatrix;  // 64 bytes, offset 128

    // mat3 in std140 is 3 vec4s (48 bytes)
    public Vector4 NormalMatrixRow0; // offset 192
    public Vector4 NormalMatrixRow1; // offset 208
    public Vector4 NormalMatrixRow2; // offset 224

    public Vector3 Light0Dir;        // offset 240
    public float _pad0;
    public Vector3 Light0Diffuse;    // offset 256
    public float _pad1;
    public Vector3 Light1Dir;        // offset 272
    public float _pad2;
    public Vector3 Light1Diffuse;    // offset 288
    public float _pad3;
    public Vector3 AmbientLight;     // offset 304
    public int EnableLighting;       // offset 316

    public float AlphaThreshold;     // offset 320
    public int UseTexture;           // offset 324
    public int ShadeModel;           // offset 328
    public int EnableFog;            // offset 332

    public int FogMode;              // offset 336
    public float FogStart;           // offset 340
    public float FogEnd;             // offset 344
    public float FogDensity;         // offset 348

    public Vector4 FogColor;         // offset 352
}

/// <summary>
/// Represents the current render state used to select/create the appropriate Pipeline.
/// </summary>
public struct PipelineStateKey : IEquatable<PipelineStateKey>
{
    public PrimitiveTopology Topology;
    public bool BlendEnabled;
    public BlendFactor SrcBlend;
    public BlendFactor DstBlend;
    public bool DepthTestEnabled;
    public bool DepthWriteEnabled;
    public ComparisonKind DepthComparison;
    public bool CullEnabled;
    public FaceCullMode CullMode;
    public bool HasVertexColor;
    public bool HasTexCoord;
    public bool HasNormal;

    public readonly bool Equals(PipelineStateKey other) =>
        Topology == other.Topology &&
        BlendEnabled == other.BlendEnabled &&
        SrcBlend == other.SrcBlend &&
        DstBlend == other.DstBlend &&
        DepthTestEnabled == other.DepthTestEnabled &&
        DepthWriteEnabled == other.DepthWriteEnabled &&
        DepthComparison == other.DepthComparison &&
        CullEnabled == other.CullEnabled &&
        CullMode == other.CullMode &&
        HasVertexColor == other.HasVertexColor &&
        HasTexCoord == other.HasTexCoord &&
        HasNormal == other.HasNormal;

    public override readonly int GetHashCode() => HashCode.Combine(
        HashCode.Combine((int)Topology, BlendEnabled, (int)SrcBlend, (int)DstBlend),
        HashCode.Combine(DepthTestEnabled, DepthWriteEnabled, (int)DepthComparison),
        HashCode.Combine(CullEnabled, (int)CullMode, HasVertexColor, HasTexCoord, HasNormal));

    public override bool Equals(object? obj) => obj is PipelineStateKey k && Equals(k);

    public static bool operator ==(PipelineStateKey left, PipelineStateKey right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PipelineStateKey left, PipelineStateKey right)
    {
        return !(left == right);
    }
}

/// <summary>
/// Manages Vuldrid pipelines, shaders, and resource layouts for the fixed-function emulation.
/// Caches pipelines keyed by render state.
/// </summary>
public class FixedFunctionPipeline : IDisposable
{
    private readonly GraphicsDevice _device;
    private readonly Vuldrid.Shader _vertexShader;
    private readonly Vuldrid.Shader _fragmentShader;

    // Pipeline cache keyed by render state
    private readonly Dictionary<PipelineStateKey, Pipeline> _pipelineCache = [];

    // Texture resource sets keyed by TextureView + Sampler
    private readonly Dictionary<(TextureView, Sampler), ResourceSet> _textureResourceSets = [];

    // Default white texture for untextured rendering
    public TextureView DefaultTextureView { get; }
    public Sampler DefaultSampler { get; }
    private readonly ResourceSet _defaultTextureResourceSet;

    public ResourceLayout UniformLayout { get; }
    public ResourceLayout TextureLayout { get; }
    public DeviceBuffer UniformBuffer { get; }
    public ResourceSet UniformResourceSet { get; }

    public FixedFunctionPipeline(GraphicsDevice device)
    {
        _device = device;
        ResourceFactory factory = device.ResourceFactory;

        byte[] vertSpirv = AssetManager.Instance.getAsset("shaders/fixed_function.vert.spv").getBinaryContent();
        byte[] fragSpirv = AssetManager.Instance.getAsset("shaders/fixed_function.frag.spv").getBinaryContent();

        _vertexShader = factory.CreateShader(new ShaderDescription(ShaderStages.Vertex, vertSpirv, "main"));
        _fragmentShader = factory.CreateShader(new ShaderDescription(ShaderStages.Fragment, fragSpirv, "main"));

        UniformBuffer = factory.CreateBuffer(new BufferDescription(
            (uint)Unsafe.SizeOf<FixedFunctionUniforms>(),
            BufferUsage.UniformBuffer | BufferUsage.Dynamic));

        UniformLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("Uniforms", ResourceKind.UniformBuffer, ShaderStages.Vertex | ShaderStages.Fragment)));

        TextureLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("u_Texture0", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
            new ResourceLayoutElementDescription("u_Texture0Sampler", ResourceKind.Sampler, ShaderStages.Fragment)));

        UniformResourceSet = factory.CreateResourceSet(new ResourceSetDescription(UniformLayout, UniformBuffer));

        Vuldrid.Texture whiteTex = factory.CreateTexture(TextureDescription.Texture2D(
            1, 1, 1, 1, Vuldrid.PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));
        device.UpdateTexture(whiteTex, new byte[] { 255, 255, 255, 255 }, 0, 0, 0, 1, 1, 1, 0, 0);
        DefaultTextureView = factory.CreateTextureView(whiteTex);

        DefaultSampler = factory.CreateSampler(new SamplerDescription(
            SamplerAddressMode.Wrap, SamplerAddressMode.Wrap, SamplerAddressMode.Wrap,
            SamplerFilter.MinPoint_MagPoint_MipPoint,
            null, 0, 0, uint.MaxValue, 0, SamplerBorderColor.TransparentBlack));

        _defaultTextureResourceSet = factory.CreateResourceSet(
            new ResourceSetDescription(TextureLayout, DefaultTextureView, DefaultSampler));
    }

    public void UpdateUniforms(ref FixedFunctionUniforms uniforms)
    {
        GLManager.CommandList.UpdateBuffer(UniformBuffer, 0, ref uniforms);
    }

    public ResourceSet GetDefaultTextureResourceSet() => _defaultTextureResourceSet;

    public ResourceSet GetOrCreateTextureResourceSet(TextureView view, Sampler sampler)
    {
        (TextureView view, Sampler sampler) key = (view, sampler);
        if (_textureResourceSets.TryGetValue(key, out ResourceSet? existing))
            return existing;

        ResourceSet rs = _device.ResourceFactory.CreateResourceSet(
            new ResourceSetDescription(TextureLayout, view, sampler));
        _textureResourceSets[key] = rs;
        return rs;
    }

    public Pipeline GetOrCreatePipeline(PipelineStateKey state)
    {
        if (_pipelineCache.TryGetValue(state, out Pipeline? existing))
            return existing;

        // Vertex struct layout (32 bytes):
        // float X,Y,Z   @ offset 0  (12 bytes) — Position
        // float U,V      @ offset 12 (8 bytes)  — TexCoord
        // int Color      @ offset 20 (4 bytes)  — Color (packed ABGR)
        // int Normal     @ offset 24 (4 bytes)  — Normal (packed)
        // int Padding    @ offset 28 (4 bytes)  — unused
        // Elements MUST be in ascending offset order for Vuldrid validation.
        var vertexLayout = new VertexLayoutDescription(
            32, // stride
            new VertexElementDescription("a_Position", VertexElementFormat.Float3, 0),
            new VertexElementDescription("a_TexCoord", VertexElementFormat.Float2, 12),
            new VertexElementDescription("a_Color", VertexElementFormat.Byte4_Norm, 20),
            new VertexElementDescription("a_Normal", VertexElementFormat.SByte4_Norm, 24)
        );

        BlendStateDescription blendState = state.BlendEnabled
            ? new BlendStateDescription
            {
                AttachmentStates =
                [
                    new BlendAttachmentDescription
                    {
                        BlendEnabled = true,
                        SourceColorFactor = state.SrcBlend,
                        DestinationColorFactor = state.DstBlend,
                        ColorFunction = BlendFunction.Add,
                        SourceAlphaFactor = state.SrcBlend,
                        DestinationAlphaFactor = state.DstBlend,
                        AlphaFunction = BlendFunction.Add,
                    }
                ]
            }
            : BlendStateDescription.SingleOverrideBlend;

        var depthStencil = new DepthStencilStateDescription(
            state.DepthTestEnabled,
            state.DepthWriteEnabled,
            state.DepthComparison);

        var rasterizer = new RasterizerStateDescription(
            state.CullEnabled ? state.CullMode : FaceCullMode.None,
            PolygonFillMode.Solid,
            FrontFace.Clockwise,
            state.DepthTestEnabled,
            false);

        var pipelineDesc = new GraphicsPipelineDescription(
            blendState,
            depthStencil,
            rasterizer,
            state.Topology,
            new ShaderSetDescription([vertexLayout], [_vertexShader, _fragmentShader]),
            [UniformLayout, TextureLayout],
            _device.MainSwapchain.Framebuffer.OutputDescription);

        Pipeline pipeline = _device.ResourceFactory.CreateGraphicsPipeline(pipelineDesc);
        _pipelineCache[state] = pipeline;
        return pipeline;
    }

    public static PrimitiveTopology GLEnumToTopology(GLEnum mode)
    {
        return mode switch
        {
            GLEnum.Triangles => PrimitiveTopology.TriangleList,
            GLEnum.TriangleStrip => PrimitiveTopology.TriangleStrip,
            GLEnum.TriangleFan => PrimitiveTopology.TriangleList, // Need to convert fan to list
            GLEnum.Lines => PrimitiveTopology.LineList,
            GLEnum.LineStrip => PrimitiveTopology.LineStrip,
            GLEnum.Points => PrimitiveTopology.PointList,
            _ => PrimitiveTopology.TriangleList,
        };
    }

    public static BlendFactor GLEnumToBlendFactor(GLEnum factor)
    {
        return factor switch
        {
            GLEnum.Zero => BlendFactor.Zero,
            GLEnum.One => BlendFactor.One,
            GLEnum.SrcAlpha => BlendFactor.SourceAlpha,
            GLEnum.OneMinusSrcAlpha => BlendFactor.InverseSourceAlpha,
            GLEnum.DstAlpha => BlendFactor.DestinationAlpha,
            GLEnum.OneMinusDstAlpha => BlendFactor.InverseDestinationAlpha,
            GLEnum.SrcColor => BlendFactor.SourceColor,
            GLEnum.DstColor => BlendFactor.DestinationColor,
            GLEnum.OneMinusSrcColor => BlendFactor.InverseSourceColor,
            GLEnum.OneMinusDstColor => BlendFactor.InverseDestinationColor,
            _ => BlendFactor.One,
        };
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        foreach (Pipeline p in _pipelineCache.Values) p.Dispose();
        _pipelineCache.Clear();

        foreach (ResourceSet rs in _textureResourceSets.Values) rs.Dispose();
        _textureResourceSets.Clear();

        _defaultTextureResourceSet?.Dispose();
        DefaultTextureView?.Dispose();
        DefaultSampler?.Dispose();
        UniformResourceSet?.Dispose();
        UniformBuffer?.Dispose();
        UniformLayout?.Dispose();
        TextureLayout?.Dispose();
        _vertexShader?.Dispose();
        _fragmentShader?.Dispose();
    }
}
