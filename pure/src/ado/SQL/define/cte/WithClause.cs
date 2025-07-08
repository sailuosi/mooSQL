using mooSQL.linq.SqlQuery;
using System;
using System.Collections.Generic;

namespace mooSQL.data.model
{
	public class WithClause :Clause, ISQLNode
	{
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitWithClause(this);
        }

		public WithClause() : base(ClauseType.WithClause, null)
        { 
		
		}

        public List<CTEClause> Clauses { get; set; } = new List<CTEClause>();

#if DEBUG
        public string DebugText => this.ToDebugString();
#endif

		public ClauseType NodeType => ClauseType.WithClause;

		public IElementWriter ToString(IElementWriter writer)
		{
			if (Clauses.Count > 0)
			{
				var first = true;

				foreach (var cte in Clauses)
				{
					if (first)
					{
						writer.Append("WITH ");

						first = false;
					}
					else
					{
						writer.Append(',').AppendLine();
					}

					using (writer.IndentScope())
						writer.AppendElement(cte);

					if (cte.Fields.Count > 3)
					{
						writer.AppendLine();
						writer.AppendLine("(");

						using (writer.IndentScope())
						{
							var firstField = true;
							foreach (var field in cte.Fields)
							{
								if (!firstField)
									writer.AppendLine(",");
								firstField = false;
								writer.AppendElement(field);
							}
						}

						writer.AppendLine();
						writer.AppendLine(")");
					}
					else if (cte.Fields.Count > 0)
					{
						writer.Append(" (");

						var firstField = true;
						foreach (var field in cte.Fields)
						{
							if (!firstField)
								writer.Append(", ");
							firstField = false;
							writer.AppendElement(field);
						}
						writer.AppendLine(")");
					}
					else
					{
						writer.Append(' ');
					}

					using (writer.IndentScope())
					{
						writer.AppendLine("AS");
						writer.AppendLine("(");

						using (writer.IndentScope())
						{
							writer.AppendElement(cte.Body!);
						}

						writer.AppendLine();
						writer.Append(')');
					}
					
				}

				writer.AppendLine();

			}

			return writer;
		}



		public ITableNode? GetTableSource(ITableNode table)
		{
			foreach (var cte in Clauses)
			{
				var ts = cte.Body!.GetTableSource(table);
				if (ts != null)
					return ts;
			}

			return null;
		}

		public void WalkQueries<TContext>(TContext context, Func<TContext, SelectQueryClause, SelectQueryClause> func)
		{
			foreach (var c in Clauses)
			{
				if (c.Body != null)
					c.Body = func(context, c.Body);
			}
		}
	}
}
