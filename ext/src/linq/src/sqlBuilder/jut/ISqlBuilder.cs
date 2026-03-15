using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace mooSQL.linq.SqlProvider
{
	using Mapping;
	using SqlQuery;
	using Common;
	using mooSQL.data;
	using mooSQL.data.model;


    public interface ISqlBuilder
	{

		/// <summary>
		/// Writes database object name into provided <see cref="StringBuilder"/> instance.
		/// </summary>
		/// <param name="sb">String builder for generated object name.</param>
		/// <param name="name">Name of database object (e.g. table, view, procedure or function).</param>
		/// <param name="objectType">Type of database object, used to select proper name converter.</param>
		/// <param name="escape">If <c>true</c>, apply required escaping to name components. Must be <c>true</c> except rare cases when escaping is not needed.</param>
		/// <param name="tableOptions">Table options if called for table. Used to properly generate names for temporary tables.</param>
		/// <param name="withoutSuffix">If object name have suffix, which could be detached from main name, this parameter disables suffix generation (enables generation of only main name part).</param>
		/// <returns><paramref name="sb"/> parameter value.</returns>
		StringBuilder BuildObjectName               (StringBuilder sb, SqlObjectName name, ConvertType objectType = ConvertType.NameToQueryTable, bool escape = true, TableOptions tableOptions = TableOptions.NotSet, bool withoutSuffix = false);



		void BuildExpression(StringBuilder sb, IExpWord expr, bool buildTableName, object? context = null);


		StringBuilder                          StringBuilder    { get; }
		SQLProviderFlags                       SqlProviderFlags { get; }

		string?                                TablePath        { get; }
		string?                                QueryName        { get; }

		string? BuildSqlID(Sql.SqlID id);
	}
}
