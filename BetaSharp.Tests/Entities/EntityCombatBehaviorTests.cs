using System.Linq;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Tests.Entities;

[Collection("EntityTests")]
public sealed class EntityCombatBehaviorTests
{
    [Fact]
    public void Skeleton_attack_spawns_arrow_and_sets_cooldown()
    {
        FakeWorldContext world = new();
        var skeleton = new TestSkeleton(world);
        skeleton.SetPositionAndAngles(8.5, 65.0, 8.5, 0f, 0f);
        Assert.True(world.Entities.SpawnEntity(skeleton));

        var pig = new EntityPig(world);
        pig.SetPositionAndAngles(10.0, 65.0, 8.5, 0f, 0f);
        Assert.True(world.Entities.SpawnEntity(pig));

        skeleton.ForceAttack(pig, 2.0f);

        Assert.Equal(30, skeleton.ExposedAttackTime);
        Assert.True(skeleton.ExposedHasAttacked);
        Assert.Equal(Item.BOW.id, skeleton.HeldItem.ItemId);
    }

    [Fact]
    public void Spider_attack_eventually_performs_leap()
    {
        FakeWorldContext world = new();
        var spider = new TestSpider(world);
        spider.OnGround = true;
        spider.SetPositionAndAngles(8.5, 65.0, 8.5, 0f, 0f);
        Assert.True(world.Entities.SpawnEntity(spider));

        var pig = new EntityPig(world);
        pig.SetPositionAndAngles(11.5, 65.0, 8.5, 0f, 0f);
        Assert.True(world.Entities.SpawnEntity(pig));

        bool leaped = false;
        for (int i = 0; i < 64; i++)
        {
            spider.ForceAttack(pig, 3.0f);
            if (spider.VelocityY > 0.0)
            {
                leaped = true;
                break;
            }
        }

        Assert.True(leaped);
    }

    [Fact]
    public void Wolf_damage_from_player_sets_angry_and_target()
    {
        FakeWorldContext world = new();
        var wolf = new EntityWolf(world);
        wolf.SetPositionAndAngles(8.5, 65.0, 8.5, 0f, 0f);
        Assert.True(world.Entities.SpawnEntity(wolf));

        var player = new TestEntityPlayer(world) { Name = "tester" };
        player.SetPositionAndAngles(9.0, 65.0, 8.5, 0f, 0f);
        Assert.True(world.Entities.SpawnEntity(player));

        Assert.True(wolf.Damage(player, 1));
        Assert.True(wolf.IsWolfAngry);
        Assert.Same(player, wolf.Target);
        Assert.Contains("angry", wolf.GetTexture(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Wolf_server_status_shaking_branch_executes_without_throwing()
    {
        FakeWorldContext world = new();
        var wolf = new EntityWolf(world);

        wolf.ProcessServerEntityStatus(8);
        wolf.Tick();

        Assert.True(wolf.getShadingWhileShaking(0.5f) >= 0f);
    }

    [Fact]
    public void Projectile_velocity_client_sets_angles_for_arrow_egg_and_snowball()
    {
        FakeWorldContext world = new();

        var arrow = new EntityArrow(world, 8.5, 65.0, 8.5);
        arrow.SetVelocityClient(0.2, 0.1, -0.3);
        Assert.NotEqual(0f, arrow.Yaw);
        Assert.NotEqual(0f, arrow.Pitch);

        var egg = new EntityEgg(world, 8.5, 65.0, 8.5);
        egg.SetVelocityClient(0.2, 0.1, -0.3);
        Assert.NotEqual(0f, egg.Yaw);
        Assert.NotEqual(0f, egg.Pitch);

        var snowball = new EntitySnowball(world, 8.5, 65.0, 8.5);
        snowball.SetVelocityClient(0.2, 0.1, -0.3);
        Assert.NotEqual(0f, snowball.Yaw);
        Assert.NotEqual(0f, snowball.Pitch);
    }

    [Fact]
    public void Wolf_nbt_roundtrip_preserves_owner_and_sitting_state()
    {
        FakeWorldContext worldA = new();
        var wolf = new EntityWolf(worldA);
        wolf.WolfOwner = "owner";
        wolf.IsWolfSitting = true;
        wolf.SetPositionAndAngles(8.5, 65.0, 8.5, 0f, 0f);

        var nbt = new NBTTagCompound();
        Assert.True(wolf.SaveSelfNbt(nbt));

        FakeWorldContext worldB = new();
        Entity? loaded = EntityRegistry.GetEntityFromNbt(nbt, worldB);
        var loadedWolf = Assert.IsType<EntityWolf>(loaded);
        Assert.Equal("owner", loadedWolf.WolfOwner);
        Assert.True(loadedWolf.IsWolfSitting);
    }

    private sealed class TestSkeleton(IWorldContext world) : EntitySkeleton(world)
    {
        public int ExposedAttackTime => AttackTime;
        public bool ExposedHasAttacked => HasAttacked;
        public void ForceAttack(Entity target, float distance) => attackEntity(target, distance);
    }

    private sealed class TestSpider(IWorldContext world) : EntitySpider(world)
    {
        public void ForceAttack(Entity target, float distance) => attackEntity(target, distance);
    }
}
