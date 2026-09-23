using System;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using mooSQL.data;
using mooSQL.data.model;
using mooSQL.linq.utils;
using mooSQL.linq.mapping;
using mooSQL.linq.SqlQuery;

namespace mooSQL.linq.clause
{
	/// <summary>
	/// 这是内部API，不应由业务侧使用.
	/// It may change or be removed without further notice.
	/// </summary>
	public static class SqlExtensions
	{
		/// <summary>
		/// 这是内部API，不应由业务侧使用.
		/// It may change or be removed without further notice.
		/// </summary>
		public static bool IsInsert(this BaseSentence statement)
		{
			return
				statement.QueryType == QueryType.Insert ||
				statement.QueryType == QueryType.InsertOrUpdate ||
				statement.QueryType == QueryType.MultiInsert;
		}



		/// <summary>
		/// 这是内部API，不应由业务侧使用.
		/// It may change or be removed without further notice.
		/// </summary>
		public static bool IsUpdate(this BaseSentence statement)
		{
			return statement != null && statement.QueryType == QueryType.Update;
		}

		/// <summary>
		/// 这是内部API，不应由业务侧使用.
		/// It may change or be removed without further notice.
		/// </summary>
		public static bool IsDelete(this BaseSentence statement)
		{
			return statement != null && statement.QueryType == QueryType.Delete;
		}

		/// <summary>
		/// 这是内部API，不应由业务侧使用.
		/// It may change or be removed without further notice.
		/// </summary>
		public static bool HasSomeModifiers(this SelectClause select, bool ignoreSkip, bool ignoreTake)
		{
			return select.IsDistinct || (!ignoreSkip && select.SkipValue != null) || (!ignoreTake && select.TakeValue != null);
		}


		internal static bool IsSqlRow(this Expression expression)
			=> expression.Type.IsSqlRow();

		private static bool IsSqlRow(this Type type)
			=> type.IsGenericType == true && type.GetGenericTypeDefinition() == typeof(SooFunctionExtension.SqlRow<,>);

		internal static ReadOnlyCollection<Expression> GetSqlRowValues(this Expression expr)
		{
			return expr is MethodCallExpression { Method.Name: "Row" } call
				? call.Arguments
				: throw new SooQueryException("Calls to DbFunc.Row() are the only valid expressions of type SqlRow.");
		}


    }
}
