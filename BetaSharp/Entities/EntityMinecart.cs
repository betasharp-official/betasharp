using BetaSharp.Blocks;
using BetaSharp.Inventorys;
using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Entities;

//TODO: BREAKING MINECART CRASHES THE GAME!!
public class EntityMinecart : Entity, IInventory
{
    private static readonly int[][][] field_855_j =
    [
        [[0, 0, -1], [0, 0, 1]],
        [[-1, 0, 0], [1, 0, 0]],
        [[-1, -1, 0], [1, 0, 0]],
        [[-1, 0, 0], [1, -1, 0]],
        [[0, 0, -1], [0, -1, 1]],
        [[0, -1, -1], [0, 0, 1]],
        [[0, 0, 1], [1, 0, 0]],
        [[0, 0, 1], [-1, 0, 0]],
        [[0, 0, -1], [-1, 0, 0]],
        [[0, 0, -1], [1, 0, 0]]
    ];

    private readonly ILogger<EntityMinecart> _logger = Log.Instance.For<EntityMinecart>();
    private ItemStack[] cargoItems;
    private double cartVelocityX;
    private double cartVelocityY;
    private double cartVelocityZ;
    private double field_9410_p;
    private double field_9411_o;
    private double field_9412_n;
    private double field_9413_m;
    private double field_9414_l;

    private int field_9415_k;
    public int fuel;
    public int minecartCurrentDamage;
    public int minecartRockDirection;
    public int minecartTimeSinceHit;
    public double pushX;
    public double pushZ;
    public int type;
    private bool yawFlipped;

    public EntityMinecart(IWorldContext world) : base(world)
    {
        cargoItems = new ItemStack[36];
        minecartCurrentDamage = 0;
        minecartTimeSinceHit = 0;
        minecartRockDirection = 1;
        yawFlipped = false;
        preventEntitySpawning = true;
        setBoundingBoxSpacing(0.98F, 0.7F);
        standingEyeHeight = height / 2.0F;
    }

    public EntityMinecart(IWorldContext world, double x, double y, double z, int type) : this(world)
    {
        setPosition(x, y + standingEyeHeight, z);
        velocityX = 0.0D;
        velocityY = 0.0D;
        velocityZ = 0.0D;
        prevX = x;
        prevY = y;
        prevZ = z;
        this.type = type;
    }

    public int size() => 27;

    public ItemStack getStack(int slotIndex) => cargoItems[slotIndex];

    public ItemStack removeStack(int slotIndex, int amount)
    {
        if (cargoItems[slotIndex] != null)
        {
            ItemStack itemStack;
            if (cargoItems[slotIndex].count <= amount)
            {
                itemStack = cargoItems[slotIndex];
                cargoItems[slotIndex] = null;
                return itemStack;
            }

            itemStack = cargoItems[slotIndex].split(amount);
            if (cargoItems[slotIndex].count == 0)
            {
                cargoItems[slotIndex] = null;
            }

            return itemStack;
        }

        return null;
    }

    public void setStack(int slotIndex, ItemStack? itemStack)
    {
        cargoItems[slotIndex] = itemStack;
        if (itemStack != null && itemStack.count > getMaxCountPerStack())
        {
            itemStack.count = getMaxCountPerStack();
        }
    }

    public string getName() => "Minecart";

    public int getMaxCountPerStack() => 64;

    public void markDirty()
    {
    }

    public bool canPlayerUse(EntityPlayer player) => dead ? false : player.getSquaredDistance(this) <= 64.0D;

    protected override bool bypassesSteppingEffects() => false;

    public override Box? getCollisionAgainstShape(Entity entity) => entity.boundingBox;

    public override Box? getBoundingBox() => null;

    public override bool isPushable() => true;

    public override double getPassengerRidingHeight() => height * 0.0D - 0.3F;

