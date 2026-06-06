using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using mooSQL.data;
	using mooSQL.linq.Expressions;
    using mooSQL.linq.ext;

    [BuildsMethodCall(

		nameof(DbQueryExtensions.IsTemporary),
		nameof(DbQueryExtensions.TableOptions)

	)]
	sealed class TableAttributeBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ClauseSqlTranslator builder)
			=> call.IsQueryable();

		protected override BuildSequenceResult BuildMethodCall(ClauseSqlTranslator builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			var table    = SequenceHelper.GetTableContext(sequence) ?? throw new SooQueryException($"Cannot get table context from {sequence.GetType()}");
			var value = methodCall.Arguments.Count == 1 && methodCall.Method.Name == nameof(DbQueryExtensions.IsTemporary)
				? true
				: builder.EvaluateExpression(methodCall.Arguments[1]);

			switch (methodCall.Method.Name)
			{

				case nameof(DbQueryExtensions.TableOptions) : table.SqlTable.TableOptions  = (TableOptions)value!; break;
				case nameof(DbQueryExtensions.IsTemporary)  : table.SqlTable.Set((bool)value!, TableOptions.IsTemporary); break;
			}

			return BuildSequenceResult.FromContext(sequence);
		}
	}
}
