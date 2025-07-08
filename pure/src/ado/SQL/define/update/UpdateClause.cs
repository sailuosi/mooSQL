using System;
using System.Collections.Generic;

namespace mooSQL.data.model
{
	/// <summary>
	/// update 语句的 update Table set Items 部分
	/// </summary>
	public class UpdateClause :Clause, ISQLNode
	{
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitUpdateClause(this);
        }
		public UpdateClause() : base(ClauseType.UpdateClause, null)
        { 
		}

		public List<SetWord> Items         { get; set; } = new();
		public List<SetWord> Keys          { get; set; } = new();
		/// <summary>
		/// 要更新的表
		/// </summary>
		public ITableNode?              Table         { get; set; }
		/// <summary>
		/// 来源表
		/// </summary>
		public ITableNode?        TableSource   { get; set; }
		public bool                   HasComparison { get; set; }

		public void Modify(ITableNode? table, ITableNode? tableSource)
		{
			Table       = table;
			TableSource = tableSource;
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
		public string DebugText => this.ToDebugString();
#endif

		public ClauseType NodeType => ClauseType.UpdateClause;

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
