

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
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
                TypeCode typeCode = value is IConvertible convertible
                    ? convertible.GetTypeCode()
                    : Type.GetTypeCode(Enum.GetUnderlyingType(value.GetType()));

                switch (typeCode)
                {
                    case TypeCode.Byte: return (byte)value;
                    case TypeCode.SByte: return (sbyte)value;
                    case TypeCode.Int16: return (short)value;
                    case TypeCode.Int32: return (int)value;
                    case TypeCode.Int64: return (long)value;
                    case TypeCode.UInt16: return (ushort)value;
                    case TypeCode.UInt32: return (uint)value;
                    case TypeCode.UInt64: return (ulong)value;
                }
            }
            return value;
        }


    }
}
