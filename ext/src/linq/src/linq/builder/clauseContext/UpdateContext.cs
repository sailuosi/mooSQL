using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using mooSQL.data.model;
	using mooSQL.linq.Common;
	using mooSQL.linq.Expressions;
	using Common;
	using mooSQL.linq.ext;
	using SqlQuery;

	internal sealed class UpdateContext : PassThroughContext
	{
		ITableContext? _targetTable;

		public UpdateContext(IClauseContext querySequence, UpdateBuilder.UpdateTypeEnum updateType, UpdateSentence updateStatement)
			: base(querySequence, querySequence.SelectQuery)
		{
			UpdateStatement = updateStatement;
			UpdateType      = updateType;
		}

		public UpdateSentence         UpdateStatement { get; }
		public UpdateBuilder.UpdateTypeEnum UpdateType      { get; set; }

		public ITableContext? TargetTable
		{
			get => _targetTable;
			set
			{
				_targetTable = value;
				UpdateStatement.Update.Table = _targetTable?.SqlTable;
			}
		}

		public IClauseContext QuerySequence
		{
			get => Context;
			set
			{
				Context     = value;
				SelectQuery = Context.SelectQuery;
			}
		}

		public BuildInfo?                  LastBuildInfo  { get; set; }
		public List<UpdateBuilder.SetExpressionEnvelope> SetExpressions { get; } = new();

		public LambdaExpression? OutputExpression { get; set; }
		public IClauseContext?    DeletedContext   { get; set; }
		public IClauseContext?    InsertedContext  { get; set; }

		public void FinalizeSetters()
		{
			var update = UpdateStatement.Update;

			if (update.Items.Count > 0 || LastBuildInfo == null)
				return;

			if (TargetTable == null)
				throw new SooQueryException("Update query has no defined target table.");

			var tableContext = TargetTable;

			update.Table                = tableContext?.SqlTable;
			UpdateStatement.SelectQuery = QuerySequence.SelectQuery;

			SetExpressions.RemoveDuplicatesFromTail((s1, s2) =>
				ExpressionEqualityComparer.Instance.Equals(s1.FieldExpression, s2.FieldExpression));

			UpdateBuilder.InitializeSetExpressions(Builder, TargetTable, QuerySequence, SetExpressions, update.Items, true);
		}

		static IEnumerable<(Expression path, SqlGenericConstructorExpression generic)> FindForRightProjectionPath(SqlGenericConstructorExpression generic, Expression currentPath, Type objecType)
		{
			if (generic.Type == objecType)
				yield return (currentPath, generic);

			foreach (var assignment in generic.Assignments)
			{
				if (assignment.Expression is SqlGenericConstructorExpression subGeneric)
				{
					var newPath = Expression.MakeMemberAccess(currentPath, assignment.MemberInfo);
					foreach (var sub in FindForRightProjectionPath(subGeneric, newPath, objecType))
						yield return sub;
				}
			}
		}

		static Expression BuildDefaultOutputExpression(ClauseSqlTranslator builder, Type outputType, IClauseContext querySequence, IClauseContext insertedContext, IClauseContext deletedContext)
		{
			var queryRef  = new ContextRefExpression(querySequence.ElementType, querySequence);
			var allFields = builder.ConvertToSqlExpr(querySequence, queryRef);

			if (allFields is not SqlGenericConstructorExpression constructorExpression)
				throw new InvalidOperationException();

			var querySequenceRef = new ContextRefExpression(constructorExpression.Type, querySequence);
			var found = FindForRightProjectionPath(constructorExpression, querySequenceRef, outputType)
				.ToList();

			if (found.Count == 0)
				throw new SooQueryException("Could not find appropriate table in expression");
			if (found.Count > 1)
				throw new SooQueryException("Ambiguous tables in expression");

			var (foundPath, foundGeneric) = found.First();

			var insertedRef = new ContextRefExpression(outputType, insertedContext, "inserted");
			var deletedRef  = new ContextRefExpression(outputType, deletedContext, "deleted");
			var returnType  = typeof(UpdateOutput<>).MakeGenericType(outputType);

			var insertedExpr = builder.RemapToNewPath(foundPath, foundGeneric, insertedRef);
			var deletedExpr  = builder.RemapToNewPath(foundPath, foundGeneric, deletedRef);

			return Expression.MemberInit(
				Expression.New(returnType),
				Expression.Bind(returnType.GetProperty(nameof(UpdateOutput<object>.Deleted))!, deletedExpr),
				Expression.Bind(returnType.GetProperty(nameof(UpdateOutput<object>.Inserted))!, insertedExpr));
		}

		public override Expression BuildProjection(Expression path, ProjectFlags flags)
		{
			if (SequenceHelper.IsSameContext(path, this) && flags.HasFlag(ProjectFlags.Expression))
			{
				FinalizeSetters();

				if (UpdateType == UpdateBuilder.UpdateTypeEnum.UpdateOutput)
				{
					if (DeletedContext == null || InsertedContext == null || LastBuildInfo == null || TargetTable == null)
						throw new InvalidOperationException();

					UpdateStatement.Output ??= new OutputClause();

					var outputSelectQuery = DeletedContext.SelectQuery;

					var insertedContext = InsertedContext;
					var deletedContext  = DeletedContext;

					var outputBody = OutputExpression == null
						? BuildDefaultOutputExpression(Builder, TargetTable.ObjectType, QuerySequence, insertedContext, deletedContext)
						: SequenceHelper.PrepareBody(OutputExpression, QuerySequence,
							deletedContext, insertedContext);

					var selectContext = new SelectContext(Parent, outputBody, insertedContext, false);
					var outputRef     = new ContextRefExpression(path.Type, selectContext);
					var outputExpressions = new List<UpdateBuilder.SetExpressionEnvelope>();

					var sqlExpr = Builder.ConvertToSqlExpr(selectContext, outputRef);
					sqlExpr = SequenceHelper.CorrectSelectQuery(sqlExpr, outputSelectQuery);

					if (sqlExpr is SqlPlaceholderExpression)
						outputExpressions.Add(new UpdateBuilder.SetExpressionEnvelope(sqlExpr, sqlExpr, false));
					else
						UpdateBuilder.ParseSetter(Builder, outputRef, sqlExpr, outputExpressions);

					var setItems = new List<SetWord>();
					UpdateBuilder.InitializeSetExpressions(Builder, selectContext, selectContext, outputExpressions, setItems, false);

					UpdateStatement.Output!.OutputColumns = setItems.Select(c => c.Column as ExpWordBase).ToList();

					return sqlExpr;
				}

				return Expression.Default(path.Type);
			}

			return base.BuildProjection(path, flags);
		}

		public override IClauseContext Clone(CloningContext context)
			=> throw new NotImplementedException();

		public override BaseSentence GetResultStatement() => UpdateStatement;
	}
}
