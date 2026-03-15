using BetaSharp.Blocks;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Worlds.Chunks;

internal class BlockSource
{
    private static readonly ILogger<BlockSource> _logger = Log.Instance.For<BlockSource>();

    private static byte[] SanitizationTable = new byte[256];

    static BlockSource()
    {
        try
        {
            for (int i = 0; i < 256; i++)
            {
                byte blockId = (byte)i;

                if (blockId != 0 && Block.Blocks[blockId] == null)
                    blockId = 0;

                SanitizationTable[i] = blockId;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing block sanitization table");
        }
    }

    public static void Fill(byte[] blocks)
    {
        Span<byte> blocksSpan = blocks;

        for (int i = 0; i < blocksSpan.Length; i++)
        {
            blocksSpan[i] = SanitizationTable[blocksSpan[i]];
        }
    }
}
