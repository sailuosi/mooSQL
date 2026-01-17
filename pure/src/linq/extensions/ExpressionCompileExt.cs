using mooSQL.data.Extensions;
using mooSQL.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{
    internal static class ExpressionCompileExt
    {
        static bool IsSimpleEvaluatable(Expression? expr)
        {
            if (expr == null)
                return true;

            switch (expr.NodeType)
            {
                case ExpressionType.Default:
                    return true;

                case ExpressionType.Constant:
                    return true;

                case ExpressionType.MemberAccess:
                    {
                        var member = (MemberExpression)expr;

                        if (member.Member.MemberType != MemberTypes.Field &&
                            member.Member.MemberType != MemberTypes.Property)
                        {
                            return false;
                        }

                        return IsSimpleEvaluatable(member.Expression);
                    }

                case ExpressionType.Call:
                    {
                        var mc = (MethodCallExpression)expr;
                        return IsSimpleEvaluatable(mc.Object) && mc.Arguments.All(IsSimpleEvaluatable);
                    }
            }

            return false;
        }

        static object? EvaluateExpressionInternal(this Expression? expr)
        {
            if (expr == null)
                return null;

            switch (expr.NodeType)
            {
                case ExpressionType.Default:
                    return GetDefaultValue(expr.Type);

                case ExpressionType.Constant:
                    return ((ConstantExpression)expr).Value;

                case ExpressionType.MemberAccess:
                    {
                        var member = (MemberExpression)expr;

                        if (member.Member is FieldInfo fieldInfo)
                            return fieldInfo.GetValue(member.Expression.EvaluateExpressionInternal());

                        if (member.Member is PropertyInfo propertyInfo)
                        {
                            var obj = member.Expression.EvaluateExpressionInternal();
                            if (obj == null)
                            {
                                if (propertyInfo.IsNullableValueMember())
                                    return null;
                                if (propertyInfo.IsNullableHasValueMember())
                                    return false;
                            }
                            return propertyInfo.GetValue(obj, null);
                        }

                        break;
                    }

                case ExpressionType.Call:
                    {
                        var mc = (MethodCallExpression)expr;
                        var arguments = mc.Arguments.Select(a => a.EvaluateExpressionInternal()).ToArray();
                        var instance = mc.Object.EvaluateExpressionInternal();

                        if (instance == null && mc.Method.IsNullableGetValueOrDefault())
                            return null;

                        return mc.Method.Invoke(instance, arguments);
                    }
            }

            throw new InvalidOperationException($"Expression '{expr}' cannot be evaluated");
        }

        public static object? EvaluateExpression(this Expression? expr)
        {
            if (expr == null)
                return null;
            try
            {
                if (IsSimpleEvaluatable(expr))
                {
                    return expr.EvaluateExpressionInternal();
                }

                var value = Expression.Lambda(expr).Compile().DynamicInvoke();
                return value;
            }
            catch (Exception e) { 
                return null;
            }

        }


        public static object? GetDefaultValue(this Type type)
        {
            if (type.IsNullableType())
                return null;

#if NET6_0_OR_GREATER
			return RuntimeHelpers.GetUninitializedObject(type);
#else
            var dtype = typeof(GetDefaultValueHelper<>).MakeGenericType(type);
            var helper = (IGetDefaultValueHelper)Activator.CreateInstance(dtype)!;

            return helper.GetDefaultValue();
#endif
        }
        interface IGetDefaultValueHelper
        {
            object? GetDefaultValue();
        }
        sealed class GetDefaultValueHelper<T> : IGetDefaultValueHelper
        {
            public object? GetDefaultValue()
            {
                return default(T)!;
            }
        }

        public static bool IsNullableType(this Type type)
            => !type.IsValueType || type.IsNullableValueType();

        public static bool IsNullableValueType(this Type type)
            => type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);

        public static bool IsNullableValueMember(this MemberInfo member)
        {
            return
                member.Name == "Value" &&
                member.DeclaringType!.IsNullable();
        }
    }
}