    public override bool damage(Entity entity, int amount)
    {
        if (!world.IsRemote && !dead)
        {
            minecartRockDirection = -minecartRockDirection;
            minecartTimeSinceHit = 10;
            scheduleVelocityUpdate();
            minecartCurrentDamage += amount * 10;
            if (minecartCurrentDamage > 40)
            {
                if (passenger != null)
                {
                    passenger.setVehicle(this);
                }

                markDead();
                dropItem(Item.Minecart.id, 1, 0.0F);
                if (type == 1)
                {
                    EntityMinecart minecart = this;

                    for (int slotIndex = 0; slotIndex < minecart.size(); ++slotIndex)
                    {
                        ItemStack itemStack = minecart.getStack(slotIndex);
                        if (itemStack != null)
                        {
                            float offsetX = random.NextFloat() * 0.8F + 0.1F;
                            float offsetY = random.NextFloat() * 0.8F + 0.1F;
                            float offsetZ = random.NextFloat() * 0.8F + 0.1F;

                            while (itemStack.count > 0)
                            {
                                int dropCount = random.NextInt(21) + 10;
                                if (dropCount > itemStack.count)
                                {
                                    dropCount = itemStack.count;
                                }

                                itemStack.count -= dropCount;
                                EntityItem entityItem = new(world, x + offsetX, y + offsetY, z + offsetZ, new ItemStack(itemStack.itemId, dropCount, itemStack.getDamage()));
                                float scatterSpeed = 0.05F;
                                entityItem.velocityX = (float)random.NextGaussian() * scatterSpeed;
                                entityItem.velocityY = (float)random.NextGaussian() * scatterSpeed + 0.2F;
                                entityItem.velocityZ = (float)random.NextGaussian() * scatterSpeed;
                                world.SpawnEntity(entityItem);
                            }
                        }
                    }

                    dropItem(Block.Chest.id, 1, 0.0F);
                }
                else if (type == 2)
                {
                    dropItem(Block.Furnace.id, 1, 0.0F);
                }
            }

            return true;
        }

        return true;
    }

    public override void animateHurt()
    {
        _logger.LogInformation("Animating hurt");
        minecartRockDirection = -minecartRockDirection;
        minecartTimeSinceHit = 10;
        minecartCurrentDamage += minecartCurrentDamage * 10;
    }

    public override bool isCollidable() => !dead;

    public override void markDead()
    {
        for (int slotIndex = 0; slotIndex < size(); ++slotIndex)
        {
            ItemStack itemStack = getStack(slotIndex);
            if (itemStack != null)
            {
                float offsetX = random.NextFloat() * 0.8F + 0.1F;
                float offsetY = random.NextFloat() * 0.8F + 0.1F;
                float offsetZ = random.NextFloat() * 0.8F + 0.1F;

                while (itemStack.count > 0)
                {
                    int dropCount = random.NextInt(21) + 10;
                    if (dropCount > itemStack.count)
                    {
                        dropCount = itemStack.count;
                    }

                    itemStack.count -= dropCount;
                    EntityItem entityItem = new(world, x + offsetX, y + offsetY, z + offsetZ, new ItemStack(itemStack.itemId, dropCount, itemStack.getDamage()));
                    float scatterSpeed = 0.05F;
                    entityItem.velocityX = (float)random.NextGaussian() * scatterSpeed;
                    entityItem.velocityY = (float)random.NextGaussian() * scatterSpeed + 0.2F;
                    entityItem.velocityZ = (float)random.NextGaussian() * scatterSpeed;
                    world.SpawnEntity(entityItem);
                }
            }
        }

        base.markDead();
    }

