using System;
using System.Collections.Generic;

namespace mooSQL.data.model
{
	/// <summary>
	/// SQL Server 风格 <c>OUTPUT</c> 子句：可引用 inserted/deleted 伪表，或将结果写入表变量。
	/// </summary>
	public class OutputClause :Clause, ISQLNode
	{
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitOutputClause(this);
        }

		List<SetWord>? _outputItems;

		/// <summary>构造空的 OUTPUT 子句。</summary>
        public OutputClause() : base(ClauseType.OutputClause, null)
        {
        }

		/// <summary>MERGE/INSERT 后的 inserted 伪表（若有）。</summary>
        public ITableNode?             InsertedTable { get; set; }
		/// <summary>DELETE/UPDATE/MERGE 中的 deleted 伪表（若有）。</summary>
		public ITableNode?             DeletedTable  { get; set; }
		/// <summary>OUTPUT INTO 的目标表（若有）。</summary>
		public ITableNode?             OutputTable   { get; set; }
		/// <summary>额外输出列表达式列表（与 <see cref="OutputItems"/> 二选一或组合，依方言）。</summary>
		public List<ExpWordBase>? OutputColumns { get; set; }

		/// <summary>是否存在任何 OUTPUT 内容。</summary>
		public bool                   HasOutput      => HasOutputItems || OutputColumns != null;
		/// <summary>是否存在 <see cref="SetWord"/> 形式的输出项。</summary>
		public bool                   HasOutputItems => _outputItems                    != null && _outputItems.Count > 0;
		/// <summary>OUTPUT 列赋值/表达式列表。</summary>
		public List<SetWord> OutputItems
		{
			get => _outputItems ??= new List<SetWord>();
			set => _outputItems = value;
		}

		/// <summary>批量设置 inserted/deleted/output 表引用。</summary>
		public void Update(ITableNode? insertedTable, ITableNode? deletedTable, ITableNode? outputTable)
		{
			InsertedTable = insertedTable;
			DeletedTable  = deletedTable;
			OutputTable   = outputTable;
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
		public ClauseType NodeType => ClauseType.OutputClause;

		/// <inheritdoc />
		IElementWriter ToString(IElementWriter writer)
		{
			writer.AppendLine()
				.AppendLine("OUTPUT");

			if (HasOutput)
			{

				using (writer.IndentScope())
				{
					var first = true;

					if (HasOutputItems)
					{
						foreach (var oi in OutputItems)
						{
							if (!first)
								writer.AppendLine(',');
							first = false;

							writer.AppendElement(oi.Expression);
						}

						writer.AppendLine();
					}
				}

				if (OutputColumns != null)
				{
					using (writer.IndentScope())
					{
						var first = true;

						foreach (var expr in OutputColumns)
						{
							if (!first)
								writer.AppendLine(',');

							first = false;

							writer.AppendElement(expr);
						}
					}

					writer.AppendLine();
				}

				if (OutputTable != null)
				{
					writer.Append("INTO ")
						.AppendLine(OutputTable.Name)
						.AppendLine('(');

					using (writer.IndentScope())
					{
						var firstColumn = true;
						if (HasOutputItems)
						{
							foreach (var oi in OutputItems)
							{
								if (!firstColumn)
									writer.AppendLine(',');
								firstColumn = false;

								writer.AppendElement(oi.Column);
							}
						}

						writer.AppendLine();
					}

					writer.AppendLine(")");
				}
			}

			return writer;
		}

		#endregion
	}
}
