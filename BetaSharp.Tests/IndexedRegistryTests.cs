using BetaSharp.Registries;

namespace BetaSharp.Tests;

public class IndexedRegistryTests
{
    // Simple value type used as registry entries.
    private sealed class Color
    {
        public string Name { get; init; } = "";
        public override string ToString() => Name;
    }

    private static readonly ResourceLocation s_regKey = ResourceLocation.Parse("test:colors");
    private static readonly ResourceLocation s_red = ResourceLocation.Parse("test:red");
    private static readonly ResourceLocation s_green = ResourceLocation.Parse("test:green");
    private static readonly ResourceLocation s_blue = ResourceLocation.Parse("test:blue");

    private static IndexedRegistry<Color> Build(bool freeze = false)
    {
        var reg = new IndexedRegistry<Color>(s_regKey);
        reg.Register(s_red, new Color { Name = "red" });
        reg.Register(s_green, new Color { Name = "green" });
        reg.Register(s_blue, new Color { Name = "blue" });
        if (freeze) reg.Freeze();
        return reg;
    }

    // ---- IReadableRegistry<T>.RegistryKey ----

    [Fact]
    public void RegistryKey_returns_constructor_key()
    {
        var reg = new IndexedRegistry<Color>(s_regKey);
        Assert.Equal(s_regKey, reg.RegistryKey);
    }

    // ---- Get by ResourceLocation ----

    [Fact]
    public void Get_by_key_returns_registered_value()
    {
        var reg = Build();
        Color? c = reg.Get(s_red)?.Value;
        Assert.NotNull(c);
        Assert.Equal("red", c.Name);
    }

    [Fact]
    public void Get_by_key_returns_null_for_unknown_key()
    {
        var reg = Build();
        Assert.Null(reg.Get(ResourceLocation.Parse("test:purple")));
    }

    // ---- Get by integer id ----

    [Fact]
    public void Get_by_id_returns_value_at_sequential_id()
    {
        var reg = Build();
        // Registration order determines implicit ids: 0=red, 1=green, 2=blue
        Assert.Equal("red", reg.Get(0)!.Name);
        Assert.Equal("green", reg.Get(1)!.Name);
        Assert.Equal("blue", reg.Get(2)!.Name);
    }

    [Fact]
    public void Get_by_id_returns_null_for_out_of_range_id()
    {
        var reg = Build();
        Assert.Null(reg.Get(-1));
        Assert.Null(reg.Get(100));
    }

    // ---- Explicit id registration ----

    [Fact]
    public void Register_with_explicit_id_places_value_at_correct_position()
    {
        var reg = new IndexedRegistry<Color>(s_regKey);
        reg.Register(10, s_red, new Color { Name = "red" });

        Assert.Equal("red", reg.Get(10)!.Name);
        Assert.Null(reg.Get(0));
    }

    // ---- GetId / GetKey ----

    [Fact]
    public void GetId_returns_correct_id_for_registered_value()
    {
        var reg = Build();
        Color green = reg.Get(s_green)!.Value;
        Assert.Equal(1, reg.GetId(green));
    }

    [Fact]
    public void GetId_returns_negative_one_for_unregistered_value()
    {
        var reg = Build();
        Assert.Equal(-1, reg.GetId(new Color { Name = "purple" }));
    }

    [Fact]
    public void GetKey_returns_location_for_registered_value()
    {
        var reg = Build();
        Color blue = reg.Get(s_blue)!.Value;
        Assert.Equal(s_blue, reg.GetKey(blue));
    }

    [Fact]
    public void GetKey_returns_null_for_unregistered_value()
    {
        var reg = Build();
        Assert.Null(reg.GetKey(new Color { Name = "purple" }));
    }

    // ---- ContainsKey / Keys ----

    [Fact]
    public void ContainsKey_returns_true_for_registered_key()
    {
        var reg = Build();
        Assert.True(reg.ContainsKey(s_red));
    }

    [Fact]
    public void ContainsKey_returns_false_for_unknown_key()
    {
        var reg = Build();
        Assert.False(reg.ContainsKey(ResourceLocation.Parse("test:yellow")));
    }

    [Fact]
    public void Keys_contains_all_registered_locations()
    {
        var reg = Build();
        Assert.Contains(s_red, reg.Keys);
        Assert.Contains(s_green, reg.Keys);
        Assert.Contains(s_blue, reg.Keys);
    }

    // ---- GetHolder ----

    [Fact]
    public void GetHolder_returns_holder_wrapping_registered_value()
    {
        var reg = Build();
        Holder<Color>? h = reg.Get(s_green);

        Assert.NotNull(h);
        Assert.Equal("green", h.Value.Name);
    }

    [Fact]
    public void GetHolder_returns_same_holder_instance_on_repeated_calls()
    {
        var reg = Build();
        Holder<Color>? h1 = reg.Get(s_red);
        Holder<Color>? h2 = reg.Get(s_red);

        Assert.Same(h1, h2);
    }

    [Fact]
    public void GetHolder_returns_null_for_unknown_key()
    {
        var reg = Build();
        Assert.Null(reg.Get(ResourceLocation.Parse("test:magenta")));
    }

    // ---- Enumeration ----

    [Fact]
    public void Enumerating_registry_yields_all_registered_values()
    {
        var reg = Build();
        var names = reg.Select(c => c.Name).OrderBy(n => n).ToList();

        Assert.Equal(["blue", "green", "red"], names);
    }

    // ---- Freeze ----

    [Fact]
    public void IsFrozen_is_false_before_freeze()
    {
        var reg = new IndexedRegistry<Color>(s_regKey);
        Assert.False(reg.IsFrozen);
    }

    [Fact]
    public void IsFrozen_is_true_after_freeze()
    {
        var reg = Build();
        reg.Freeze();
        Assert.True(reg.IsFrozen);
    }

    [Fact]
    public void Register_after_freeze_throws()
    {
        var reg = Build(freeze: true);
        Assert.Throws<InvalidOperationException>(() => reg.Register(ResourceLocation.Parse("test:yellow"), new Color()));
    }

    // ---- Duplicate guards ----

    [Fact]
    public void Register_duplicate_key_throws()
    {
        var reg = Build();
        Assert.Throws<ArgumentException>(() => reg.Register(s_red, new Color { Name = "red2" }));
    }

    [Fact]
    public void Register_duplicate_id_throws()
    {
        var reg = new IndexedRegistry<Color>(s_regKey);
        reg.Register(5, s_red, new Color { Name = "red" });
        Assert.Throws<ArgumentException>(() => reg.Register(5, s_green, new Color { Name = "green" }));
    }
}
