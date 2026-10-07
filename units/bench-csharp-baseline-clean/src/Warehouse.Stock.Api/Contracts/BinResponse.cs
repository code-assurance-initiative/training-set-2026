using Warehouse.Stock.Application.Catalog;

namespace Warehouse.Stock.Api.Contracts;

public sealed record BinResponse(string Code, string Zone, int Capacity)
{
    public static BinResponse From(BinLocation bin) => new(bin.Code, bin.Zone, bin.Capacity);
}
