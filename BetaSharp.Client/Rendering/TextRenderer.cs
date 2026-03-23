using System.Runtime.InteropServices;
using BetaSharp.Client.Options;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.Textures;
using Microsoft.Extensions.Logging;
using Silk.NET.OpenGL;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BetaSharp.Client.Rendering;

public class TextRenderer
{
    private readonly ILogger<TextRenderer> _logger = Log.Instance.For<TextRenderer>();

    private const char ColorCodeChar = '§';
    private const string FontPath = "assets/font/Monocraft.ttc";
    private const int AtlasSize = 2048;
    private const int AtlasFontSize = 64;
    private const int GlyphPadding = 2;
    private const float DisplayScale = 0.125f;

    private readonly Font _font;
    private readonly TextOptions _textOptions;
    private readonly Image<Rgba32> _atlasImage;
    private readonly Dictionary<char, GlyphInfo> _glyphCache = [];
    private int _atlasX;
    private int _atlasY;
    private readonly int _rowHeight;

    private TextureHandle? fontTextureName { get; }

    private readonly TextureManager _textureManager;

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct GlyphInfo(float advanceWidth, float u0, float v0, float u1, float v1, float width, float height)
    {
        public readonly float AdvanceWidth = advanceWidth;
        public readonly float U0 = u0, V0 = v0, U1 = u1, V1 = v1;
        public readonly float Width = width, Height = height;
    }

    private void LoadClassicFontIntoAtlas(Image<Rgba32> classicFontImage)
    {
        int scale = AtlasFontSize / 8;
        int imgWidth = classicFontImage.Width;
        int imgHeight = classicFontImage.Height;

        Rgba32[] pixels = new Rgba32[imgWidth * imgHeight];
        classicFontImage.CopyPixelDataTo(pixels);

        for (int charIndex = 32; charIndex < 127; ++charIndex)
        {
            int col = charIndex % 16;
            int row = charIndex / 16;
            int lastSolidPixel = -1;

            for (int bit = 7; bit >= 0; --bit)
            {
                int xOffset = col * 8 + bit;
                bool columnIsEmpty = true;

                for (int yOffset = 0; yOffset < 8 && columnIsEmpty; ++yOffset)
                {
                    int pixelIndex = (row * 8 + yOffset) * imgWidth + xOffset;
                    if (pixels[pixelIndex].A > 0)
                    {
                        columnIsEmpty = false;
                    }
                }

                if (!columnIsEmpty)
                {
                    lastSolidPixel = bit;
                    break;
                }
            }

            int advancePixels = lastSolidPixel + 2;
            if (charIndex == 32) advancePixels = 4;

            int cellW = 8 * scale;
            int cellH = 8 * scale;

            if (_atlasX + cellW > AtlasSize)
            {
                _atlasX = 0;
                _atlasY += _rowHeight;
            }

            using (Image<Rgba32> glyphImage = classicFontImage.Clone(ctx => ctx
                .Crop(new Rectangle(col * 8, row * 8, 8, 8))
                .Resize(cellW, cellH, KnownResamplers.NearestNeighbor)))
            {
                glyphImage.ProcessPixelRows(_atlasImage, (srcAccessor, dstAccessor) =>
                {
                    for (int y = 0; y < cellH; y++)
                    {
                        Span<Rgba32> srcRow = srcAccessor.GetRowSpan(y);
                        Span<Rgba32> dstRow = dstAccessor.GetRowSpan(_atlasY + y);
                        srcRow.Slice(0, cellW).CopyTo(dstRow.Slice(_atlasX, cellW));
                    }
                });
            }

            float u0 = (float)_atlasX / AtlasSize;
            float v0 = (float)_atlasY / AtlasSize;
            float u1 = (float)(_atlasX + cellW) / AtlasSize;
            float v1 = (float)(_atlasY + cellH) / AtlasSize;

            char c = (char)charIndex;

            float advanceWidth = advancePixels * scale;
            _glyphCache[c] = new GlyphInfo(advanceWidth, u0, v0, u1, v1, cellW, cellH);

            _atlasX += cellW;
        }

        UploadAtlasSubImage(0, 0, AtlasSize, _atlasY + _rowHeight);
    }

    public TextRenderer(GameOptions options, TextureManager textureManager)
    {
        _textureManager = textureManager;

        string path = Path.Combine(AppContext.BaseDirectory, "font", "Monocraft.ttc");
        if (!File.Exists(path)) path = FontPath;

        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Font file not found at {path}");
        }

        var collection = new FontCollection();
        _font = collection.AddCollection(path).First().CreateFont(AtlasFontSize);
        _textOptions = new TextOptions(_font);

        _rowHeight = AtlasFontSize + GlyphPadding;
        _atlasImage = new Image<Rgba32>(AtlasSize, AtlasSize);
        ClearAtlasRegion(0, 0, AtlasSize, AtlasSize);

        fontTextureName = textureManager.Load(_atlasImage);
        fontTextureName.Texture?.SetFilter(TextureMinFilter.Nearest, TextureMagFilter.Nearest);

