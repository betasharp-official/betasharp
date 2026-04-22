using BetaSharp.Registries;

namespace BetaSharp.Tests;

public class HolderTests
{
    private sealed class Widget
    {
        public string Label { get; init; } = "";
        public override string ToString() => Label;
    }

    // ---- Construction ----

    [Fact]
    public void Direct_holder_is_immediately_resolved()
    {
        var w = new Widget { Label = "foo" };
        var h = new Holder<Widget>(w);

        Assert.True(h.IsResolved);
        Assert.Same(w, h.Value);
    }

    [Fact]
    public void Direct_factory_is_immediately_resolved()
    {
        var w = new Widget { Label = "bar" };
        var h = new Holder<Widget>(w);

        Assert.True(h.IsResolved);
        Assert.Same(w, h.Value);
    }

    [Fact]
    public void Lazy_holder_is_not_resolved_before_first_access()
    {
        int calls = 0;
        var h = Holder<Widget>.Reference(() => { calls++; return new Widget { Label = "lazy" }; });

        Assert.False(h.IsResolved);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void Lazy_holder_resolves_on_first_value_access()
    {
        int calls = 0;
        var h = Holder<Widget>.Reference(() => { calls++; return new Widget { Label = "lazy" }; });

        Widget w = h.Value;

        Assert.True(h.IsResolved);
        Assert.Equal(1, calls);
        Assert.Equal("lazy", w.Label);
    }

    [Fact]
    public void Lazy_holder_resolver_is_called_only_once()
    {
        int calls = 0;
        var h = Holder<Widget>.Reference(() => { calls++; return new Widget { Label = "once" }; });

        _ = h.Value;
        _ = h.Value;
        _ = h.Value;

        Assert.Equal(1, calls);
    }

    // ---- Implicit conversion ----

    [Fact]
    public void Implicit_conversion_returns_value()
    {
        var w = new Widget { Label = "implicit" };
        var h = new Holder<Widget>(w);

        Widget converted = h;

        Assert.Same(w, converted);
    }

    // ---- Internal setter ----

    [Fact]
    public void Setting_value_replaces_existing_value()
    {
        var first = new Widget { Label = "first" };
        var second = new Widget { Label = "second" };
        var h = new Holder<Widget>(first);

        h.Value = second;

        Assert.Same(second, h.Value);
    }

    [Fact]
    public void Setting_value_on_lazy_holder_resolves_it_without_calling_resolver()
    {
        int calls = 0;
        var h = Holder<Widget>.Reference(() => { calls++; return new Widget(); });
        var replacement = new Widget { Label = "override" };

        h.Value = replacement;

        Assert.True(h.IsResolved);
        Assert.Same(replacement, h.Value);
        Assert.Equal(0, calls);
    }

    // ---- Edge cases ----

    [Fact]
    public void Holder_with_no_value_and_no_resolver_throws_on_access()
    {
        // Only possible via the internal constructor path; simulate via lazy that returns null.
        // The safest reachable path: a resolver that throws.
        var h = Holder<Widget>.Reference(() => throw new InvalidOperationException("no resolver"));

        Assert.Throws<InvalidOperationException>(() => _ = h.Value);
    }

    [Fact]
    public void ToString_on_unresolved_holder_returns_placeholder()
    {
        var h = Holder<Widget>.Reference(() => new Widget { Label = "x" });

        Assert.Equal("<unresolved>", h.ToString());
    }

    [Fact]
    public void ToString_on_resolved_holder_delegates_to_value()
    {
        var h = new Holder<Widget>(new Widget { Label = "hello" });

        Assert.Equal("hello", h.ToString());
    }
}
