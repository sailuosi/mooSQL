
using mooSQL.data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{
    /// <summary>
    /// 访问Set语句的子表达式的访问器
    /// </summary>
    public class SetExpressionSQLBuildVisitor : BaseExpressionSQLBuildVisitor
    {
        public SetExpressionSQLBuildVisitor(FastCompileContext builder) : base(builder)
        {
            valueVisitor = new ValueExpressionVisitor();
        }

        private ValueExpressionVisitor valueVisitor;


        public EntityContext EntityContext { get {
                return Builder.DBLive.client.EntityCash;
            }  
        }


        /// <summary>
        /// 二元
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitBinary(BinaryExpression node) {

            if (node.NodeType == ExpressionType.Equal)
            {
                if (node.Left is MemberExpression member)
                {
                    var field = VisitMemberExpression(member);

                    var val =valueVisitor.Visit( node.Right);
                    if (val is ConstantExpression constant) { 
                        Builder.set(field, constant.Value);
                    }
                }

            }
            else if (node.NodeType == ExpressionType.Add) { 
                return base.VisitBinary(node);
            }
            return node;
        }
        /// <summary>
        /// 属性访问
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected string VisitMemberExpression(MemberExpression node) {
            var prop = node.Member;
            return EntityContext.getFieldName(prop);
        }



    }
}
