using System;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using Mapping;
    using mooSQL.data.model;
    using SqlQuery;

	sealed class SingleExpressionContext : ClauseContextBase
	{
		public SingleExpressionContext(ClauseSqlTranslator builder, IExpWord sqlExpression, SelectQueryClause selectQuery)
			: base(builder, sqlExpression.SystemType ?? typeof(object), selectQuery)
		{
			SqlExpression = sqlExpression;
		}

		public IExpWord SqlExpression { get; }



		public override Expression BuildProjection(Expression path, ProjectFlags flags)
		{
			if (SequenceHelper.IsSameContext(path, this))
			{
				return ClauseSqlTranslator.CreatePlaceholder(this, SqlExpression, path);
			}

			throw new NotImplementedException();
		}

		public override IClauseContext Clone(CloningContext context)
		{
			return new SingleExpressionContext(Builder, context.CloneElement(SqlExpression as Clause) as IExpWord, context.CloneElement(SelectQuery));
		}


		public override BaseSentence GetResultStatement()
		{
			return new SelectSentence(SelectQuery);
		}
	}
}
