using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using mooSQL.data.model;
	using mooSQL.linq.Expressions;
	using SqlQuery;

	internal sealed class DeleteContext : PassThroughContext
	{
		internal enum DeleteTypeEnum
		{
			Delete,
			DeleteOutput,
			DeleteOutputInto,
		}

		public IBuildContext QuerySequence => Context;

		public DeleteTypeEnum     DeleteType       { get; }
		public IBuildContext?     DeletedContext   { get; }
		public LambdaExpression?  OutputExpression { get; }
		public DeleteSentence DeleteStatement  { get; }

		public DeleteContext(IBuildContext querySequence, DeleteTypeEnum deleteType,
			LambdaExpression? outputExpression, DeleteSentence deleteStatement, IBuildContext? deletedContext)
			: base(querySequence, querySequence.SelectQuery)
		{
			DeleteType       = deleteType;
			OutputExpression = outputExpression;
			DeleteStatement  = deleteStatement;
			DeletedContext   = deletedContext;
		}

		public override IBuildContext Clone(CloningContext context)
			=> new DeleteContext(
				context.CloneContext(QuerySequence),
				DeleteType,
				context.CloneExpression(OutputExpression),
				context.CloneElement(DeleteStatement),
				context.CloneContext(DeletedContext));

		public override Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			if (SequenceHelper.IsSameContext(path, this) && flags.HasFlag(ProjectFlags.Expression))
			{
				if (DeleteType == DeleteTypeEnum.DeleteOutput)
				{
					if (DeletedContext == null || OutputExpression == null)
						throw new InvalidOperationException();

					DeleteStatement.Output ??= new OutputClause();

					var outputSelectQuery = new SelectQueryClause();

					var outputBody = SequenceHelper.PrepareBody(OutputExpression, DeletedContext);

					var selectContext     = new SelectContext(Parent, outputBody, QuerySequence, false);
					var outputRef         = new ContextRefExpression(path.Type, selectContext);
					var outputExpressions = new List<UpdateBuilder.SetExpressionEnvelope>();

					var sqlExpr = Builder.BuildSqlExpression(selectContext, outputRef, ProjectFlags.SQL);
					sqlExpr = SequenceHelper.CorrectSelectQuery(sqlExpr, outputSelectQuery);

					if (sqlExpr is SqlPlaceholderExpression)
						outputExpressions.Add(new UpdateBuilder.SetExpressionEnvelope(sqlExpr, sqlExpr, false));
					else
						UpdateBuilder.ParseSetter(Builder, outputRef, sqlExpr, outputExpressions);

					var setItems = new List<SetWord>();
					UpdateBuilder.InitializeSetExpressions(Builder, selectContext, selectContext, outputExpressions, setItems, createColumns: false);

					DeleteStatement.Output!.OutputColumns = setItems.Select(c => c.Column as ExpWordBase).ToList();

					return sqlExpr;
				}

				return Expression.Default(path.Type);
			}

			return base.MakeExpression(path, flags);
		}

		public override BaseSentence GetResultStatement() => DeleteStatement;
	}
}
