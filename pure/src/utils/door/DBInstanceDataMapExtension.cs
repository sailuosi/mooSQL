using System.Collections.Generic;
using System.Data;

namespace mooSQL.data
{
    /// <summary>
    /// 将 DataRow / DataTable 映射为强类型实体的 DBInstance 扩展（语义对齐 SQLBuilder.query&lt;T&gt;）。
    /// </summary>
    public static class DBInstanceDataMapExtension
    {
        /// <summary>
        /// 将 <paramref name="row"/> 映射为实体 <typeparamref name="T"/>。
        /// 有 EntityInfo 列特性时按 DbColumnName/Alias 匹配；否则按属性名精确、忽略大小写、去下划线匹配。
        /// </summary>
        /// <typeparam name="T">目标实体类型</typeparam>
        /// <param name="db">数据库实例（需已设置 client）</param>
        /// <param name="row">数据行；为 null 时返回 default</param>
        public static T toEntity<T>(this DBInstance db, DataRow row)
        {
            return DataRowMaterializer.ToEntity<T>(db, row);
        }

        /// <summary>
        /// 将 <paramref name="table"/> 的各行映射为 <typeparamref name="T"/> 列表。
        /// 匹配规则与 <see cref="toEntity{T}"/> 相同；表为 null 或无行时返回空列表。
        /// </summary>
        /// <typeparam name="T">目标实体类型</typeparam>
        /// <param name="db">数据库实例（需已设置 client）</param>
        /// <param name="table">数据表</param>
        public static List<T> toList<T>(this DBInstance db, DataTable table)
        {
            return DataRowMaterializer.ToList<T>(db, table);
        }
    }
}