    public override void tick()
    {
        if (minecartTimeSinceHit > 0)
        {
            --minecartTimeSinceHit;
        }

        if (minecartCurrentDamage > 0)
        {
            --minecartCurrentDamage;
        }

        double var7;
        if (world.IsRemote && field_9415_k > 0)
        {
            if (field_9415_k > 0)
            {
                double var46 = x + (field_9414_l - x) / field_9415_k;
                double var47 = y + (field_9413_m - y) / field_9415_k;
                double var5 = z + (field_9412_n - z) / field_9415_k;

                for (var7 = field_9411_o - yaw; var7 < -180.0D; var7 += 360.0D)
                {
                }

                while (var7 >= 180.0D)
                {
                    var7 -= 360.0D;
                }

                yaw = (float)(yaw + var7 / field_9415_k);
                pitch = (float)(pitch + (field_9410_p - pitch) / field_9415_k);
                --field_9415_k;
                setPosition(var46, var47, var5);
                setRotation(yaw, pitch);
            }
            else
            {
                setPosition(x, y, z);
                setRotation(yaw, pitch);
            }
        }
        else
        {
            prevX = x;
            prevY = y;
            prevZ = z;
            velocityY -= 0.04F;
            int floorX = MathHelper.Floor(x);
            int floorY = MathHelper.Floor(y);
            int floorZ = MathHelper.Floor(z);
            if (BlockRail.IsRail(world, floorX, floorY - 1, floorZ))
            {
                --floorY;
            }

            double var4 = 0.4D;
            bool var6 = false;
            var7 = 1.0D / 128.0D;
            int var9 = world.Reader.GetBlockId(floorX, floorY, floorZ);
            if (BlockRail.IsRail(var9))
            {
                Vec3D? var10 = func_514_g(x, y, z);
                int var11 = world.Reader.GetBlockMeta(floorX, floorY, floorZ);
                y = floorY;
                bool var12 = false;
                bool var13 = false;
                if (var9 == Block.PoweredRail.id)
                {
                    var12 = (var11 & 8) != 0;
                    var13 = !var12;
                }

                if (((BlockRail)Block.Blocks[var9]).isAlwaysStraight())
                {
                    var11 &= 7;
                }

                if (var11 >= 2 && var11 <= 5)
                {
                    y = floorY + 1;
                }

                if (var11 == 2)
                {
                    velocityX -= var7;
                }

                if (var11 == 3)
                {
                    velocityX += var7;
                }

                if (var11 == 4)
                {
                    velocityZ += var7;
                }

                if (var11 == 5)
                {
                    velocityZ -= var7;
                }

                int[][] var14 = field_855_j[var11];
                double var15 = var14[1][0] - var14[0][0];
                double var17 = var14[1][2] - var14[0][2];
                double var19 = Math.Sqrt(var15 * var15 + var17 * var17);
                double var21 = velocityX * var15 + velocityZ * var17;
                if (var21 < 0.0D)
                {
                    var15 = -var15;
                    var17 = -var17;
                }

                double var23 = Math.Sqrt(velocityX * velocityX + velocityZ * velocityZ);
                velocityX = var23 * var15 / var19;
                velocityZ = var23 * var17 / var19;
                double var25;
                if (var13)
                {
                    var25 = Math.Sqrt(velocityX * velocityX + velocityZ * velocityZ);
                    if (var25 < 0.03D)
                    {
                        velocityX *= 0.0D;
                        velocityY *= 0.0D;
                        velocityZ *= 0.0D;
                    }
                    else
                    {
                        velocityX *= 0.5D;
                        velocityY *= 0.0D;
                        velocityZ *= 0.5D;
                    }
                }

                var25 = 0.0D;
                double var27 = floorX + 0.5D + var14[0][0] * 0.5D;
                double var29 = floorZ + 0.5D + var14[0][2] * 0.5D;
                double var31 = floorX + 0.5D + var14[1][0] * 0.5D;
                double var33 = floorZ + 0.5D + var14[1][2] * 0.5D;
                var15 = var31 - var27;
                var17 = var33 - var29;
                double var35;
                double var37;
                double var39;
                if (var15 == 0.0D)
                {
                    x = floorX + 0.5D;
                    var25 = z - floorZ;
                }
                else if (var17 == 0.0D)
                {
                    z = floorZ + 0.5D;
                    var25 = x - floorX;
                }
                else
                {
                    var35 = x - var27;
                    var37 = z - var29;
                    var39 = (var35 * var15 + var37 * var17) * 2.0D;
                    var25 = var39;
                }

                x = var27 + var15 * var25;
                z = var29 + var17 * var25;
                setPosition(x, y + standingEyeHeight, z);
                var35 = velocityX;
                var37 = velocityZ;
                if (passenger != null)
                {
                    var35 *= 0.75D;
                    var37 *= 0.75D;
                }

                if (var35 < -var4)
                {
                    var35 = -var4;
                }

                if (var35 > var4)
                {
                    var35 = var4;
                }

                if (var37 < -var4)
                {
                    var37 = -var4;
                }

                if (var37 > var4)
                {
                    var37 = var4;
                }

                move(var35, 0.0D, var37);
                if (var14[0][1] != 0 && MathHelper.Floor(x) - floorX == var14[0][0] && MathHelper.Floor(z) - floorZ == var14[0][2])
                {
                    setPosition(x, y + var14[0][1], z);
                }
                else if (var14[1][1] != 0 && MathHelper.Floor(x) - floorX == var14[1][0] && MathHelper.Floor(z) - floorZ == var14[1][2])
                {
                    setPosition(x, y + var14[1][1], z);
                }

                if (passenger != null)
                {
                    velocityX *= 0.997F;
                    velocityY *= 0.0D;
                    velocityZ *= 0.997F;
                }
                else
                {
                    if (type == 2)
                    {
                        var39 = MathHelper.Sqrt(pushX * pushX + pushZ * pushZ);
                        if (var39 > 0.01D)
                        {
                            var6 = true;
                            pushX /= var39;
                            pushZ /= var39;
                            double var41 = 0.04D;
                            velocityX *= 0.8F;
                            velocityY *= 0.0D;
                            velocityZ *= 0.8F;
                            velocityX += pushX * var41;
                            velocityZ += pushZ * var41;
                        }
                        else
                        {
                            velocityX *= 0.9F;
                            velocityY *= 0.0D;
                            velocityZ *= 0.9F;
                        }
                    }

                    velocityX *= 0.96F;
                    velocityY *= 0.0D;
                    velocityZ *= 0.96F;
                }

                Vec3D? var52 = func_514_g(x, y, z);
                if (var52 != null && var10 != null)
                {
                    double var40 = (var10.Value.y - var52.Value.y) * 0.05D;
                    var23 = Math.Sqrt(velocityX * velocityX + velocityZ * velocityZ);
                    if (var23 > 0.0D)
                    {
                        velocityX = velocityX / var23 * (var23 + var40);
                        velocityZ = velocityZ / var23 * (var23 + var40);
                    }

                    setPosition(x, var52.Value.y, z);
                }

                int var53 = MathHelper.Floor(x);
                int var54 = MathHelper.Floor(z);
                if (var53 != floorX || var54 != floorZ)
                {
                    var23 = Math.Sqrt(velocityX * velocityX + velocityZ * velocityZ);
                    velocityX = var23 * (var53 - floorX);
                    velocityZ = var23 * (var54 - floorZ);
                }

                double var42;
                if (type == 2)
                {
                    var42 = MathHelper.Sqrt(pushX * pushX + pushZ * pushZ);
                    if (var42 > 0.01D && velocityX * velocityX + velocityZ * velocityZ > 0.001D)
                    {
                        pushX /= var42;
                        pushZ /= var42;
                        if (pushX * velocityX + pushZ * velocityZ < 0.0D)
                        {
                            pushX = 0.0D;
                            pushZ = 0.0D;
                        }
                        else
                        {
                            pushX = velocityX;
                            pushZ = velocityZ;
                        }
                    }
                }

                if (var12)
                {
                    var42 = Math.Sqrt(velocityX * velocityX + velocityZ * velocityZ);
                    if (var42 > 0.01D)
                    {
                        double var44 = 0.06D;
                        velocityX += velocityX / var42 * var44;
                        velocityZ += velocityZ / var42 * var44;
                    }
                    else if (var11 == 1)
                    {
                        if (world.Reader.ShouldSuffocate(floorX - 1, floorY, floorZ))
                        {
                            velocityX = 0.02D;
                        }
                        else if (world.Reader.ShouldSuffocate(floorX + 1, floorY, floorZ))
                        {
                            velocityX = -0.02D;
                        }
                    }
                    else if (var11 == 0)
                    {
                        if (world.Reader.ShouldSuffocate(floorX, floorY, floorZ - 1))
                        {
                            velocityZ = 0.02D;
                        }
                        else if (world.Reader.ShouldSuffocate(floorX, floorY, floorZ + 1))
                        {
                            velocityZ = -0.02D;
                        }
                    }
                }
            }
            else
            {
                if (velocityX < -var4)
                {
                    velocityX = -var4;
                }

                if (velocityX > var4)
                {
                    velocityX = var4;
                }

                if (velocityZ < -var4)
                {
                    velocityZ = -var4;
                }

                if (velocityZ > var4)
                {
                    velocityZ = var4;
                }

                if (onGround)
                {
                    velocityX *= 0.5D;
                    velocityY *= 0.5D;
                    velocityZ *= 0.5D;
                }

                move(velocityX, velocityY, velocityZ);
                if (!onGround)
                {
                    velocityX *= 0.95F;
                    velocityY *= 0.95F;
                    velocityZ *= 0.95F;
                }
            }

            pitch = 0.0F;
            double var48 = prevX - x;
            double var49 = prevZ - z;
            if (var48 * var48 + var49 * var49 > 0.001D)
            {
                yaw = (float)(Math.Atan2(var49, var48) * 180.0D / Math.PI);
                if (yawFlipped)
                {
                    yaw += 180.0F;
                }
            }

            double var50;
            for (var50 = yaw - prevYaw; var50 >= 180.0D; var50 -= 360.0D)
            {
            }

            while (var50 < -180.0D)
            {
                var50 += 360.0D;
            }

            if (var50 < -170.0D || var50 >= 170.0D)
            {
                yaw += 180.0F;
                yawFlipped = !yawFlipped;
            }

            setRotation(yaw, pitch);
            var var16 = world.Entities.GetEntities(this, boundingBox.Expand(0.2F, 0.0D, 0.2F));
            if (var16 != null && var16.Count > 0)
            {
                for (int var51 = 0; var51 < var16.Count; ++var51)
                {
                    Entity var18 = var16[var51];
                    if (var18 != passenger && var18.isPushable() && var18 is EntityMinecart)
                    {
                        var18.onCollision(this);
                    }
                }
            }

            if (passenger != null && passenger.dead)
            {
                passenger = null;
            }

            if (var6 && random.NextInt(4) == 0)
            {
                --fuel;
                if (fuel < 0)
                {
                    pushX = pushZ = 0.0D;
                }

                world.Broadcaster.AddParticle("largesmoke", x, y + 0.8D, z, 0.0D, 0.0D, 0.0D);
            }
        }
    }

