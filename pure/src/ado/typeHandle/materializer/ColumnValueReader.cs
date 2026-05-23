using System;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Reflection;
using mooSQL.data.reader;

namespace mooSQL.data
{
    /// <summary>
    /// 从 DbDataReader 读取列值并转换为目标成员类型（供反射物化器与源生成器复用）。
    /// </summary>
    public static class ColumnValueReader
    {
        /// <summary>
        /// 读取指定列并转换为 <paramref name="memberType"/>；DBNull 时返回 null 或 default。
        /// </summary>
        public static object ReadValue(
            DbDataReader reader,
            int index,
            Type memberType,
            PackUp packUp,
            DBInstance db,
            bool applyNullValues = true)
        {
            if (reader is null) throw new ArgumentNullException(nameof(reader));
            if (memberType is null) throw new ArgumentNullException(nameof(memberType));

            var colType = reader.GetFieldType(index);

            if (packUp != null && packUp.UseGetFieldValueFor(memberType))
            {
                if (reader.IsDBNull(index))
                    return NullOrDefault(memberType, applyNullValues);
                return ReadViaGetFieldValue(reader, index, memberType);
            }

            var value = reader.GetValue(index);
            if (value is DBNull || value is null)
                return NullOrDefault(memberType, applyNullValues);

            return ConvertValue(value, colType, memberType, packUp, db);
        }

        internal static object ReadViaGetFieldValue(DbDataReader reader, int index, Type memberType)
        {
            var underlying = Nullable.GetUnderlyingType(memberType) ?? memberType;
            var method = typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetFieldValue), new[] { typeof(int) })
                ?.MakeGenericMethod(underlying);
            if (method is null)
                throw new InvalidOperationException($"GetFieldValue<{underlying.Name}> 不可用。");
            var val = method.Invoke(reader, new object[] { index });
            if (underlying != memberType)
                return Activator.CreateInstance(memberType, val);
            return val;
        }

        internal static object ConvertValue(object value, Type colType, Type memberType, PackUp packUp, DBInstance db)
        {
            if (memberType == typeof(char))
                return MapperUntils.ReadChar(value);
            if (memberType == typeof(char?))
                return MapperUntils.ReadNullableChar(value);

            var nullUnderlying = Nullable.GetUnderlyingType(memberType);
            var unboxType = nullUnderlying?.IsEnum == true ? nullUnderlying : memberType;
            var targetType = nullUnderlying ?? memberType;

            if (unboxType.IsEnum)
            {
                object enumVal;
                if (colType == typeof(string))
                {
                    enumVal = Enum.Parse(unboxType, (string)value, ignoreCase: true);
                }
                else
                {
                    enumVal = FlexibleConvert(value, colType, unboxType, Enum.GetUnderlyingType(unboxType), db);
                }
                if (nullUnderlying != null)
                    return Activator.CreateInstance(memberType, enumVal);
                return enumVal;
            }

            if (memberType.FullName == MapperUntils.LinqBinary)
            {
                var bytes = value as byte[] ?? (byte[])Convert.ChangeType(value, typeof(byte[]), CultureInfo.InvariantCulture);
                return Activator.CreateInstance(memberType, bytes);
            }

            if (packUp != null && packUp.HasTypeHandler(unboxType))
                return packUp.ParseUntyped(unboxType, value);

            if (colType == targetType || Type.GetTypeCode(colType) == Type.GetTypeCode(targetType))
            {
                if (targetType.IsInstanceOfType(value))
                {
                    if (nullUnderlying != null)
                        return Activator.CreateInstance(memberType, value);
                    return value;
                }
            }

            var converted = FlexibleConvert(value, colType, targetType, null, db);
            if (nullUnderlying != null && converted != null)
                return Activator.CreateInstance(memberType, converted);
            return converted;
        }

        private static object FlexibleConvert(object value, Type from, Type to, Type via, DBInstance db)
        {
            if (from == to || from == (via ?? to))
                return Convert.ChangeType(value, to, CultureInfo.InvariantCulture);

            var op = GetOperator(from, to);
            if (op != null)
            {
                var unboxed = Convert.ChangeType(value, from, CultureInfo.InvariantCulture);
                return op.Invoke(null, new[] { unboxed });
            }

            if (from == typeof(string) && to == typeof(Guid))
                return ReadTypeConverter.StringToGuid<string, Guid>((string)value, db);

            if (from == typeof(byte[]) && to == typeof(string))
                return ReadTypeConverter.ByteArrToString<byte[], string>((byte[])value, db);

            if (to == typeof(string) && from != typeof(string))
                return value.ToString();

            return ReadTypeConverter.ChangeType(value, via ?? to, CultureInfo.InvariantCulture);
        }

        private static MethodInfo GetOperator(Type from, Type to)
        {
            if (to is null) return null;
            return ResolveOperator(from.GetMethods(BindingFlags.Static | BindingFlags.Public), from, to, "op_Implicit")
                ?? ResolveOperator(to.GetMethods(BindingFlags.Static | BindingFlags.Public), from, to, "op_Implicit")
                ?? ResolveOperator(from.GetMethods(BindingFlags.Static | BindingFlags.Public), from, to, "op_Explicit")
                ?? ResolveOperator(to.GetMethods(BindingFlags.Static | BindingFlags.Public), from, to, "op_Explicit");
        }

        private static MethodInfo ResolveOperator(MethodInfo[] methods, Type from, Type to, string name)
        {
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name != name || methods[i].ReturnType != to) continue;
                var args = methods[i].GetParameters();
                if (args.Length != 1 || args[0].ParameterType != from) continue;
                return methods[i];
            }
            return null;
        }

        private static object NullOrDefault(Type memberType, bool applyNullValues)
        {
            if (!applyNullValues && !Settings.ApplyNullValues)
            {
                if (memberType.IsValueType && Nullable.GetUnderlyingType(memberType) == null)
                    return Activator.CreateInstance(memberType);
            }
            if (memberType.IsValueType)
            {
                var underlying = Nullable.GetUnderlyingType(memberType);
                if (underlying != null) return null;
                return Activator.CreateInstance(memberType);
            }
            return null;
        }
    }
}
