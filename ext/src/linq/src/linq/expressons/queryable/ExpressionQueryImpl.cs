using mooSQL.data;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq
{
	sealed class ExpressionQueryImpl<T> : ExpressionQuery<T>
	{
		public ExpressionQueryImpl(DBInstance dataContext, Expression? expression)
		{
			Init(dataContext, expression);
		}

		public override string ToString()
		{
			return SqlText;
		}
	}

	static class ExpressionQueryImpl
	{
		public static IQueryable CreateQuery(Type entityType, DBInstance dataContext, Expression? expression)
		{
			var queryType = typeof(ExpressionQueryImpl<>).MakeGenericType(entityType);
			var query     = (IQueryable)Activator.CreateInstance(queryType, dataContext, expression)!;
			return query;
		}
	}
}
