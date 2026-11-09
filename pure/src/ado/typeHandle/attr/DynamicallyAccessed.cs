#if !NET5_0_OR_GREATER
using System;

namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>
    /// 指示动态访问的成员种类（netfx polyfill；trim 分析仅在 net5+ BCL 生效）。
    /// </summary>
    [Flags]
    public enum DynamicallyAccessedMemberTypes
    {
        None = 0,
        PublicParameterlessConstructor = 0x0001,
        PublicConstructors = 0x0003,
        NonPublicConstructors = 0x0004,
        PublicMethods = 0x0008,
        NonPublicMethods = 0x0010,
        PublicFields = 0x0020,
        NonPublicFields = 0x0040,
        PublicNestedTypes = 0x0080,
        NonPublicNestedTypes = 0x0100,
        PublicProperties = 0x0200,
        NonPublicProperties = 0x0400,
        PublicEvents = 0x0800,
        NonPublicEvents = 0x1000,
        Interfaces = 0x2000,
        All = ~None
    }

    /// <summary>
    /// 指示类型或其成员经反射等方式被动态访问（netfx polyfill）。
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method |
        AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Interface |
        AttributeTargets.Parameter | AttributeTargets.ReturnValue,
        Inherited = false)]
    public sealed class DynamicallyAccessedMembersAttribute : Attribute
    {
        public DynamicallyAccessedMembersAttribute(DynamicallyAccessedMemberTypes memberTypes)
            => MemberTypes = memberTypes;

        public DynamicallyAccessedMemberTypes MemberTypes { get; }
    }
}
#endif
