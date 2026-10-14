

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 参数处理器，提供参数处理和转换的静态方法
    /// </summary>
    internal static partial  class ParameterHandler
    {

        //private static Deserializer deserializer = new Deserializer();

        /// <summary>
        /// 已过时：仅供内部使用。使用适当的类型转换清理参数值。
        /// </summary>
        /// <param name="value">要清理的值。</param>

        internal static object SanitizeParameterValue(object value)
        {
            if (value is null) return DBNull.Value;
            if (value is Enum)
            {
                return ToUnderlyingNumeric(value);
            }
            return value;
        }

        /// <summary>
        /// 按 <see cref="EnumParameterConvert"/> 转换枚举，供 ADO 绑定使用。非枚举或 <see cref="EnumParameterConvert.None"/> 原样返回。
        /// </summary>
        internal static object ApplyEnumParameterConvert(object value, EnumParameterConvert mode)
        {
            if (value is null || mode == EnumParameterConvert.None)
                return value;
            if (value is not Enum)
                return value;

            switch (mode)
            {
                case EnumParameterConvert.ToInt:
                    return ToUnderlyingNumeric(value);
                case EnumParameterConvert.ToName:
                    return value.ToString();
                case EnumParameterConvert.ToIntString:
                    var num = ToUnderlyingNumeric(value);
                    return Convert.ToString(num, CultureInfo.InvariantCulture);
                default:
                    return value;
            }
        }

        static object ToUnderlyingNumeric(object enumValue)
        {
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
