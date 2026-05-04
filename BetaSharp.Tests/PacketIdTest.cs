using System.Reflection;
using BetaSharp.Network.Packets;
using SkipException = Xunit.SkipException;

namespace BetaSharp.Tests;

public class PacketIdTest
{
    public static IEnumerable<object[]> PacketIds =>
        Enum.GetValues(typeof(PacketId))
            .Cast<PacketId>()
            .Select(v => new object[] { v });

    [Fact]
    public void VerifyRegistryIds()
    {
        for (int i = 0; i < Packet.Registry.Count; i++)
        {
            if (Packet.Registry[i] == null) continue;
            Assert.StrictEqual(i, Packet.Registry[i]!.Id);
        }
    }

    [Fact]
    public void VerifyPacketIdMatchRegistryIds()
    {
        for (int i = 0; i < Packet.Registry.Count; i++)
        {
            if (Packet.Registry[i] == null) continue;
            Assert.StrictEqual(i, Packet.Registry[i]!.Get().Id);
        }
    }

    [Theory, MemberData(nameof(PacketIds))]
    public void VerifyPacketEnum(PacketId value)
    {
        Assert.StrictEqual((int)value, Packet.Registry[(int)value]?.Id);
    }

    [SkippableTheory, MemberData(nameof(PacketIds))]
    public void VerifyPacketGetMethods(PacketId value)
    {
        var t = Packet.Registry[(int)value]!.Get().GetType();
        var methods = t.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Where(m => m.Name == "Get");

        if (!methods.Any())
            throw new SkipException(t.Name + " has no Get method.");

        foreach (MethodInfo method in methods)
        {
            object?[] args = method.GetParameters()
                .Select(p => p.HasDefaultValue
                    ? p.DefaultValue
                    : (p.ParameterType.IsValueType
                        ? Activator.CreateInstance(p.ParameterType)
                        : p.ParameterType == typeof(string)
                            ? ""
                            : p.ParameterType.IsArray
                                ? Array.CreateInstance(p.ParameterType.GetElementType()!, 0)
                                : p.IsOptional || Nullable.GetUnderlyingType(p.ParameterType) != null
                                    ? null
                                    : throw new SkipException($"one or more parameters dont have default value. ({p.ParameterType})")))
                .ToArray();

            object? packet = method.Invoke(null, args);
            Assert.NotNull(packet);
            Assert.StrictEqual((int)value, ((Packet)packet).Id);
        }
    }

    [Fact]
    public void VerifyPacketCount()
    {
        int count = 0;
        var registry = Packet.Registry;
        for (int i = 0; i < registry.Count; i++)
        {
            if (registry[i] == null) continue;
            count++;
        }

        Assert.StrictEqual(Enum.GetValues(typeof(PacketId)).Length, count);
    }
}
