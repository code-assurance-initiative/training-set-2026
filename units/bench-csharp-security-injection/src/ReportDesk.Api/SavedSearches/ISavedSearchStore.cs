namespace ReportDesk.Api.SavedSearches;

public interface ISavedSearchStore
{
    Task SaveAsync(string owner, SavedSearchDefinition search, CancellationToken cancellationToken);

    Task<int> DeleteAsync(string owner, string name, CancellationToken cancellationToken);
}
