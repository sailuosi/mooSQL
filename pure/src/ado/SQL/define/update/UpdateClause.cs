using System;
using System.Collections.Generic;

namespace mooSQL.data.model
{
	/// <summary>
	/// UPDATE 语句中「更新目标表 + SET 列赋值」片段（不含 WHERE 等，后者在 <see cref="SelectQueryClause"/> 上）。
	/// </summary>
	public class UpdateClause :Clause, ISQLNode
	{
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitUpdateClause(this);
        }
		/// <summary>构造空的 UPDATE 片段。</summary>
		public UpdateClause() : base(ClauseType.UpdateClause, null)
        { 
		}

		/// <summary><c>SET</c> 子句中的列赋值列表。</summary>
		public List<SetWord> Items         { get; set; } = new();
		/// <summary>参与定位行的键列赋值（方言/合并场景可选）。</summary>
		public List<SetWord> Keys          { get; set; } = new();
		/// <summary>
		/// 要更新的表
		/// </summary>
		public ITableNode?              Table         { get; set; }
		/// <summary>
		/// 来源表
		/// </summary>
		public ITableNode?        TableSource   { get; set; }
		/// <summary>是否包含与来源的比较/连接语义（由上层设置）。</summary>
		public bool                   HasComparison { get; set; }

		/// <summary>同时设置目标表与来源表引用。</summary>
		public void Modify(ITableNode? table, ITableNode? tableSource)
		{
			Table       = table;
			TableSource = tableSource;
		}

		#region Overrides

#if OVERRIDETOSTRING

		/// <inheritdoc />
		public override string ToString()
		{
			return this.ToDebugString();
		}

#endif

		#endregion

		#region IQueryElement Members

#if DEBUG
		/// <summary>调试输出文本。</summary>
		public string DebugText => this.ToDebugString();
#endif

		/// <inheritdoc />
		public ClauseType NodeType => ClauseType.UpdateClause;

		/// <inheritdoc />
		IElementWriter ToString(IElementWriter writer)
		{
			writer
				.Append('\t');

			if (Table != null)
				writer.AppendElement(Table);
			if (TableSource != null)
				writer.AppendElement(TableSource);

			writer.AppendLine()
				.Append("SET ")
				.AppendLine();

			using (writer.IndentScope())
				foreach (var e in Items)
				{
					writer
						.AppendElement(e)
						.AppendLine();
				}

			return writer;
		}

		#endregion
	}
}
