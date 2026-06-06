using System.Linq.Expressions;
using System.Reflection;

namespace mooSQL.linq.Linq.Builder
{
	using mooSQL.linq.Expressions;
	using mooSQL.linq.ext;
	using Reflection;

	static class DistinctBuilder
	{
		static readonly MethodInfo[] _supportedMethods = { Methods.Queryable.Distinct, Methods.Enumerable.Distinct, Methods.SooQuery.SelectDistinct };

		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ClauseSqlTranslator builder)
			=> call.IsSameGenericMethod(_supportedMethods);

		internal class DistinctContext : PassThroughContext
		{
			public DistinctContext(IBuildContext context) : base(context)
			{
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (SequenceHelper.IsSameContext(path, this) && (flags.IsRoot() || flags.IsAssociationRoot()))
					return path;

				var corrected = SequenceHelper.CorrectExpression(path, this, Context);

				if (flags.IsTable() || flags.IsTraverse() || flags.IsSubquery())
					return corrected;

				Expression result;
				if (flags.IsExtractProjection())
				{
					result = Builder.MakeExpression(Context, corrected, flags);
				}
				else
				{
					result = Builder.BuildSqlExpression(Context, corrected, flags.SqlFlag());
					result = Builder.UpdateNesting(Context, result);
				}

				return result;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new DistinctContext(context.CloneContext(Context));
			}
		}
	}
}
