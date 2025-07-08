/*
 * 表结构修改相关：
 */
using mooSQL.data.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{
    public partial class ClauseTranslateVisitor
    {

        /// <summary>
        /// 清空表的语句
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public override Clause VisitTruncateTableSentence(TruncateTableSentence clause)
        {
            var tag= TranslateTag(clause);

            var res= new StringBuilder();
            res.Append(tag);
            res.Append(TranslateTruncate());
            var tb=VisitTableNode(clause.Table);
            res.Append(tb.ToString());
            return new SQLFragClause(res.ToString());
        }
        /// <summary>
        /// 转译truncate语句
        /// </summary>
        /// <returns></returns>
        protected virtual string TranslateTruncate() {
            //DELETE FROM 
            return "TRUNCATE TABLE";
        }
        /// <summary>
        /// 转译标签
        /// </summary>
        /// <param name="sentence"></param>
        /// <returns></returns>
        protected virtual string TranslateTag(BaseSentence sentence) {
            if (sentence.Tag != null) { 
                
                var res= VisitComment(sentence.Tag);
                return res.ToString();
            }
            return null;
        }
        /// <summary>
        /// 删表
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public override Clause VisitDropTableSentence(DropTableSentence clause)
        {
            var tag = TranslateTag(clause);

            var res = new StringBuilder();
            res.Append(tag);
            res.Append(" DROP TABLE ");
            var tb = VisitTableNode(clause.Table);
            res.Append(tb.ToString());
            return new SQLFragClause(res.ToString());
        }

        public override Clause VisitCreateTableSentence(CreateTableSentence clause)
        {
            var res = new StringBuilder();

            res.Append("CREATE TABLE ");
            var tb = VisitTableNode(clause.Table);
            res.Append(tb.ToString());

            //字段语句创建

            //var orderedFields = clause.Fields.OrderBy(_ => _.CreateOrder >= 0 ? 0 : (_.CreateOrder == null ? 1 : 2)).ThenBy(_ => _.CreateOrder);
            //var fields = orderedFields.Select(f => new CreateFieldInfo { Field = f, StringBuilder = new StringBuilder() }).ToList();
            var maxlen = 0;

            return new SQLFragClause(res.ToString());
        }
    }
}
