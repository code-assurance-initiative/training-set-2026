namespace Warehouse.Stock.Application.Common;

/// <summary>A one-based page of a listing. Bounds are validated at the API boundary.</summary>
public readonly record struct PageRequest(int Number, int Size)
{
    public int Skip => (Number - 1) * Size;
}
