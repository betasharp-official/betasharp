using BetaSharp.Entities;

namespace BetaSharp.Tests.Entities;

/// <summary>Smoke checks that concrete entity types sit under the expected abstract bases (regression guard for refactors).</summary>
[Collection("EntityTests")]
public sealed class EntityHierarchySmokeTests
{
    private readonly FakeWorldContext _world = new();

    [Fact]
    public void Mobs_and_projectiles_follow_expected_bases()
    {
        var zombie = new EntityZombie(_world);
        Assert.IsAssignableFrom<EntityCreature>(zombie);
        Assert.IsAssignableFrom<EntityMonster>(zombie);
        Assert.IsAssignableFrom<EntityLiving>(zombie);

        var pig = new EntityPig(_world);
        Assert.IsAssignableFrom<EntityAnimal>(pig);
        Assert.IsAssignableFrom<EntityCreature>(pig);

        var squid = new EntitySquid(_world);
        Assert.IsAssignableFrom<EntityWaterMob>(squid);
        Assert.IsAssignableFrom<EntityCreature>(squid);

        var ghast = new EntityGhast(_world);
        Assert.IsAssignableFrom<EntityFlying>(ghast);
        Assert.IsAssignableFrom<EntityLiving>(ghast);

        Assert.IsAssignableFrom<Entity>(new EntityArrow(_world));
        Assert.IsAssignableFrom<Entity>(new EntitySnowball(_world));
        Assert.False(zombie.Dead);
        Assert.False(pig.Dead);
        Assert.False(squid.Dead);
        Assert.False(ghast.Dead);
    }

    [Fact]
    public void Weather_effect_base()
    {
        Assert.IsAssignableFrom<EntityWeatherEffect>(new EntityLightningBolt(_world));
    }
}