    public Vec3D? func_515_a(double x, double y, double z, double var7)
    {
        int var9 = MathHelper.Floor(x);
        int var10 = MathHelper.Floor(y);
        int var11 = MathHelper.Floor(z);
        if (BlockRail.IsRail(world, var9, var10 - 1, var11))
        {
            --var10;
        }

        int var12 = world.Reader.GetBlockId(var9, var10, var11);
        if (!BlockRail.IsRail(var12))
        {
            return null;
        }

        int var13 = world.Reader.GetBlockMeta(var9, var10, var11);
        if (((BlockRail)Block.Blocks[var12]).isAlwaysStraight())
        {
            var13 &= 7;
        }

        y = var10;
        if (var13 >= 2 && var13 <= 5)
        {
            y = var10 + 1;
        }

        int[][] var14 = field_855_j[var13];
        double var15 = var14[1][0] - var14[0][0];
        double var17 = var14[1][2] - var14[0][2];
        double var19 = Math.Sqrt(var15 * var15 + var17 * var17);
        var15 /= var19;
        var17 /= var19;
        x += var15 * var7;
        z += var17 * var7;
        if (var14[0][1] != 0 && MathHelper.Floor(x) - var9 == var14[0][0] && MathHelper.Floor(z) - var11 == var14[0][2])
        {
            y += var14[0][1];
        }
        else if (var14[1][1] != 0 && MathHelper.Floor(x) - var9 == var14[1][0] && MathHelper.Floor(z) - var11 == var14[1][2])
        {
            y += var14[1][1];
        }

        return func_514_g(x, y, z);
    }

