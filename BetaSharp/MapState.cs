using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Worlds;


namespace BetaSharp;

public class MapState : PersistentState
{
    public int centerX;
    public int centerZ;
    public sbyte dimension;
    public sbyte scale;
    public byte[] colors = new byte[128*128];
    public int inventoryTicks;
    private List<MapInfo> updateTrackers = new List<MapInfo>();
    private Dictionary<EntityPlayer, MapInfo> updateTrackersByPlayer = new Dictionary<EntityPlayer, MapInfo>();
    public List<MapCoord> icons = new List<MapCoord>();

    public MapState(string var1) : base(new(var1))
    {
    }

    public override void readNBT(NBTTagCompound nbt)
    {
        dimension = nbt.GetByte("dimension");
        centerX = nbt.GetInteger("xCenter");
        centerZ = nbt.GetInteger("zCenter");
        scale = nbt.GetByte("scale");
        if (scale < 0)
        {
            scale = 0;
        }

        if (scale > 4)
        {
            scale = 4;
        }

        short var2 = nbt.GetShort("width");
        short var3 = nbt.GetShort("height");
        if (var2 == 128 && var3 == 128)
        {
            colors = nbt.GetByteArray("colors");
        }
        else
        {
            byte[] var4 = nbt.GetByteArray("colors");
            colors = new byte[128*128];
            int var5 = (128 - var2) / 2;
            int var6 = (128 - var3) / 2;

            for (int var7 = 0; var7 < var3; ++var7)
            {
                int var8 = var7 + var6;
                if (var8 >= 0 || var8 < 128)
                {
                    for (int var9 = 0; var9 < var2; ++var9)
                    {
                        int var10 = var9 + var5;
                        if (var10 >= 0 || var10 < 128)
                        {
                            colors[var10 + var8 * 128] = var4[var9 + var7 * var2];
                        }
                    }
                }
            }
        }

    }

    public override void writeNBT(NBTTagCompound nbt)
    {
        nbt.SetByte("dimension", dimension);
        nbt.SetInteger("xCenter", centerX);
        nbt.SetInteger("zCenter", centerZ);
        nbt.SetByte("scale", scale);
        nbt.SetShort("width", (short)128);
        nbt.SetShort("height", (short)128);
        nbt.SetByteArray("colors", colors);
    }

    public void update(EntityPlayer var1, ItemStack var2)
    {
        if (!updateTrackersByPlayer.ContainsKey(var1))
        {
            MapInfo var3 = new MapInfo(this, var1);
            updateTrackersByPlayer[var1] = var3;
            updateTrackers.Add(var3);
        }

        icons.Clear();

        for (int var14 = 0; var14 < updateTrackers.Count; ++var14)
        {
            MapInfo var4 = updateTrackers[var14];
            if (!var4.player.dead && var4.player.inventory.contains(var2))
            {
                float var5 = (float)(var4.player.x - (double)centerX) / (float)(1 << scale);
                float var6 = (float)(var4.player.z - (double)centerZ) / (float)(1 << scale);
                byte var7 = 64;
                byte var8 = 64;
                if (var5 >= (float)(-var7) && var6 >= (float)(-var8) && var5 <= (float)var7 && var6 <= (float)var8)
                {
                    byte var9 = 0;
                    byte var10 = (byte)((int)((double)(var5 * 2.0F) + 0.5D));
                    byte var11 = (byte)((int)((double)(var6 * 2.0F) + 0.5D));
                    byte var12 = (byte)((int)((double)(var1.yaw * 16.0F / 360.0F) + 0.5D));
                    if (dimension < 0)
                    {
                        int var13 = inventoryTicks / 10;
                        var12 = (byte)(var13 * var13 * 34187121 + var13 * 121 >> 15 & 15);
                    }

                    if (var4.player.dimensionId == dimension)
                    {
                        icons.Add(new MapCoord(this, var9, var10, var11, var12));
                    }
                }
            }
            else
            {
                updateTrackersByPlayer.Remove(var4.player);
                updateTrackers.Remove(var4);
            }
        }

    }

    public byte[] getPlayerMarkerPacket(EntityPlayer player)
    {
        MapInfo var4 = updateTrackersByPlayer[player];
        return var4 == null ? null : var4.getUpdateData();
    }

    public void markDirty(int var1, int var2, int var3)
    {
        base.markDirty();

        for (int var4 = 0; var4 < updateTrackers.Count; ++var4)
        {
            MapInfo var5 = updateTrackers[var4];
            if (var5.startZ[var1] < 0 || var5.startZ[var1] > var2)
            {
                var5.startZ[var1] = var2;
            }

            if (var5.endZ[var1] < 0 || var5.endZ[var1] < var3)
            {
                var5.endZ[var1] = var3;
            }
        }

    }

    public void updateData(byte[] var1)
    {
        int var2;
        if (var1[0] == 0)
        {
            var2 = var1[1] & 255;
            int var3 = var1[2] & 255;

            for (int var4 = 0; var4 < var1.Length - 3; ++var4)
            {
                colors[(var4 + var3) * 128 + var2] = var1[var4 + 3];
            }

            markDirty();
        }
        else if (var1[0] == 1)
        {
            icons.Clear();

            for (var2 = 0; var2 < (var1.Length - 1) / 3; ++var2)
            {
                byte var7 = (byte)(var1[var2 * 3 + 1] % 16);
                byte var8 = var1[var2 * 3 + 2];
                byte var5 = var1[var2 * 3 + 3];
                byte var6 = (byte)(var1[var2 * 3 + 1] / 16);
                icons.Add(new MapCoord(this, var7, var8, var5, var6));
            }
        }

    }
}
