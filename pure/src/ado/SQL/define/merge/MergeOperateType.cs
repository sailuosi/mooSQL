using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model
{
    public enum MergeOperateType
    {
        Insert,
        Update,
        Delete,
        UpdateWithDelete,
        UpdateBySource,
        DeleteBySource
    }
}
