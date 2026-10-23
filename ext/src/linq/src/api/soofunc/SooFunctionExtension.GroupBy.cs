namespace mooSQL.linq;

public static partial class SooFunctionExtension
{
	/// <summary>
	/// IGroupBy
	/// </summary>
	/// <summary>
	/// IGroupBy
	/// </summary>
	public interface IGroupBy
	{
		/// <summary>
		/// None
		/// </summary>
		/// <summary>
		/// Gets or sets None.
		/// </summary>
		bool None { get; }
		/// <summary>
		/// Rollup&lt;T&gt;
		/// </summary>
		/// <summary>
		/// Rollup&lt;T&gt;
		/// </summary>
		T Rollup<T>(T rollupKey);
		/// <summary>
		/// Cube&lt;T&gt;
		/// </summary>
		/// <summary>
		/// Cube&lt;T&gt;
		/// </summary>
		T Cube<T>(T cubeKey);
		/// <summary>
		/// GroupingSets&lt;T&gt;
		/// </summary>
		/// <summary>
		/// GroupingSets&lt;T&gt;
		/// </summary>
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
	/// <summary>
	/// Gets or sets GroupBy.
	/// </summary>

	public static IGroupBy GroupBy { get; } = new GroupByImpl();
	/// <summary>
	/// Grouping
	/// </summary>

	[Extension("GROUPING({fields, ', '})", ServerSideOnly = true, CanBeNull = false, IsAggregate = true)]
	public static int Grouping([ExprParameter(ParameterKind = ExprParameterKind.Values)] params object[] fields)
		=> throw new LinqException($"'{nameof(Grouping)}' should not be called directly.");
}
