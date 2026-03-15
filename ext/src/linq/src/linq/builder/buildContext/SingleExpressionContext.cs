using System;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using Mapping;
    using mooSQL.data.model;
    using SqlQuery;

	sealed class SingleExpressionContext : BuildContextBase
	{
		public SingleExpressionContext(ExpressionBuilder builder, IExpWord sqlExpression, SelectQueryClause selectQuery)
			: base(builder, sqlExpression.SystemType ?? typeof(object), selectQuery)
		{
			SqlExpression = sqlExpression;
		}

		public IExpWord SqlExpression { get; }



		public override Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			if (SequenceHelper.IsSameContext(path, this))
			{
				return ExpressionBuilder.CreatePlaceholder(this, SqlExpression, path);
			}

			throw new NotImplementedException();
		}

		public override IBuildContext Clone(CloningContext context)
		{
			return new SingleExpressionContext(Builder, context.CloneElement(SqlExpression as Clause) as IExpWord, context.CloneElement(SelectQuery));
		}

		public override void SetRunQuery<T>(SentenceBag<T> query, Expression expr)
		{
			var mapper = Builder.BuildMapper<T>(SelectQuery, expr);

			QueryRunner.SetRunQuery(query, mapper);
		}

		public override BaseSentence GetResultStatement()
		{
			return new SelectSentence(SelectQuery);
		}
	}
}