    public Vec3D? func_514_g(double x, double y, double z)
    {
        int floorX = MathHelper.Floor(x);
        int floorY = MathHelper.Floor(y);
        int floorZ = MathHelper.Floor(z);
        if (BlockRail.IsRail(world, floorX, floorY - 1, floorZ))
        {
            --floorY;
        }

        int blockId = world.Reader.GetBlockId(floorX, floorY, floorZ);
        if (BlockRail.IsRail(blockId))
        {
            int meta = world.Reader.GetBlockMeta(floorX, floorY, floorZ);
            y = floorY;
            if (((BlockRail)Block.Blocks[blockId]).isAlwaysStraight())
            {
                meta &= 7;
            }

            if (meta >= 2 && meta <= 5)
            {
                y = floorY + 1;
            }

            int[][] var12 = field_855_j[meta];
            double var13 = 0.0D;
            double var15 = floorX + 0.5D + var12[0][0] * 0.5D;
            double var17 = floorY + 0.5D + var12[0][1] * 0.5D;
            double var19 = floorZ + 0.5D + var12[0][2] * 0.5D;
            double var21 = floorX + 0.5D + var12[1][0] * 0.5D;
            double var23 = floorY + 0.5D + var12[1][1] * 0.5D;
            double var25 = floorZ + 0.5D + var12[1][2] * 0.5D;
            double var27 = var21 - var15;
            double var29 = (var23 - var17) * 2.0D;
            double var31 = var25 - var19;
            if (var27 == 0.0D)
            {
                x = floorX + 0.5D;
                var13 = z - floorZ;
            }
            else if (var31 == 0.0D)
            {
                z = floorZ + 0.5D;
                var13 = x - floorX;
            }
            else
            {
                double var33 = x - var15;
                double var35 = z - var19;
                double var37 = (var33 * var27 + var35 * var31) * 2.0D;
                var13 = var37;
            }

            x = var15 + var27 * var13;
            y = var17 + var29 * var13;
            z = var19 + var31 * var13;
            if (var29 < 0.0D)
            {
                ++y;
            }

            if (var29 > 0.0D)
            {
                y += 0.5D;
            }

            return new Vec3D(x, y, z);
        }

        return null;
    }

