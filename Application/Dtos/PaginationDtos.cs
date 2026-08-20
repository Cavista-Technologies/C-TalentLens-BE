namespace C_TalentLens.Application.Dtos;

public record PageRequest(int Page = 1, int PageSize = 50)
{
    public const int MaxPageSize = 200;

    public int NormalizedPage => Page < 1 ? 1 : Page;

    public int NormalizedPageSize => PageSize switch
    {
        < 1 => 50,
        > MaxPageSize => MaxPageSize,
        _ => PageSize
    };
}

public record PageMetadata(
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage);

public record PagedResponse<T>(
    bool Success,
    string Message,
    IReadOnlyCollection<T> Items,
    PageMetadata Pagination);

public static class Pagination
{
    public static PagedResponse<T> ToPagedResponse<T>(
        IEnumerable<T> source,
        PageRequest request,
        string message)
    {
        var items = source.ToList();
        var page = request.NormalizedPage;
        var pageSize = request.NormalizedPageSize;
        var totalItems = items.Count;
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);
        var pagedItems = items
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResponse<T>(
            true,
            message,
            pagedItems,
            new PageMetadata(
                page,
                pageSize,
                totalItems,
                totalPages,
                page > 1 && totalPages > 0,
                totalPages > 0 && page < totalPages));
    }
}
