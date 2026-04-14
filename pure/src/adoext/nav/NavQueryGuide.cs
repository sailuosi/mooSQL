using mooSQL.data.clip;
using mooSQL.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;


namespace mooSQL.data
{
    /// <summary>
    /// 导航查询指引：在已有主表结果集上，按外键批量加载子实体并回填到导航集合属性，支持链式继续下一级（<see cref="thenInclude"/>）。
    /// </summary>
    /// <typeparam name="T">主实体类型。</typeparam>
    /// <typeparam name="Child">子实体类型（从表），须为已注册的实体类型。</typeparam>
    public class NavQueryGuide<T, Child>: NavGuideBase<T>
    {

        /// <summary>
        /// 使用与基类相同的构建器与主列表创建导航查询指引。
        /// </summary>
        /// <param name="builder">SQL 构建器。</param>
        /// <param name="mainList">主实体集合，子数据将按外键匹配后写入各自主体的导航集合。</param>
        public NavQueryGuide(SQLBuilder builder, IEnumerable<T> mainList) : base(builder, mainList)
        {
        }

        /// <summary>
        /// 最近一次 <c>include</c> / <c>includeNav</c> 查询得到的子实体列表（扁平结果，随后会按外键分发到各主实体）。
        /// </summary>
        public IEnumerable<Child> ChildList { get; set; }



