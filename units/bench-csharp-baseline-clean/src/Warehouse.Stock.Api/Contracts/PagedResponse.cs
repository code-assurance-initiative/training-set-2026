using Warehouse.Stock.Application.Common;

namespace Warehouse.Stock.Api.Contracts;

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public static class PagedResponse
{
    public static PagedResponse<TResponse> From<T, TResponse>(Page<T> page, Func<T, TResponse> map) =>
        new(page.Items.Select(map).ToList(), page.Number, page.Size, page.TotalCount);
}
