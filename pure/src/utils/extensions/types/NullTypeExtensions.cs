using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.utils
{
    /// <summary>
    /// 可空类判断工具
    /// </summary>
    public static class NullTypeExtensions
    {


        /// <summary>
        /// 是否引用类型，包含非值类型和可空类型
        /// </summary>
        /// <param name="type">Type to test.</param>
        /// <returns><c>true</c> if type is reference type or <see cref="Nullable{T}"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsReferType(this Type type)
            => !type.IsValueType || type.IsNullable();
        /// <summary>
        /// 如果是 <see cref="Nullable{T}"/> 类则返回true.
        /// </summary>
        /// <param name="type">A <see cref="Type"/> instance. </param>
        /// <returns><c>true</c>, if <paramref name="type"/> represents <see cref="Nullable{T}"/> type; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullable(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static Type UnwrapNullable(this Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            //return type.IsNullable() ? type.GetGenericArguments()[0] : type;
            return Nullable.GetUnderlyingType(type) ?? type;
        }

        /// <summary>
        ///包装类为可空类<see cref="Nullable{T}"/> class.
        /// </summary>
        /// <param name="type">Value type to wrap. Must be value type (except <see cref="Nullable{T}"/> itself).</param>
        /// <returns>Type, wrapped by <see cref="Nullable{T}"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type WrapNullable(this Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (!type.IsValueType) return type;
            if (type.IsNullable()) return type;

            return typeof(Nullable<>).MakeGenericType(type);
        }

        //public static Type GetNonNullableType(this Type type) => IsNullableType(type) ? type.GetGenericArguments()[0] : type;

        //public static bool IsNullableOrReferenceType(this Type type) => !type.IsValueType || type.IsNullable();
    }
}
