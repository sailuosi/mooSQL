using mooSQL.data.clip;
using mooSQL.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 暴露给业务侧使用的SQL标准化扩展，用于快速安全的创建SQL命令
    /// </summary>
    public static class SQLCmdExtensions
    {
        /// <summary>
        /// 参数占位符使用 string.Format方法的格式传入，即{0}...{1}...{2}
        /// </summary>
        /// <param name="SQL"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public static SQLCmd formatSQL(this string SQL, params object[] values) {
            var ps = new Paras();
            var key=ps.formatSQL(SQL, values);
            var cmd = new SQLCmd(key, ps);
            return cmd;
        }

        /// <summary>
        /// 类似mybatis的模式，SQL中使用#{Name}占位，需要传入的对象有一个 Name属性。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="SQL"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static SQLCmd formatSQLBy<T>(this string SQL, T target) where T:class
        {

            string key = SQL;
            string prefixName = "psfmt_";
            var ps = new Paras();

            var t = typeof(T);

            var props = SimpleTypePropsCache.TypePropsCache.GetOrSet(t, (k) =>k.GetProperties() );         
            

            for (int i = 0; i < props.Length; i++)
            {
                var p = props[i];
                string reg = "#{" + p.Name + "}";
                if (SQL.Contains(reg) == false) {
                    continue;
                }
                //直接反射调用，消耗性能，调试时使用
                //var v = p.GetValue(target);
                //委托
                var v = p.GetValueCached(target);
                if (v == null)
                {
                    key = key.Replace(reg, " null ");
                }
                else
                {
                    string paraName = prefixName + ps.Count + "_" + i;
                    var holderName = "#{" + paraName + "}";
                    key = key.Replace(reg, holderName);
                    ps.AddRaw(paraName, holderName, v);
                }

            }
            var cmd = new SQLCmd(key, ps);
            return cmd;
        }

        /// <summary>
        /// 树的向上查找主键方法
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="kit"></param>
        /// <param name="pks"></param>
        /// <param name="loadParentOIDByPK"></param>
        /// <returns></returns>
        public static List<T> findTreeParentOIDs<T>(this SQLBuilder kit, List<T> pks,Func<SQLBuilder ,List<T>, List<T>> loadParentOIDByPK)
        {
            var lvpoids =  pks ;
            var allPoids = new List<T>();
            int pkcount = pks.Count;
            while (pkcount > 0)
            {
                lvpoids = loadParentOIDByPK(kit, lvpoids);
                pkcount = lvpoids.Count;
                allPoids.AddNotRepeat(lvpoids);
            }
            return allPoids;
        }

        /// <summary>
        /// 树的向上查找工具方法
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="K"></typeparam>
        /// <param name="kit"></param>
        /// <param name="pks"></param>
        /// <param name="loadParentRowByPK"></param>
        /// <param name="pkloader"></param>
        /// <returns></returns>
        public static List<T> findTreeParentRows<T,K>(this SQLBuilder kit, List<K> pks, Func<SQLBuilder, List<K>, List<T>> loadParentRowByPK,Func<T,K> pkloader)
        {
            var lvOIDs = pks;
            var lvRows = new List<T>();
            var allRows = new List<T>();
            var allPKs = new List<K>();
            int pkcount = pks.Count;
            while (pkcount > 0)
            {
                lvRows = loadParentRowByPK(kit, lvOIDs);
                pkcount = lvRows.Count;
                foreach (var row in lvRows) {
                    var key = pkloader(row);
                    if (key != null) {
                        if (!allPKs.Contains(key))
                        {
                            allPKs.Add(key);
                            allRows.Add(row);
                        }
                    }
                }

            }
            return allRows;
        }

        internal static object GetValueCached<T>(this PropertyInfo obj, T row)
        {

            var getter = SimpleTypePropsCache.PropLoaderCache.GetOrSet(obj, (p) =>
            {
                var t = typeof(T);
                return CreateGetDelegate(t, p.Name);
            });

            return getter(obj);
        }

        private static Func<object, object> CreateGetDelegate(Type type, string propertyName)
        {
            var parameter = Expression.Parameter(typeof(object), "obj");
            var instance = Expression.Convert(parameter, type);
            var property = Expression.Property(instance, propertyName);
            var convert = Expression.Convert(property, typeof(object));
            return Expression.Lambda<Func<object, object>>(convert, parameter).Compile();
        }
    }


    internal class SimpleTypePropsCache
    {
        static FrequencyBasedCache<Type, PropertyInfo[]> _cache;

        static FrequencyBasedCache<PropertyInfo, Func<object,object>> _propLoader;

        public static FrequencyBasedCache<Type, PropertyInfo[]> TypePropsCache
        {
            get
            {

                if (_cache == null)
                {
                    _cache = new FrequencyBasedCache<Type, PropertyInfo[]>(TimeSpan.FromMinutes(10));
                }
                return _cache;
            }
        }

        public static FrequencyBasedCache<PropertyInfo, Func<object, object>> PropLoaderCache
        {
            get
            {

                if (_propLoader == null)
                {
                    _propLoader = new FrequencyBasedCache<PropertyInfo, Func<object, object>>(TimeSpan.FromMinutes(10));
                }
                return _propLoader;
            }
        }
    }
}
