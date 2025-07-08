using mooSQL.data;
using mooSQL.data.model;
using mooSQL.linq.SqlQuery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 由于一个linq查询可能产生多个查询，本类表示一个linq的查询构建结果
    /// </summary>
    public class SQLCmds<T>
    {

        /// <summary>
        /// 待执行的SQL完全体
        /// </summary>
        public List<SQLCmd> cmds = new List<SQLCmd>();
        /// <summary>
        /// SQL定义体
        /// </summary>
        public T Sql;

        public IReadOnlyCollection<string>? QueryHints;
        /// <summary>
        /// 命令数
        /// </summary>
        public int Count { 
            get { return cmds.Count; }
        }

        public void Add(SQLCmd cmd)
        {
            cmds.Add(cmd);
        }
    }
}
