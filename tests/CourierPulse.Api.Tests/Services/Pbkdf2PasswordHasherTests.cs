using CourierPulse.Api.Services;

namespace CourierPulse.Api.Tests.Services;

public class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _sut = new();

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ReturnsTrue()
    {
        var hash = _sut.HashPassword("Correct#Horse9");

        Assert.True(_sut.VerifyPassword("Correct#Horse9", hash));
    }

    [Fact]
    public void VerifyPassword_WithWrongPassword_ReturnsFalse()
    {
        var hash = _sut.HashPassword("Correct#Horse9");

        Assert.False(_sut.VerifyPassword("Wrong#Password1", hash));
    }

    [Fact]
    public void HashPassword_ProducesDifferentHashesForTheSamePassword()
    {
        var first = _sut.HashPassword("Correct#Horse9");
        var second = _sut.HashPassword("Correct#Horse9");

        Assert.NotEqual(first, second);
        Assert.True(_sut.VerifyPassword("Correct#Horse9", first));
        Assert.True(_sut.VerifyPassword("Correct#Horse9", second));
    }

    [Fact]
    public void VerifyPassword_WithMalformedHash_ReturnsFalseInsteadOfThrowing()
    {
        Assert.False(_sut.VerifyPassword("anything", "not-a-valid-hash"));
    }
}
