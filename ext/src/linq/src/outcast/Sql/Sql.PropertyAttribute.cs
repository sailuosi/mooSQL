using System;
using System.Linq.Expressions;

// ReSharper disable CheckNamespace

namespace mooSQL.linq
{
	using Expressions;
	using Linq.Builder;
    using mooSQL.data;
    using mooSQL.data.model;

	using SqlQuery;

	partial class Sql
	{
		/// <summary>
		/// An attribute used to define a static value or
		/// a Database side property/method that takes no parameters.
		/// </summary>
		[Serializable]
		[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
		public class PropertyAttribute : ExpressionAttribute
		{
			/// <summary>
			/// Creates a property to be used in SQL
			/// The name of the Property/Method will be used.
			/// </summary>
			public PropertyAttribute()
				: base(null)
			{
			}

			/// <summary>
			/// Creates a Property to be used in SQL.
			/// </summary>
			/// <param name="name">The name of the property.</param>
			public PropertyAttribute(string name)
				: base(name)
			{
			}

			/// <summary>
			/// Creates a property to be used in SQL
			/// for the given <see cref="ProviderName"/>.
			/// </summary>
			/// <param name="configuration">The <see cref="ProviderName"/>
			/// the property will be used under.</param>
			/// <param name="name">The name of the property.</param>
			public PropertyAttribute(string configuration, string name)
				: base(configuration, name)
			{
			}

			/// <summary>
			/// The name of the Property.
			/// </summary>
			public string? Name
			{
				get => Expression;
				set => Expression = value;
			}

			public override Expression GetExpression<TContext>(
				TContext              context,
				DBInstance          dataContext,
				IExpressionEvaluator  evaluator,
                SelectQueryClause           query,
				Expression            expression,
				ConvertFunc<TContext> converter)
			{
				var name = Name;

				if (name == null)
				{
					if (expression is MethodCallExpression mc)
						name = mc.Method.Name;
					else if (expression is MemberExpression me)
						name = me.Member.Name;
				}

				if (string.IsNullOrEmpty(name))
					throw new LinqToDBException($"Cannot retrieve property name for expression '{expression}'.");

				var sqlExpr = new ExpressionWord(expression.Type, name!, PrecedenceLv.Primary, SqlFlags.IsPure,
                    ToParametersNullabilityType(IsNullable), _canBeNull);

				return ExpressionBuilder.CreatePlaceholder(query, sqlExpr, expression);
			}

			public override string GetObjectID()
			{
				return $"{base.GetObjectID()}.{Name}.";
			}
		}
	}
}
