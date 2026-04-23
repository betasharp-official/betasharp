using BetaSharp.Blocks.Materials;
using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntityPainting : Entity
{
    public override EntityType Type => EntityRegistry.Painting;
    private int _tickCounter;
    public int Direction;
    public int XPosition;
    public int YPosition;
    public int ZPosition;
    public Painting Art;

    public EntityPainting(IWorldContext world) : base(world)
    {
        _tickCounter = 0;
        Direction = 0;
        StandingEyeHeight = 0.0F;
        SetBoundingBoxSpacing(0.5F, 0.5F);
    }

    public EntityPainting(IWorldContext world, int xPosition, int yPosition, int zPosition, int direction) : this(world)
    {
        XPosition = xPosition;
        YPosition = yPosition;
        ZPosition = zPosition;

        List<Painting> validPaintings = new();

        foreach (var art in Painting.Values)
        {
            Art = art;
            SetFacing(direction);
            if (CanHangOnWall())
            {
                validPaintings.Add(art);
            }
        }

        if (validPaintings.Count > 0)
        {
            Art = validPaintings[Random.NextInt(validPaintings.Count)];
        }

        SetFacing(direction);
    }

    public EntityPainting(IWorldContext world, int x, int y, int z, int direction, String title) : this(world)
    {
        XPosition = x;
        YPosition = y;
        ZPosition = z;

        Art = Painting.Values.FirstOrDefault(art => art.Title == title) ?? Painting.Kebab;

        SetFacing(direction);
    }


    private void SetFacing(int facing)
    {
        Direction = facing;
        PrevYaw = Yaw = (facing * 90);

        float halfWidth = Art.SizeX;
        float halfHeight = Art.SizeY;
        float halfDepth = Art.SizeX;

        if (facing != 0 && facing != 2)
            halfWidth = 0.5F;
        else
            halfDepth = 0.5F;

        halfWidth /= 32.0F;
        halfHeight /= 32.0F;
        halfDepth /= 32.0F;

        float centerX = XPosition + 0.5F;
        float centerY = YPosition + 0.5F;
        float centerZ = ZPosition + 0.5F;
        float wallOffset = 9.0F / 16.0F;

        switch (facing)
        {
            case 0:
                centerZ -= wallOffset;
                centerX -= GetArtOffset(Art.SizeX);
                break;
            case 1:
                centerX -= wallOffset;
                centerZ += GetArtOffset(Art.SizeX);
                break;
            case 2:
                centerZ += wallOffset;
                centerX += GetArtOffset(Art.SizeX);
                break;
            case 3:
                centerX += wallOffset;
                centerZ -= GetArtOffset(Art.SizeX);
                break;
        }

        centerY += GetArtOffset(Art.SizeY);
        SetPosition(centerX, centerY, centerZ);

        float margin = -(0.1F / 16.0F);
        BoundingBox = new Box(
            centerX - halfWidth - margin,
            centerY - halfHeight - margin,
            centerZ - halfDepth - margin,
            centerX + halfWidth + margin,
            centerY + halfHeight + margin,
            centerZ + halfDepth + margin);
    }

    private static float GetArtOffset(int artSize)
    {
        return artSize == 32 ? 0.5F : (artSize == 64 ? 0.5F : 0.0F);
    }

    public override void Tick()
    {
        if (_tickCounter++ == 100 && !World.IsRemote)
        {
            _tickCounter = 0;
            if (!CanHangOnWall())
            {
                DropAsItem();
            }
        }
    }

    public bool CanHangOnWall()
    {
        if (World.Entities.GetEntityCollisionsScratch(this, BoundingBox).Count > 0)
        {
            return false;
        }

        int widthInBlocks = Art.SizeX / 16;
        int heightInBlocks = Art.SizeY / 16;
        int startX = XPosition;
        int startZ = ZPosition;

        switch (Direction)
        {
            case 0:
            case 2:
                startX = MathHelper.Floor(X - (Art.SizeX / 32.0F));
                break;
            case 1:
            case 3:
                startZ = MathHelper.Floor(Z - (Art.SizeX / 32.0F));
                break;
        }

        int startY = MathHelper.Floor(Y - (Art.SizeY / 32.0F));

        for (int dx = 0; dx < widthInBlocks; ++dx)
        {
            for (int dy = 0; dy < heightInBlocks; ++dy)
            {
                Material material;
                if (Direction != 0 && Direction != 2)
                {
                    material = World.Reader.GetMaterial(XPosition, startY + dy, startZ + dx);
                }
                else
                {
                    material = World.Reader.GetMaterial(startX + dx, startY + dy, ZPosition);
                }

                if (!material.IsSolid)
                {
                    return false;
                }
            }
        }

        var entitiesInBox = World.Entities.GetEntities(this, BoundingBox);

        foreach (var entity in entitiesInBox)
        {
            if (entity is EntityPainting)
            {
                return false;
            }
        }

        return true;
    }

    public override bool IsCollidable() => true;

    public override bool Damage(Entity entity, int amount)
    {
        if (!Dead && !World.IsRemote)
        {
            ScheduleVelocityUpdate();
            DropAsItem();
        }
        return true;
    }

    public override void WriteNbt(NBTTagCompound nbt)
    {
        nbt.SetByte("Dir", (sbyte)Direction);
        nbt.SetString("Motive", Art.Title);
        nbt.SetInteger("TileX", XPosition);
        nbt.SetInteger("TileY", YPosition);
        nbt.SetInteger("TileZ", ZPosition);
    }

    public override void ReadNbt(NBTTagCompound nbt)
    {
        Direction = nbt.GetByte("Dir");
        XPosition = nbt.GetInteger("TileX");
        YPosition = nbt.GetInteger("TileY");
        ZPosition = nbt.GetInteger("TileZ");

        string motiveTitle = nbt.GetString("Motive");
        Art = Painting.Values.FirstOrDefault(art => art.Title == motiveTitle) ?? Painting.Kebab;

        SetFacing(Direction);
    }

    public override void Move(double dx, double dy, double dz)
    {
        if (!World.IsRemote && dx * dx + dy * dy + dz * dz > 0.0D)
        {
            DropAsItem();
        }
    }

    public override void AddVelocity(double dx, double dy, double dz)
    {
        if (!World.IsRemote && dx * dx + dy * dy + dz * dz > 0.0D)
        {
            DropAsItem();
        }
    }

    private void DropAsItem()
    {
        if (Dead || World.IsRemote) return;

        MarkDead();
        World.SpawnEntity(new EntityItem(World, X, Y, Z, new ItemStack(Item.Painting)));
    }
}
