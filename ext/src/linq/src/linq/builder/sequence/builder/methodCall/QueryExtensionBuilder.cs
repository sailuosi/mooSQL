using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using Extensions;
    using mooSQL.data;
    using mooSQL.data.model;
    using mooSQL.linq.Expressions;
	using SqlQuery;

	[BuildsExpression(ExpressionType.Call)]
	sealed class QueryExtensionBuilder : MethodCallBuilder
	{
		public static bool CanBuild(Expression expr, BuildInfo info, ExpressionBuilder builder)
			=> Sql.QueryExtensionAttribute.GetExtensionAttributes(expr, builder.DBLive).Length > 0;

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var methodParams = methodCall.Method.GetParameters();
			var list         = new List<SqlQueryExtensionData>
			{
				new(".MethodName", methodCall, methodParams[0])
				{
					SqlExpression = new ValueWord(methodCall.Method.Name),
				}
			};

			var startIndex = methodCall.Object == null ? 1 : 0;

			for (var i = startIndex; i < methodCall.Arguments.Count; i++)
			{
				var arg  = methodCall.Arguments[i].Unwrap();
				var p    = methodParams[i];
				var name = p.Name!;

				if (arg is LambdaExpression)
				{
					list.Add(new(name, arg, p));
				}
				else if (arg is NewArrayExpression ae)
				{
					list.Add(new($"{name}.Count", arg, p)
					{
						SqlExpression = new ValueWord(ae.Expressions.Count),
					});

					for (var j = 0; j < ae.Expressions.Count; j++)
					{
						var ex = ae.Expressions[j];

						list.Add(new($"{name}.{j}", ex, p, j));
					}
				}
				else
				{
					var ex   = methodCall.Arguments[i];

					list.Add(new(name, ex, p));
				}
			}

			var attrs = Sql.QueryExtensionAttribute.GetExtensionAttributes(methodCall, builder.DBLive);

			var prevTablesInScope = builder.TablesInScope;

			if (attrs.Any(a => a.Scope == QueryExtensionScope.TablesInScopeHint))
				builder.TablesInScope = new();

			var sequence = builder.BuildSequence(new(buildInfo, methodCall.Object ?? methodCall.Arguments[0]));

			for (var i = startIndex; i < list.Count; i++)
			{
				var data = list[i];

				if (data.SqlExpression == null)
				{
					if (data.ParamsIndex >= 0)
					{
						var converted = data.Expression.Unwrap() switch
						{
							LambdaExpression lex => builder.ConvertToExtensionSql(sequence, buildInfo.GetFlags(), lex, null, null),
							var ex => builder.ConvertToSqlExpr(sequence, ex)
						};

						if (converted is SqlPlaceholderExpression placeholder)
							data.SqlExpression = placeholder.Sql;
						else
							return BuildSequenceResult.Error(methodCall);
					}
					else if (data.Expression is LambdaExpression le)
					{
						var converted = builder.ConvertToExtensionSql(sequence, buildInfo.GetFlags(), le, null, null);

						if (converted is SqlPlaceholderExpression placeholder)
							data.SqlExpression = placeholder.Sql;
						else
							return BuildSequenceResult.Error(methodCall);
					}
					else
					{
						var converted = builder.ConvertToSqlExpr(sequence, data.Expression);

						if (converted is SqlPlaceholderExpression placeholder)
							data.SqlExpression = placeholder.Sql;
						else
							return BuildSequenceResult.Error(methodCall);
					}
				}
			}

            List<QueryExtension>? joinExtensions = null;

			foreach (var attr in attrs)
			{
				switch (attr.Scope)
				{
					case QueryExtensionScope.TableHint    :
					case QueryExtensionScope.IndexHint    :
					case QueryExtensionScope.TableNameHint:
					{
						var table = SequenceHelper.GetTableOrCteContext(sequence) ?? throw new LinqToDBException($"Cannot get table context from {sequence.GetType()}");
						attr.ExtendTable(table.SqlTable, list);
						break;
					}
					case QueryExtensionScope.TablesInScopeHint:
					{
						foreach (var table in builder.TablesInScope!)
							attr.ExtendTable(table.SqlTable, list);
						break;
					}
					case QueryExtensionScope.JoinHint:
					{
						attr.ExtendJoin(joinExtensions ??= new(), list);
						break;
					}
					case QueryExtensionScope.SubQueryHint:
					{
						if (sequence is SetOperationBuilder.SetOperationContext { SubQuery.SelectQuery : { HasSetOperators: true } q })
							attr.ExtendSubQuery(q.SetOperators[q.SetOperators.Count-1].SelectQuery.SqlQueryExtensions ??= new(), list);
						else
						{
							var queryToUpdate = sequence.SelectQuery;
                                //{ SelectQuery.IsSimple: true }
                            if (sequence is AsSubqueryContext  subquery )
							{
								queryToUpdate = subquery.SubQuery.SelectQuery;
							}

							if (!queryToUpdate.IsSimple())
							{
								sequence      = new SubQueryContext(sequence);
								queryToUpdate = sequence.SelectQuery;
							}

							attr.ExtendSubQuery(queryToUpdate.SqlQueryExtensions ??= new(), list);
						}
						break;
					}
					case QueryExtensionScope.QueryHint:
					{
						attr.ExtendQuery(builder.SqlQueryExtensions ??= new(), list);
						break;
					}
					case QueryExtensionScope.None:
					{
						break;
					}
				}
			}

			builder.TablesInScope = prevTablesInScope;

			return BuildSequenceResult.FromContext(joinExtensions != null ? new JoinHintContext(sequence, joinExtensions) : sequence);
		}

		public sealed class JoinHintContext : PassThroughContext
		{
			public JoinHintContext(IBuildContext context, List<QueryExtension> extensions)
				: base(context)
			{
				Extensions = extensions;
			}

			public List<QueryExtension> Extensions { get; }

			public override IBuildContext Clone(CloningContext context)
			{
				return new JoinHintContext(context.CloneContext(Context),
					Extensions.Select(e => new QueryExtension()
					{
						Configuration = e.Configuration,
						Arguments     = e.Arguments.ToDictionary(a => a.Key, a => context.CloneElement(a.Value as Clause) as IExpWord),
						BuilderType   = e.BuilderType,
						Scope         = e.Scope
					}).ToList());
			}
		}
	}
}
