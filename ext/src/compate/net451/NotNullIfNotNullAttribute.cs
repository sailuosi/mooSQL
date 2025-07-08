using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{
#if NETFRAMEWORK

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
    public sealed class NotNullIfNotNullAttribute : Attribute
    {
        //
        // 摘要:
        //     Initializes the attribute with the associated parameter name.
        //
        // 参数:
        //   parameterName:
        //     The associated parameter name. The output will be non-null if the argument to
        //     the parameter specified is non-null.
        public NotNullIfNotNullAttribute(string parameterName) { }

        //
        // 摘要:
        //     Gets the associated parameter name.
        //
        // 返回结果:
        //     The associated parameter name. The output will be non-null if the argument to
        //     the parameter specified is non-null.
        public string ParameterName { get; }
    }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    internal sealed class NotNullWhenAttribute : Attribute
    {
        //
        // 摘要:
        //     Gets the return value condition.
        public bool ReturnValue { get; }

        //
        // 摘要:
        //     Initializes the attribute with the specified return value condition.
        //
        // 参数:
        //   returnValue:
        //     The return value condition. If the method returns this value, the associated
        //     parameter will not be null.
        public NotNullWhenAttribute(bool returnValue)
        {
            ReturnValue = returnValue;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, Inherited = false)]
    internal sealed class AllowNullAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue, Inherited = false)]
    internal sealed class MaybeNullAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
    internal sealed class MemberNotNullAttribute : Attribute
    {
        public string[] Members { get; }

        public MemberNotNullAttribute(string member)
        {
            Members = new string[1] { member };
        }

        public MemberNotNullAttribute(params string[] members)
        {
            Members = members;
        }
    }
    [AttributeUsage(AttributeTargets.Parameter)]
    internal sealed class MaybeNullWhenAttribute : Attribute
    {
        public bool ReturnValue { get; }

        public MaybeNullWhenAttribute(bool returnValue)
        {
            ReturnValue = returnValue;
        }
    }
#endif
}
