using BetaSharp.Items;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntityChicken : EntityAnimal
{
    private float _flapSpeed = 1.0F;
    private int _timeUntilNextEgg;
    public float DestPos;
    public float FlapProgress;
    public bool Jockey = false;
    public float PrevDestPos;
    public float PrevFlapProgress;

    public EntityChicken(IWorldContext world) : base(world)
    {
        Texture = "/mob/chicken.png";
        SetBoundingBoxSpacing(0.3F, 0.4F);
        Health = 4;
        _timeUntilNextEgg = Random.NextInt(6000) + 6000;
    }

    public override EntityType Type => EntityRegistry.Chicken;

    protected sealed override void SetBoundingBoxSpacing(float widthOffset, float heightOffset) => base.SetBoundingBoxSpacing(widthOffset, heightOffset);

    protected override void TickMovement()
    {
        base.TickMovement();
        if (World.IsRemote)
        {
            OnGround = Math.Abs(Y - PrevY) < 0.02D;
        }

        PrevFlapProgress = FlapProgress;
        PrevDestPos = DestPos;
        DestPos = (float)(DestPos + (OnGround ? -1 : 4) * 0.3D);
        if (DestPos < 0.0F)
        {
            DestPos = 0.0F;
        }

        if (DestPos > 1.0F)
        {
            DestPos = 1.0F;
        }

        if (!OnGround && _flapSpeed < 1.0F)
        {
            _flapSpeed = 1.0F;
        }

        _flapSpeed = (float)(_flapSpeed * 0.9D);
        if (!OnGround && VelocityY < 0.0D)
        {
            VelocityY *= 0.6D;
        }

        FlapProgress += _flapSpeed * 2.0F;
        if (World.IsRemote || --_timeUntilNextEgg > 0)
        {
            return;
        }

        World.Broadcaster.PlaySoundAtEntity(this, "mob.chickenplop", 1.0F, (Random.NextFloat() - Random.NextFloat()) * 0.2F + 1.0F);
        DropItem(Item.Egg.id, 1);
        _timeUntilNextEgg = Random.NextInt(6000) + 6000;
    }

    protected override void OnLanding(float fallDistance)
    {
    }

    protected override string? LivingSound => "mob.chicken";

    protected override string? HurtSound => "mob.chickenhurt";

    protected override string? DeathSound => "mob.chickenhurt";

    protected override int DropItemId => Item.Feather.id;
}
