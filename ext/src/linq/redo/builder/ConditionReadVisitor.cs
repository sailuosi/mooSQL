using mooSQL.data.model;
using mooSQL.linq.SqlQuery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.lingq.visitor
{
    /// <summary>
    /// 构建条件语句的访问者
    /// </summary>
    internal class ConditionReadVisitor:ExpressionVisitor
    {

        SearchConditionWord BuildingCondition;

        protected override Expression VisitBinary(BinaryExpression node)
        {

            if (node.NodeType == ExpressionType.And || node.NodeType == ExpressionType.AndAlso)
            {
                //如果当前不是and，则需要新建一个and条件
                var nowCondi = this.BuildingCondition;
                var andCondition = nowCondi.IsAnd ? nowCondi : new SearchConditionWord(false);
                //
                this.BuildingCondition = andCondition;
                //编译左表达式
                this.Visit(node.Left);
                //编译右表达式
                this.Visit(node.Right);
                //将当前条件设置为上一个条件
                if (!nowCondi.IsAnd)
                {
                    nowCondi.Add(andCondition);
                    this.BuildingCondition = nowCondi;
                }

            }
            else if (node.NodeType == ExpressionType.Or || node.NodeType == ExpressionType.OrElse) { 
                var isOr = this.BuildingCondition.IsOr;
                var nowCondi = this.BuildingCondition;
                if (isOr == false) { 
                    //切换环境
                    this.BuildingCondition = new SearchConditionWord(true);
                }
                //编译左表达式
                this.Visit(node.Left);
                //编译右表达式
                this.Visit(node.Right);
                if (isOr == false) { 
                    nowCondi.Add(this.BuildingCondition);
                    this.BuildingCondition = nowCondi;
                }
            
            }

            //处理比较操作符
            if (node.NodeType == ExpressionType.AndAlso) { 
            
            }


            return base.VisitBinary(node);
        }
        /// <summary>
        /// 访问一元表达式
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.NodeType == ExpressionType.Not) { 
                var nowCondi = this.BuildingCondition;
                var notCondi= new SearchConditionWord();
                this.BuildingCondition = notCondi;
                this.Visit(node.Operand);
                nowCondi.Add(notCondi.MakeNot());
                this.BuildingCondition = nowCondi;
            }


            return base.VisitUnary(node);
        }
    }
}
