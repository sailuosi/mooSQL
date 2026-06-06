using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using mooSQL.data;
	using mooSQL.data.model;
	using mooSQL.linq.Expressions;
	using mooSQL.linq.ext;
	using mooSQL.utils;
	using SqlQuery;

	/// <summary>Static DML set-expression helpers shared by update/insert/merge builders.</summary>
	static class UpdateBuilder
	{
		public static (IBuildContext deleted, IBuildContext inserted, TableWord deletedTable, TableWord insertedTable) CreateDeletedInsertedContexts(ClauseSqlTranslator builder, ITableContext targetTableContext, out IBuildContext outputContext)
		{
			var outputSelectQuery = new SelectQueryClause();

			IBuildContext deletedContext;
			IBuildContext insertedContext;
			if (targetTableContext is CteTableContext cteTable)
			{
				insertedContext = new CteTableContext(builder, null,
					targetTableContext.SqlTable.ObjectType, outputSelectQuery, cteTable.CteContext, false);
				deletedContext = new CteTableContext(builder, null,
					targetTableContext.SqlTable.ObjectType, outputSelectQuery, cteTable.CteContext, false);
			}
			else
			{
				insertedContext = new TableContext(builder, builder.DBLive, outputSelectQuery, targetTableContext.SqlTable, false);
				deletedContext  = new TableContext(builder, builder.DBLive, outputSelectQuery, targetTableContext.SqlTable, false);
			}

			outputContext = deletedContext;

			outputSelectQuery.From.Tables.Clear();

			var deletedTable = ((ITableContext)deletedContext).SqlTable;
			var insertedTable = ((ITableContext)insertedContext).SqlTable;

			if (builder.DBLive.dialect.Option.ProviderFlags.OutputUpdateUseSpecialTables)
			{
				insertedContext = new AnchorContext(null, insertedContext, AnchorWord.AnchorKindEnum.Inserted);
				deletedContext  = new AnchorContext(null, deletedContext, AnchorWord.AnchorKindEnum.Deleted);
			}

			return (deletedContext, insertedContext, deletedTable, insertedTable);
		}

		public enum UpdateTypeEnum
		{
			Update,
			UpdateOutput,
			UpdateOutputInto,
		}

		internal static void InitializeSetExpressions(
			ClauseSqlTranslator           builder,
			IBuildContext               fieldsContext,
			IBuildContext               valuesContext,
			List<SetExpressionEnvelope> envelopes,
			List<SetWord>      items,
			bool                        createColumns
			)
		{
			IExpWord GetFieldExpression(Expression fieldExpr, bool isPureExpression)
				=> builder.ConvertToSql(fieldsContext, fieldExpr, isPureExpression: isPureExpression);

			SetWord  setExpression;
			EntityColumn? columnDescriptor = null;

			foreach (var envelope in envelopes)
			{
				var fieldExpression = envelope.FieldExpression;
				var valueExpression = envelope.ValueExpression;

				if (fieldExpression.IsSqlRow())
				{
					var row = fieldExpression.GetSqlRowValues()
						.Select(e => GetFieldExpression(e, false))
						.ToArray();

					var rowExpression = new RowWord(row);

					setExpression = new SetWord(rowExpression, null);
				}
				else
				{
					var column = GetFieldExpression(fieldExpression, valueExpression == null);
					columnDescriptor = QueryHelper.GetColumnDescriptor(column);
					setExpression    = new SetWord(column, null);
				}

				if (valueExpression != null)
				{
					if (valueExpression.Unwrap() is LambdaExpression lambda)
						valueExpression = lambda.Body;
					else if (fieldExpression.Type != valueExpression.Type)
						valueExpression = Expression.Convert(valueExpression, fieldExpression.Type);

					var sqlExpr = builder.ConvertToSqlExpr(valuesContext, valueExpression, unwrap: false, columnDescriptor: columnDescriptor, forceParameter: envelope.ForceParameter);

					if (sqlExpr is not SqlPlaceholderExpression placeholder)
					{
						if (sqlExpr is SqlErrorExpression errorExpr)
							throw errorExpr.CreateException();

						throw SqlErrorExpression.CreateException(valueExpression, null);
					}

					var sql = createColumns
						? valuesContext.SelectQuery.Select.AddNewColumn(placeholder.Sql)
						: placeholder.Sql;

					setExpression.Expression = sql;
				}

				items.Add(setExpression);
			}
		}

		internal static void ParseSet(
			ClauseSqlTranslator           builder,
			IBuildContext               buildContext,
			Expression                  targetPath,
			Expression                  fieldExpression,
			Expression                  valueExpression,
			List<SetExpressionEnvelope> envelopes,
			bool                        forceParameters)
		{
			var correctedField = builder.ParseGenericConstructor(fieldExpression, ProjectFlags.SQL, null);

			if (correctedField is SqlGenericConstructorExpression fieldGeneric)
			{
				var correctedValue = builder.ParseGenericConstructor(valueExpression, ProjectFlags.SQL, null);

				if (correctedValue is not SqlGenericConstructorExpression valueGeneric)
					throw SqlErrorExpression.CreateException(valueExpression, null);

				var pairs =
					from f in fieldGeneric.Assignments
					join v in valueGeneric.Assignments on f.MemberInfo equals v.MemberInfo
					select (f, v);

				foreach (var (f, v) in pairs)
				{
					var currentPath = Expression.MakeMemberAccess(targetPath, f.MemberInfo);
					ParseSet(builder, buildContext, currentPath, f.Expression, v.Expression, envelopes, false);
				}
			}
			else
			{
				var hasConversion = false;
				var targetColumn  = builder.ConvertToSqlExpr(buildContext, fieldExpression);
				if (targetColumn is SqlPlaceholderExpression placeholder)
					_ = QueryHelper.GetColumnDescriptor(placeholder.Sql);

				if (hasConversion)
				{
					envelopes.Add(new SetExpressionEnvelope(correctedField.UnwrapConvert(), valueExpression, true));
				}
				else
				{
					var correctedValue = builder.ParseGenericConstructor(valueExpression, ProjectFlags.SQL, null);

					if (correctedValue is SqlGenericConstructorExpression valueGeneric)
					{
						foreach (var assignment in valueGeneric.Assignments)
						{
							var currentPath = Expression.MakeMemberAccess(targetPath, assignment.MemberInfo);
							ParseSet(builder, buildContext, currentPath, currentPath, assignment.Expression, envelopes, false);
						}
					}
					else
						envelopes.Add(new SetExpressionEnvelope(correctedField.UnwrapConvert(), valueExpression, forceParameters));
				}
			}
		}

		internal static void ParseSet(
			ContextRefExpression        targetRef,
			Expression                  fieldExpression,
			Expression                  valueExpression,
			List<SetExpressionEnvelope> envelopes,
			bool                        forceParameters)
			=> ParseSet(targetRef.BuildContext.Builder, targetRef.BuildContext, targetRef, fieldExpression, valueExpression, envelopes, forceParameters);

		internal static void ParseSetter(
			ClauseSqlTranslator           builder,
			ContextRefExpression        targetRef,
			Expression                  setterExpression,
			List<SetExpressionEnvelope> envelopes)
		{
			var correctedSetter = builder.ParseGenericConstructor(setterExpression, ProjectFlags.SQL, null);

			if (correctedSetter is not SqlGenericConstructorExpression)
				correctedSetter = builder.ConvertToSqlExpr(targetRef.BuildContext, correctedSetter);

			if (correctedSetter is SqlGenericConstructorExpression generic)
			{
				foreach (var assignment in generic.Assignments)
				{
					var memberAccess = Expression.MakeMemberAccess(targetRef, assignment.MemberInfo);
					ParseSet(builder, targetRef.BuildContext, memberAccess, memberAccess, assignment.Expression, envelopes, false);
				}
			}
			else
			{
				if (correctedSetter is SqlPlaceholderExpression { Sql: ValueWord { Value: null } })
					return;

				if (correctedSetter is ConstantExpression { Value: null })
					return;

				throw new NotImplementedException();
			}
		}

		[DebuggerDisplay("{FieldExpression} = {ValueExpression}")]
		public sealed class SetExpressionEnvelope
		{
			public SetExpressionEnvelope(Expression fieldExpression, Expression? valueExpression, bool forceParameter)
			{
				FieldExpression = fieldExpression;
				ValueExpression = valueExpression;
				ForceParameter  = forceParameter;
			}

			public Expression  FieldExpression { get; }
			public Expression? ValueExpression { get; }
			public bool        ForceParameter  { get; }

			public SetExpressionEnvelope WithValueExpression(Expression? valueExpression)
			{
				if (valueExpression == null || ValueExpression == null)
				{
					if (ReferenceEquals(ValueExpression, valueExpression))
						return this;
				}
				else if (ExpressionEqualityComparer.Instance.Equals(ValueExpression, valueExpression))
					return this;

				return new SetExpressionEnvelope(FieldExpression, valueExpression, ForceParameter);
			}
		}
	}
}
