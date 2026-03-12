using System;
using System.Collections.Generic;
using System.Linq;

namespace mooSQL.linq.DataProvider.SqlServer
{
	using SqlProvider;
	using SqlQuery;
	using Mapping;
    using mooSQL.data.model;
    using mooSQL.data;

    abstract class SqlServerSqlOptimizer : BasicSqlOptimizer
	{


		protected SqlServerSqlOptimizer(SQLProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{

		}

		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
		{
			return new SqlServerSqlExpressionConvertVisitor(allowModify);
		}

		protected BaseSentence ReplaceSkipWithRowNumber(BaseSentence statement)
			=> ReplaceTakeSkipWithRowNumber((object?)null, statement, static (_, query) => query.Select.SkipValue != null, false);

		protected BaseSentence WrapRootTakeSkipOrderBy(BaseSentence statement)
		{
			var query = statement.SelectQuery;
			if (query == null)
				return statement;

			if (query.Select.SkipValue != null ||
				!query.Select.OrderBy.IsEmpty)
			{
				statement = QueryHelper.WrapQuery(statement, query, true);
			}

			return statement;
		}

		protected override BaseSentence FinalizeUpdate(BaseSentence statement)
		{
			var newStatement = base.FinalizeUpdate(statement);

			if (newStatement is UpdateSentence updateStatement)
			{
				updateStatement = CorrectSqlServerUpdate(updateStatement);
				newStatement    = updateStatement;
			}

			return newStatement;
		}

		static bool IsUpdateUsingSingeTable(UpdateSentence updateStatement)
		{
			return QueryHelper.IsSingleTableInQuery(updateStatement.SelectQuery, updateStatement.Update.Table as TableWord);
		}

		UpdateSentence CorrectSqlServerUpdate(UpdateSentence updateStatement)
		{
			if (updateStatement.Update.Table == null)
				throw new InvalidOperationException();

			var correctionFinished = false;

			TableSourceWord? removedTableSource = null;

			var hasUpdateTableInQuery = QueryHelper.HasTableInQuery(updateStatement.SelectQuery, updateStatement.Update.Table as TableWord);

			if (hasUpdateTableInQuery)
			{
				// do not remove if there is other tables
				if (QueryHelper.EnumerateAccessibleTables(updateStatement.SelectQuery).Take(2).Count() == 1)
				{
					if (RemoveUpdateTableIfPossible(updateStatement.SelectQuery, updateStatement.Update.Table as TableWord, out removedTableSource))
					{
						hasUpdateTableInQuery = false;
					}
				}
			}

			if (hasUpdateTableInQuery)
			{
				// handle simple UPDATE TOP n case
				if (updateStatement.SelectQuery.Select.SkipValue == null && updateStatement.SelectQuery.Select.TakeValue != null)
				{
					if (IsUpdateUsingSingeTable(updateStatement))
					{
						updateStatement.SelectQuery.From.Tables.Clear();
						updateStatement.Update.TableSource = null;
						correctionFinished = true;
					}
				}

				if (!correctionFinished)
				{
					var isCompatibleForUpdate = IsCompatibleForUpdate(updateStatement.SelectQuery, updateStatement.Update.Table as TableWord);
					if (isCompatibleForUpdate)
					{
						// for OUTPUT we have to use datached variant
						if (!IsUpdateUsingSingeTable(updateStatement) && updateStatement.Output != null)
						{
							// check that UpdateTable is visible for SET and OUTPUT
							if (QueryHelper.EnumerateLevelSources(updateStatement.SelectQuery).All(e => e.Source != updateStatement.Update.Table))
							{
								isCompatibleForUpdate = false;
							}
						}
					}

					if (isCompatibleForUpdate)
					{
						var needsWrapping = updateStatement.SelectQuery.Select.SkipValue != null;
						if (needsWrapping)
						{
							updateStatement = QueryHelper.WrapQuery(updateStatement, updateStatement.SelectQuery, true);
						}

						var (ts, path) = FindTableSource(new Stack<Clause>(), updateStatement.SelectQuery,
							updateStatement.Update.Table as TableWord);

						updateStatement.Update.TableSource = ts;
					}
					else
					{
						updateStatement = DetachUpdateTableFromUpdateQuery(updateStatement,  moveToJoin: false, addNewSource: true, out var sqlTableSource);
						updateStatement.Update.TableSource = sqlTableSource;

						OptimizeQueries(updateStatement, updateStatement,   new EvaluateContext());
					}
				}
			}
			else
			{
				if (updateStatement.Update.TableSource == null)
				{
					var ts = updateStatement.Update.Table as TableWord;

                    var tableName      = ts.TableName;
					var hasComplexName = !string.IsNullOrEmpty(tableName.Server) || !string.IsNullOrEmpty(tableName.Schema) || !string.IsNullOrEmpty(tableName.Database);

					if (updateStatement.SelectQuery.From.Tables.Count > 0 || hasComplexName)
					{
						var suggestedSource = new TableSourceWord(updateStatement.Update.Table!,
							QueryHelper.SuggestTableSourceAlias(updateStatement.SelectQuery, "u"));

						updateStatement.SelectQuery.From.Tables.Insert(0, suggestedSource);

						updateStatement.Update.TableSource = suggestedSource;
					}
				}
			}

			CorrectUpdateSetters(updateStatement);

			if (updateStatement.Update.TableSource != null)
			{
				updateStatement.Update.Table = null;
			}

			return updateStatement;
		}
	}
}
