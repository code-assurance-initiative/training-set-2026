using ReportDesk.Api.Shares;

namespace ReportDesk.UnitTests.Security;

public sealed class SharePasswordHasherTests
{
    [Fact]
    public void VerifiesThePasswordItHashed()
    {
        var hash = SharePasswordHasher.Hash("correct horse battery");
        Assert.True(SharePasswordHasher.Verify("correct horse battery", hash));
    }

    [Fact]
    public void RejectsADifferentPassword()
    {
        var hash = SharePasswordHasher.Hash("correct horse battery");
        Assert.False(SharePasswordHasher.Verify("correct horse staple", hash));
    }
}
