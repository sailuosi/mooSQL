using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace mooSQL.linq.builder
{
    using mooSQL.data.model;
    using mooSQL.linq.clause;
	/// <summary>
	/// SqlQueryExtensionData type.
	/// </summary>
	[DebuggerDisplay("{ToDebugString()}")]
	public sealed class SqlQueryExtensionData
	{
		/// <summary>
		/// SqlQueryExtensionData type.
		/// </summary>
		/// <summary>
		/// SqlQueryExtensionData type.
		/// </summary>
		public SqlQueryExtensionData(string name, Expression expr, ParameterInfo parameter, int paramsIndex = -1)
		{
			Name        = name;
			Expression  = expr;
			Parameter   = parameter;
			ParamsIndex = paramsIndex;
		}
		/// <summary>
		/// Gets or sets Name.
		/// </summary>

		public string          Name          { get; }
		/// <summary>
		/// Expression
		/// </summary>
		/// <summary>
		/// Gets or sets Expression.
		/// </summary>
		public Expression      Expression    { get; }
		/// <summary>
		/// Parameter
		/// </summary>
		/// <summary>
		/// Gets or sets Parameter.
		/// </summary>
		public ParameterInfo   Parameter     { get; }
		/// <summary>
		/// ParamsIndex
		/// </summary>
		/// <summary>
		/// Gets or sets ParamsIndex.
		/// </summary>
		public int             ParamsIndex   { get; }
		/// <summary>
		/// SqlExpression
		/// </summary>
		/// <summary>
		/// Gets or sets SqlExpression.
		/// </summary>
		public IExpWord? SqlExpression { get; set; }
		/// <summary>
		/// ToDebugString
		/// </summary>

		public string ToDebugString()
		{
			return $"{Name} = {SqlExpression?.ToDebugString() ?? "Expr:" + Expression.ToString()}";
		}
	}
}
