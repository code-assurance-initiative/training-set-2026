using FleetOps.Domain.Common;
using FleetOps.Domain.Vehicles;

namespace FleetOps.Application.Tests.Domain;

public sealed class VinTests
{
    [Fact]
    public void NormalisesToUpperCase()
    {
        Assert.Equal("WVWZZZ1KZAW000001", new Vin(" wvwzzz1kzaw000001 ").Value);
    }

    [Theory]
    [InlineData("WVWZZZ1KZAW00000")]
    [InlineData("WVWZZZ1KZAW0000011")]
    [InlineData("WVWZZZ1KZAW00000I")]
    [InlineData("WVWZZZ1KZAW00000O")]
    [InlineData("WVWZZZ1KZAW00000Q")]
    public void RejectsWrongLengthAndExcludedLetters(string value)
    {
        Assert.Throws<DomainException>(() => new Vin(value));
    }
}