        /// <summary>
        /// 根据主表主键集合与子表外键列批量查询子数据，并按主键/外键匹配关系写入每个主实体上由 <paramref name="childSelector"/> 指定的集合。
        /// </summary>
        /// <typeparam name="K">主外键比较时使用的键类型（须与主键、外键取值可比较）。</typeparam>
        /// <param name="childSelector">从主实体取得要填充的子集合（通常为导航属性对应的 <c>ICollection&lt;Child&gt;</c>）。</param>
        /// <param name="findListPKValue">从主实体取出用于与子表外键比对的主键（或关联键）值。</param>
        /// <param name="childFKSelector">从子实体取出外键字段值。</param>
        /// <param name="childFKName">子表上外键对应的数据库列名（用于 <c>WHERE … IN</c>）。</param>
        /// <param name="childFilter">对子查询追加额外条件（可为 <c>null</c>）。</param>
        /// <returns>当前实例，用于继续链式导航。</returns>
        public NavQueryGuide<T,Child> include<K>(Func<T, ICollection<Child>> childSelector, Func<T, K> findListPKValue, Func<Child, K> childFKSelector, string childFKName, Action<SQLBuilder> childFilter)
        {

            //先获取主键集合

            var pkValues = MainList.map(findListPKValue);

            var childEn = Builder.DBLive.client.EntityCash.getEntityInfo<Child>();
            if (childEn == null)
            {
                throw new Exception("子表" + typeof(Child).Name + "不是注册实体，无法定位其数据库信息！");
            }

            var kit0 = Builder.useSQL();
            Builder.Client.Translator.BuildSelectFrom(kit0, childEn);

            kit0.whereIn(childFKName, pkValues);
            //子表的更多条件过滤，允许为空
            if (childFilter != null)
            {
                childFilter(kit0);
            }
            var chidren = kit0.query<Child>();
            this.ChildList = chidren;

            //把子表数据分配回去
            foreach (var row in MainList)
            {
                var pkv = findListPKValue(row);

                var coll = childSelector(row);
                //
                foreach (var ch in chidren)
                {

                    var cv = childFKSelector(ch);
                    if (cv != null && cv.Equals(pkv))
                    {
                        coll.Add(ch);
                    }
                }
            }
            return this;
        }
        /// <summary>
        /// 根据主实体上导航属性的 <see cref="EntityColumn.Navigat"/> 元数据，自动解析主键列、子表外键列名与取值，批量加载子集合并回填。
        /// </summary>
        /// <param name="childSelector">指向主实体上子集合导航属性的表达式。</param>
        /// <param name="childFilter">对子查询追加额外条件（可为 <c>null</c>）。</param>
        /// <returns>当前实例，用于继续链式导航。</returns>
        public NavQueryGuide<T, Child> includeNav(Expression<Func<T, ICollection<Child>>> childSelector, Action<SQLBuilder> childFilter = null)
        {

            //先尝试找导航的属性
            var field = Builder.DBLive.FindField(childSelector);
            if (field == null)
            {
                throw new Exception("未找到导航属性对应的实体字段信息，无法加载子表集合！");
            }
            //从实体的导航定义中，获取外键信息
            var navMark = field.Column.Navigat;
            if (navMark == null)
            {
                throw new Exception("实体" + typeof(T).Name + "的属性" + field.Column.PropertyName + "，未定义导航信息，无法加载子表集合！");
            }
            //获取主表主键值
            EntityColumn pkCol = null;
            var pkey = navMark.BossKey;
            if (pkey == null)
            {
                var pk = field.Column.belongTable.GetPK();
                if (pk.Count != 1)
                {
                    //只能加载有唯一主键的实体
                    throw new Exception("实体" + typeof(T).Name + "未定义主键，无法加载子表集合！");
                }
                pkCol = pk[0];
            }
            else
            {
                pkCol = field.Column.belongTable.GetColumn(pkey);
            }

            if (pkCol == null)
            {
                throw new Exception("导航加载时，为找到主表的键值！");
            }
            Func<T, object> findListPKValue = (item) =>
            {
                return pkCol.PropertyInfo.GetValue(item, null);
            };
            //子表外键加载逻辑
            var childFK = navMark.SlaveKey;
            var fkCol = Builder.DBLive.client.EntityCash.getField(typeof(Child), childFK);
            if (fkCol == null)
            {
                throw new Exception("导航属性的外键字段【" + childFK + "】不存在！");
            }
            //fkCol.PropertyInfo.GetValue(item, null)
            Func<Child, object> childFKSelector = (item) =>
            {
                return fkCol.PropertyInfo.GetValue(item, null);
            };
            var funChild = childSelector.Compile();
            return this.include<object>( funChild, findListPKValue, childFKSelector, childFK, childFilter);

        }
        /// <summary>
        /// 在已加载的 <typeparamref name="Child"/> 集合上开启下一级导航：以子实体为新的「主」类型，按指定外键关系加载 <typeparamref name="Next"/> 集合并回填。
        /// </summary>
        /// <typeparam name="Next">再下一级（孙级）实体类型。</typeparam>
        /// <typeparam name="K">本层关联键类型。</typeparam>
        /// <param name="childSelector">从子实体取得要填充的下一级集合。</param>
        /// <param name="findListPKValue">从子实体取出主键（或关联键）值。</param>
        /// <param name="childFKSelector">从下一级实体取出外键值。</param>
        /// <param name="childFKName">下一级表中外键列名。</param>
        /// <param name="childFilter">对下一级查询的附加条件（可为 <c>null</c>）。</param>
        /// <returns>新的 <see cref="NavQueryGuide{Child, Next}"/>，主数据为当前 <see cref="ChildList"/>。</returns>
        public NavQueryGuide<Child,Next> thenInclude<Next,K>(Func<Child, ICollection<Next>> childSelector, Func<Child, K> findListPKValue, Func<Next, K> childFKSelector, string childFKName, Action<SQLBuilder> childFilter) { 
            var gide=new NavQueryGuide<Child,Next>(this.Builder,this.ChildList);
            return gide.include<K>(childSelector, findListPKValue, childFKSelector, childFKName, childFilter);
        }
        /// <summary>
        /// <see cref="thenInclude{Next, K}(Func{Child, ICollection{Next}}, Func{Child, K}, Func{Next, K}, string, Action{SQLBuilder})"/> 的重载：通过表达式解析下一级实体上外键属性对应的数据库列名。
        /// </summary>
        /// <typeparam name="Next">再下一级实体类型。</typeparam>
        /// <typeparam name="K">外键属性类型。</typeparam>
        /// <param name="childSelector">从子实体取得要填充的下一级集合。</param>
        /// <param name="findListPKValue">从子实体取出主键（或关联键）值。</param>
        /// <param name="childFKSelector">指向下一级实体中外键属性的表达式，用于解析列名与取值。</param>
        /// <param name="childFilter">对下一级查询的附加条件（可为 <c>null</c>）。</param>
        /// <returns>新的 <see cref="NavQueryGuide{Child, Next}"/>。</returns>
        public NavQueryGuide<Child, Next> thenInclude<Next, K>(Func<Child, ICollection<Next>> childSelector, Func<Child, K> findListPKValue, Expression<Func<Next, K>> childFKSelector, Action<SQLBuilder> childFilter = null)
        {
            var fk = this.Builder.DBLive.FindFieldName(childFKSelector);
            var childFunc = childFKSelector.Compile();
            return thenInclude<Next, K>(childSelector, findListPKValue, childFunc, fk, childFilter);
        }

    }
}
