using BetaSharp.Client.Rendering.Core.Textures;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BetaSharp.Client.Resource.Pack;

public class BuiltInTexturePack : TexturePack
{
    private readonly ILogger _logger = Log.Instance.For<BuiltInTexturePack>();
    private TextureHandle? _texturePackName;
    private readonly Image<Rgba32>? texturePackThumbnail;

    public BuiltInTexturePack()
    {
        TexturePackFileName = "Default";
        FirstDescriptionLine = "The default look of Minecraft";

        try
        {
            byte[] content = AssetManager.Instance.getAsset("pack.png").GetBinaryContent();
            using (var ms = new MemoryStream(content))
            {
                texturePackThumbnail = Image.Load<Rgba32>(ms);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load built in texture pack");
        }

    }

    public override void Unload(ITextureManager textureManager)
    {
        if (texturePackThumbnail != null && _texturePackName != null)
        {
            textureManager.Delete(_texturePackName);

        }

    }

    public override void BindThumbnailTexture(ITextureManager textureManager)
    {
        if (texturePackThumbnail != null && _texturePackName == null)
        {
            _texturePackName = textureManager.Load(texturePackThumbnail);
        }

        if (texturePackThumbnail != null && _texturePackName != null)
        {
            textureManager.BindTexture(_texturePackName);
        }
        else
        {
            textureManager.BindTexture(textureManager.GetTextureId("/gui/unknown_pack.png"));
        }

    }
}
