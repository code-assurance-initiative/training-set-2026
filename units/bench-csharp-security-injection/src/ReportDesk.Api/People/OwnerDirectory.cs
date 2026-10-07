using System.DirectoryServices.Protocols;
using Microsoft.Extensions.Options;

namespace ReportDesk.Api.People;

/// <summary>Finds the person behind a document owner's e-mail address.</summary>
public sealed class OwnerDirectory(ILdapSearcher searcher, IOptions<PeopleDirectoryOptions> options)
{
    public async Task<IReadOnlyList<PersonEntry>> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var request = new SearchRequest { DistinguishedName = options.Value.BaseDn, Scope = SearchScope.Subtree };
        request.Filter = "(&(objectClass=person)(mail=" + email + "))";
        request.Attributes.AddRange(["cn", "department"]);
        request.SizeLimit = 20;

        var entries = await searcher.SearchAsync(request, cancellationToken).ConfigureAwait(false);
        return entries
            .Select(entry => new PersonEntry(entry.DistinguishedName, entry.Single("cn"), entry.Single("department")))
            .ToList();
    }
}
