using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    public class PageOutput<T>
    {
        public IEnumerable<T> Items { get; set; } = default!;

        public int Total { get; set; }

        public int PageSize { get; set; }

        public int PageNum { get; set; }
    }
}
