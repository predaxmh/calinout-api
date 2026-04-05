using API_Calinout_Project.DTOs;
using Microsoft.EntityFrameworkCore;

namespace API_Calinout_Project.Extensions
{
    public static class PaginationExtensions
    {

        public static async Task<PagedResponse<TDestination>> ToPagedResponseAsync<TDestination, TSource>
            (
            this IQueryable<TSource> query,
            int page,
            int pageSize,
            Func<TSource, TDestination> mapFunc
            )
        {
            var totalRecords = await query.CountAsync();

            if (totalRecords == 0)
            {
                return new PagedResponse<TDestination>(new List<TDestination>(), 0, page, pageSize);
            }

            var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

            var mappedItems = items.Select(mapFunc).ToList();

            return new PagedResponse<TDestination>(mappedItems, totalRecords, page, pageSize);
        }
    }
}