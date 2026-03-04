using BetaSharp.Client.Options;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Util;
using Silk.NET.OpenGL.Legacy;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BetaSharp.Client.Rendering;

public class TextRenderer
{
    private readonly int[] _charWidth = new int[256];
    public TextureHandle? fontTextureName;

    public TextRenderer(GameOptions options, TextureManager textureManager)
    {
        Image<Rgba32> fontImage;
        try
        {
            AssetManager.Asset asset = AssetManager.Instance.getAsset("font/default.png");
            using var stream = new MemoryStream(asset.getBinaryContent());
            fontImage = Image.Load<Rgba32>(stream);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to load font", ex);
        }

        int imgWidth = fontImage.Width;
        int imgHeight = fontImage.Height;
        int[] pixels = new int[imgWidth * imgHeight];

        fontImage.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                Span<Rgba32> row = accessor.GetRowSpan(y);
                for (int x = 0; x < accessor.Width; x++)
                {
                    Rgba32 p = row[x];
                    pixels[y * imgWidth + x] = (p.A << 24) | (p.R << 16) | (p.G << 8) | p.B;
                }
            }
        });

        for (int charIndex = 0; charIndex < 256; ++charIndex)
        {
            int col = charIndex % 16;
            int row = charIndex / 16;
            int widthInPixels = 0;

            for (int bit = 7; bit >= 0; --bit)
            {
                int xOffset = col * 8 + bit;
                bool columnIsEmpty = true;

                for (int yOffset = 0; yOffset < 8 && columnIsEmpty; ++yOffset)
                {
                    int pixelIndex = (row * 8 + yOffset) * imgWidth + xOffset;
                    int alpha = pixels[pixelIndex] & 255;

                    if (alpha > 0)
                    {
                        columnIsEmpty = false;
                    }
                }

                if (!columnIsEmpty)
                {
                    widthInPixels = bit;
                    break;
                }
            }

            if (charIndex == 32)
            {
                widthInPixels = 2;
            }

            _charWidth[charIndex] = widthInPixels + 2;
        }

        fontTextureName = textureManager.Load(fontImage);
    }

    public void DrawStringWithShadow(ReadOnlySpan<char> text, int x, int y, uint color)
    {
        RenderString(text, x + 1, y + 1, color, true);
        DrawString(text, x, y, color);
    }

    public void DrawString(ReadOnlySpan<char> text, int x, int y, uint color)
    {
        RenderString(text, x, y, color, false);
    }

    public void RenderString(ReadOnlySpan<char> text, int x, int y, uint color, bool darken)
    {
        if (text.IsEmpty) return;

        uint alpha = color & 0xFF000000;
        if (darken)
        {
            color = (color & 0xFCFCFC) >> 2;
            color |= alpha;
        }

        fontTextureName?.Bind();
        float a = (color >> 24 & 255) / 255.0F;
        float r = (color >> 16 & 255) / 255.0F;
        float g = (color >> 8 & 255) / 255.0F;
        float b = (color & 255) / 255.0F;

        if (a == 0.0F) a = 1.0F;

        Tessellator tessellator = Tessellator.instance;
        tessellator.startDrawingQuads();
        tessellator.setColorRGBA_F(r, g, b, a);

        float currentX = x;
        float currentY = y;

        for (int i = 0; i < text.Length; ++i)
        {
            for (; text.Length > i + 1 && text[i] == 167; i += 2)
            {
                int colorCode = HexToDec(text[i + 1]);

                int baseColorOffset = (colorCode >> 3 & 1) * 85;
                int cr = (colorCode >> 2 & 1) * 170 + baseColorOffset;
                int cg = (colorCode >> 1 & 1) * 170 + baseColorOffset;
                int cb = (colorCode >> 0 & 1) * 170 + baseColorOffset;

                if (colorCode == 6)
                {
                    cr += 85;
                }

                if (darken)
                {
                    cr /= 4;
                    cg /= 4;
                    cb /= 4;
                }

                tessellator.setColorRGBA_F(cr / 255.0F, cg / 255.0F, cb / 255.0F, a);
            }

            if (i < text.Length)
            {
                int charIndex = ChatAllowedCharacters.allowedCharacters.IndexOf(text[i]);
                if (charIndex >= 0)
                {
                    int fontIndex = charIndex + 32;
                    int u = (fontIndex % 16) * 8;
                    int v = (fontIndex / 16) * 8;

                    float quadSize = 7.99F;
                    float uvOffset = 0.0F;

                    tessellator.addVertexWithUV(currentX + 0.0D, currentY + quadSize, 0.0D, (u / 128.0F) + uvOffset, ((v + quadSize) / 128.0F) + uvOffset);
                    tessellator.addVertexWithUV(currentX + quadSize, currentY + quadSize, 0.0D, ((u + quadSize) / 128.0F) + uvOffset, ((v + quadSize) / 128.0F) + uvOffset);
                    tessellator.addVertexWithUV(currentX + quadSize, currentY + 0.0D, 0.0D, ((u + quadSize) / 128.0F) + uvOffset, (v / 128.0F) + uvOffset);
                    tessellator.addVertexWithUV(currentX + 0.0D, currentY + 0.0D, 0.0D, (u / 128.0F) + uvOffset, (v / 128.0F) + uvOffset);

                    currentX += _charWidth[fontIndex];
                }
            }
        }

        tessellator.draw();
    }

    /// <summary>
    /// Get decimal value of give hex char.
    /// Non-hex characters are not handled,
    /// but will still return a value between 0 and 15 inclusive.
    /// </summary>
    /// <param name="c">input character (case-insensitive)</param>
    /// <returns>value between 0-15 inclusive</returns>
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

        int totalWidth = 0;

        for (int i = 0; i < text.Length; ++i)
        {
            if (text[i] == 167)
            {
                ++i;
            }
            else
            {
                int charIndex = ChatAllowedCharacters.allowedCharacters.IndexOf(text[i]);
                if (charIndex >= 0)
                {
                    totalWidth += _charWidth[charIndex + 32];
                }
            }
        }

        return totalWidth;
    }

    private int GetStringFitLength(ReadOnlySpan<char> text, int maxWidth)
    {
        int width = 0;
        int lastSpaceIndex = -1;
        int i = 0;
        for (; i < text.Length; ++i)
        {
            if (text[i] == 167)
            {
                ++i;
                continue;
            }
            if (text[i] == ' ')
            {
                lastSpaceIndex = i;
            }

            int charIndex = ChatAllowedCharacters.allowedCharacters.IndexOf(text[i]);
            if (charIndex >= 0)
            {
                width += _charWidth[charIndex + 32];
            }

            if (width > maxWidth)
            {
                if (lastSpaceIndex > 0)
                {
                    return lastSpaceIndex;
                }
                return Math.Max(1, i);
            }
        }
        return text.Length;
    }

    private void ProcessWrappedText(ReadOnlySpan<char> text, int x, int y, int maxWidth, uint color, bool draw, ref int outHeight)
    {
        if (text.IsEmpty) return;

        int totalHeight = 0;
        int currentY = y;

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

                while(subline.Length > 0 && subline[subline.Length - 1] == ' ')
                {
                    subline = subline.Slice(0, subline.Length - 1);
                }

                if (subline.Length > 0 || fitLength > 0)
                {
                    if (draw && subline.Length > 0)
                    {
                        DrawString(subline, x, currentY, color);
                    }
                    currentY += 8;
                    totalHeight += 8;
                }

                line = line.Slice(Math.Min(fitLength, line.Length));
                while(line.Length > 0 && line[0] == ' ')
                {
                    line = line.Slice(1);
                }
            }
        }

        if (totalHeight < 8) totalHeight = 8;
        outHeight = totalHeight;
    }

    public void DrawStringWrapped(ReadOnlySpan<char> text, int x, int y, int maxWidth, uint color)
    {
        int dummyHeight = 0;
        ProcessWrappedText(text, x, y, maxWidth, color, true, ref dummyHeight);
    }

    public int GetStringHeight(ReadOnlySpan<char> text, int maxWidth)
    {
        int height = 0;
        ProcessWrappedText(text, 0, 0, maxWidth, 0, false, ref height);
        return height;
    }
}
