using System;
using System.Collections.Generic;
using System.Linq;

namespace mooSQL.data.model
{
	public sealed class QueryExtension :Clause, ISQLNode
	{
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
		public string           DebugText   => this.ToDebugString();
#endif

		public ClauseType NodeType => ClauseType.SqlQueryExtension;

		public IElementWriter ToString(IElementWriter writer)
		{
			return writer.Append("extension");
		}
	}
}
