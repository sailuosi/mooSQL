using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using mooSQL.data.model;
	using mooSQL.linq.Expressions;
	using SqlQuery;

	internal enum MergeKind
	{
		Merge,
		MergeWithOutput,
		MergeWithOutputSource,
		MergeWithOutputInto,
		MergeWithOutputIntoSource
	}

	internal sealed class MergeContext : SequenceContextBase
	{
		public MergeContext(MergeSentence merge, IBuildContext target)
			: base(null, target, null)
		{
			Merge = merge;
		}

		public MergeContext(MergeSentence merge, IBuildContext target, TableLikeQueryContext source)
			: base(null, new[] { target, source }, null)
		{
			Merge        = merge;
			Merge.Source = source.Source;
		}

		public MergeSentence Merge { get; }

		public ITableContext         TargetContext => (ITableContext)Sequence;
		public TableLikeQueryContext SourceContext => (TableLikeQueryContext)Sequences[1];

		public MergeKind    Kind             { get; set; }
		public Expression?  OutputExpression { get; set; }
		public IBuildContext? OutputContext  { get; set; }

		public override BaseSentence GetResultStatement()
		{
			return Merge;
		}

		public override Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			if (SequenceHelper.IsSameContext(path, this) && flags.IsExpression() &&
			    (Kind == MergeKind.MergeWithOutput || Kind == MergeKind.MergeWithOutputSource))
			{
				if (OutputExpression == null || OutputContext == null)
					throw new InvalidOperationException();

				var selectContext = new SelectContext(Parent, OutputExpression, OutputContext, false);
				var outputRef     = new ContextRefExpression(OutputExpression.Type, selectContext);

				var outputExpressions = new List<UpdateBuilder.SetExpressionEnvelope>();

				var sqlExpr = Builder.BuildSqlExpression(selectContext, outputRef, ProjectFlags.SQL);
				if (sqlExpr is SqlPlaceholderExpression)
					outputExpressions.Add(new UpdateBuilder.SetExpressionEnvelope(sqlExpr, sqlExpr, false));
				else
					UpdateBuilder.ParseSetter(Builder, outputRef, sqlExpr, outputExpressions);

				var setItems = new List<SetWord>();
				UpdateBuilder.InitializeSetExpressions(Builder, selectContext, selectContext, outputExpressions, setItems, false);

				Merge.Output!.OutputColumns = setItems.Select(c => c.Expression as ExpWordBase!).ToList();

				return sqlExpr;
			}
			return path;
		}

		public override IBuildContext Clone(CloningContext context)
		{
			throw new NotImplementedException();
		}

		public override IBuildContext? GetContext(Expression expression, BuildInfo buildInfo)
		{
			return null;
		}

		internal static TableWord? GetTargetTable(IBuildContext target)
		{
			var tableContext = SequenceHelper.GetTableOrCteContext(target);
			return tableContext?.SqlTable;
		}

		internal static SelectQueryClause ReplaceSourceInQuery(SelectQueryClause query, TableWord toReplace, TableWord replaceBy)
		{
			var clonedTableSource = query.From.Tables[0];
			var joins= clonedTableSource.GetJoins();

			while (joins.Count > 0)
			{
				var join = joins[0];
				query.From.Tables.Add(join.Table);
				joins.RemoveAt(0);
			}

			query.From.Tables.RemoveAt(0);

			query = query.Convert((toReplace, replaceBy), allowMutation: true, static (visitor, e) =>
			{
				if (e is FieldWord field)
				{
					if (field.Table == visitor.Context.toReplace)
					{
						return visitor.Context.replaceBy.FindFieldByMemberName(field.Name) ?? throw new InvalidOperationException();
					}
				}

				return e;
			});

			return query;
		}
	}
}
