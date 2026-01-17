using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model
{
    /// <summary>
    /// SQL语句类型
    /// </summary>
    public enum SentenceType
    {
        None =0,

        Select=1,

        Insert=2,

        Update=3,

        Delete=4,

        MergeInto=5,

        CreateTable=6,


    }
}
