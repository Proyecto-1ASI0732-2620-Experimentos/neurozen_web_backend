namespace neurozen.API.Shared.Domain.Model.Pagination;

public sealed record PageResult<T>(IReadOnlyList<T> Items, int? TotalCount)
{
    public static PageResult<T> Unpaged(IReadOnlyList<T> items) => new(items, null);
}
