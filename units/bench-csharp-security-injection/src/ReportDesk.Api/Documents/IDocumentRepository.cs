namespace ReportDesk.Api.Documents;

public interface IDocumentRepository
{
    Task<DocumentRecord?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<DocumentRecord>> ListAsync(string? sort, bool descending, int limit, CancellationToken cancellationToken);
}
