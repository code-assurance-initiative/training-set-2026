namespace Rentals.Lending.Domain.Catalogue;

/// <summary>Where an equipment type is kept for collection: the branch, and the shelf within it.</summary>
public sealed class StorageLocation
{
    public StorageLocation(string branch, string shelf)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(branch);
        ArgumentException.ThrowIfNullOrWhiteSpace(shelf);
        Branch = branch.Trim();
        Shelf = shelf.Trim().ToUpperInvariant();
    }

    public string Branch { get; set; }

    public string Shelf { get; set; }

    public override string ToString() => $"{Branch}, shelf {Shelf}";
}
