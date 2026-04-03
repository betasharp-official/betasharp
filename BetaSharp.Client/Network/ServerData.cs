using BetaSharp.NBT;

namespace BetaSharp.Client.Network;

public class ServerData(string name, string ip)
{
    public string Name { get; set; } = name;
    public string Ip { get; set; } = ip;
    public string? PopulationInfo { get; set; }
    public string? Motd { get; set; }
    public long Lag { get; set; }
    public bool Polled { get; set; } = false;

    public NBTTagCompound ToNBT()
    {
        var tag = new NBTTagCompound();
        tag.SetString("name", Name);
        tag.SetString("ip", Ip);
        return tag;
    }

    public static ServerData FromNBT(NBTTagCompound tag)
    {
        return new ServerData(
            tag.GetString("name"),
            tag.GetString("ip")
        );
    }
}
