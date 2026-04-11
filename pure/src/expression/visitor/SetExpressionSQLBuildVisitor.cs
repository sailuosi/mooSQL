
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
        /// <summary>
        /// 使用指定的编译上下文创建 SET（UPDATE 赋值等）表达式访问器。
        /// </summary>
        /// <param name="builder">快速编译上下文。</param>
        public SetExpressionSQLBuildVisitor(FastCompileContext builder) : base(builder)
        {
            valueVisitor = new ValueExpressionVisitor();
        }

        private ValueExpressionVisitor valueVisitor;


        /// <summary>
        /// 当前数据库客户端上的实体映射上下文（列名解析等）。
        /// </summary>
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
