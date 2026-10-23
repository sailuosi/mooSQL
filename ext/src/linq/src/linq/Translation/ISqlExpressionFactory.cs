using System;
using mooSQL.data;
using mooSQL.data.model;
using mooSQL.linq.utils;
using mooSQL.linq.clause;

namespace mooSQL.linq.translator
{
	// Empty, but should be extended
	/// <summary>
	/// ISqlExpressionFactory type.
	/// </summary>
	/// <summary>
	/// ISqlExpressionFactory interface.
	/// </summary>
	public interface ISqlExpressionFactory
	{
		/// <summary>
		/// Gets or sets DBLive.
		/// </summary>

		DBInstance DBLive {  get; }
		/// <summary>
		/// Gets GetDbDataType.
		/// </summary>
		/// <summary>
		/// Gets GetDbDataType.
		/// </summary>
		DbDataType  GetDbDataType(IExpWord expression);
		/// <summary>
		/// Gets GetDbDataType.
		/// </summary>
		/// <summary>
		/// Gets GetDbDataType.
		/// </summary>
		DbDataType  GetDbDataType(Type           type);
	}
}
