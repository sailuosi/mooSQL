using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    [Flags]
    internal enum Row
    {
        First = 0,
        FirstOrDefault = 1, //  & FirstOrDefault != 0: allow zero rows
        Single = 2, // & Single != 0: demand at least one row
        SingleOrDefault = 3
    }
}
