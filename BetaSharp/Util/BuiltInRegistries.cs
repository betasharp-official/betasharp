using BetaSharp.Entities;

namespace BetaSharp.Util;

public static class BuiltInRegistries
{
    public static readonly IRegistry<EntityType> EntityTypes =
        new MappedRegistry<EntityType>(new ResourceLocation("betasharp", "entity_types"));

    public static void FreezeAll()
    {
        EntityTypes.Freeze();
    }
}
