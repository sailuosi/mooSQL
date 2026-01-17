using System.Text;

namespace mooSQL.data.model
{
	/// <summary>
	/// Sql AST node interface.
	/// </summary>
	public interface ISQLNode
	{
#if DEBUG
		//public string DebugText { get; }
#endif
		/// <summary>
		/// 节点类型
		/// </summary>
		ClauseType       NodeType { get; }
		/// <summary>
		/// 调试字符串
		/// </summary>
		//IElementWriter ToString(IElementWriter writer);

		Clause Accept(ClauseVisitor visitor);

    }
}
