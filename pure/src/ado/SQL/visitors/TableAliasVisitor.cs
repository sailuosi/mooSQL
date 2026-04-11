using mooSQL.data.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{
    /// <summary>
    /// 从表节点中解析出用于 SQL 片段的别名或表名（优先别名，否则使用物理名）。
    /// </summary>
    public class TableAliasVisitor:ClauseVisitor
    {

        /// <summary>
        /// 访问普通表词，返回别名或表名对应的 SQL 片段。
        /// </summary>
        /// <param name="clause">表词节点。</param>
        /// <returns>表示别名/表名的片段子句。</returns>
        public override Clause VisitTableWord(TableWord clause)
        {
            var name= clause.Alias;
            if (string.IsNullOrWhiteSpace(name)) {
                name = clause.Name;
            }
            return new SQLFragClause(name);
        }

        /// <summary>
        /// 访问派生表（子查询等）：若已命名则返回该名；否则递归解析其源表节点。
        /// </summary>
        /// <param name="clause">派生表词节点。</param>
        /// <returns>别名或解析得到的表片段。</returns>
        public override Clause VisitDerivatedTable(DerivatedTableWord clause)
        {
            if (!string.IsNullOrWhiteSpace(clause.Name)) { 
                return new SQLFragClause(clause.Name);
            }
            var tar= VisitTableNode(clause.src);
            return tar;
        }
    }
}
