using BetaSharp.Blocks.Materials;

namespace BetaSharp.Blocks;

internal class BlockCobblestone(int id) : Block(id, BlockTextures.Cobblestone, Material.Stone);
