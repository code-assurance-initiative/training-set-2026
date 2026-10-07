using System.Security.Claims;
using Warehouse.Stock.Api.Security;

namespace Warehouse.Stock.UnitTests.Security;

public sealed class ScopeClaimsTests
{
    [Theory]
    [InlineData("stock.read", true)]
    [InlineData("openid stock.read reservations.write", true)]
    [InlineData("stock.readonly", false)]
    [InlineData("STOCK.READ", false)]
    [InlineData("", false)]
    public void AScopeMatchesOnlyAsAWholeSpaceDelimitedEntry(string scopes, bool expected)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ScopeClaims.ClaimType, scopes)], "Bearer"));

        Assert.Equal(expected, ScopeClaims.HasScope(user, "stock.read"));
    }

    [Fact]
    public void APrincipalWithoutScopeClaimsHasNoScope()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "picking-terminal-7")], "Bearer"));

        Assert.False(ScopeClaims.HasScope(user, "stock.read"));
    }
}
