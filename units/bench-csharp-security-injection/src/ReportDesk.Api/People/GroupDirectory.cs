using System.DirectoryServices.Protocols;
using Microsoft.Extensions.Options;

namespace ReportDesk.Api.People;

/// <summary>Reads the members of a directory group, for sharing a report with a whole team.</summary>
public sealed class GroupDirectory(ILdapSearcher searcher, IOptions<PeopleDirectoryOptions> options)
{
    public async Task<GroupEntry?> FindAsync(string groupName, CancellationToken cancellationToken)
    {
        var request = new SearchRequest { DistinguishedName = options.Value.GroupsDn, Scope = SearchScope.OneLevel };
        request.Filter = "(&(objectClass=groupOfNames)(cn=" + LdapFilter.Escape(groupName) + "))";
        request.Attributes.AddRange(["cn", "member"]);
        request.SizeLimit = 2;

        var entries = await searcher.SearchAsync(request, cancellationToken).ConfigureAwait(false);
        return entries.Count == 1
            ? new GroupEntry(entries[0].DistinguishedName, entries[0].Single("cn"), entries[0].All("member"))
            : null;
    }
}
