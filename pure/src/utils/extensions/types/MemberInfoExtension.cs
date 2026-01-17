using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.utils
{
    /// <summary>
    /// MemberInfo的扩展
    /// </summary>
    public static class MemberInfoExtension
    {
        /// <summary>
        /// 获取成员的类型。
        /// </summary>
        /// <param name="memberInfo">成员信息对象。</param>
        /// <returns>返回成员的类型。</returns>
        /// <exception cref="InvalidOperationException">如果成员类型不被支持，则抛出此异常。</exception>
        public static Type GetMemberType(this MemberInfo memberInfo)
        {
            return memberInfo.MemberType switch
            {
                MemberTypes.Property => ((PropertyInfo)memberInfo).PropertyType,
                MemberTypes.Field => ((FieldInfo)memberInfo).FieldType,
                MemberTypes.Method => ((MethodInfo)memberInfo).ReturnType,
                MemberTypes.Constructor => memberInfo.DeclaringType!,
                _ => throw new InvalidOperationException(),
            };
        }

        /// <summary>
        /// 判断一个成员是否为可空类型的Value成员
        /// </summary>
        /// <param name="member">要判断的成员</param>
        /// <returns>如果是可空类型的Value成员，则返回true；否则返回false</returns>
        public static bool IsNullableValueMember(this MemberInfo member)
        {
            return
                member.Name == "Value" &&
                member.DeclaringType!.IsNullable();
        }

        /// <summary>
        /// 判断指定的成员是否是可空类型的HasValue成员。
        /// </summary>
        /// <param name="member">要判断的成员信息。</param>
        /// <returns>如果成员是可空类型的HasValue成员，则返回true；否则返回false。</returns>
        public static bool IsNullableHasValueMember(this MemberInfo member)
        {
            return
                member.Name == "HasValue" &&
                member.DeclaringType!.IsNullable();
        }
        /// <summary>
        /// 判断指定的成员是否是可空类型的GetValueOrDefault方法。
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public static bool IsNullableGetValueOrDefault(this MemberInfo member)
        {
            return
                member.Name == "GetValueOrDefault" &&
                member.DeclaringType!.IsNullable();
        }

        /// <summary>
        /// 判断指定的成员信息是否为属性。
        /// </summary>
        /// <param name="memberInfo">要判断的成员信息。</param>
        /// <returns>如果成员是属性，则返回 true；否则返回 false。</returns>
        public static bool IsPropertyEx(this MemberInfo memberInfo)
        {
            return memberInfo.MemberType == MemberTypes.Property;
        }

        /// <summary>
        /// 判断指定的成员信息是否为字段。
        /// </summary>
        /// <param name="memberInfo">要判断的成员信息。</param>
        /// <returns>如果成员信息是字段，则返回true；否则返回false。</returns>
        public static bool IsFieldEx(this MemberInfo memberInfo)
        {
            return memberInfo.MemberType == MemberTypes.Field;
        }

        /// <summary>
        /// 判断给定的成员信息是否为方法。
        /// </summary>
        /// <param name="memberInfo">要判断的成员信息。</param>
        /// <returns>如果成员是方法，则返回 true；否则返回 false。</returns>
        public static bool IsMethodEx(this MemberInfo memberInfo)
        {
            return memberInfo.MemberType == MemberTypes.Method;
        }
    }
}
