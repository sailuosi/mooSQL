/*
 * * Update Insert 语句构造
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
        /// Insert 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public override Clause VisitInsertSentence(InsertSentence clause)
        {
            
            var tag= VisitComment(clause.Tag);
            if (tag != null) { 
                builder.prefix(tag.ToString());
            }
            if (clause.With != null) { 
                VisitWithClause(clause.With);
            }
            this.VisitInsertClause(clause.Insert);

            //检查insert from
            if (clause.SelectQuery != null && clause.SelectQuery.From.Tables.Count>0) { 
                VisitSelectQuery(clause.SelectQuery);
            }
            //输出值部分
            if (clause.Insert.WithIdentity)
            {
                this.TranlateGetIdentity(clause.Insert);
            }
            else {
                this.VisitOutputClause(clause.Output);
            }

            
            return new SQLBuilderClause(builder, (tar) => {
                return tar.Builder.toInsert();
            });
        }
        /// <summary>
        /// 主键输出
        /// </summary>
        /// <param name="clause"></param>
        protected virtual void TranlateGetIdentity(InsertClause clause) { 
        }
        /// <summary>
        /// 构造插入部分
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public override Clause VisitInsertClause(InsertClause clause)
        {
            //没有写入字段时，不执行转译
            if (clause.Items.Count == 0)
            {
                return clause;
            }
                //插入目标的表
            if (clause.Into != null) { 
                var tb= VisitTableNode(clause.Into);
                builder.setTable(tb.ToString());
            }

            foreach (var setPair in clause.Items) {
                this.VisitSetWord(setPair);
            }
            return null;
        }
        /// <summary>
        /// 赋值部分转译
        /// </summary>
        /// <param name="clause"></param>
        /// <returns>null</returns>
        public override Clause VisitSetWord(SetWord clause)
        {
            var field = clause.Column;
            var val= clause.Expression;

            var fid= this.VisitIExpWord(field);
            var v = this.VisitIExpWord(field);
            //这里的逻辑存在可塑性。 当值部分是变量时，会被作为Paramete解析，然后转译为占位字符串。否则直接作为SQL。这2者均不需参数化。
            //后续可以通过检查返回值，来重塑这里的逻辑，从而利用SQLBuilder的参数化功能。
            builder.set(fid.ToString(), v.ToString(),false);
            return null;
        }

        /// <summary>
        /// 构造删除SQL
        /// </summary>
        /// <param name="clause"></param>
        /// <returns>SQLbuilder编织器</returns>
        public override Clause VisitDeleteSentence(DeleteSentence clause)
        {

            var tag = TranslateTag(clause);
            builder.prefix(tag);

            if (clause.With != null) {
                VisitWithClause(clause.With);
            }

            var tarTable= VisitTableNode(clause.Table);
            builder.setTable(tarTable.ToString());
            //可简化
            VisitSelectQuery(clause.SelectQuery);

            if (clause.Output != null) { 
                VisitOutputClause(clause.Output);
            }

            return new SQLBuilderClause(builder, (tar) => {
                return tar.Builder.toDelete();
            });
        }
        /// <summary>
        /// update 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public override Clause VisitUpdateSentence(UpdateSentence clause)
        {
            var tag = TranslateTag(clause);
            builder.prefix(tag);

            if (clause.With != null)
            {
                VisitWithClause(clause.With);
            }

            //构建主体部分
            VisitUpdateClause(clause.Update);
            //可简化
            VisitSelectQuery(clause.SelectQuery);
            if (clause.Output != null) {
                VisitOutputClause(clause.Output);
            }
            return new SQLBuilderClause(builder, (tar) => { return tar.Builder.toUpdate(); });
        }
        /// <summary>
        /// 构建更新语句的主体部分
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public override Clause VisitUpdateClause(UpdateClause clause)
        {
            //没有写入字段时，不执行转译
            if (clause.Items.Count == 0)
            {
                return clause;
            }
            //插入目标的表
            if (clause.Table != null)
            {
                var tb = VisitTableNode(clause.Table);
                builder.setTable(tb.ToString());
            }

            foreach (var setPair in clause.Items)
            {
                this.VisitSetWord(setPair);
            }
            return null;
        }
    }
}
