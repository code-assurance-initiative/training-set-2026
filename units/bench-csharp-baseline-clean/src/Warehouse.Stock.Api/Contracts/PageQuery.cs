using System.ComponentModel.DataAnnotations;
using Warehouse.Stock.Application.Common;

namespace Warehouse.Stock.Api.Contracts;

public sealed record PageQuery
{
    [Range(1, 100_000)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 25;

    public PageRequest ToPageRequest() => new(Page, PageSize);
}
