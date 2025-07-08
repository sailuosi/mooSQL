using System;
using System.Collections.Generic;

namespace mooSQL.data.model
{
	public class OutputClause :Clause, ISQLNode
	{
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitOutputClause(this);
        }

		List<SetWord>? _outputItems;

        public OutputClause() : base(ClauseType.OutputClause, null)
        {
        }

        public ITableNode?             InsertedTable { get; set; }
		public ITableNode?             DeletedTable  { get; set; }
		public ITableNode?             OutputTable   { get; set; }
		public List<ExpWordBase>? OutputColumns { get; set; }

		public bool                   HasOutput      => HasOutputItems || OutputColumns != null;
		public bool                   HasOutputItems => _outputItems                    != null && _outputItems.Count > 0;
		public List<SetWord> OutputItems
		{
			get => _outputItems ??= new List<SetWord>();
			set => _outputItems = value;
		}

		public void Update(ITableNode? insertedTable, ITableNode? deletedTable, ITableNode? outputTable)
		{
			InsertedTable = insertedTable;
			DeletedTable  = deletedTable;
			OutputTable   = outputTable;
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
		public ClauseType NodeType => ClauseType.OutputClause;

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
