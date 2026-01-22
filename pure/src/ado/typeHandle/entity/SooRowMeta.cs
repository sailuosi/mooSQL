using System;
using System.Collections.Generic;
using System.Reflection;
namespace mooSQL.data
{

    internal sealed partial class SooRow : System.Dynamic.IDynamicMetaObjectProvider
    {
        System.Dynamic.DynamicMetaObject System.Dynamic.IDynamicMetaObjectProvider.GetMetaObject(
System.Linq.Expressions.Expression parameter)
        {
            return new SooRowMeta(parameter, System.Dynamic.BindingRestrictions.Empty, this);
        }
    }

    /// <summary>
    /// SooRow 的动态元对象，用于支持动态属性访问
    /// </summary>
    internal sealed class SooRowMeta : System.Dynamic.DynamicMetaObject
    {
        private static readonly MethodInfo getValueMethod = typeof(IDictionary<string, object>).GetProperty("Item").GetGetMethod();
        private static readonly MethodInfo setValueMethod = typeof(SooRow).GetMethod("SetValue", new Type[] { typeof(string), typeof(object) });

        public SooRowMeta(
            System.Linq.Expressions.Expression expression,
            System.Dynamic.BindingRestrictions restrictions
            )
            : base(expression, restrictions)
        {
        }

        public SooRowMeta(
            System.Linq.Expressions.Expression expression,
            System.Dynamic.BindingRestrictions restrictions,
            object value
            )
            : base(expression, restrictions, value)
        {
        }

        private System.Dynamic.DynamicMetaObject CallMethod(
            MethodInfo method,
            System.Linq.Expressions.Expression[] parameters
            )
        {
            var callMethod = new System.Dynamic.DynamicMetaObject(
                System.Linq.Expressions.Expression.Call(
                    System.Linq.Expressions.Expression.Convert(Expression, LimitType),
                    method,
                    parameters),
                System.Dynamic.BindingRestrictions.GetTypeRestriction(Expression, LimitType)
                );
            return callMethod;
        }

        public override System.Dynamic.DynamicMetaObject BindGetMember(System.Dynamic.GetMemberBinder binder)
        {
            var parameters = new System.Linq.Expressions.Expression[]
                                    {
                                        System.Linq.Expressions.Expression.Constant(binder.Name)
                                    };

            var callMethod = CallMethod(getValueMethod, parameters);

            return callMethod;
        }

        // Needed for Visual basic dynamic support
        public override System.Dynamic.DynamicMetaObject BindInvokeMember(System.Dynamic.InvokeMemberBinder binder, System.Dynamic.DynamicMetaObject[] args)
        {
            var parameters = new System.Linq.Expressions.Expression[]
                                    {
                                        System.Linq.Expressions.Expression.Constant(binder.Name)
                                    };

            var callMethod = CallMethod(getValueMethod, parameters);

            return callMethod;
        }

        public override System.Dynamic.DynamicMetaObject BindSetMember(System.Dynamic.SetMemberBinder binder, System.Dynamic.DynamicMetaObject value)
        {
            var parameters = new System.Linq.Expressions.Expression[]
                                    {
                                        System.Linq.Expressions.Expression.Constant(binder.Name),
                                        value.Expression,
                                    };

            var callMethod = CallMethod(setValueMethod, parameters);

            return callMethod;
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            if(HasValue && Value is IDictionary<string, object> lookup) return lookup.Keys;
            return new List<string>();
        }
    }
    
}
