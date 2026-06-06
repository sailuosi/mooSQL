using mooSQL.linq;

namespace mooSQL.data
{
    /// <summary>
    /// Ext LINQ 标准 Queryable 入口扩展（对标 EF / 通用 IQueryable 习惯）。
    /// </summary>
    public static class DBExtLinqExtension
    {
        /// <summary>
        /// 获取实体表的标准 Queryable 源（Ext LINQ 推荐入口，项目 useXXX 约定）。
        /// </summary>
        public static ITable<T> useQueryable<T>(this DBInstance db) where T : notnull
            => ExtLinqEntry.CreateTable<T>(db);

        /// <summary>
        /// <see cref="useQueryable{T}"/> 的别名，命名更贴近通用 Queryable 习惯。
        /// </summary>
        public static ITable<T> AsQueryable<T>(this DBInstance db) where T : notnull
            => db.useQueryable<T>();
    }
}
