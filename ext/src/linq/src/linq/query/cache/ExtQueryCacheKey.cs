using System;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq
{
	readonly struct ExtQueryCacheKey : IEquatable<ExtQueryCacheKey>
	{
		public Expression Query { get; }
		public Type ResultType { get; }
		public Type DialectType { get; }
		public QueryFlags Flags { get; }
		public bool ParameterizeTakeSkip { get; }
		public bool PreferApply { get; }

		public ExtQueryCacheKey(
			Expression query,
			Type resultType,
			Type dialectType,
			QueryFlags flags,
			bool parameterizeTakeSkip,
			bool preferApply)
		{
			Query = query;
			ResultType = resultType;
			DialectType = dialectType;
			Flags = flags;
			ParameterizeTakeSkip = parameterizeTakeSkip;
			PreferApply = preferApply;
		}

		public bool Equals(ExtQueryCacheKey other)
			=> ExtExpressionStructuralComparer.Instance.Equals(Query, other.Query)
				&& ResultType == other.ResultType
				&& DialectType == other.DialectType
				&& Flags == other.Flags
				&& ParameterizeTakeSkip == other.ParameterizeTakeSkip
				&& PreferApply == other.PreferApply;

		public override bool Equals(object? obj)
			=> obj is ExtQueryCacheKey other && Equals(other);

		public override int GetHashCode()
		{
			unchecked
			{
				var hash = new HashCode();
				hash.Add(Query, ExtExpressionStructuralComparer.Instance);
				hash.Add(ResultType);
				hash.Add(DialectType);
				hash.Add(Flags);
				hash.Add(ParameterizeTakeSkip);
				hash.Add(PreferApply);
				return hash.ToHashCode();
			}
		}

		public static bool operator ==(ExtQueryCacheKey left, ExtQueryCacheKey right) => left.Equals(right);

		public static bool operator !=(ExtQueryCacheKey left, ExtQueryCacheKey right) => !left.Equals(right);
	}
}
