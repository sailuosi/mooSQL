namespace mooSQL.linq.DataProvider.MySql
{
	using Mapping;
    using mooSQL.data;
    using mooSQL.data.model;

	using SqlProvider;
	using SqlQuery;

	sealed class MySqlSqlOptimizer : BasicSqlOptimizer
	{
		public MySqlSqlOptimizer(SQLProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
		{
			return new MySqlSqlExpressionConvertVisitor(allowModify);
		}

		public override BaseSentence TransformStatement(BaseSentence statement)
		{
			return statement.QueryType switch
			{
                QueryType.Update => CorrectMySqlUpdate((UpdateSentence)statement),
                QueryType.Delete => PrepareDelete((DeleteSentence)statement),
				_                => statement,
			};
		}

        BaseSentence PrepareDelete(DeleteSentence statement)
		{
			var tables = statement.SelectQuery.From.Tables;

			if (tables.Count == 1 && tables[0].GetJoins().Count == 0
				&& !statement.SelectQuery.Select.HasSomeModifiers(SqlProviderFlags.IsUpdateSkipTakeSupported, SqlProviderFlags.IsUpdateTakeSupported))
				tables[0].setAlias( "$");

			return statement;
		}

		private UpdateSentence CorrectMySqlUpdate(UpdateSentence statement)
		{
			if (statement.SelectQuery.Select.SkipValue != null)
				throw new LinqToDBException("MySql does not support Skip in update query");

			statement = CorrectUpdateTable(statement, leaveUpdateTableInQuery: true);

			// Mysql do not allow Update table usage in FROM clause. Moving it to subquery
			// https://stackoverflow.com/a/14302701/10646316
			// See UpdateIssue319Regression test
			var changed = false;
			statement.SelectQuery.VisitParentFirst(e =>
			{
				// Skip root query FROM clause
				if (ReferenceEquals(e, statement.SelectQuery.From))
				{
					return false;
				}

				if (e is TableSourceWord ts)
				{
					if (ts.Source is TableWord table 
						&& !ReferenceEquals(table, statement.Update.Table) 
						&& QueryHelper.IsEqualTables(table, statement.Update.Table as TableWord))
					{
						var subQuery = new SelectQueryClause
						{
							DoNotRemove = true,
						};
						subQuery.From.Tables.Add(new TableSourceWord(table, ts.Alias));
						ts.Source = subQuery;
						changed = true;

						return false;
					}
				}

				return true;
			});

			//if (!statement.SelectQuery.OrderBy.IsEmpty)
			//	statement.SelectQuery.OrderBy.Items.Clear();

			CorrectUpdateSetters(statement);

			if (changed)
			{
				var corrector = new SqlQueryColumnNestingCorrector();
				corrector.CorrectColumnNesting(statement);
			}

			return statement;
		}
	}
}
