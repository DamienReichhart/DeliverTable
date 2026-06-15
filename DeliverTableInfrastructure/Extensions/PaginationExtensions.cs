using DeliverTableSharedLibrary.Dtos;

namespace DeliverTableInfrastructure.Extensions;

public static class PaginationExtensions
{
    public static (int Skip, int Take) GetPaginationOffsets(int pageNumber, int pageSize)
    {
        int page = pageNumber > 0 ? pageNumber : 1;
        int skip = (page - 1) * pageSize;
        return (skip, pageSize);
    }

    public static IQueryable<T> Paginate<T>(this IQueryable<T> query, int pageNumber, int pageSize)
    {
        (int skip, int take) = GetPaginationOffsets(pageNumber, pageSize);
        return query.Skip(skip).Take(take);
    }

    public static IEnumerable<T> Paginate<T>(this IEnumerable<T> source, int pageNumber, int pageSize)
    {
        (int skip, int take) = GetPaginationOffsets(pageNumber, pageSize);
        return source.Skip(skip).Take(take);
    }

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
