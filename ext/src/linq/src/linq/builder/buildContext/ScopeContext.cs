using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
    using mooSQL.data.model;
    using mooSQL.linq.Expressions;
	using mooSQL.linq.Mapping;
	using mooSQL.linq.SqlQuery;

	class ScopeContext : BuildContextBase
	{
		public IBuildContext Context    { get; }
		public IBuildContext UpTo       { get; }
		public bool          OnlyForSql { get; }

		public ScopeContext(IBuildContext context, IBuildContext upTo) : base(context.Builder, context.ElementType, upTo.SelectQuery)
		{
			Context = context;
			UpTo    = upTo;
		}

		public ScopeContext(IBuildContext context, IBuildContext upTo, bool onlyForSql) : this(context, upTo)
		{
			OnlyForSql = onlyForSql;
		}



		public override Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			var correctedPath = SequenceHelper.CorrectExpression(path, this, Context);
			var newExpr       = Builder.MakeExpression(Context, correctedPath, flags);

			if (flags.IsTable())
				return newExpr;

			if (flags.IsAggregationRoot())
			{
				return newExpr;
			}

			// nothing changed, return as is
			if (ExpressionEqualityComparer.Instance.Equals(newExpr, correctedPath))
				return path;

			if (!flags.IsTest())
			{
				if (flags.IsSql())
				{
					newExpr = Builder.BuildSqlExpression(UpTo, newExpr, flags,
						buildFlags : BuildFlags.ForceAssignments);

					newExpr = Builder.UpdateNesting(UpTo, newExpr);
				}
			}

			return newExpr;
		}

		public override void SetRunQuery<T>(SentenceBag<T> query, Expression expr)
		{
			Context.SetRunQuery(query, expr);
		}

		public override IBuildContext Clone(CloningContext context)
		{
			return new ScopeContext(context.CloneContext(Context), context.CloneContext(UpTo));
		}

		public override BaseSentence GetResultStatement()
		{
			return Context.GetResultStatement();
		}

		public override IBuildContext? GetContext(Expression expression, BuildInfo buildInfo)
		{
			expression = SequenceHelper.CorrectExpression(expression, this, Context);
			return Context.GetContext(expression, buildInfo);
		}

		public override bool IsOptional => Context.IsOptional;

		protected bool Equals(ScopeContext other)
		{
			return Context.Equals(other.Context) && UpTo.Equals(other.UpTo) && OnlyForSql == other.OnlyForSql;
		}

		public override bool Equals(object? obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != this.GetType())
			{
				return false;
			}

			return Equals((ScopeContext)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = Context.GetHashCode();
				hashCode = (hashCode * 397) ^ UpTo.GetHashCode();
				hashCode = (hashCode * 397) ^ OnlyForSql.GetHashCode();
				return hashCode;
			}
		}
	}
}
