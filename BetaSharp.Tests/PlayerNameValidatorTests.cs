namespace BetaSharp.Tests;

public class PlayerNameValidatorTests
{
    [Fact]
    public void Validate_accepts_sixteen_chars_without_whitespace()
    {
        string name = new('a', PlayerNameValidator.MaxLength);
        PlayerNameValidator.Validate(name);
    }

    [Fact]
    public void Validate_accepts_default_style_name()
    {
        PlayerNameValidator.Validate("Player123456789");
    }

    [Fact]
    public void Validate_throws_when_longer_than_max()
    {
        string name = new('a', PlayerNameValidator.MaxLength + 1);
        InvalidPlayerNameException ex = Assert.Throws<InvalidPlayerNameException>(() => PlayerNameValidator.Validate(name));
        Assert.Contains("16", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_throws_on_interior_space()
    {
        InvalidPlayerNameException ex = Assert.Throws<InvalidPlayerNameException>(() => PlayerNameValidator.Validate("Foo Bar"));
        Assert.Equal(InvalidPlayerNameException.InvalidChar().Message, ex.Message);
    }

    [Fact]
    public void Validate_throws_on_whitespace_only()
    {
        InvalidPlayerNameException ex = Assert.Throws<InvalidPlayerNameException>(() => PlayerNameValidator.Validate("   "));
        Assert.Equal(InvalidPlayerNameException.NameEmpty().Message, ex.Message);
    }

    [Fact]
    public void Validate_throws_on_empty_string()
    {
        InvalidPlayerNameException ex = Assert.Throws<InvalidPlayerNameException>(() => PlayerNameValidator.Validate(""));
        Assert.Equal(InvalidPlayerNameException.NameEmpty().Message, ex.Message);
    }

    [Fact]
    public void Validate_throws_on_leading_whitespace()
    {
        InvalidPlayerNameException ex = Assert.Throws<InvalidPlayerNameException>(() => PlayerNameValidator.Validate(" x"));
        Assert.Equal(InvalidPlayerNameException.TrimDifferent().Message, ex.Message);
    }

    [Fact]
    public void Validate_throws_on_trailing_whitespace()
    {
        InvalidPlayerNameException ex = Assert.Throws<InvalidPlayerNameException>(() => PlayerNameValidator.Validate("x "));
        Assert.Equal(InvalidPlayerNameException.TrimDifferent().Message, ex.Message);
    }

    [Fact]
    public void Validate_throws_on_tab_character()
    {
        InvalidPlayerNameException ex = Assert.Throws<InvalidPlayerNameException>(() => PlayerNameValidator.Validate("foo\tbar"));
        Assert.Equal(InvalidPlayerNameException.InvalidChar().Message, ex.Message);
    }

    [Fact]
    public void Validate_throws_on_null()
    {
        InvalidPlayerNameException ex = Assert.Throws<InvalidPlayerNameException>(() => PlayerNameValidator.Validate(null));
        Assert.Equal(InvalidPlayerNameException.NameNull().Message, ex.Message);
    }
}
