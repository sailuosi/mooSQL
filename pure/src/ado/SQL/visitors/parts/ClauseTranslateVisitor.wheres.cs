using mooSQL.auth;
using mooSQL.data.model;
using mooSQL.data.model.affirms;
using mooSQL.utils;
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
        /// 转译比较符
        /// </summary>
        /// <param name="expr"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        protected virtual string TranslateCompareOperator(ExprExpr expr)
        {
            switch (expr.Operator)
            {
                case AffirmWord.Operator.Equal: return " = ";
                case AffirmWord.Operator.NotEqual: return " <> "; 
                case AffirmWord.Operator.Greater: return " > ";
                case AffirmWord.Operator.GreaterOrEqual: return " >= "; 
                case AffirmWord.Operator.NotGreater: return " !> "; 
                case AffirmWord.Operator.Less: return " < "; 
                case AffirmWord.Operator.LessOrEqual: return " <= "; 
                case AffirmWord.Operator.NotLess: return " !< "; 
                case AffirmWord.Operator.Overlaps: return " OVERLAPS ";
            }
            throw new NotSupportedException("未知的操作符："+expr.Operator);
        }
        /// <summary>
        /// 二元条件
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public override Clause VisitAffirmExprExpr(ExprExpr clause)
        {
            var field = VisitIExpWord(clause.Expr1).ToString();

            var val = VisitIExpWord(clause.Expr2);
            builder.where(field,val.ToString(),TranslateCompareOperator(clause),false);
            return clause;
        }

        public override Clause VisitNullabilityExpression(NullabilityWord clause)
        {
            var tar = clause.SqlExpression;
            return VisitIExpWord(tar);
        }

        /// <summary>
        /// between
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public override Clause VisitAffirmBetween(Between clause)
        {
            var field = VisitIExpWord(clause.Expr1).ToString();

            var val1= VisitIExpWord(clause.Expr2);
            var val2 = VisitIExpWord(clause.Expr3);
            builder.whereBetween(field, val1, val2);
            return clause;
        }
        /// <summary>
        /// is null
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public override Clause VisitAffirmIsNull(IsNull clause)
        {
            var field = VisitIExpWord(clause.Expr1).ToString();
            builder.where(field.ToString() + (clause.IsNot?" IS NOT NULL ": " IS NULL "));
            return clause;
        }
        /// <summary>
        /// 永真
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public override Clause VisitTrueAffirm(TrueAffirm clause)
        {
            builder.where("1=1");
            return clause;
        }
        /// <summary>
        /// 永假
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public override Clause VisitFalseAffirm(FalseAffirm clause)
        {
            builder.where("1=0");
            return clause;
        }
        /// <summary>
        /// where in list
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public override Clause VisitAffirmInList(InList clause)
        {

            var vals = clause.Values;
            var field = VisitIExpWord(clause.Expr1).ToString();
            var vallist= vals.map((v) => VisitIExpWord(v));
            if (clause.IsNot)
            {
                builder.whereNotIn(field, vallist);
            }
            else {
                builder.whereIn(field, vallist);
            }
            return null;
        }
        /// <summary>
        ///  where like
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public override Clause VisitAffirmLike(Like clause)
        {
            var field = VisitIExpWord(clause.Expr1).ToString();
            var value = VisitIExpWord(clause.Expr2).ToString();
            if (clause.IsNot)
            {
                builder.where(field, value, " NOT LIKE");
            }
            else {
                builder.where(field, value, "LIKE");
            }
            return null;
        }
        /// <summary>
        /// 访问子查询，此时，临时切换编织器。
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public override Clause VisitAffirmInSubQuery(InSubQuery clause) {
            var field = VisitIExpWord(clause.Expr1).ToString();
            var op = clause.IsNot ? "NOT IN" : "IN";
            var nowBuilder= this.builder;
            builder.where(field, op, (kit) => {
                this.builder = kit;
                this.VisitSqlQuery(clause.SubQuery);
            });
            this.builder = nowBuilder;
            return null;
        }
    }
}
