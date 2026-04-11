using System;
using System.Collections.Generic;

namespace mooSQL.data.model
{
	/// <summary>
	/// merge 词组
	/// </summary>
	public class MergeOperationClause :Clause, ISQLNode
	{
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitMergeOperationClause(this);
        }
		
		/// <summary>仅指定动作类型。</summary>
		public MergeOperationClause(MergeOperateType type) : base(ClauseType.MergeOperationClause, null)
        {
			OperationType = type;
		}

        /// <summary>指定动作类型、可选条件与 SET 项列表。</summary>
        public MergeOperationClause(
			MergeOperateType            type,
			SearchConditionWord?           where,
			SearchConditionWord?           whereDelete,
			IEnumerable<SetWord> items) : base(ClauseType.MergeOperationClause, null)
        {
			OperationType = type;
			Where         = where;
			WhereDelete   = whereDelete;

			foreach (var item in items)
				Items.Add(item);
		}

		/// <summary>WHEN 分支种类。</summary>
		public MergeOperateType     OperationType { get; }

		/// <summary>匹配/非匹配时的附加 AND 条件。</summary>
		public SearchConditionWord?    Where         { get; set; }

		/// <summary><see cref="MergeOperateType.UpdateWithDelete"/> 时 DELETE WHERE 条件。</summary>
		public SearchConditionWord?    WhereDelete   { get;  set; }

		/// <summary>INSERT/UPDATE 的列赋值列表。</summary>
		public List<SetWord> Items         { get; } = new List<SetWord>();

		#region IQueryElement

#if DEBUG
		/// <summary>调试文本。</summary>
		public string DebugText => this.ToDebugString();
#endif
		/// <inheritdoc />
		ClauseType ISQLNode.NodeType => ClauseType.MergeOperationClause;

		/// <inheritdoc />
		public IElementWriter ToString(IElementWriter writer)
		{
			switch (OperationType)
			{
				case MergeOperateType.Delete:
					writer.Append("WHEN MATCHED");

					if (Where != null)
					{
						writer
							.Append(" AND ")
							.AppendElement(Where);
					}

					writer.AppendLine(" THEN DELETE");

					break;

				case MergeOperateType.DeleteBySource:
					writer.Append("WHEN NOT MATCHED BY SOURCE");

					if (Where != null)
					{
						writer
							.Append(" AND ")
							.AppendElement(Where);
					}

					writer.AppendLine(" THEN DELETE");

					break;

				case MergeOperateType.Insert:
					writer.Append("WHEN NOT MATCHED");

					if (Where != null)
					{
						writer.Append(" AND ");
						//((ISQLNode)Where).ToString(writer);
					}

					writer.AppendLine(" THEN INSERT");

					using (writer.IndentScope())
						for (var index = 0; index < Items.Count; index++)
						{
							var item = Items[index];
							writer.AppendElement(item);
							if (index < Items.Count - 1)
								writer.AppendLine();
						}

					break;

				case MergeOperateType.Update:
					writer.Append("WHEN MATCHED");

					if (Where != null)
					{
						writer
							.Append(" AND ")
							.AppendElement(Where);
					}

					writer.AppendLine(" THEN UPDATE");

					using (writer.IndentScope())
						for (var index = 0; index < Items.Count; index++)
						{
							var item = Items[index];
							writer.AppendElement(item);
							if (index < Items.Count - 1)
								writer.AppendLine();
						}

					break;

				case MergeOperateType.UpdateBySource:
					writer.Append("WHEN NOT MATCHED BY SOURCE");

					if (Where != null)
					{
						writer
							.Append(" AND ")
							.AppendElement(Where);
					}

					writer.AppendLine(" THEN UPDATE");

					using (writer.IndentScope())
						for (var index = 0; index < Items.Count; index++)
						{
							var item = Items[index];
							writer.AppendElement(item);
							if (index < Items.Count - 1)
								writer.AppendLine();
						}

					break;

				case MergeOperateType.UpdateWithDelete:
					writer.Append("WHEN MATCHED THEN UPDATE");

					if (Where != null)
					{
						writer
							.Append(" WHERE ")
							.AppendElement(Where);
					}

					if (WhereDelete != null)
					{
						writer
							.Append(" DELETE WHERE ")
							.AppendElement(WhereDelete);
					}

					break;
			}

			return writer;
		}

		#endregion
	}
}
