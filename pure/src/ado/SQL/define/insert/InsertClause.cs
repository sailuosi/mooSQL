using System;
using System.Collections.Generic;

namespace mooSQL.data.model
{
	/// <summary>
	/// 单行写入 insert into values () 语句
	/// </summary>
	public class InsertClause :Clause, ISQLNode
	{
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitInsertClause(this);
        }
		/// <summary>初始化空 VALUES 列表。</summary>
		public InsertClause() : base(ClauseType.InsertClause, null)
        {
			Items        = new List<SetWord>();
		}

		/// <summary>各列赋值（SET/VALUES 行）。</summary>
		public List<SetWord> Items        { get; set; }
		/// <summary>目标表。</summary>
		public ITableNode?              Into         { get; set; }
		/// <summary>是否请求返回标识列（OUTPUT/RETURNING）。</summary>
		public bool                   WithIdentity { get; set; }

		/// <summary>替换目标表。</summary>
		public void Update(ITableNode? into)
		{
			Into  = into;
		}

		#region Overrides

#if OVERRIDETOSTRING

			public override string ToString()
			{
				return this.ToDebugString();
			}

#endif

		#endregion

		#region IQueryElement Members

#if DEBUG
		/// <summary>调试文本。</summary>
		public string DebugText => this.ToDebugString();
#endif
		/// <inheritdoc />
		public ClauseType NodeType => ClauseType.InsertClause;

		/// <inheritdoc />
		public IElementWriter ToString(IElementWriter writer)
		{
			writer
				.Append("INSERT ")
				.AppendElement(Into)
				.Append(" VALUES ")
				.AppendLine();

			using (writer.IndentScope())
			{
				for (var index = 0; index < Items.Count; index++)
				{
					var e = Items[index];
					writer.AppendElement(e);
					if (index < Items.Count - 1)
						writer.AppendLine();
				}
			}

			return writer;
		}

		#endregion
	}
}
