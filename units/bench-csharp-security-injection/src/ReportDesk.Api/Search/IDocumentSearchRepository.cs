namespace ReportDesk.Api.Search;

public interface IDocumentSearchRepository
{
    Task<IReadOnlyList<DocumentHit>> SearchAsync(string term, int limit, CancellationToken cancellationToken);
}
