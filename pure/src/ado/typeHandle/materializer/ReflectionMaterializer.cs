using System;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace mooSQL.data
{
    internal static class ReflectionMaterializer
    {
        internal static bool TryCreate(
            PackUp packUp,
            Type type,
            int startBound,
            int length,
            bool returnNullIfFirstMissing,
            out Func<DbDataReader, DBInstance, object> materializer)
        {
            materializer = null;
            if (packUp is null || type is null) return false;

            if (type.IsValueType && Nullable.GetUnderlyingType(type) == null)
            {
                // 非 Nullable 值类型：尝试无参构造或 default + 字段赋值
            }
            else if (type.IsInterface || type.IsAbstract)
            {
                return false;
            }

            try
            {
                var ctor = type.GetConstructor(Type.EmptyTypes);
                if (ctor is null && !type.IsValueType)
                    return false;
            }
            catch
            {
                return false;
            }

            materializer = (reader, db) => Deserialize(packUp, type, reader, startBound, length, returnNullIfFirstMissing, db);
            return true;
        }

        private static object Deserialize(
            PackUp packUp,
            Type type,
            DbDataReader reader,
            int startBound,
            int length,
            bool returnNullIfFirstMissing,
            DBInstance db)
        {
            if (length < 0)
                length = reader.FieldCount - startBound;

            if (returnNullIfFirstMissing && length > 0 && reader.IsDBNull(startBound))
                return type.IsValueType && Nullable.GetUnderlyingType(type) == null
                    ? Activator.CreateInstance(type)
                    : null;

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

            var typeMap = packUp.GetTypeMap(type);
            var end = startBound + length;

            for (int i = startBound; i < end; i++)
            {
                var columnName = reader.GetName(i);
                var memberMap = typeMap.GetMember(columnName);
                if (memberMap is null) continue;

                object valueCopy = null;
                try
                {
                    if (reader.IsDBNull(i))
                    {
                        if (!Settings.ApplyNullValues) continue;
                        var memberType = memberMap.MemberType;
                        if (memberType.IsValueType && Nullable.GetUnderlyingType(memberType) == null)
                            continue;
                        SetMember(instance, type, memberMap, null);
                        continue;
                    }

                    valueCopy = reader.GetValue(i);
                    var converted = ColumnValueReader.ReadValue(reader, i, memberMap.MemberType, packUp, db);
                    SetMember(instance, type, memberMap, converted);
                }
                catch (Exception ex)
                {
                    MapperUntils.ThrowDataException(ex, i, reader, valueCopy);
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
    }
}
