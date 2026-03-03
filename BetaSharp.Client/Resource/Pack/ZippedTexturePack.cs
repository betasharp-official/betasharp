using System.IO.Compression;
using BetaSharp.Client.Rendering.Core.Textures;
using Microsoft.Extensions.Logging;
using Silk.NET.OpenGL.Legacy;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BetaSharp.Client.Resource.Pack;

public class ZippedTexturePack : TexturePack
{
    private readonly ILogger _logger = Log.Instance.For<ZippedTexturePack>();
    private ZipArchive? _texturePackZipFile;
    private TextureHandle? _texturePackName;
    private Image<Rgba32>? _texturePackThumbnail;

    private readonly FileInfo _texturePackFile;

    public ZippedTexturePack(FileInfo file)
    {
        TexturePackFileName = file.Name;
        _texturePackFile = file;
    }

    private static string TruncateString(string? str)
    {
        if (str != null && str.Length > 34)
        {
            return str[..34];
        }
        return str ?? string.Empty;
    }

    public override void func_6485_a(BetaSharp game)
    {
        try
        {
            using var archive = ZipFile.OpenRead(_texturePackFile.FullName);

            var packTxtEntry = archive.GetEntry("pack.txt");
            if (packTxtEntry != null)
            {
                using var stream = packTxtEntry.Open();
                using var reader = new StreamReader(stream); // Replaces BufferedReader
                FirstDescriptionLine = TruncateString(reader.ReadLine());
                SecondDescriptionLine = TruncateString(reader.ReadLine());
            }

            var packPngEntry = archive.GetEntry("pack.png");
            if (packPngEntry != null)
            {
                using var stream = packPngEntry.Open();
                _texturePackThumbnail = Image.Load<Rgba32>(stream); // Native ImageSharp load
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load zipped texture pack {File}", _texturePackFile.Name);
        }
    }

    public override void Unload(BetaSharp game)
    {
        if (_texturePackThumbnail != null && _texturePackName != null)
        {
            game.textureManager.Delete(_texturePackName);
            _texturePackThumbnail.Dispose();

        }

        CloseTexturePackFile();
    }

    public override void BindThumbnailTexture(BetaSharp game)
    {
        if (_texturePackThumbnail != null && _texturePackName == null)
        {
            _texturePackName = game.textureManager.Load(_texturePackThumbnail);
        }

        if (_texturePackThumbnail != null && _texturePackName != null)
        {
            game.textureManager.BindTexture(_texturePackName);
        }
        else
        {
            game.textureManager.BindTexture(game.textureManager.GetTextureId("/gui/unknown_pack.png"));
        }

    }

    public override void func_6482_a()
    {
        try
        {
            // Opens the zip file and keeps it open for reading resources
            _texturePackZipFile = ZipFile.OpenRead(_texturePackFile.FullName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open texture pack zip file {File}", _texturePackFile.Name);
        }
    }

    public override void CloseTexturePackFile()
    {
        _texturePackZipFile?.Dispose();
        _texturePackZipFile = null;
    }

    public override Stream? GetResourceAsStream(string path)
    {
        try
        {
            string entryName = path.StartsWith("/") ? path[1..] : path;

            var entry = _texturePackZipFile?.GetEntry(entryName);
            if (entry != null)
            {
                var ms = new MemoryStream();
                using (var entryStream = entry.Open())
                {
                    entryStream.CopyTo(ms);
                }
                ms.Position = 0;
                return ms;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get resource as stream for path {Path}", path);
        }

        return base.GetResourceAsStream(path);
    }
}
