using System;
using System.Text;

namespace mooSQL.linq.SqlQuery
{
    using mooSQL.data.model;
    using SqlProvider;

	/// <summary>
	/// Base interface for all extension builders.
	/// </summary>
	public interface ISqlExtensionBuilder
	{
	}

	/// <summary>
	/// Interface for custom query extension builder.
	/// </summary>
	public interface ISqlQueryExtensionBuilder : ISqlExtensionBuilder
	{
		/// <summary>
		/// Emits query extension SQL.
		/// </summary>
		/// <param name="nullability">Current nullability context.</param>
		/// <param name="sqlBuilder">SQL builder interface.</param>
		/// <param name="stringBuilder">String builder to emit extension SQL to.</param>
		/// <param name="sqlQueryExtension">Extension instance.</param>
		void Build(NullabilityContext nullability, ISqlBuilder sqlBuilder, StringBuilder stringBuilder, QueryExtension sqlQueryExtension);
	}


}
