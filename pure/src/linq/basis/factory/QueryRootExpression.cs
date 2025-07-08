using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{
    internal class QueryRootExpression:Expression
    {

        Type EntityType;

        IAsyncQueryProvider QueryProvider;


        public QueryRootExpression(Type EntityType) { 
            this.EntityType = EntityType;
            this.Type = typeof(IQueryable<>).MakeGenericType(EntityType);
        }

        public override Type Type { get; }
    }
}
