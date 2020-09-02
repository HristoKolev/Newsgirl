namespace Newsgirl.Shared.Postgres
{
    using System;
    using System.Linq;

    public static class QueryableExtensions
    {
        public static IQueryable<T> Page<T>(this IQueryable<T> collection, int page, int pageSize)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            page = Math.Max(page, 1);
            pageSize = Math.Max(pageSize, 1);

            return collection.Skip((page - 1) * pageSize).Take(pageSize);
        }
    }
}
