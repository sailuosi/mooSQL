using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mooSQL.utils;

namespace mooSQL.data
{
    public static class SQLBuilderExtension
    {
        /* 废弃转换器，改由核心引擎下的泛型方法处理。
        /// <summary>
        /// 查询数据并转换为指定的实体类。查询结果为null时返回空列表。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static List<T> query<T>(this SQLBuilder kit)
        {
            var dt = kit.query();
            if (dt == null)
            {
                return new List<T>();
            }
            return dt.ToList<T>();
        }

        public static T queryRow<T>(this SQLBuilder kit)
        {
            DataTable dt = kit.query();
            if (dt.Rows.Count == 1)
            {
                return dt.Rows[0].ToEntity<T>();
            }
            return default(T);
        }*/
    }
}