    public override void writeNbt(NBTTagCompound nbt)
    {
        nbt.SetInteger("Type", type);
        if (type == 2)
        {
            nbt.SetDouble("PushX", pushX);
            nbt.SetDouble("PushZ", pushZ);
            nbt.SetShort("Fuel", (short)fuel);
        }
        else if (type == 1)
        {
            NBTTagList items = new();

            for (int slotIndex = 0; slotIndex < cargoItems.Length; ++slotIndex)
            {
                if (cargoItems[slotIndex] != null)
                {
                    NBTTagCompound itemTag = new();
                    itemTag.SetByte("Slot", (sbyte)slotIndex);
                    cargoItems[slotIndex].writeToNBT(itemTag);
                    items.SetTag(itemTag);
                }
            }

            nbt.SetTag("Items", items);
        }
    }

    public override void readNbt(NBTTagCompound nbt)
    {
        type = nbt.GetInteger("Type");
        if (type == 2)
        {
            pushX = nbt.GetDouble("PushX");
            pushZ = nbt.GetDouble("PushZ");
            fuel = nbt.GetShort("Fuel");
        }
        else if (type == 1)
        {
            NBTTagList items = nbt.GetTagList("Items");
            cargoItems = new ItemStack[size()];

            for (int i = 0; i < items.TagCount(); ++i)
            {
                NBTTagCompound itemTag = (NBTTagCompound)items.TagAt(i);
                int slotIndex = itemTag.GetByte("Slot") & 255;
                if (slotIndex >= 0 && slotIndex < cargoItems.Length)
                {
                    cargoItems[slotIndex] = new ItemStack(itemTag);
                }
            }
        }
    }

    public override float getShadowRadius() => 0.0F;

