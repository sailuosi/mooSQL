using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace mooSQL.data
{
    public partial class SQLClip
    {
        /// <summary>
        /// 对已物化主列表加载一对多导航（复用 Builder.includeNav）。
        /// </summary>
        public NavQueryGuide<T, Child> include<T, Child>(
            IEnumerable<T> list,
            Expression<Func<T, ICollection<Child>>> nav,
            Action<SQLBuilder> childFilter = null)
            where T : class, new()
            where Child : class, new()
        {
            return Context.Builder.includeNav(list, nav, childFilter);
        }
    }
}
