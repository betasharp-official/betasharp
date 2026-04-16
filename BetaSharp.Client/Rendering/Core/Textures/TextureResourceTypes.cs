namespace BetaSharp.Client.Rendering.Core.Textures;

public enum TextureMinificationFilter
{
    Nearest,
    Linear,
    NearestMipmapNearest
}

public enum TextureMagnificationFilter
{
    Nearest,
    Linear
}

public enum TextureAddressMode
{
    Repeat,
    ClampToEdge
}

public enum TextureDataFormat
{
    Rgba,
    Rgb
}

public enum TextureStorageFormat
{
    Rgba,
    Rgba8
}
