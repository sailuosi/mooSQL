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
        /// <summary>未分类。</summary>
        None =0,

        /// <summary>SELECT。</summary>
        Select=1,

        /// <summary>INSERT。</summary>
        Insert=2,

        /// <summary>UPDATE。</summary>
        Update=3,

        /// <summary>DELETE。</summary>
        Delete=4,

        /// <summary>MERGE。</summary>
        MergeInto=5,

        /// <summary>CREATE TABLE。</summary>
        CreateTable=6,


    }
}
