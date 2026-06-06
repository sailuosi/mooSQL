using System;
using System.Linq.Expressions;
using System.Reflection;

namespace mooSQL.linq.Extensions
{
	using Common;

	static class MappingExpressionsExtensions
	{
		public static TExpression GetExpressionFromExpressionMember<TExpression>(this Type type, string memberName)
			where TExpression : Expression
		{
			var members = type.GetStaticMembersEx(memberName);

			if (members.Length == 0)
				throw new SooQueryException($"Static member '{memberName}' for type '{type.Name}' not found");

			if (members.Length > 1)
				throw new SooQueryException($"Ambiguous members '{memberName}' for type '{type.Name}' has been found");

			switch (members[0])
			{
				case PropertyInfo propInfo:
					{
						if (propInfo.GetValue(null, null) is TExpression expression)
							return expression;

						throw new SooQueryException($"Property '{memberName}' for type '{type.Name}' should return expression");
					}
				case MethodInfo method:
					{
						if (method.GetParameters().Length > 0)
							throw new SooQueryException($"Method '{memberName}' for type '{type.Name}' should have no parameters");

						if (method.Invoke(null, new object[] { }) is TExpression expression)
							return expression;

						throw new SooQueryException($"Method '{memberName}' for type '{type.Name}' should return expression");
					}
				default:
					throw new SooQueryException(
						$"Member '{memberName}' for type '{type.Name}' should be static property or method");
			}
		}
	}
}
