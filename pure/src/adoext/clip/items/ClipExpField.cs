using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.clip
{
    /// <summary>
    /// 代表从表达式中识别出来的字段信息，包含字段映射、实体信息，解析后的SQL碎片等。
    /// </summary>
    internal class ClipExpField
    {
        /// <summary>
        /// SQL下字段名称
        /// </summary>
        public string SQLField { get; set; }
        /// <summary>
        /// 源表别名
        /// </summary>
        public string SQLAlias { get; set; }
        /// <summary>
        /// 实体字段信息
        /// </summary>
        public EntityColumn EnColumn { get; set; }
        /// <summary>
        /// 实体表信息
        /// </summary>
        public EntityInfo EnTable { get; set; }
        /// <summary>
        /// 源表达式信息。
        /// </summary>
        public Expression SrcExp { get; set; }
        /// <summary>
        /// 源表达式中的成员访问表达式。如果源表达式是成员访问表达式，则为该表达式；否则为null。
        /// </summary>
        public MemberExpression MemExp { get; set; }
        /// <summary>
        /// 别名名称，如果需要则为非空。例如：a.b as c
        /// </summary>
        public string AsName { get; set; }
        /// <summary>
        /// 别名对应的成员信息，如果需要则为非空。例如：a.b as c
        /// </summary>
        public MemberInfo AsToMember { get; set; }
        /// <summary>
        /// 在clip中注册的表信息
        /// </summary>
        public ClipTable CTable { get; set; }
        /// <summary>
        /// 成员信息，如果需要则为非空。例如：a.b as c
        /// </summary>
        public MemberInfo Member { get; set; }
        /// <summary>
        /// 转换成SQL字段表达式，如果需要别名则加上别名。
        /// </summary>
        /// <param name="needAlias"></param>
        /// <returns></returns>
        public string toSQLField(bool needAlias) {

            var sb = new StringBuilder();
            if (needAlias)
            {
                if (!string.IsNullOrWhiteSpace(SQLAlias)) { 
                    sb.Append(SQLAlias);
                    sb.Append(".");                
                }

                sb.Append(SQLField);
            }
            else
            {
                sb.Append(SQLField);
            }
            return sb.ToString();
        }
    }
}
