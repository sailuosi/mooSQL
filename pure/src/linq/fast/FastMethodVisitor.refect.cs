using mooSQL.data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{
    internal partial class FastMethodVisitor
    {
        #region 被反射调用的执行方法组
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="kit"></param>
        /// <returns></returns>
        private IEnumerable<T> ExecuteQueryEnumT<T>(SQLBuilder kit)
        {
            var res = kit.query<T>();
            this.LoadNavChilds<T>(res);
            return res;
        }
        private T ExecuteQuerySingleT<T>(SQLBuilder kit, QueryContext context)
        {
            return kit.queryUnique<T>();
        }

        private PageOutput<T> ExecuteQueryPageT<T>(SQLBuilder kit, QueryContext context)
        {
            var res = new PageOutput<T>();
            res.Total = kit.count();
            res.Items = kit.query<T>();
            res.PageSize = kit.current.pageSize;
            res.PageNum = kit.current.pageNum;
            this.LoadNavChilds<T>(res.Items);
            return res;
        }
        /// <summary>
        /// 加载导航属性的子属性
        /// </summary>
        private void LoadNavChilds<T>(IEnumerable<T> items)
        {
            var ent = typeof(T);
            if (this.Context.NavColumns.ContainsKey(ent) == false) return;
            foreach (var col in this.Context.NavColumns[ent])
            {
                //子表的类型
                var chidType = col.Navigat.ChildType;
                if (chidType == null) continue;

                var pks = new List<object>();
                EntityColumn pkCol = null;
                var pkey = col.Navigat.BossKey;
                if (pkey == null)
                {
                    var pk = col.belongTable.GetPK();
                    if (pk.Count != 1)
                    {
                        //只能加载有唯一主键的实体
                        continue;
                    }
                    pkCol = pk[0];
                    pks = this.loadEntityFieldValues(items, pk[0]);
                }
                else
                {
                    pkCol = col.belongTable.GetColumn(pkey);
                }

                if (pkCol == null)
                {
                    continue;
                }
                pks = this.loadEntityFieldValues(items, pkCol);
                //执行子属性的读取
                var mothod = this.GetType().GetMethod("loadNavChild", BindingFlags.NonPublic | BindingFlags.Instance);
                var mot = mothod.MakeGenericMethod(chidType);

                var para = new object[2] { col.Navigat.SlaveKey, pks };
                var res = mot.Invoke(this, para);
                if (res != null)
                {
                    //执行赋值
                    var filterByFKMethod = this.GetType().GetMethod("filterByFK", BindingFlags.NonPublic | BindingFlags.Instance);
                    var filterByFKMethodT = filterByFKMethod.MakeGenericMethod(chidType);
                    foreach (var row in items)
                    {
                        var pkVal = pkCol.PropertyInfo.GetValue(row, null);
                        var paraSetNav = new object[3] { res, col.Navigat.SlaveKey, pkVal };
                        var navVal = filterByFKMethodT.Invoke(this, paraSetNav);
                        if (navVal != null)
                        {
                            col.PropertyInfo.SetValue(row, navVal);
                        }
                    }

                }
            }
        }

        private List<object> loadEntityFieldValues<T>(IEnumerable<T> items, EntityColumn column)
        {
            var list = new List<object>();
            foreach (var item in items)
            {
                var val = column.PropertyInfo.GetValue(item, null);
                if (val != null)
                {
                    list.Add(val);
                }
            }
            return list;
        }
        /// <summary>
        /// 反射调用
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fk"></param>
        /// <param name="fks"></param>
        /// <returns></returns>
        private IEnumerable<T> loadNavChild<T>(string fk, List<object> fks)
        {
            var kit = this.Context.DB.useSQL();
            var en = this.Context.DB.client.EntityCash.getTableName(typeof(T));
            var tar = kit.from(en)
               .whereIn(fk, fks)
               .query<T>();
            return tar;
        }
        /// <summary>
        /// 反射调用
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <param name="fk"></param>
        /// <param name="fkVal"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private IEnumerable<T> filterByFK<T>(IEnumerable<T> values, string fk, object fkVal)
        {
            var res = new List<T>();
            var fkCol = this.Context.DB.client.EntityCash.getField(typeof(T), fk);
            if (fkCol == null)
            {
                throw new Exception("导航属性的外键字段【" + fk + "】不存在！");
            }
            foreach (var item in values)
            {
                var v = fkCol.PropertyInfo.GetValue(item, null);
                if (v != null && v.Equals(fkVal))
                {
                    res.Add(item);
                }
            }
            return res;
        }
        #endregion
    }
}
