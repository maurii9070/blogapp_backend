namespace Blog.Api.Shared;

public record PaginatedList<T>(
    List<T> Items,
    int TotalCount,
    int CurrentPage,
    int PageSize)
{
    public bool HasNextPage => CurrentPage * PageSize < TotalCount;
}
