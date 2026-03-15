using System.Linq.Expressions;

namespace mooSQL.linq.Linq
{
	using Common.Internal;
    using mooSQL.data;

    sealed class CteTable<T> : ExpressionQuery<T>
	{
		public CteTable(DBInstance dataContext)
		{
			Init(dataContext, null);
		}

		public CteTable(DBInstance dataContext, Expression expression)
		{
			Init(dataContext, expression);
		}

		public string? TableName { get; set; }

		public string GetTableName()
		{
			using var sb = Pools.StringBuilder.Allocate();

			return DBLive.dialect.clauseTranslator
				.TranslateObjectName( new(TableName!));
		}

		#region Overrides

		public override string ToString()
		{
			return "CteTable(" + typeof(T).Name + ")";
		}

		#endregion
	}
}
