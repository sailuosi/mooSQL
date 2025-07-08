
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{
    internal class StringExpression : Expression
    {
        public string Value { get; set; }

        public StringExpression(string value) { 
            this.Value = value;
        }

        public override ExpressionType NodeType => ExpressionType.Constant;
        public override Type Type => typeof(string);
    }
}
