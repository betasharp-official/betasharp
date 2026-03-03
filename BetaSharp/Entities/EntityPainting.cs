using BetaSharp.Blocks.Materials;
using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;

namespace BetaSharp.Entities;

public class EntityPainting : Entity
{
    private int _tickCounter;
    public int Direction;
    public int XPosition;
    public int YPosition;
    public int ZPosition;
    public EnumArt Art;

    public EntityPainting(World world) : base(world)
    {
        _tickCounter = 0;
        Direction = 0;
        standingEyeHeight = 0.0F;
        setBoundingBoxSpacing(0.5F, 0.5F);
    }

    public EntityPainting(World world, int xPosition, int yPosition, int zPosition, int direction) : this(world)
    {
        XPosition = xPosition;
        YPosition = yPosition;
        ZPosition = zPosition;

        List<EnumArt> validPaintings = new();

        foreach (var art in EnumArt.Values)
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
            Art = validPaintings[random.NextInt(validPaintings.Count)];
        }

        SetFacing(direction);
    }

    public EntityPainting(World world, int x, int y, int z, int direction, String title) : this(world)
    {
        XPosition = x;
        YPosition = y;
        ZPosition = z;

        Art = EnumArt.Values.FirstOrDefault(art => art.Title == title) ?? EnumArt.Kebab;

        SetFacing(direction);
    }

    protected override void initDataTracker()
    {
    }

    private void SetFacing(int facing)
    {
        Direction = facing;
        prevYaw = yaw = (facing * 90);

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
        setPosition(centerX, centerY, centerZ);

        float margin = -(0.1F / 16.0F);
        boundingBox = new Box(
            centerX - halfWidth - margin,
            centerY - halfHeight - margin,
            centerZ - halfDepth - margin,
            centerX + halfWidth + margin,
            centerY + halfHeight + margin,
            centerZ + halfDepth + margin);
    }

    private float GetArtOffset(int artSize)
    {
        return artSize == 32 ? 0.5F : (artSize == 64 ? 0.5F : 0.0F);
    }

    public override void tick()
    {
        if (_tickCounter++ == 100 && !world.isRemote)
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
        if (world.GetEntityCollisions(this, boundingBox).Count > 0)
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
                startX = MathHelper.Floor(x - (Art.SizeX / 32.0F));
                break;
            case 1:
            case 3:
                startZ = MathHelper.Floor(z - (Art.SizeX / 32.0F));
                break;
        }

        int startY = MathHelper.Floor(y - (Art.SizeY / 32.0F));

        for (int dx = 0; dx < widthInBlocks; ++dx)
        {
            for (int dy = 0; dy < heightInBlocks; ++dy)
            {
                Material material;
                if (Direction != 0 && Direction != 2)
                {
                    material = world.getMaterial(XPosition, startY + dy, startZ + dx);
                }
                else
                {
                    material = world.getMaterial(startX + dx, startY + dy, ZPosition);
                }

                if (!material.IsSolid)
                {
                    return false;
                }
            }
        }

        var entitiesInBox = world.getEntities(this, boundingBox);

        foreach (var entity in entitiesInBox)
        {
            if (entity is EntityPainting)
            {
                return false;
            }
        }

        return true;
    }

    public override bool isCollidable() => true;

    public override bool damage(Entity entity, int amount)
    {
        if (!dead && !world.isRemote)
        {
            scheduleVelocityUpdate();
            DropAsItem();
        }
        return true;
    }

    public override void writeNbt(NBTTagCompound nbt)
    {
        nbt.SetByte("Dir", (sbyte)Direction);
        nbt.SetString("Motive", Art.Title);
        nbt.SetInteger("TileX", XPosition);
        nbt.SetInteger("TileY", YPosition);
        nbt.SetInteger("TileZ", ZPosition);
    }

    public override void readNbt(NBTTagCompound nbt)
    {
        Direction = nbt.GetByte("Dir");
        XPosition = nbt.GetInteger("TileX");
        YPosition = nbt.GetInteger("TileY");
        ZPosition = nbt.GetInteger("TileZ");

        string motiveTitle = nbt.GetString("Motive");
        Art = EnumArt.Values.FirstOrDefault(art => art.Title == motiveTitle) ?? EnumArt.Kebab;

        SetFacing(Direction);
    }

    public override void move(double dx, double dy, double dz)
    {
        if (!world.isRemote && dx * dx + dy * dy + dz * dz > 0.0D)
        {
            DropAsItem();
        }
    }

    public override void addVelocity(double dx, double dy, double dz)
    {
        if (!world.isRemote && dx * dx + dy * dy + dz * dz > 0.0D)
        {
            DropAsItem();
        }
    }

    private void DropAsItem()
    {
        if (dead || world.isRemote) return;

        markDead();
        world.SpawnEntity(new EntityItem(world, x, y, z, new ItemStack(Item.Painting)));
    }
}
