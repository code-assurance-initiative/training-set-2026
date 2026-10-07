using System.DirectoryServices.Protocols;
using ReportDesk.Api.People;

namespace ReportDesk.UnitTests.People;

public sealed class DirectoryTests
{
    [Theory]
    [InlineData("finance", "finance")]
    [InlineData("a*b", @"a\2ab")]
    [InlineData("x)(cn=*", @"x\29\28cn=\2a")]
    [InlineData(@"back\slash", @"back\5cslash")]
    public void EscapesFilterMetacharacters(string value, string expected)
    {
        Assert.Equal(expected, LdapFilter.Escape(value));
    }

    [Fact]
    public async Task GroupLookupsEscapeTheGroupName()
    {
        var searcher = new RecordingSearcher();
        var groups = new GroupDirectory(searcher, TestOptions.Directory());

        var group = await groups.FindAsync("*)(cn=admins", TestContext.Current.CancellationToken);

        Assert.Null(group);
        Assert.Equal(@"(&(objectClass=groupOfNames)(cn=\2a\29\28cn=admins))", searcher.LastFilter);
    }

    [Fact]
    public async Task OwnerLookupsSearchByMail()
    {
        var searcher = new RecordingSearcher();
        var owners = new OwnerDirectory(searcher, TestOptions.Directory());

        Assert.Empty(await owners.FindByEmailAsync("k.holm@archive.test", TestContext.Current.CancellationToken));
        Assert.Equal("(&(objectClass=person)(mail=k.holm@archive.test))", searcher.LastFilter);
    }

    private sealed class RecordingSearcher : ILdapSearcher
    {
        public string? LastFilter { get; private set; }

        public Task<IReadOnlyList<SearchResultEntry>> SearchAsync(SearchRequest request, CancellationToken cancellationToken)
        {
            LastFilter = request.Filter as string;
            return Task.FromResult<IReadOnlyList<SearchResultEntry>>([]);
        }
    }
}