        try
        {
            AssetManager.Asset asset = AssetManager.Instance.getAsset("font/default.png");
            using var stream = new MemoryStream(asset.GetBinaryContent());
            using var classicFontImage = Image.Load<Rgba32>(stream);

            LoadClassicFontIntoAtlas(classicFontImage);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load classic font. Falling back entirely to TrueType.");
        }
    }
    private static void ClearAtlasRegion(Image<Rgba32> image, int x, int y, int w, int h)
    {
        image.Mutate(ctx => ctx.Fill(SixLabors.ImageSharp.Color.Transparent, new Rectangle(x, y, w, h)));
    }

    private void ClearAtlasRegion(int x, int y, int w, int h)
    {
        ClearAtlasRegion(_atlasImage, x, y, w, h);
    }

    private GlyphInfo GetOrCreateGlyph(char c)
    {
        if (_glyphCache.TryGetValue(c, out GlyphInfo info))
            return info;

        ReadOnlySpan<char> charSpan = stackalloc char[] { c };
        FontRectangle advanceRect = TextMeasurer.MeasureAdvance(charSpan, _textOptions);

        FontRectangle boundsRect = TextMeasurer.MeasureBounds(charSpan, _textOptions);

        float advanceWidth = advanceRect.Width;
        int cellW = Math.Max(1, (int)Math.Ceiling(advanceRect.Width) + GlyphPadding);
        int cellH = _rowHeight;

        if (_atlasX + cellW > AtlasSize)
        {
            _atlasX = 0;
            _atlasY += _rowHeight;
        }

        if (_atlasY + cellH > AtlasSize)
        {
            _glyphCache.Clear();
            ClearAtlasRegion(0, 0, AtlasSize, AtlasSize);
            _atlasX = 0;
            _atlasY = 0;

            AssetManager.Asset asset = AssetManager.Instance.getAsset("font/default.png");
            using var stream = new MemoryStream(asset.GetBinaryContent());
            using var classicFontImage = Image.Load<Rgba32>(stream);
            LoadClassicFontIntoAtlas(classicFontImage);
        }

        using (Image<Rgba32> glyphImage = new Image<Rgba32>(cellW, cellH))
        {
            ClearAtlasRegion(glyphImage, 0, 0, cellW, cellH);

            float drawX = 1f;
            float drawY = 1f;

            glyphImage.Mutate(ctx => ctx.DrawText(
                c.ToString(),
                _font,
                Color.White,
                new PointF(drawX, drawY)));

            glyphImage.ProcessPixelRows(_atlasImage, (srcAccessor, dstAccessor) =>
            {
                for (int gy = 0; gy < cellH; gy++)
                {
                    Span<Rgba32> srcRow = srcAccessor.GetRowSpan(gy);
                    Span<Rgba32> dstRow = dstAccessor.GetRowSpan(_atlasY + gy);
                    srcRow.Slice(0, cellW).CopyTo(dstRow.Slice(_atlasX, cellW));
                }
            });
        }

        float u0 = (float)_atlasX / AtlasSize;
        float v0 = (float)_atlasY / AtlasSize;
        float u1 = (float)(_atlasX + cellW) / AtlasSize;
        float v1 = (float)(_atlasY + cellH) / AtlasSize;

        UploadAtlasSubImage(_atlasX, _atlasY, cellW, cellH);

        info = new GlyphInfo(advanceWidth, u0, v0, u1, v1, cellW, cellH);
        _glyphCache[c] = info;
        _atlasX += cellW;
        return info;
    }

    private unsafe void UploadAtlasSubImage(int x, int y, int width, int height)
    {
        if (fontTextureName?.Texture == null) return;

        int bufferSize = width * height * 4;

        byte[] region = System.Buffers.ArrayPool<byte>.Shared.Rent(bufferSize);
        try
        {
            int idx = 0;
            _atlasImage.ProcessPixelRows(accessor =>
            {
                for (int row = y; row < y + height; row++)
                {
                    Span<Rgba32> pixelRow = accessor.GetRowSpan(row);
                    for (int col = x; col < x + width; col++)
                    {
                        Rgba32 p = pixelRow[col];
                        region[idx++] = p.R;
                        region[idx++] = p.G;
                        region[idx++] = p.B;
                        region[idx++] = p.A;
                    }
                }
            });

            fixed (byte* ptr = region)
            {
                fontTextureName.Texture!.UploadSubImage(x, y, width, height, ptr);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading atlas sub image");
        }
        finally
        {
            System.Buffers.ArrayPool<byte>.Shared.Return(region);
        }
    }

    public void DrawStringWithShadow(ReadOnlySpan<char> text, int x, int y, Guis.Color color, HorizontalAlignment align = HorizontalAlignment.Left)
    {
        RenderString(text, x + 1, y + 1, color, true, align);
        DrawString(text, x, y, color, align);
    }

    public void DrawString(ReadOnlySpan<char> text, int x, int y, Guis.Color color, HorizontalAlignment align = HorizontalAlignment.Left)
    {
        RenderString(text, x, y, color, false, align);
    }

    public void RenderString(ReadOnlySpan<char> text, int x, int y, Guis.Color color, bool darken, HorizontalAlignment align)
    {
        if (text.IsEmpty) return;

        if (darken)
            color = color.Darken();

        fontTextureName?.Bind();

        Tessellator tessellator = Tessellator.instance;
        tessellator.startDrawingQuads();
        tessellator.setColorRGBA(color);

        float currentX = x;
        float currentY = y;

        int width = GetStringWidth(text);
        if (align == HorizontalAlignment.Center) currentX -= width / 2;
        else if (align == HorizontalAlignment.Right) currentX -= width;

        for (int i = 0; i < text.Length; ++i)
        {
            for (; text.Length > i + 1 && text[i] == ColorCodeChar; i += 2)
            {
                int colorCode = HexToDec(text[i + 1]);
                tessellator.setColorRGBA(Guis.Color.FromColorCode(colorCode, (byte)color.A, darken));
            }

            if (i < text.Length)
            {
                GlyphInfo glyph = GetOrCreateGlyph(text[i]);
                if (glyph.Width > 0 && glyph.Height > 0)
                {
                    float w = glyph.Width * DisplayScale;
                    float h = glyph.Height * DisplayScale;
                    tessellator.addVertexWithUV(currentX + 0, currentY + h, 0, glyph.U0, glyph.V1);
                    tessellator.addVertexWithUV(currentX + w, currentY + h, 0, glyph.U1, glyph.V1);
                    tessellator.addVertexWithUV(currentX + w, currentY + 0, 0, glyph.U1, glyph.V0);
                    tessellator.addVertexWithUV(currentX + 0, currentY + 0, 0, glyph.U0, glyph.V0);
                }

                currentX += glyph.AdvanceWidth * DisplayScale;
            }
        }

        tessellator.draw();
    }

    private static int HexToDec(char c)
    {
        int v = c;
        if (c <= '9') v -= '0';
        else if (c <= 'F') v += 10 - 'A';
        else if (c <= 'f') v += 10 - 'a';
        else return 15;
        return v <= 0 ? 0 : v;
    }

    public int GetStringWidth(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty) return 0;
        float total = 0;
        for (int i = 0; i < text.Length; ++i)
        {
            if (text[i] == ColorCodeChar)
                ++i;
            else
                total += GetOrCreateGlyph(text[i]).AdvanceWidth * DisplayScale;
        }

        return (int)Math.Ceiling(total);
    }

    private int GetStringFitLength(ReadOnlySpan<char> text, int maxWidth)
    {
        float width = 0;
        int lastSpaceIndex = -1;
        int i = 0;
        for (; i < text.Length; ++i)
        {
            if (text[i] == ColorCodeChar)
            {
                ++i;
                continue;
            }

            if (text[i] == ' ')
                lastSpaceIndex = i;
            width += GetOrCreateGlyph(text[i]).AdvanceWidth * DisplayScale;
            if (width > maxWidth)
            {
                if (lastSpaceIndex > 0)
                    return lastSpaceIndex;
                return Math.Max(1, i);
            }
        }

        return text.Length;
    }

    private void ProcessWrappedText(ReadOnlySpan<char> text, int x, int y, int maxWidth, Guis.Color color, bool draw, ref int outHeight, HorizontalAlignment align)
    {
        if (text.IsEmpty) return;

        int totalHeight = 0;
        int currentY = y;
        int lineHeight = (int)((AtlasFontSize + GlyphPadding) * DisplayScale);
        while (text.Length > 0)
        {
            int newlineIndex = text.IndexOf('\n');
            ReadOnlySpan<char> line;
            if (newlineIndex >= 0)
            {
                line = text.Slice(0, newlineIndex);
                text = text.Slice(newlineIndex + 1);
            }
            else
            {
                line = text;
                text = [];
            }

            while (line.Length > 0)
            {
                int fitLength = GetStringFitLength(line, maxWidth);
                ReadOnlySpan<char> subline = line.Slice(0, Math.Min(fitLength, line.Length));

                while (subline.Length > 0 && subline[subline.Length - 1] == ' ')
                    subline = subline.Slice(0, subline.Length - 1);

                if (subline.Length > 0 || fitLength > 0)
                {
                    if (draw && subline.Length > 0)
                        DrawString(subline, x, currentY, color, align);
                    currentY += lineHeight;
                    totalHeight += lineHeight;
                }

                line = line.Slice(Math.Min(fitLength, line.Length));
                while (line.Length > 0 && line[0] == ' ')
                    line = line.Slice(1);
            }
        }

        if (totalHeight < lineHeight) totalHeight = lineHeight;
        outHeight = totalHeight;
    }

    public void DrawStringWrapped(ReadOnlySpan<char> text, int x, int y, int maxWidth, Guis.Color color, HorizontalAlignment align = HorizontalAlignment.Left)
    {
        int dummyHeight = 0;
        ProcessWrappedText(text, x, y, maxWidth, color, true, ref dummyHeight, align);
    }

    public int GetStringHeight(ReadOnlySpan<char> text, int maxWidth)
    {
        int height = 0;
        ProcessWrappedText(text, 0, 0, maxWidth, Guis.Color.Black, false, ref height, HorizontalAlignment.Left);
        return height;
    }
}
