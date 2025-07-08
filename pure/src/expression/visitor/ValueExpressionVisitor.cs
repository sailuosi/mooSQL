
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{
    /// <summary>
    /// 表值表达式的值获取，这里一定是个值，而不是子查询之类的
    /// </summary>
    public class ValueExpressionVisitor:ExpressionVisitor
    {

        /// <summary>
        /// 入口
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public override Expression Visit(Expression node)
        {

            if (node is ConstantExpression v)
            {
                return v;
            }

            var res= base.Visit(node);
            if (res is ConstantExpression resv)
            {
                return resv;
            }

            var val = node.EvaluateExpression();

            return Expression.Constant(val);

        }
        /// <summary>
        /// 二元
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitBinary(BinaryExpression node)
        {


            if (node.NodeType == ExpressionType.Add)
            {
                var left= Visit(node.Left);
                var right = Visit(node.Right);
                if (left is ConstantExpression val && right is ConstantExpression rightV) { 
                    var leftVal=val.Value;
                    if (leftVal is string valStr)
                    {
                        var res = valStr + rightV.Value.ToString();
                        return Expression.Constant(res);
                    }
                    else if (leftVal is int valInt && rightV.Value is int rightint) {
                        var res = valInt + rightint;
                        return Expression.Constant(res);
                    }
                    else if (leftVal is double valdoub && rightV.Value is double rightdoub)
                    {
                        var res = valdoub + rightdoub;
                        return Expression.Constant(res);
                    }
                }
            }
            return node;
        }
        /// <summary>
        /// 一元表达式
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitUnary(UnaryExpression node) {
            var op = node.Operand;
            if (node.NodeType == ExpressionType.Convert) { 
                return Visit(op);
            }
            return base.Visit(op);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            var mem = node.Member;
            if (mem.MemberType == System.Reflection.MemberTypes.Property) {
                var prop= mem as System.Reflection.PropertyInfo;
                var obj = Visit(node.Expression);
                if (obj is ConstantExpression con) {

                    var v = prop.GetValue(con.Value);
                    return Expression.Constant(v);
                }
            }
            if (mem.MemberType == MemberTypes.Field) {
                var field= mem as System.Reflection.FieldInfo;
                if (node.Expression is ConstantExpression con)
                {
                    var v= field.GetValue( con.Value);
                    return Expression.Constant(v);
                }
                var t = Visit(node.Expression);
                if (node.Expression is ConstantExpression co)
                {
                    var v = field.GetValue(co.Value);
                    return Expression.Constant(v);
                }
                return t;
            }
            if (node.Expression is MemberExpression memberBinding) {
                var a = 1;
            }
            if (node.Expression is ConstantExpression cons)
            {
                var a = 1;
            }

            return base.VisitMember(node);
        }



    }
}
