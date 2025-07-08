

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
    internal static partial  class ParameterHandler
    {

        //private static Deserializer deserializer = new Deserializer();

        /// <summary>
        /// OBSOLETE: For internal usage only. Sanitizes the parameter value with proper type casting.
        /// </summary>
        /// <param name="value">The value to sanitize.</param>

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
