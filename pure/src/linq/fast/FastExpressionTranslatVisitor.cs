using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using mooSQL.data.call;



namespace mooSQL.linq
{
    internal class FastExpressionTranslatVisitor : BaseTranslateVisitor
    {

        public FastCompileContext Context { get; set; }

        public FastExpressionTranslatVisitor(MethodVisitor visitor) : base(visitor)
        {
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            var val = node.Value;
            var type = val.GetType();
            var t2 = type.GetGenericTypeDefinition();
            if(t2 == typeof(EntityQueryable<>)) {
                var argu = type.GetGenericArguments();
                if (argu.Length > 0)
                {
                    Context.EntityType = argu[0];
                }
            }

            return base.VisitConstant(node);
        }
    }
}
