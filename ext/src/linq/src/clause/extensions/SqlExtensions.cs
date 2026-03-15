using System;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using mooSQL.data;
using mooSQL.data.model;
using mooSQL.linq.Extensions;
using mooSQL.linq.Mapping;

namespace mooSQL.linq.SqlQuery
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



		/// <summary>
		/// 这是内部API，不应由业务侧使用.
		/// It may change or be removed without further notice.
		/// </summary>
		public static InsertClause? GetInsertClause(this BaseSentence statement)
		{
			return statement switch
			{
				InsertSentence insert         => insert.Insert,
				InsertOrUpdateSentence update => update.Insert,
				_                                 => null,
			};
		}



		/// <summary>
		/// 这是内部API，不应由业务侧使用.
		/// It may change or be removed without further notice.
		/// </summary>
		public static UpdateClause? GetUpdateClause(this BaseSentence statement)
		{
			return statement switch
			{
				UpdateSentence update                 => update.Update,
				InsertOrUpdateSentence insertOrUpdate => insertOrUpdate.Update,
				_                                         => null,
			};
		}


		internal static bool IsSqlRow(this Expression expression)
			=> expression.Type.IsSqlRow();

		private static bool IsSqlRow(this Type type)
			=> type.IsGenericType == true && type.GetGenericTypeDefinition() == typeof(Sql.SqlRow<,>);

		internal static ReadOnlyCollection<Expression> GetSqlRowValues(this Expression expr)
		{
			return expr is MethodCallExpression { Method.Name: "Row" } call
				? call.Arguments
				: throw new LinqToDBException("Calls to Sql.Row() are the only valid expressions of type SqlRow.");
		}


        public static ValueWord GetSqlValue(this ObjectWord objectWord, DBInstance mappingSchema, object obj, int index)
        {
            var p = objectWord._infoParameters[index];

            object? value;
			return null;
            //if (p.ColumnDescriptor != null)
            //{
            //    return mappingSchema.GetSqlValueFromObject(p.ColumnDescriptor, obj);
            //}

            //if (p.GetValueFunc != null)
            //{
            //    value = p.GetValueFunc(obj);
            //}
            //else
            //    throw new InvalidOperationException();

            //return mappingSchema.GetSqlValue(p.ValueType, value, null);
        }
    }
}
