using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{
    public interface IDbBusProvider: IQueryProvider
    {
        IDbBus<TElement> CreateBus<TElement>(Expression expression);
    }
}
