using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 本类可直接输出结果，而不是构造基础的SQL
    /// </summary>
    public abstract partial class SQLSentence
    {
        public virtual string GetDataBaseSql { get; }

        public virtual string GetViewInfoListSql { get; }

        public virtual string GetTableInfoListSql { get; }

        public virtual string GetColumnInfosByTableNameSql { get; }



        protected virtual int DefultLength { get; set; } = 255;


        protected virtual int DefultNumLength { get; set; } = 11;
    }
}
