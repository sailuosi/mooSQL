using mooSQL.data.Extensions;
using mooSQL.data.model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.utils
{
    /// <summary>
    /// 判断一个类是否是某个类的扩展
    /// </summary>
    public static class TypeIsExtensions
    {


        //internal static bool IsNumeric(this Type? type)
        //{
        //    if (type == null)
        //        return false;

        //    type = type.UnwrapNullable();

        //    return type.IsInteger()
        //           || type == typeof(decimal)
        //           || type == typeof(float)
        //           || type == typeof(double);
        //}

        internal static bool IsSignedInteger(this Type type)
        {
            return type == typeof(int)
                   || type == typeof(long)
                   || type == typeof(short)
                   || type == typeof(sbyte);
        }

        public static bool IsSignedType(this Type? type)
        {
            return type != null &&
                   (IsSignedInteger(type)
                    || type == typeof(decimal)
                    || type == typeof(double)
                    || type == typeof(float)
                   );
        }

        /// <summary>
        /// 判断给定的类型是否为整数类型。
        /// </summary>
        /// <param name="type">要判断的类型。</param>
        /// <returns>如果类型为整数类型，则返回true；否则返回false。</returns>
        public static bool IsInteger(this Type type)
        {
            type = type.UnwrapNullable();
            if (!type.IsEnum)
            {
                switch (type.GetTypeCode())
                {
                    case TypeCode.Byte:
                    case TypeCode.SByte:
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断指定类型是否为布尔类型。
        /// </summary>
        /// <param name="type">要判断的类型。</param>
        /// <returns>如果类型为布尔类型，则返回 true；否则返回 false。</returns>
        public static bool IsBool(this Type type) => type.UnwrapNullable() == typeof(bool);

        /// <summary>
        /// 判断给定的类型是否为数值类型。
        /// </summary>
        /// <param name="type">要判断的类型。</param>
        /// <returns>如果类型是数值类型，则返回true；否则返回false。</returns>
        public static bool IsNumeric(this Type type)
        {
            type = type.UnwrapNullable();
            if (!type.IsEnum)
            {
                switch (type.GetTypeCode())
                {
                    case TypeCode.Char:
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.Double:
                    case TypeCode.Single:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                        return true;
                }
            }

            return false;
        }


        /*
        public static bool IsInteger(this Type type)
        {
            type = type.UnwrapNullableType();

            return type == typeof(int)
                   || type == typeof(long)
                   || type == typeof(short)
                   || type == typeof(byte)
                   || type == typeof(uint)
                   || type == typeof(ulong)
                   || type == typeof(ushort)
                   || type == typeof(sbyte)
                   || type == typeof(char);
        }*/

        /// <summary>
        /// 判断给定的类型是否为64位整数类型或枚举类型。
        /// </summary>
        /// <param name="type">需要判断的类型。</param>
        /// <returns>如果类型是可空类型，则先将其转换为非空类型；如果类型是非枚举的64位整数类型（Int64或UInt64），则返回true；否则返回false。</returns>
        public static bool IsInteger64(this Type type)
        {
            type = type.UnwrapNullable();
            if (!type.IsEnum)
            {
                switch (type.GetTypeCode())
                {
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断给定类型是否为算术类型
        /// </summary>
        /// <param name="type">要判断的类型</param>
        /// <returns>如果给定类型是算术类型，则返回true；否则返回false</returns>
        public static bool IsArithmetic(this Type type)
        {
            type = type.UnwrapNullable();
            if (!type.IsEnum)
            {
                switch (type.GetTypeCode())
                {
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.Double:
                    case TypeCode.Single:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断给定的类型是否为无符号整数类型。
        /// </summary>
        /// <param name="type">要判断的类型。</param>
        /// <returns>如果类型为无符号整数类型，则返回 true；否则返回 false。</returns>
        public static bool IsUnsignedInt(this Type type)
        {
            type = type.UnwrapNullable();
            if (!type.IsEnum)
            {
                switch (type.GetTypeCode())
                {
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断一个类型是否为整数类型或布尔类型
        /// </summary>
        /// <param name="type">待判断的类型</param>
        /// <returns>如果类型是整数类型或布尔类型，则返回true；否则返回false</returns>
        public static bool IsIntegerOrBool(this Type type)
        {
            type = type.UnwrapNullable();
            if (!type.IsEnum)
            {
                switch (type.GetTypeCode())
                {
                    case TypeCode.Int64:
                    case TypeCode.Int32:
                    case TypeCode.Int16:
                    case TypeCode.UInt64:
                    case TypeCode.UInt32:
                    case TypeCode.UInt16:
                    case TypeCode.Boolean:
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断一个类型是否为浮点类型。
        /// </summary>
        /// <param name="type">要判断的类型。</param>
        /// <returns>如果类型为 float、double 或 decimal，返回 true；否则返回 false。</returns>
        public static bool IsFloatType(this Type type)
        {
            if (type.IsNullable())
                type = type.GetGenericArguments()[0];

            switch (type.GetTypeCodeEx())
            {
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal: return true;
            }

            return false;
        }

        /// <summary>
        /// 判断类型是否为数值类型或布尔类型。
        /// </summary>
        /// <param name="type">要判断的类型。</param>
        /// <returns>如果类型是数值类型或布尔类型，则返回true；否则返回false。</returns>
        public static bool IsNumericOrBool(this Type type) => IsNumeric(type) || IsBool(type);

        static readonly ConcurrentDictionary<string, bool> _isSameOrParentOf = new();


        /// <summary>
        /// 判断一个类型是否是另一个类型的相同类型或其父类型。
        /// </summary>
        /// <param name="parent">要判断是否为父类型的类型。</param>
        /// <param name="child">要判断是否为子类型的类型。</param>
        /// <returns>如果 <paramref name="child"/> 是 <paramref name="parent"/> 的相同类型或其子类型，则返回 true；否则返回 false。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="parent"/> 或 <paramref name="child"/> 为 null。</exception>
        public static bool IsSameOrParentOf(this Type parent, Type child)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (child == null) throw new ArgumentNullException(nameof(child));

            if (parent == child)
                return true;
            var key = parent.FullName + "." + child.FullName;
            return _isSameOrParentOf.GetOrAdd(key, key =>
            {


                if (child.IsEnum && Enum.GetUnderlyingType(child) == parent ||
                    child.IsSubclassOf(parent))
                    return true;

                if (parent.IsGenericTypeDefinition)
                    for (var t = child; t != typeof(object) && t != null; t = t.BaseType)
                        if (t.IsGenericType && t.GetGenericTypeDefinition() == parent)
                            return true;

                if (parent.IsInterface)
                {
                    var interfaces = child.GetInterfaces();

                    foreach (var t in interfaces)
                    {
                        if (parent.IsGenericTypeDefinition)
                        {
                            if (t.IsGenericType && t.GetGenericTypeDefinition() == parent)
                                return true;
                        }
                        else if (t == parent)
                            return true;
                    }
                }

                return false;
            });
        }



        /// <summary>
        /// 判断指定的类型是否为另一个类型的子类或实现了该类型表示的接口。
        /// </summary>
        /// <param name="type">要判断的类型。</param>
        /// <param name="check">要检查的父类或接口。</param>
        /// <returns>如果 <paramref name="type"/> 是 <paramref name="check"/> 的子类或实现了 <paramref name="check"/> 表示的接口，则返回 true；否则返回 false。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> 或 <paramref name="check"/> 为 null。</exception>
        public static bool IsSubClassOf(this Type type, Type check)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (check == null) throw new ArgumentNullException(nameof(check));

            if (type == check)
                return false;

            while (true)
            {
                if (check.IsInterface)
                    // ReSharper disable once LoopCanBeConvertedToQuery
                    foreach (var interfaceType in type.GetInterfaces())
                        if (interfaceType == check || interfaceType.IsSubClassOf(check))
                            return true;

                if (type.IsGenericType && !type.IsGenericTypeDefinition)
                {
                    var definition = type.GetGenericTypeDefinition();
                    if (definition == check || definition.IsSubClassOf(check))
                        return true;
                }

                if (type.BaseType == null)
                    return false;

                type = type.BaseType;

                if (type == check)
                    return true;
            }
        }
    }
}
