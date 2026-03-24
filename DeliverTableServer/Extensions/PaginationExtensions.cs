using DeliverTableSharedLibrary.Dtos;

namespace DeliverTableServer.Extensions;

public static class PaginationExtensions
{
    public static PaginatedResult<TDto> ToPaginatedResult<TEntity, TDto>(
        this (List<TEntity> Items, int TotalCount) data,
        Func<TEntity, TDto> mapper,
        int pageNumber,
        int pageSize)
    {
        return new PaginatedResult<TDto>
        {
            Items = data.Items.Select(mapper).ToList(),
            TotalCount = data.TotalCount,
            Page = pageNumber > 0 ? pageNumber : 1,
            PageSize = pageSize
        };
    }
}