    public override void onCollision(Entity entity)
    {
        if (!world.IsRemote)
        {
            if (entity != passenger)
            {
                if (entity is EntityLiving && entity is not EntityPlayer && type == 0 && velocityX * velocityX + velocityZ * velocityZ > 0.01D && passenger == null && entity.vehicle == null)
                {
                    entity.setVehicle(this);
                }

                double var2 = entity.x - x;
                double var4 = entity.z - z;
                double var6 = var2 * var2 + var4 * var4;
                if (var6 >= 1.0E-4F)
                {
                    var6 = MathHelper.Sqrt(var6);
                    var2 /= var6;
                    var4 /= var6;
                    double var8 = 1.0D / var6;
                    if (var8 > 1.0D)
                    {
                        var8 = 1.0D;
                    }

                    var2 *= var8;
                    var4 *= var8;
                    var2 *= 0.1F;
                    var4 *= 0.1F;
                    var2 *= 1.0F - pushSpeedReduction;
                    var4 *= 1.0F - pushSpeedReduction;
                    var2 *= 0.5D;
                    var4 *= 0.5D;
                    if (entity is EntityMinecart)
                    {
                        double var10 = entity.x - x;
                        double var12 = entity.z - z;
                        double var14 = var10 * entity.velocityZ + var12 * entity.prevX;
                        var14 *= var14;
                        if (var14 > 5.0D)
                        {
                            return;
                        }

                        double var16 = entity.velocityX + velocityX;
                        double var18 = entity.velocityZ + velocityZ;
                        if (((EntityMinecart)entity).type == 2 && type != 2)
                        {
                            velocityX *= 0.2F;
                            velocityZ *= 0.2F;
                            addVelocity(entity.velocityX - var2, 0.0D, entity.velocityZ - var4);
                            entity.velocityX *= 0.7F;
                            entity.velocityZ *= 0.7F;
                        }
                        else if (((EntityMinecart)entity).type != 2 && type == 2)
                        {
                            entity.velocityX *= 0.2F;
                            entity.velocityZ *= 0.2F;
                            entity.addVelocity(velocityX + var2, 0.0D, velocityZ + var4);
                            velocityX *= 0.7F;
                            velocityZ *= 0.7F;
                        }
                        else
                        {
                            var16 /= 2.0D;
                            var18 /= 2.0D;
                            velocityX *= 0.2F;
                            velocityZ *= 0.2F;
                            addVelocity(var16 - var2, 0.0D, var18 - var4);
                            entity.velocityX *= 0.2F;
                            entity.velocityZ *= 0.2F;
                            entity.addVelocity(var16 + var2, 0.0D, var18 + var4);
                        }
                    }
                    else
                    {
                        addVelocity(-var2, 0.0D, -var4);
                        entity.addVelocity(var2 / 4.0D, 0.0D, var4 / 4.0D);
                    }
                }
            }
        }
    }

    public override bool interact(EntityPlayer player)
    {
        if (type == 0)
        {
            if (passenger != null && passenger is EntityPlayer && passenger != player)
            {
                return true;
            }

            if (!world.IsRemote)
            {
                player.setVehicle(this);
            }
        }
        else if (type == 1)
        {
            if (!world.IsRemote)
            {
                player.openChestScreen(this);
            }
        }
        else if (type == 2)
        {
            ItemStack heldItem = player.inventory.getSelectedItem();
            if (heldItem != null && heldItem.itemId == Item.Coal.id)
            {
                if (--heldItem.count == 0)
                {
                    player.inventory.setStack(player.inventory.selectedSlot, null);
                }

                fuel += 1200;
            }

            pushX = x - player.x;
            pushZ = z - player.z;
        }

        return true;
    }

    public override void setPositionAndAnglesAvoidEntities(double var1, double var3, double var5, float var7, float var8, int var9)
    {
        field_9414_l = var1;
        field_9413_m = var3;
        field_9412_n = var5;
        field_9411_o = var7;
        field_9410_p = var8;
        field_9415_k = var9 + 2;
        velocityX = cartVelocityX;
        velocityY = cartVelocityY;
        velocityZ = cartVelocityZ;
    }

    public override void setVelocityClient(double velocityX, double velocityY, double velocityZ)
    {
        cartVelocityX = this.velocityX = velocityX;
        cartVelocityY = this.velocityY = velocityY;
        cartVelocityZ = this.velocityZ = velocityZ;
    }
}
