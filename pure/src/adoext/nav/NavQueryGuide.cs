using mooSQL.data.clip;
using mooSQL.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace mooSQL.data
{
    /// <summary>
    /// 导航指引类，用于实现多级导航
    /// </summary>
    public class NavQueryGuide<T, Child>: NavGuideBase<T>
    {

        public NavQueryGuide(SQLBuilder builder, IEnumerable<T> mainList) : base(builder, mainList)
        {
        }

        public IEnumerable<Child> ChildList { get; set; }



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
        /// 按导航特性进行加载子集合
        /// </summary>

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
        /// 开始下一级导航
        /// </summary>
        /// <typeparam name="Next"></typeparam>
        /// <typeparam name="K"></typeparam>
        /// <param name="childSelector"></param>
        /// <param name="findListPKValue"></param>
        /// <param name="childFKSelector"></param>
        /// <param name="childFKName"></param>
        /// <param name="childFilter"></param>
        /// <returns></returns>
        public NavQueryGuide<Child,Next> thenInclude<Next,K>(Func<Child, ICollection<Next>> childSelector, Func<Child, K> findListPKValue, Func<Next, K> childFKSelector, string childFKName, Action<SQLBuilder> childFilter) { 
            var gide=new NavQueryGuide<Child,Next>(this.Builder,this.ChildList);
            return gide.include<K>(childSelector, findListPKValue, childFKSelector, childFKName, childFilter);
        }
        /// <summary>
        /// 开始下一级导航
        /// </summary>
        /// <typeparam name="Next"></typeparam>
        /// <typeparam name="K"></typeparam>
        /// <param name="childSelector"></param>
        /// <param name="findListPKValue"></param>
        /// <param name="childFKSelector"></param>
        /// <param name="childFilter"></param>
        /// <returns></returns>
        public NavQueryGuide<Child, Next> thenInclude<Next, K>(Func<Child, ICollection<Next>> childSelector, Func<Child, K> findListPKValue, Expression<Func<Next, K>> childFKSelector, Action<SQLBuilder> childFilter = null)
        {
            var fk = this.Builder.DBLive.FindFieldName(childFKSelector);
            var childFunc = childFKSelector.Compile();
            return thenInclude<Next, K>(childSelector, findListPKValue, childFunc, fk, childFilter);
        }

    }
}
