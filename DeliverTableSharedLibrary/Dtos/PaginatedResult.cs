namespace DeliverTableSharedLibrary.Dtos;

/// <summary>
///     Generic wrapper for paginated API responses.
///     Shared between server (producer) and client (consumer).
/// </summary>
public sealed class PaginatedResult<T>
{
    public List<T> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public bool HasNextPage => Page * PageSize < TotalCount;
}
