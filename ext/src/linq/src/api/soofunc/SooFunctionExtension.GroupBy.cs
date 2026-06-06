namespace mooSQL.linq;

using Linq;

public static partial class SooFunctionExtension
{
	public interface IGroupBy
	{
		bool None { get; }
		T Rollup<T>(T rollupKey);
		T Cube<T>(T cubeKey);
		T GroupingSets<T>(T setsExpression);
	}

	sealed class GroupByImpl : IGroupBy
	{
		public bool None => true;

		public T Rollup<T>(T rollupKey)
			=> throw new LinqException($"'{nameof(Rollup)}' should not be called directly.");

		public T Cube<T>(T cubeKey)
			=> throw new LinqException($"'{nameof(Cube)}' should not be called directly.");

		public T GroupingSets<T>(T setsExpression)
			=> throw new LinqException($"'{nameof(GroupingSets)}' should not be called directly.");
	}

	public static IGroupBy GroupBy { get; } = new GroupByImpl();

	[Extension("GROUPING({fields, ', '})", ServerSideOnly = true, CanBeNull = false, IsAggregate = true)]
	public static int Grouping([ExprParameter(ParameterKind = ExprParameterKind.Values)] params object[] fields)
		=> throw new LinqException($"'{nameof(Grouping)}' should not be called directly.");
}
