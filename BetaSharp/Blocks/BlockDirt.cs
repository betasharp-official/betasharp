using BetaSharp.Blocks.Materials;

namespace BetaSharp.Blocks;

internal class BlockDirt(int id, int textureId) : Block(id, textureId, Material.Soil);
