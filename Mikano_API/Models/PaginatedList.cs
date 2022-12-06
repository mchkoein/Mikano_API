using System;
using System.Collections.Generic;
using System.Linq;

namespace Mikano_API.Helpers
{

    public class PaginatedList<T> : List<T>
    {

        public int PageIndex { get; private set; }
        public int PageSize { get; private set; }
        public int TotalCount { get; private set; }
        public int TotalPages { get; private set; }

        public PaginatedList(IQueryable<T> source, int pageIndex, int pageSize)
        {
            PageIndex = pageIndex;
            PageSize = pageSize;
            TotalCount = source.Count();

            if (pageSize != 0)
            {
                TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
                this.AddRange(source.Skip(PageIndex * PageSize).Take(PageSize));
            }
            else
            {
                TotalPages = 1;
                this.AddRange(source);
            }
        }

        public bool HasPreviousPage
        {
            get
            {
                return (PageIndex > 0);
            }
        }

        public bool HasNextPage
        {
            get
            {
                return (PageIndex + 1 < TotalPages);
            }
        }
    }
}
