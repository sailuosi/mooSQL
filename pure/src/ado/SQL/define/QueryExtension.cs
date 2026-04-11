using System;
using System.Collections.Generic;
using System.Linq;

namespace mooSQL.data.model
{
	/// <summary>附在查询或表片段上的方言扩展（hint、优化器指令等）。</summary>
	public sealed class QueryExtension :Clause, ISQLNode
	{
		/// <summary>构造空扩展节点。</summary>
		public QueryExtension() : base(ClauseType.SqlQueryExtension, null)
        {
		}

		/// <summary>
		/// Gets optional configuration, to which extension should be applied.
		/// </summary>
		public string?                            Configuration      { get; set; }
		/// <summary>
		/// Gets extension apply scope/location.
		/// </summary>
		public QueryExtensionScope            Scope              { get; set; }
		/// <summary>
		/// Gets extension arguments.
		/// </summary>
		public  Dictionary<string,IExpWord>  Arguments { get; set; }
		/// <summary>
		/// Gets optional extension builder type. Must implement <see cref="ISqlQueryExtensionBuilder"/> or <see cref="ISqlTableExtensionBuilder"/> interface.
		/// </summary>
		public Type?                              BuilderType        { get; set; }

#if DEBUG
		/// <summary>调试文本。</summary>
		public string           DebugText   => this.ToDebugString();
#endif

		/// <inheritdoc />
		public ClauseType NodeType => ClauseType.SqlQueryExtension;

		/// <inheritdoc />
		public IElementWriter ToString(IElementWriter writer)
		{
			return writer.Append("extension");
		}
	}
}
