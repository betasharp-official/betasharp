using System.Reflection;
using BetaSharp.Entities;
using BetaSharp.NBT;

namespace BetaSharp.Tests.Entities;

/// <summary>
/// Exercises <see cref="Entity.Write"/> / <see cref="Entity.Read"/> for each registry type to lift line coverage on entity-specific NBT and shared serialization.
/// </summary>
[Collection("EntityTests")]
public sealed class EntityNbtRoundTripTests
{
    public static IEnumerable<object[]> RegistryEntityTypesExceptPlayer()
    {
        foreach (FieldInfo fi in typeof(EntityRegistry).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (fi.FieldType != typeof(EntityType))
            {
                continue;
            }

            if (fi.Name == nameof(EntityRegistry.Player))
            {
                continue;
            }

            yield return [(EntityType)fi.GetValue(null)!];
        }
    }

    [Theory]
    [MemberData(nameof(RegistryEntityTypesExceptPlayer))]
    public void SaveSelfNbt_round_trips_through_GetEntityFromNbt(EntityType type)
    {
        FakeWorldContext worldA = new();
        EntityTestHarness.PlaceStoneFloor(worldA, 0, 15, 0, 15, 63);

        Entity original = EntityTestHarness.CreateForNbtRoundTrip(type, worldA);
        var nbt = new NBTTagCompound();
        Assert.True(original.SaveSelfNbt(nbt));

        FakeWorldContext worldB = new();
        EntityTestHarness.PlaceStoneFloor(worldB, 0, 15, 0, 15, 63);

        Entity? loaded = EntityRegistry.GetEntityFromNbt(nbt, worldB);
        Assert.NotNull(loaded);
        Assert.Same(type, loaded!.Type);
        Assert.IsAssignableFrom(type.BaseType, loaded);
        Assert.Equal(original.X, loaded.X, 6);
        Assert.Equal(original.Y, loaded.Y, 6);
        Assert.Equal(original.Z, loaded.Z, 6);
    }
}
