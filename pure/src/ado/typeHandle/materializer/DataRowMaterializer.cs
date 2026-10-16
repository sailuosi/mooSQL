using System;
using System.Collections.Generic;
using System.Data;

namespace mooSQL.data
{
    /// <summary>
    /// 将 <see cref="DataRow"/> / <see cref="DataTable"/> 映射为强类型实体（语义对齐 query&lt;T&gt; 的 DefaultTypeMap）。
    /// </summary>
    internal static class DataRowMaterializer
    {
        internal static T ToEntity<T>(DBInstance db, DataRow row)
        {
            if (db is null) throw new ArgumentNullException(nameof(db));
            if (row is null) return default;

            EnsureClient(db);
            var type = typeof(T);
            var packUp = new PackUp(db.client);
            var typeMap = new DefaultTypeMap(type, db.client, matchUnderscores: true);
            var bindings = BuildBindings(row.Table, typeMap);
            return (T)MaterializeRow(type, row, bindings, packUp, db);
        }

        internal static List<T> ToList<T>(DBInstance db, DataTable table)
        {
            if (db is null) throw new ArgumentNullException(nameof(db));
            var list = new List<T>();
            if (table is null || table.Rows.Count == 0)
                return list;

            EnsureClient(db);
            var type = typeof(T);
            var packUp = new PackUp(db.client);
            var typeMap = new DefaultTypeMap(type, db.client, matchUnderscores: true);
            var bindings = BuildBindings(table, typeMap);

            foreach (DataRow row in table.Rows)
            {
                list.Add((T)MaterializeRow(type, row, bindings, packUp, db));
            }
            return list;
        }

        private static void EnsureClient(DBInstance db)
        {
            if (db.client is null)
                throw new InvalidOperationException("DBInstance.client 未设置，无法解析实体映射信息。");
        }

        private static ColumnBinding[] BuildBindings(DataTable table, ITypeMap typeMap)
        {
            if (table is null)
                return new ColumnBinding[0];

            var columns = table.Columns;
            var bindings = new List<ColumnBinding>(columns.Count);
            for (int i = 0; i < columns.Count; i++)
            {
                var column = columns[i];
                var memberMap = typeMap.GetMember(column.ColumnName);
                if (memberMap is null) continue;
                bindings.Add(new ColumnBinding(i, column.DataType, memberMap));
            }
            return bindings.ToArray();
        }

        private static object MaterializeRow(
            Type type,
            DataRow row,
            ColumnBinding[] bindings,
            PackUp packUp,
            DBInstance db)
        {
            object instance;
            if (type.IsValueType)
            {
                instance = Activator.CreateInstance(type);
            }
            else
            {
                instance = Activator.CreateInstance(type)
                    ?? throw new InvalidOperationException($"无法创建类型 {type.FullName} 的实例。");
            }

            for (int b = 0; b < bindings.Length; b++)
            {
                var binding = bindings[b];
                object valueCopy = null;
                try
                {
                    var raw = row[binding.Ordinal];
                    if (raw is DBNull || raw is null)
                    {
                        if (!Settings.ApplyNullValues) continue;
                        var memberType = binding.Member.MemberType;
                        if (memberType.IsValueType && Nullable.GetUnderlyingType(memberType) == null)
                            continue;
                        SetMember(instance, type, binding.Member, null);
                        continue;
                    }

                    valueCopy = raw;
                    var converted = ColumnValueReader.ConvertValue(
                        raw, binding.ColumnType, binding.Member.MemberType, packUp, db);
                    SetMember(instance, type, binding.Member, converted);
                }
                catch (Exception ex)
                {
                    ThrowDataException(ex, binding.Ordinal, binding.Member.ColumnName, valueCopy);
                }
            }

            return instance;
        }

        private static void SetMember(object target, Type declaringType, IPropertyMap map, object value)
        {
            if (map.Property != null)
            {
                var setter = DefaultTypeMap.GetPropertySetterOrThrow(map.Property, declaringType);
                setter.Invoke(target, new[] { value });
                return;
            }
            if (map.Field != null)
            {
                map.Field.SetValue(target, value);
                return;
            }
            throw new InvalidOperationException($"成员 {map.ColumnName} 不可写。");
        }

        private static void ThrowDataException(Exception ex, int index, string columnName, object value)
        {
            var msg = $"Error parsing column {index} ({columnName})";
            if (value != null)
                msg += $", value={value}";
            throw new DataException(msg, ex);
        }

        private readonly struct ColumnBinding
        {
            public ColumnBinding(int ordinal, Type columnType, IPropertyMap member)
            {
                Ordinal = ordinal;
                ColumnType = columnType;
                Member = member;
            }

            public int Ordinal { get; }
            public Type ColumnType { get; }
            public IPropertyMap Member { get; }
        }
    }
}
