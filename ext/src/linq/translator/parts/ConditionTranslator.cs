using mooSQL.data.call;

using mooSQL.data.model;
using mooSQL.linq.Linq.Builder;
using mooSQL.linq.SqlQuery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq.translator
{
    internal class ConditionTranslator : BaseTranslateVisitor
    {
        public SearchConditionWord root;
        public SearchConditionWord building;

        public ProjectFlags flags;

        public ConditionTranslator(MethodVisitor visitor) : base(visitor)
        {
        }



        protected override Expression VisitBinary(BinaryExpression node)
        {

            if (node.NodeType == ExpressionType.And|| node.NodeType == ExpressionType.AndAlso) {

                var now = this.building;
                if (building.IsAnd==false) { 
                    building= new SearchConditionWord(false);
                }

                var l= Visit(node.Left);

                var r= Visit(node.Right);

                if (now.IsAnd==false) { 
                    now.Add(building);
                    building = now;
                }
            }

            if (node.NodeType == ExpressionType.Or || node.NodeType == ExpressionType.OrElse) {
                var now = this.building;
                //外界and or相悖时，需要切换执行环境。
                if (building.IsOr == false)
                {
                    building = new SearchConditionWord(true);
                }

                var l = Visit(node.Left);

                var r = Visit(node.Right);

                if (now.IsOr == false)
                {
                    now.Add(building);
                    building = now;
                }

            }

            return node;
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {

            if (node.NodeType == ExpressionType.Not) { 
                //切换环境并执行
                var now= this.building;
                this.building = new SearchConditionWord();
                Visit(node.Operand);

                now.Add(building.MakeNot());
                building = now;
            }

            return base.VisitUnary(node);
        }
    }
}
