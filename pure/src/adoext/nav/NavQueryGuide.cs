using mooSQL.data.clip;
using mooSQL.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace mooSQL.data
{
    /// <summary>
    /// 导航查询指引：在已有主表结果集上，按外键批量加载子实体并回填到导航集合属性，支持链式继续下一级（<see cref="thenInclude"/>）。
    /// </summary>
    /// <typeparam name="T">主实体类型。</typeparam>
    /// <typeparam name="Child">子实体类型（从表），须为已注册的实体类型。</typeparam>
    public class NavQueryGuide<T, Child> : NavGuideBase<T>
    {
        /// <summary>使用与基类相同的构建器与主列表创建导航查询指引。</summary>
        public NavQueryGuide(SQLBuilder builder, IEnumerable<T> mainList) : base(builder, mainList)
        {
        }

        /// <summary>限流 / 跨片策略；空则用 <see cref="NavIncludeOptions.Default"/>。</summary>
        public NavIncludeOptions Options { get; set; }

        /// <summary>当前 Include 深度（首层=1）。</summary>
        public int Depth { get; set; } = 1;

        /// <summary>最近一次 include 得到的子实体列表。</summary>
        public IEnumerable<Child> ChildList { get; set; }

        NavIncludeOptions EffectiveOptions => Options ?? NavIncludeOptions.Default;

        void EnsureSafeToLoad()
        {
            var opt = EffectiveOptions;
            if (opt == null) return;

            if (opt.MaxDepth > 0 && Depth > opt.MaxDepth)
                throw new InvalidOperationException(
                    $"Include 深度超限：Depth={Depth} > MaxDepth={opt.MaxDepth}");

            var mains = MainList as ICollection<T> ?? MainList?.ToList();
            if (mains != null && opt.MaxParentCount > 0 && mains.Count > opt.MaxParentCount)
                throw new InvalidOperationException(
                    $"Include 主列表超限：Count={mains.Count} > MaxParentCount={opt.MaxParentCount}");

            if (!opt.AllowCrossShard)
            {
                AssertNotSharded(typeof(T), "主实体");
                AssertNotSharded(typeof(Child), "子实体");
            }
        }

        void AssertNotSharded(Type type, string role)
        {
            var en = Builder?.DBLive?.client?.EntityCash?.getEntityInfo(type);
            if (en?.Shard != null && en.Shard.IsActive)
                throw new InvalidOperationException(
                    $"Include 默认禁止跨分片：{role} {type.Name} 已启用分片。若确需加载，请设 NavIncludeOptions.AllowCrossShard=true 并自行限定物理表。");
        }

        /// <summary>
        /// 根据主表主键集合与子表外键列批量查询子数据，并按主键/外键匹配关系写入每个主实体上的集合。
        /// </summary>
        public NavQueryGuide<T, Child> include<K>(
            Func<T, ICollection<Child>> childSelector,
            Func<T, K> findListPKValue,
            Func<Child, K> childFKSelector,
            string childFKName,
            Action<SQLBuilder> childFilter)
        {
            EnsureSafeToLoad();

            var pkValues = MainList.map(findListPKValue);

            var childEn = Builder.DBLive.client.EntityCash.getEntityInfo<Child>();
            if (childEn == null)
                throw new Exception("子表" + typeof(Child).Name + "不是注册实体，无法定位其数据库信息！");

            var kit0 = Builder.useSQL();
            Builder.Client.Translator.BuildSelectFrom(kit0, childEn);

            kit0.whereIn(childFKName, pkValues);
            if (childFilter != null)
                childFilter(kit0);

            var chidren = kit0.query<Child>()?.ToList() ?? new List<Child>();
            var opt = EffectiveOptions;
            if (opt != null && opt.MaxChildRows > 0 && chidren.Count > opt.MaxChildRows)
                throw new InvalidOperationException(
                    $"Include 子行超限：Count={chidren.Count} > MaxChildRows={opt.MaxChildRows}");

            this.ChildList = chidren;

            foreach (var row in MainList)
            {
                var pkv = findListPKValue(row);
                var coll = childSelector(row);
                foreach (var ch in chidren)
                {
                    var cv = childFKSelector(ch);
                    if (cv != null && cv.Equals(pkv))
                        coll.Add(ch);
                }
            }
            return this;
        }

        /// <summary>按导航元数据自动解析主键/外键并加载。</summary>
        public NavQueryGuide<T, Child> includeNav(
            Expression<Func<T, ICollection<Child>>> childSelector,
            Action<SQLBuilder> childFilter = null)
        {
            var field = Builder.DBLive.FindField(childSelector);
            if (field == null)
                throw new Exception("未找到导航属性对应的实体字段信息，无法加载子表集合！");

            var navMark = field.Column.Navigat;
            if (navMark == null)
                throw new Exception("实体" + typeof(T).Name + "的属性" + field.Column.PropertyName + "，未定义导航信息，无法加载子表集合！");

            EntityColumn pkCol = null;
            var pkey = navMark.BossKey;
            if (pkey == null)
            {
                var pk = field.Column.belongTable.GetPK();
                if (pk.Count != 1)
                    throw new Exception("实体" + typeof(T).Name + "未定义主键，无法加载子表集合！");
                pkCol = pk[0];
            }
            else
            {
                pkCol = field.Column.belongTable.GetColumn(pkey);
            }

            if (pkCol == null)
                throw new Exception("导航加载时，为找到主表的键值！");

            Func<T, object> findListPKValue = item => pkCol.PropertyInfo.GetValue(item, null);

            var childFK = navMark.SlaveKey;
            var fkCol = Builder.DBLive.client.EntityCash.getField(typeof(Child), childFK);
            if (fkCol == null)
                throw new Exception("导航属性的外键字段【" + childFK + "】不存在！");

            Func<Child, object> childFKSelector = item => fkCol.PropertyInfo.GetValue(item, null);
            var funChild = childSelector.Compile();
            var navProp = field.Column.PropertyInfo;
            if (navProp != null && navProp.CanWrite)
            {
                foreach (var row in MainList)
                {
                    if (row == null) continue;
                    if (navProp.GetValue(row) == null)
                        navProp.SetValue(row, new List<Child>());
                }
            }
            return this.include<object>(funChild, findListPKValue, childFKSelector, fkCol.DbColumnName, childFilter);
        }

        /// <summary>在已加载的 Child 集合上开启下一级导航。</summary>
        public NavQueryGuide<Child, Next> thenInclude<Next, K>(
            Func<Child, ICollection<Next>> childSelector,
            Func<Child, K> findListPKValue,
            Func<Next, K> childFKSelector,
            string childFKName,
            Action<SQLBuilder> childFilter)
        {
            var gide = new NavQueryGuide<Child, Next>(this.Builder, this.ChildList)
            {
                Options = EffectiveOptions,
                Depth = Depth + 1
            };
            return gide.include<K>(childSelector, findListPKValue, childFKSelector, childFKName, childFilter);
        }

        /// <summary>thenInclude 表达式外键重载。</summary>
        public NavQueryGuide<Child, Next> thenInclude<Next, K>(
            Func<Child, ICollection<Next>> childSelector,
            Func<Child, K> findListPKValue,
            Expression<Func<Next, K>> childFKSelector,
            Action<SQLBuilder> childFilter = null)
        {
            var fk = this.Builder.DBLive.FindFieldName(childFKSelector);
            var childFunc = childFKSelector.Compile();
            return thenInclude<Next, K>(childSelector, findListPKValue, childFunc, fk, childFilter);
        }
    }
}
