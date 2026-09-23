using mooSQL.data.model;

namespace mooSQL.linq.clause
{
	/// <summary>
	/// Ext 侧调试字符串扩展。pure 的 <c>ToDebugExtension</c> 为 assembly-internal，
	/// 本类与其行为对齐（委托 <see cref="object.ToString"/>），供 Ext LINQ 编译层使用。
	/// </summary>
	public static class DebugStringExtensions
	{
		internal static string ToDebugString<T>(this T element, SelectQueryClause? selectQuery = null)
			where T : ISQLNode
		{
			_ = selectQuery;
			try
			{
				return element.ToString() ?? string.Empty;
			}
			catch
			{
				return $"FAIL ToDebugString('{element.GetType().Name}').";
			}
		}
	}
}
