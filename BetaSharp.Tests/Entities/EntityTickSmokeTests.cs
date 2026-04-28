using System.Reflection;
using BetaSharp.Entities;

namespace BetaSharp.Tests.Entities;

/// <summary>Runs a short simulated tick loop for each registry-spawnable entity to catch immediate exceptions.</summary>
[Collection("EntityTests")]
public sealed class EntityTickSmokeTests
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

            yield return [fi.Name, (EntityType)fi.GetValue(null)!];
        }
    }

    [Theory]
    [MemberData(nameof(RegistryEntityTypesExceptPlayer))]
    public void Spawn_on_stone_floor_then_tick_does_not_throw(string registryFieldName, EntityType type)
    {
        FakeWorldContext world = new();
        EntityTestHarness.PlaceStoneFloor(world, 0, 15, 0, 15, 63);
        Entity entity = EntityTestHarness.CreateSpawned(world, type, 8.5, 65.0, 8.5);
        int beforeTicksAlive = EntityTestHarness.AliveEntityCount(world);
        EntityTestHarness.AdvanceGameTicks(world, 64);
        Assert.False(string.IsNullOrEmpty(registryFieldName));
        Assert.NotNull(entity);
        Assert.IsAssignableFrom(type.BaseType, entity);
        Assert.True(beforeTicksAlive >= 1);
        Assert.True(EntityTestHarness.AliveEntityCount(world) >= 0);
    }
}
