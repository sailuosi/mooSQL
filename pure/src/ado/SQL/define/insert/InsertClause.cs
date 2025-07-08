using System;
using System.Collections.Generic;

namespace mooSQL.data.model
{
	/// <summary>
	/// 单行写入 insert into values () 语句
	/// </summary>
	public class InsertClause :Clause, ISQLNode
	{
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitInsertClause(this);
        }
		public InsertClause() : base(ClauseType.InsertClause, null)
        {
			Items        = new List<SetWord>();
		}

		public List<SetWord> Items        { get; set; }
		public ITableNode?              Into         { get; set; }
		public bool                   WithIdentity { get; set; }

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
		public string DebugText => this.ToDebugString();
#endif
		public ClauseType NodeType => ClauseType.InsertClause;

		IElementWriter ToString(IElementWriter writer)
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
