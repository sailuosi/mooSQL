using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace mooSQL.data
{
    public partial class SooRepository<T>
    {
        /// <summary>
        /// 对已物化主列表加载一对多导航（二次 IN，复用 <c>includeNav</c> / NavQueryGuide）。
        /// </summary>
        public NavQueryGuide<T, Child> Include<Child>(
            IEnumerable<T> list,
            Expression<Func<T, ICollection<Child>>> nav,
            Action<SQLBuilder> childFilter = null)
            where Child : class, new()
        {
            return Include(list, nav, childFilter, null);
        }

        /// <summary>Include，并可指定 <see cref="NavIncludeOptions"/>（深度/条数/跨片）。</summary>
        public NavQueryGuide<T, Child> Include<Child>(
            IEnumerable<T> list,
            Expression<Func<T, ICollection<Child>>> nav,
            Action<SQLBuilder> childFilter,
            NavIncludeOptions options)
            where Child : class, new()
        {
            return getKit().includeNav(list, nav, childFilter, options);
        }
    }
}
