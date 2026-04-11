using mooSQL.linq.SqlQuery;
using System;
using System.Collections.Generic;

namespace mooSQL.data.model
{
	/// <summary>语句顶层的 <c>WITH</c> 子句，包含若干 <see cref="CTEClause"/>。</summary>
	public class WithClause :Clause, ISQLNode
	{
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitWithClause(this);
        }

		/// <summary>构造空的 WITH 子句容器。</summary>
		public WithClause() : base(ClauseType.WithClause, null)
        { 
		
		}

        /// <summary>按书写顺序排列的 CTE 定义。</summary>
        public List<CTEClause> Clauses { get; set; } = new List<CTEClause>();

#if DEBUG
        /// <summary>调试文本。</summary>
        public string DebugText => this.ToDebugString();
#endif

		/// <inheritdoc />
		public ClauseType NodeType => ClauseType.WithClause;

		/// <inheritdoc />
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



		/// <summary>在嵌套查询中解析表引用对应的 <see cref="ITableNode"/>。</summary>
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

		/// <summary>对每个 CTE 的 <see cref="CTEClause.Body"/> 执行变换。</summary>
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
