using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.linq
{
    /// <summary>
    /// 从LINQ表达式中解析出来的字段信息。
    /// </summary>
    public class ParsedField
    {
        /// <summary>
        /// 字段所属的列信息。
        /// </summary>
        public EntityColumn Column { get; set; }
        /// <summary>
        /// 所属实体类型。
        /// </summary>
        public Type EntityType { get; set; }
        /// <summary>
        /// 所属表的别名，可为空。
        /// </summary>
        public string EntityAlias { get; set; }
        /// <summary>
        /// 字段 as 的别名，可为空。
        /// </summary>
        public string ColumnAlias { get; set; }
        /// <summary>
        /// 字段名称，不带别名。
        /// </summary>
        public string ColumnName { get; set; }
        /// <summary>
        /// 调用者昵称，可为空。
        /// </summary>
        public string CallerNick { get; set; }

        public MemberInfo Member { get; set; }

        public Expression Exp { get; set; }
    }
}
