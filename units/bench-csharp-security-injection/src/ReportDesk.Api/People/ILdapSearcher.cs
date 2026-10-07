using System.DirectoryServices.Protocols;

namespace ReportDesk.Api.People;

/// <summary>Runs one LDAP search and returns its entries.</summary>
public interface ILdapSearcher
{
    Task<IReadOnlyList<SearchResultEntry>> SearchAsync(SearchRequest request, CancellationToken cancellationToken);
}
