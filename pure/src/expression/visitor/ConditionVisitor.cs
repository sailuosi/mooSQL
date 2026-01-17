using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using mooSQL.data;
using mooSQL.data.call;

namespace mooSQL.linq
{
    /// <summary>
    /// 抽象的条件构造器，抽离了方法访问器、值访问器、字段访问器的组装，以便于在子类中重写。
    /// </summary>
    public abstract class ConditionVisitor : BaseExpressionSQLBuildVisitor
    {
        protected ConditionVisitor(FastCompileContext context) : base(context)
        {
        }

        protected ExpressionVisitor ValueVisitor { get; set; }
        protected MethodVisitor MethodVisitor { get; set; }



        /// <summary>
        /// 成员
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public override Expression Visit(Expression node)
        {
            //if (node is BinaryExpression barr)
            //{
            //    return VisitBinaryExpression(barr);
            //}
            return base.Visit(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {

            var method = node.Method;
            var call = CallUntil.CreateCall(node);
            //方法交由方法访问器进行处理。
            var callRes = MethodVisitor.Visit(call);
            if (callRes is ExpressionCall expressionCall)
            {
                return expressionCall.Value;
            }
            if (callRes is ConstantCall constant)
            {
                return Expression.Constant(constant.Value);
            }

            return node;
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            if (node.Parameters.Count > 0)
            {
                var ent = node.Parameters[0];
                this.Context.CurrentLayer.suck(ent.Type, ent.Name);
            }
            return base.VisitLambda(node);
        }

        protected override Expression VisitBlock(BlockExpression node)
        {
            return base.VisitBlock(node);
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {

            if (node.NodeType == ExpressionType.Quote)
            {
                var body = node.Operand;
                Context.CurrentLayer.Current.sink();
                Visit(body);
                Context.CurrentLayer.Current.rise();
                return node;
            }

            return base.VisitUnary(node);
        }

        public abstract string VisitToGotField(Expression node);

        public abstract string VisitToGotField(Expression node, out MemberInfo member);

        public virtual object VisitToGotValue(Expression node)
        {
            try
            {
                var val = ValueVisitor.Visit(node);
                if (val is ConstantExpression constant)
                {
                    return constant.Value;
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// 二元
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.NodeType == ExpressionType.And || node.NodeType == ExpressionType.AndAlso)
            {
                Builder.sink();
                Visit(node.Left);
                Visit(node.Right);
                Builder.rise();
                return node;
            }
            else if (node.NodeType == ExpressionType.Or || node.NodeType == ExpressionType.OrElse)
            {
                Builder.sinkOR();
                Visit(node.Left);
                Visit(node.Right);
                Builder.rise();
                return node;
            }

            var op = this.GetCompareOp(node.NodeType);
            if (op != null)
            {
                //此时是支持的比较符，则获取左右值
                var fie = VisitToGotField(node.Left);
                //暂时允许
                WhereFrag wh = new WhereFrag();
                wh.op = op;
                bool fail = false;
                if (fie != null)
                {
                    wh.key = fie;
                }
                else
                {
                    //作为字段解析失败，尝试作为值来解析
                    var leftVal = VisitToGotValue(node.Left);
                    if (leftVal != null)
                    {
                        wh.leftValue = leftVal;
                    }
                    else
                    {
                        fail = true;
                    }
                }

                //如果未失败，则解析右侧表达式
                if (fail == false)
                {
                    //值部分，同样优先解析为SQL
                    var valuesql = VisitToGotField(node.Right);
                    if (valuesql != null)
                    {
                        wh.value = valuesql;
                        wh.paramed = false;
                        Builder.where(wh);
                    }
                    else
                    {
                       var constant = VisitToGotValue(node.Right);
                        if (constant != null)
                        {
                            wh.value = constant;
                            wh.paramed = true;
                            Builder.where(wh);
                        }
                    }
 

                }
            }

            return node;
        }

        private string GetCompareOp(ExpressionType type)
        {
            switch (type)
            {

                case ExpressionType.Equal:
                    return "=";
                case ExpressionType.GreaterThan:
                    return ">";
                case ExpressionType.GreaterThanOrEqual:
                    return ">=";
                case ExpressionType.LessThan:
                    return "<";
                case ExpressionType.LessThanOrEqual:
                    return "<=";
                case ExpressionType.NotEqual:
                    return "<>";

            }
            return null;
        }

        /// <summary>
        /// 属性
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitMember(MemberExpression node)
        {
            var prop = node.Member;
            var t = this.Builder.DBLive.client.EntityCash.getFieldName(prop);
            var nick = Context.CurrentLayer.getNick(prop.ReflectedType);
            if (nick != null)
            {
                t = nick + "." + t;
            }
            return new StringExpression(t);
        }
    }
}
