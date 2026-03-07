using BetaSharp.Blocks;
using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Items;
using BetaSharp.Util.Maths;

namespace BetaSharp.Worlds.Gen.Features;

internal class DungeonFeature : Feature
{

    public override bool Generate(World world, JavaRandom rand, int x, int y, int z)
    {
        byte height = 3;
        int radiusX = rand.NextInt(2) + 2;
        int radiusZ = rand.NextInt(2) + 2;
        int openingsCount = 0;


        for (int cx = x - radiusX - 1; cx <= x + radiusX + 1; ++cx)
        {
            for (int cy = y - 1; cy <= y + height + 1; ++cy)
            {
                for (int cz = z - radiusZ - 1; cz <= z + radiusZ + 1; ++cz)
                {
                    Material mat = world.getMaterial(cx, cy, cz);
                    if ((cy == y - 1 || cy == y + height + 1) && !mat.IsSolid)
                    {
                        return false;
                    }

                    bool isWall = cx == x - radiusX - 1 ||
                                  cx == x + radiusX + 1 ||
                                  cz == z - radiusZ - 1 ||
                                  cz == z + radiusZ + 1;

                    if (isWall && cy == y && world.isAir(cx, cy, cz) && world.isAir(cx, cy + 1, cz))
                    {
                        ++openingsCount;
                    }
                }
            }
        }

        if (openingsCount < 1 || openingsCount > 5) return false;

        for (int cx = x - radiusX - 1; cx <= x + radiusX + 1; ++cx)
        {
            for (int cy = y + height; cy >= y - 1; --cy)
            {
                for (int cz = z - radiusZ - 1; cz <= z + radiusZ + 1; ++cz)
                {
                    bool isInside = cx != x - radiusX - 1 &&
                                    cy != y - 1 &&
                                    cz != z - radiusZ - 1 &&
                                    cx != x + radiusX + 1 &&
                                    cy != y + height + 1 &&
                                    cz != z + radiusZ + 1;
                    if (isInside)
                    {
                        world.setBlock(cx, cy, cz, 0);
                    }
                    else if (cy >= 0 && !world.getMaterial(cx, cy - 1, cz).IsSolid)
                    {
                        world.setBlock(cx, cy, cz, 0);
                    }
                    else if (world.getMaterial(cx, cy, cz).IsSolid)
                    {
                        if (cy == y - 1 && rand.NextInt(4) != 0)
                            world.setBlock(cx, cy, cz, Block.MossyCobblestone.id);
                        else
                            world.setBlock(cx, cy, cz, Block.Cobblestone.id);
                    }
                }
            }
        }


        for (int i = 0; i < 2; ++i)
        {
            for (int j = 0; j < 3; ++j)
            {
                int chestX = x + rand.NextInt(radiusX * 2 + 1) - radiusX;
                int chestZ = z + rand.NextInt(radiusZ * 2 + 1) - radiusZ;
                if (world.isAir(chestX, y, chestZ))
                {
                    int neighbors = 0;
                    if (world.getMaterial(chestX - 1, y, chestZ).IsSolid) ++neighbors;
                    if (world.getMaterial(chestX + 1, y, chestZ).IsSolid) ++neighbors;
                    if (world.getMaterial(chestX, y, chestZ - 1).IsSolid) ++neighbors;
                    if (world.getMaterial(chestX, y, chestZ + 1).IsSolid) ++neighbors;

                    if (neighbors != 1) continue;

                    world.setBlock(chestX, y, chestZ, Block.Chest.id);

                    BlockEntityChest chest = (BlockEntityChest)world.getBlockEntity(chestX, y, chestZ);
                    for (int k = 0; k < 8; ++k)
                    {
                        ItemStack? loot = PickCheckLootItem(rand);
                        if (loot != null)
                        {
                            chest.setStack(rand.NextInt(chest.size()), loot);
                        }
                    }

                }
            }
        }

        world.setBlock(x, y, z, Block.Spawner.id);
        BlockEntityMobSpawner var19 = (BlockEntityMobSpawner)world.getBlockEntity(x, y, z);
        var19.SetSpawnedEntityId(PickMobSpawner(rand));
        return true;


    }

    private ItemStack? PickCheckLootItem(JavaRandom rand)
    {
        int chance = rand.NextInt(11);

        return chance switch
        {
            0 => new ItemStack(Item.Saddle),
            1 => new ItemStack(Item.IronIngot, rand.NextInt(4) + 1),
            2 => new ItemStack(Item.Bread),
            3 => new ItemStack(Item.Wheat, rand.NextInt(4) + 1),
            4 => new ItemStack(Item.Gunpowder, rand.NextInt(4) + 1),
            5 => new ItemStack(Item.String, rand.NextInt(4) + 1),
            6 => new ItemStack(Item.Bucket),
            7 => rand.NextInt(100) == 0 ? new ItemStack(Item.GoldenApple) : null,
            8 => rand.NextInt(2) == 0 ? new ItemStack(Item.Redstone, rand.NextInt(4) + 1) : null,
            9 => rand.NextInt(10) == 0 ? new ItemStack(Item.ITEMS[Item.RecordThirteen.id + rand.NextInt(2)]) : null,
            10 => new ItemStack(Item.Dye, 1, 3),
            _ => null,
        };
    }

    private string PickMobSpawner(JavaRandom rand)
    {
        return rand.NextInt(4) switch
        {
            0 => "Skeleton",
            1 => "Zombie",
            2 => "Zombie",
            3 => "Spider",
            _ => "Zombie"
        };
    }
}
