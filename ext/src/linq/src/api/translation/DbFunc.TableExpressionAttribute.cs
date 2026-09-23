using System;
using System.Linq;
using System.Linq.Expressions;

// ReSharper disable CheckNamespace

namespace mooSQL.linq
{
	using mooSQL.linq.mapping;
    using mooSQL.data;
    using mooSQL.data.model;

	using mooSQL.linq.provider;
	using mooSQL.linq.clause;
	partial class DbFunc
	{
		[Serializable]
		[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
		public class TableExpressionAttribute : TableFunctionAttribute
		{
			public TableExpressionAttribute(string expression)
				: base(expression)
			{
			}

			public TableExpressionAttribute(string expression, params int[] argIndices)
				: base(expression, argIndices)
			{
			}

			public TableExpressionAttribute(string sqlProvider, string expression)
				: base(sqlProvider, expression)
			{
			}

			public TableExpressionAttribute(string sqlProvider, string expression, params int[] argIndices)
				: base(sqlProvider, expression, argIndices)
			{
			}

			// TODO: V5 consider removal of Name+Expression
			protected new string? Name => base.Name;

			public string? Expression
			{
				get => base.Name;
				set => base.Name = value;
			}


		}
	}
}
