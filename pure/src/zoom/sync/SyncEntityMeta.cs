///////////////////////////////////////////////////////////
//  SyncEntityMeta.cs
///////////////////////////////////////////////////////////

using System;
using System.Reflection;

namespace mooSQL.data.sync
{
    /// <summary>实体表名 / 主键 / 时序列解析。</summary>
    internal static class SyncEntityMeta
    {
        public static string ResolveTableName(Type entityType)
        {
            var table = entityType.GetCustomAttribute<SooTableAttribute>(inherit: true);
            if (table != null && !string.IsNullOrWhiteSpace(table.Name))
                return table.Name;
            return entityType.Name;
        }

        public static (PropertyInfo Prop, string DbColumn) ResolveOrderBy(Type entityType, string orderByColumn)
        {
            if (string.IsNullOrWhiteSpace(orderByColumn))
                throw new ArgumentException("orderBy 列名不能为空", nameof(orderByColumn));

            var hit = FindColumn(entityType, orderByColumn);
            if (hit.Prop != null)
                return (hit.Prop, hit.DbColumn!);

            throw new InvalidOperationException(
                "实体 " + entityType.Name + " 未找到 orderBy 列/属性: " + orderByColumn);
        }

        /// <summary>
        /// 解析主键。优先显式 <paramref name="keyColumn"/>，否则取实体 IsPrimaryKey。
        /// </summary>
        public static (PropertyInfo Prop, string DbColumn) ResolvePrimaryKey(Type entityType, string? keyColumn = null)
        {
            if (!string.IsNullOrWhiteSpace(keyColumn))
            {
                var hit = FindColumn(entityType, keyColumn);
                if (hit.Prop != null)
                    return (hit.Prop, hit.DbColumn!);
                throw new InvalidOperationException(
                    "实体 " + entityType.Name + " 未找到 key 列/属性: " + keyColumn);
            }

            foreach (var prop in entityType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var col = prop.GetCustomAttribute<SooColumnAttribute>(inherit: true);
                if (col == null || !col.HasIsPrimaryKey() || !col.IsPrimaryKey)
                    continue;

                var dbName = string.IsNullOrWhiteSpace(col.Name) ? prop.Name : col.Name;
                return (prop, dbName);
            }

            throw new InvalidOperationException("实体 " + entityType.Name + " 未定义主键列（可 .key(...) 显式指定）");
        }

        static (PropertyInfo? Prop, string? DbColumn) FindColumn(Type entityType, string name)
        {
            foreach (var prop in entityType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var col = prop.GetCustomAttribute<SooColumnAttribute>(inherit: true);
                var dbName = col == null || string.IsNullOrWhiteSpace(col.Name) ? prop.Name : col.Name;
                if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(dbName, name, StringComparison.OrdinalIgnoreCase))
                {
                    return (prop, dbName);
                }
            }
            return (null, null);
        }

        public static void EnsureSafeIdent(string ident)
        {
            if (string.IsNullOrWhiteSpace(ident))
                throw new ArgumentException("标识符为空");

            for (var i = 0; i < ident.Length; i++)
            {
                var c = ident[i];
                if (!(char.IsLetterOrDigit(c) || c == '_' || c == '.'))
                    throw new ArgumentException("非法标识符: " + ident);
            }
        }
    }
}
