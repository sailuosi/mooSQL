using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using mooSQL.data.model;
	using mooSQL.linq.Common;
	using mooSQL.linq.Expressions;
	using mooSQL.linq.ext;
	using Common;
	using mooSQL.utils;
	using SqlQuery;

	internal sealed class InsertContext : PassThroughContext
	{
		public InsertSentence InsertStatement { get; }

		internal enum InsertTypeEnum
		{
			Insert,
			InsertWithIdentity,
			InsertOutput,
			InsertOutputInto
		}

		public InsertContext(IClauseContext querySequence, InsertTypeEnum insertType, InsertSentence insertStatement, LambdaExpression? outputExpression)
			: base(querySequence, querySequence.SelectQuery)
		{
			QuerySequence    = querySequence;
			InsertType       = insertType;
			InsertStatement  = insertStatement;
			OutputExpression = outputExpression;
		}

		public InsertTypeEnum InsertType { get; set; }

		public List<UpdateBuilder.SetExpressionEnvelope> SetExpressions { get; } = new();

		public IClauseContext              QuerySequence    { get; set; }
		public IClauseContext?             Into             { get; set; }
		public BuildInfo?                 LastBuildInfo    { get; set; }
		public LambdaExpression?          OutputExpression { get; set; }
		public TableContext? OutputContext    { get; set; }
		public bool                       RequiresSetters  { get; set; }

		public override Expression BuildProjection(Expression path, ProjectFlags flags)
		{
			if (SequenceHelper.IsSameContext(path, this) && flags.HasFlag(ProjectFlags.Expression))
			{
				FinalizeSetters();

				if (InsertType == InsertTypeEnum.InsertOutput)
				{
					if (OutputExpression == null || OutputContext == null || LastBuildInfo == null)
						throw new InvalidOperationException();

					var selectContext = new SelectContext(Parent, OutputExpression, false, OutputContext);
					var outputRef = new ContextRefExpression(path.Type, selectContext);

					var outputExpressions = new List<UpdateBuilder.SetExpressionEnvelope>();

					var sqlExpr = Builder.ConvertToSqlExpr(selectContext, outputRef);
					if (sqlExpr is SqlPlaceholderExpression)
						outputExpressions.Add(new UpdateBuilder.SetExpressionEnvelope(sqlExpr, sqlExpr, false));
					else
						UpdateBuilder.ParseSetter(Builder, outputRef, sqlExpr, outputExpressions);

					var setItems = new List<SetWord>();
					UpdateBuilder.InitializeSetExpressions(Builder, selectContext, selectContext, outputExpressions, setItems, false);

					InsertStatement.Output!.OutputColumns = setItems.Select(c => c.Expression as ExpWordBase).ToList();

					return sqlExpr;
				}

				return Expression.Default(path.Type);
			}

			return base.BuildProjection(path, flags);
		}

		public void FinalizeSetters()
		{
			var insert = InsertStatement.Insert;

			if (insert.Items.Count > 0 || LastBuildInfo == null)
				return;

			if (Into == null)
				throw new SooQueryException("Insert query has no defined target table.");

			var tableContext = SequenceHelper.GetTableContext(Into);

			insert.Into = tableContext?.SqlTable;

			if (tableContext == null || insert.Into == null)
				throw new SooQueryException("Insert query has no setters defined.");

			SetExpressions.RemoveDuplicatesFromTail((s1, s2) =>
				ExpressionEqualityComparer.Instance.Equals(s1.FieldExpression, s2.FieldExpression));

			UpdateBuilder.InitializeSetExpressions(Builder, tableContext, QuerySequence, SetExpressions, insert.Items, true);

			var q = insert.Into.FindIdentityFields()
				.Except(insert.Items.Select(e => e.Column).OfType<FieldWord>());

			foreach (var field in q)
			{
				var expr = Builder.DBLive.dialect.clauseTranslator.GetIdentityExpression(field);
				if (expr != null)
				{
					var identitySet = new SetWord(field, expr);
					insert.Items.Insert(0, identitySet);

					QuerySequence.SelectQuery.Select.Columns.content.Insert(0, new ColumnWord(QuerySequence.SelectQuery, identitySet.Expression!));
				}
			}
		}

		public override BaseSentence GetResultStatement() => InsertStatement;

		public override IClauseContext Clone(CloningContext context)
			=> new InsertContext(context.CloneContext(QuerySequence), InsertType, context.CloneElement(InsertStatement), context.CloneExpression(OutputExpression));
	}
}
