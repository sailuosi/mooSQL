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
		/// <summary>
		/// TableExpressionAttribute
		/// </summary>
		[Serializable]
		[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
		public class TableExpressionAttribute : TableFunctionAttribute
		{
			/// <summary>
			/// Initializes a new instance of the <see cref="DbFunc.TableExpressionAttribute"/> class.
			/// </summary>
			/// <summary>
			/// Initializes a new instance of the <see cref="DbFunc.TableExpressionAttribute"/> class.
			/// </summary>
			public TableExpressionAttribute(string expression)
				: base(expression)
			{
			}
			/// <summary>
			/// Initializes a new instance of the <see cref="DbFunc.TableExpressionAttribute"/> class.
			/// </summary>

			public TableExpressionAttribute(string expression, params int[] argIndices)
				: base(expression, argIndices)
			{
			}
			/// <summary>
			/// Initializes a new instance of the <see cref="DbFunc.TableExpressionAttribute"/> class.
			/// </summary>

			public TableExpressionAttribute(string sqlProvider, string expression)
				: base(sqlProvider, expression)
			{
			}
			/// <summary>
			/// Initializes a new instance of the <see cref="DbFunc.TableExpressionAttribute"/> class.
			/// </summary>

			public TableExpressionAttribute(string sqlProvider, string expression, params int[] argIndices)
				: base(sqlProvider, expression, argIndices)
			{
			}

			// TODO: V5 consider removal of Name+Expression
			/// <summary>
			/// Name
			/// </summary>
			/// <summary>
			/// Name
			/// </summary>
			protected new string? Name => base.Name;
			/// <summary>
			/// Expression
			/// </summary>

			public string? Expression
			{
				get => base.Name;
				set => base.Name = value;
			}


		}
	}
}
