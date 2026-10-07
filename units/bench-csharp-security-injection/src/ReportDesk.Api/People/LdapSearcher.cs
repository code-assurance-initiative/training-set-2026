using System.DirectoryServices.Protocols;
using System.Net;
using Microsoft.Extensions.Options;

namespace ReportDesk.Api.People;

/// <summary>LDAPS searches against the archive directory, bound as the service's own account.</summary>
public sealed class LdapSearcher(IOptions<PeopleDirectoryOptions> options) : ILdapSearcher
{
    public async Task<IReadOnlyList<SearchResultEntry>> SearchAsync(SearchRequest request, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        using var connection = new LdapConnection(new LdapDirectoryIdentifier(settings.Server, settings.Port));
        connection.SessionOptions.SecureSocketLayer = true;
        connection.SessionOptions.ProtocolVersion = 3;
        connection.AuthType = AuthType.Negotiate;
        connection.Credential = CredentialCache.DefaultNetworkCredentials;

        var response = await Task.Factory
            .FromAsync(connection.BeginSendRequest, connection.EndSendRequest, request, PartialResultProcessing.NoPartialResultSupport, state: null)
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
        return ((SearchResponse)response).Entries.Cast<SearchResultEntry>().ToList();
    }
}
