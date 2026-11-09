namespace neurozen.API.Shared.Domain.Model.Pagination;

public sealed record PageRequest(int Page, int PageSize)
{
    public int Offset => (int)Math.Min((long)(Page - 1) * PageSize, int.MaxValue);

    public static PageRequest? From(int? page, int? pageSize)
    {
        if (page is null && pageSize is null) return null;

        var size = Math.Clamp(pageSize ?? 20, 1, 100);
        var maximumPage = (int)Math.Min(int.MaxValue, (long)int.MaxValue / size + 1);
        var number = Math.Clamp(page ?? 1, 1, maximumPage);
        return new PageRequest(number, size);
    }
}
