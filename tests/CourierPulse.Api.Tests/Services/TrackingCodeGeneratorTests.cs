using CourierPulse.Api.Services;

namespace CourierPulse.Api.Tests.Services;

public class TrackingCodeGeneratorTests
{
    [Fact]
    public void Generate_StartsWithCpPrefix()
    {
        var code = TrackingCodeGenerator.Generate();

        Assert.StartsWith("CP-", code);
    }

    [Fact]
    public void Generate_HasTenCharactersIncludingPrefix()
    {
        var code = TrackingCodeGenerator.Generate();

        Assert.Equal(10, code.Length);
    }

    [Fact]
    public void Generate_OnlyUsesUnambiguousUppercaseAlphabet()
    {
        var code = TrackingCodeGenerator.Generate();
        var suffix = code["CP-".Length..];

        Assert.All(suffix, c => Assert.DoesNotContain(c, "01IO"));
        Assert.All(suffix, c => Assert.True(char.IsUpper(c) || char.IsDigit(c)));
    }

    [Fact]
    public void Generate_ProducesDistinctCodesAcrossManyCalls()
    {
        var codes = Enumerable.Range(0, 200).Select(_ => TrackingCodeGenerator.Generate()).ToHashSet();

        Assert.Equal(200, codes.Count);
    }
}
