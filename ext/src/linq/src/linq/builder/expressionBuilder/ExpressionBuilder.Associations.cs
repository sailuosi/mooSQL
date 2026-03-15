using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace mooSQL.linq.Linq.Builder
{
	using Common;
	using Extensions;
	using mooSQL.linq.Expressions;
	using Mapping;
	using Reflection;
	using SqlQuery;
	using mooSQL.data.model;
    using mooSQL.utils;
    using mooSQL.data;
    using mooSQL.data.mapping;

    partial class ExpressionBuilder
	{
		bool IsAssociationInRealization(Expression? expression, MemberInfo member, [NotNullWhen(true)] out MemberInfo? associationMember)
		{
			if (InternalExtensions.IsAssociation(member, DBLive))
			{
				associationMember = member;
				return true;
			}

			if (expression?.Type.IsInterface == true)
			{
				if (expression is ContextRefExpression contextRef && contextRef.BuildContext.ElementType != expression.Type)
				{
					var newMember = contextRef.BuildContext.ElementType.GetMemberEx(member);
					if (newMember != null)
					{
						if (InternalExtensions.IsAssociation(newMember, DBLive))
						{
							associationMember = newMember;
							return true;
						}
					}
				}
			}

			associationMember = null;
			return false;
		}

		public bool IsAssociation(Expression expression, [NotNullWhen(true)] out MemberInfo? associationMember)
		{
			switch (expression)
			{
				case MemberExpression memberExpression:
					return IsAssociationInRealization(memberExpression.Expression, memberExpression.Member, out associationMember);

				case MethodCallExpression methodCall:
					return IsAssociationInRealization(methodCall.Object, methodCall.Method, out associationMember);

				default:
					associationMember = null;
					return false;
			}
		}



		Dictionary<SqlCacheKey, Expression>? _associations;



		public static Expression AdjustType(Expression expression, Type desiredType, DBInstance mappingSchema)
		{
			if (desiredType.IsSameOrParentOf(expression.Type))
				return expression;

			if (typeof(IGrouping<,>).IsSameOrParentOf(desiredType))
			{
				if (typeof(IEnumerable<>).IsSameOrParentOf(expression.Type))
					return expression;
			}

			var elementType = TypeHelper.GetEnumerableElementType(desiredType);

			var result = (Expression?)null;

			if (desiredType.IsArray)
			{
				var method = typeof(IQueryable<>).IsSameOrParentOf(expression.Type)
					? Methods.Queryable.ToArray
					: Methods.Enumerable.ToArray;

				result = Expression.Call(method.MakeGenericMethod(elementType),
					expression);
			}
			else if (typeof(IOrderedEnumerable<>).IsSameOrParentOf(desiredType))
			{
				result = expression;
			}
			else if (!typeof(IQueryable<>).IsSameOrParentOf(desiredType) && !desiredType.IsArray)
			{
				var convertExpr = TypeConverterUtil.GetConvertType( mappingSchema,
					typeof(IEnumerable<>).MakeGenericType(elementType), desiredType);
				if (convertExpr != null)
					result = convertExpr.GetBody(expression);
			}

			if (result == null)
			{
				result = expression;
				if (result.Type == typeof(object))
				{
					result = Expression.Convert(result, typeof(IEnumerable<>).MakeGenericType(elementType));
				}

				if (!typeof(IQueryable<>).IsSameOrParentOf(result.Type))
				{
					result = Expression.Call(Methods.Queryable.AsQueryable.MakeGenericMethod(elementType),
						result);
				}

				if (typeof(ITable<>).IsSameOrParentOf(desiredType))
				{
					var tableType = typeof(PersistentTable<>).MakeGenericType(elementType);
					result = Expression.New(tableType.GetConstructor(new[] { result.Type })!,
						result);
				}

				if (result.Type != desiredType)
					result = Expression.Convert(result, desiredType);

			}

			return result;
		}

		public bool GetAssociationTransformation(IBuildContext buildContext, [NotNullWhen(true)] out Expression? transformation)
		{
			transformation = null;
			if (_associations == null)
				return false;

			foreach (var pair in _associations)
			{
				if (pair.Value is ContextRefExpression contextRef && contextRef.BuildContext == buildContext)
				{
					transformation = pair.Key.Expression!;
					return true;
				}
			}

			return false;
		}
	}
}
