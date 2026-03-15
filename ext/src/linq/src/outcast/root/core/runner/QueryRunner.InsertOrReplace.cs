using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace mooSQL.linq.Linq
{
	using Common.Internal.Cache;
	using Mapping;

	using mooSQL.data;
    using mooSQL.data.model;
    using SqlQuery;
	using Tools;

	static partial class QueryRunner
	{


		public static void MakeAlternativeInsertOrUpdate(SentenceBag query)
		{
			var firstStatement  = (InsertOrUpdateSentence)query.Sentences[0].Statement;
			var cloned          = firstStatement.Clone();
			var insertStatement = new InsertSentence(cloned.SelectQuery)
			{
				Insert             = cloned.Insert,
				Tag                = cloned.Tag,
				SqlQueryExtensions = cloned.SqlQueryExtensions
			};

			insertStatement.SelectQuery.From.Tables.Clear();

			query.Sentences.Add(new SentenceItem
			{
				Statement          = insertStatement,
				ParameterAccessors = query.Sentences[0].ParameterAccessors
			});

			var keys = firstStatement.Update.Keys;

			var wsc = firstStatement.SelectQuery.Where.EnsureConjunction();

			foreach (var key in keys)
				wsc.AddEqual(key.Column, key.Expression!, false);

			// TODO! 看起来不可行。当存在更新的列是，
			if (firstStatement.Update.Items.Count > 0)
			{
				query.Sentences[0].Statement = new UpdateSentence(firstStatement.SelectQuery)
				{
					Update             = firstStatement.Update,
					Tag                = firstStatement.Tag,
					SqlQueryExtensions = firstStatement.SqlQueryExtensions
				};
				query.IsFinalized = false; 
				SetNonQueryQuery2(query);
			}
			else
			{
				firstStatement.SelectQuery.Select.Columns.Clear();
				firstStatement.SelectQuery.Select.Columns.Add(new ColumnWord(firstStatement.SelectQuery, new ExpressionWord("1")));
				query.Sentences[0].Statement = new SelectSentence(firstStatement.SelectQuery);
				query.IsFinalized          = false;
				SetQueryQuery2(query);
			}

			query.Sentences.Add(new SentenceItem
			{
				Statement  = new SelectSentence(firstStatement.SelectQuery),
				ParameterAccessors = query.Sentences[0].ParameterAccessors.ToList(),
			});
			query.IsFinalized = false;
		}
	}
}
