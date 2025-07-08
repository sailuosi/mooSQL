using mooSQL.data.call;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{
    /// <summary>
    /// 基础LINQ构造产物的翻译器，执行核心逻辑的翻译
    /// </summary>
    public class BaseTranslateVisitor:ExpressionVisitor
    {

        protected MethodVisitor methodVisitor;

        public BaseTranslateVisitor(MethodVisitor visitor) { 
            this.methodVisitor = visitor;
        }



        protected override Expression VisitMethodCall(MethodCallExpression node) { 

            var method=node.Method;
            var call= CallUntil.CreateCall(node);
            //方法交由方法访问器进行处理。
            var callRes= methodVisitor.Visit(call);
            if (callRes is ExpressionCall expressionCall) {
                return expressionCall.Value;
            }
            if (callRes is ConstantCall constant)
            {
                return Expression.Constant(constant.Value);
            }

            return node; 
        }

    }
}
