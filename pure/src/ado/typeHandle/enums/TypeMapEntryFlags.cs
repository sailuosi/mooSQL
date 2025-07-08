using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    [Flags]
    internal enum TypeMapEntryFlags
    {
        None = 0,
        SetType = 1 << 0,
        UseGetFieldValue = 1 << 1,
    }
}
