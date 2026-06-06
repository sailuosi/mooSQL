using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using mooSQL.linq.Expressions;

	sealed class CastContext : PassThroughContext
	{
		readonly MethodCallExpression _methodCall;

		public CastContext(IClauseContext context, MethodCallExpression methodCall)
			: base(context)
		{
			_methodCall = methodCall;
		}

		public override IClauseContext Clone(CloningContext context)
			=> new CastContext(context.CloneContext(Context), _methodCall);

		public override Expression BuildProjection(Expression path, ProjectFlags flags)
		{
			var corrected = base.BuildProjection(path, flags);

			if (flags.IsTable())
				return corrected;

			var type = _methodCall.Method.GetGenericArguments()[0];

			if (corrected.Type != type)
				corrected = Expression.Convert(corrected, type);

			return corrected;
		}
	}
}
