using System;
using System.Globalization;
using mooSQL.data.model;
using mooSQL.utils;

namespace mooSQL.data
{
    /// <summary>
    /// 枚举列默认映射（对齐 EF Core：无自定义类型时按底层整型）。
    /// 供自动建表推断与 <see cref="SQLExpression.ResolveColumnValue"/> 算法零件共用。
    /// </summary>
    public static class EnumColumnMapping
    {
        /// <summary>
        /// 将 CLR 类型推断为 <see cref="DataFam"/>：枚举取其底层类型，非枚举返回 <see cref="DataFam.Undefined"/>。
        /// </summary>
        public static DataFam TryMapClrTypeToDataFam(Type clrType)
        {
            if (clrType == null) return DataFam.Undefined;
            var t = clrType.UnwrapNullable();
            if (!t.IsEnum) return DataFam.Undefined;
            return MapUnderlyingTypeToDataFam(Enum.GetUnderlyingType(t));
        }

        /// <summary>
        /// 底层数值类型 → DataFam（与 DBTableCreator / DDL 命名对齐：long → Long）。
        /// </summary>
        public static DataFam MapUnderlyingTypeToDataFam(Type underlying)
        {
            if (underlying == null) return DataFam.Undefined;
            if (underlying == typeof(byte)) return DataFam.Byte;
            if (underlying == typeof(sbyte)) return DataFam.SByte;
            if (underlying == typeof(short)) return DataFam.Int16;
            if (underlying == typeof(ushort)) return DataFam.UInt16;
            if (underlying == typeof(int)) return DataFam.Int32;
            if (underlying == typeof(uint)) return DataFam.UInt32;
            if (underlying == typeof(long)) return DataFam.Long;
            if (underlying == typeof(ulong)) return DataFam.UInt64;
            return DataFam.Undefined;
        }

        /// <summary>
        /// 列是否视为「已自定义」数据类型（非 Undefined）。
        /// </summary>
        public static bool HasCustomDataType(EntityColumn col)
        {
            if (col == null) return false;
            if (col.DataType != DataFam.Undefined) return true;
            if (col.DbType != DbDataType.Undefined && col.DbType.DataType != DataFam.Undefined)
                return true;
            return false;
        }

        /// <summary>
        /// 有效列类型：优先 DataType，其次 DbType.DataType。
        /// </summary>
        public static DataFam GetEffectiveDataType(EntityColumn col)
        {
            if (col == null) return DataFam.Undefined;
            if (col.DataType != DataFam.Undefined) return col.DataType;
            if (col.DbType != DbDataType.Undefined && col.DbType.DataType != DataFam.Undefined)
                return col.DbType.DataType;
            return DataFam.Undefined;
        }

        /// <summary>
        /// 是否为字符类列类型（枚举应按名称字符串绑定）。
        /// </summary>
        public static bool IsCharacterDataType(DataFam fam)
        {
            return fam == DataFam.Char
                || fam == DataFam.VarChar
                || fam == DataFam.NChar
                || fam == DataFam.NVarChar
                || fam == DataFam.Text
                || fam == DataFam.NText
                || fam == DataFam.LongText;
        }

        /// <summary>
        /// 按 <see cref="EnumParaConvert"/> 转换（供绑定层与测试）；委托 <see cref="ParameterHandler"/>。
        /// </summary>
        public static object ApplyEnumParameterConvert(object value, EnumParaConvert mode)
            => ParameterHandler.ApplyEnumParameterConvert(value, mode);

        /// <summary>
        /// 枚举 → 底层数值（与 ParameterHandler.SanitizeParameterValue 一致）。
        /// </summary>
        public static object ToUnderlyingNumeric(object enumValue)
        {
            if (enumValue is null) return null;
            if (enumValue is not Enum) return enumValue;

            TypeCode typeCode = enumValue is IConvertible convertible
                ? convertible.GetTypeCode()
                : Type.GetTypeCode(Enum.GetUnderlyingType(enumValue.GetType()));

            switch (typeCode)
            {
                case TypeCode.Byte: return (byte)enumValue;
                case TypeCode.SByte: return (sbyte)enumValue;
                case TypeCode.Int16: return (short)enumValue;
                case TypeCode.Int32: return (int)enumValue;
                case TypeCode.Int64: return (long)enumValue;
                case TypeCode.UInt16: return (ushort)enumValue;
                case TypeCode.UInt32: return (uint)enumValue;
                case TypeCode.UInt64: return (ulong)enumValue;
                default:
                    return Convert.ChangeType(enumValue, Enum.GetUnderlyingType(enumValue.GetType()), CultureInfo.InvariantCulture);
            }
        }
    }
}
