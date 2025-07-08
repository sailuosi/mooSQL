using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{
    public class UpdateOutput<T>
    {
        public T Deleted { get; set; } = default!;
        public T Inserted { get; set; } = default!;
    }
}
