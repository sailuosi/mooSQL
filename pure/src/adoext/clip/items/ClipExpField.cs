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
        public EntityColumn EnColumn { get; set; }
        public EntityInfo EnTable { get; set; }

        public Expression SrcExp { get; set; }

        public MemberExpression MemExp { get; set; }

        public string AsName { get; set; }

        public MemberInfo AsToMember { get; set; }
        /// <summary>
        /// 在clip中注册的表信息
        /// </summary>
        public ClipTable CTable { get; set; }

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
