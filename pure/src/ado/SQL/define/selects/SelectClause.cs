using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace mooSQL.data.model
{
	/// <summary>
	/// select 部分的词组，持有列信息Columns，以及distinct /take/skip等标识
	/// </summary>
	public class SelectClause : ClauseBase, ISQLNode
	{

        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitSelectClause(this);
        }

		private ContentBag<ColumnWord> _Columns=new ContentBag<ColumnWord>();
		/// <summary>
		/// 字段集合
		/// </summary>
        public ContentBag<ColumnWord> Columns {
			get { 
				return _Columns;
			}
		} 

		public int Count
		{
			get { 
				return Columns.Count;
			}
		}
        /// <summary>
        /// distinct 标识
        /// </summary>
        public bool IsDistinct { get; set; }
        public bool OptimizeDistinct { get; set; }

        public IExpWord? TakeValue { get;  set; }
        public TakeHintType? TakeHints { get;  set; }

        public IExpWord? SkipValue { get; set; }
        #region Init

        public SelectClause(SelectQueryClause selectQuery, Type type = null) : base(selectQuery, ClauseType.SelectClause, type)
        {
		}

        public SelectClause(bool isDistinct, IExpWord? takeValue, TakeHintType? takeHints, IExpWord? skipValue, IEnumerable<ColumnWord> columns, Type type = null) : base(null, ClauseType.SelectClause, type)
        {
			IsDistinct = isDistinct;
			TakeValue  = takeValue;
			TakeHints  = takeHints;
			SkipValue  = skipValue;
			Columns.content.AddRange(columns);
		}

		#endregion



		#region HasModifier
		/// <summary>
		/// 是否含有distinct /top 等修饰符
		/// </summary>
		public bool HasModifier => IsDistinct || SkipValue != null || TakeValue != null;

		#endregion

		#region Distinct


		#endregion

		#region Take

		public SelectClause Take(int value, TakeHintType? hints)
		{
			TakeValue = new ValueWord(value);
			TakeHints = hints;
			return this;
		}

		public SelectClause Take(IExpWord? value, TakeHintType? hints)
		{
			TakeHints = hints;
			TakeValue = value;
			return this;
		}



		#endregion

		#region Skip

		public SelectClause Skip(int value)
		{
			SkipValue = new ValueWord(value);
			return this;
		}

		public SelectClause Skip(IExpWord value)
		{
			SkipValue = value;
			return this;
		}



		#endregion

		#region Overrides

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return this.ToDebugString();
		}

#endif

		#endregion

		#region QueryElement overrides

		public override ClauseType NodeType => ClauseType.SelectClause;

		public IElementWriter ToString(IElementWriter writer)
		{
			if (!writer.AddVisited(this))
				return writer.Append("...");

			writer.Append("SELECT ");

			if (IsDistinct) writer.Append("DISTINCT ");

			if (SkipValue != null)
			{
				writer
					.Append("SKIP ")
					.AppendElement(SkipValue);
				writer.Append(' ');
			}

			if (TakeValue != null)
			{
				writer
					.Append("TAKE ")
					.AppendElement(TakeValue)
					.Append(' ');

				if (TakeHints != null)
				{
					if (TakeHints.Value.HasFlag(TakeHintType.Percent))
						writer.Append("PERCENT ");

					if (TakeHints.Value.HasFlag(TakeHintType.WithTies))
						writer.Append("WITH TIES ");
				}
			}

			writer.AppendLine();



			writer.RemoveVisited(this);

			return writer;
		}

		#endregion

		public void Cleanup()
		{
			IsDistinct = false;
			TakeValue  = null;
			TakeHints  = null;
			SkipValue  = null;
			Columns.Clear();
		}

				#region Columns

		public SelectClause Field(FieldWord field)
		{
			AddOrFindColumn(new ColumnWord(SelectQuery, field));
			return this;
		}

		public SelectClause Field(FieldWord field, string alias)
		{
			AddOrFindColumn(new ColumnWord(SelectQuery, field, alias));
			return this;
		}

		public SelectClause SubQuery(SelectQueryClause subQuery)
		{
			AddOrFindColumn(new ColumnWord(SelectQuery, subQuery));
			return this;
		}

		public SelectClause SubQuery(SelectQueryClause selectQuery, string alias)
		{
			AddOrFindColumn(new ColumnWord(SelectQuery, selectQuery, alias));
			return this;
		}

		public SelectClause Expr(IExpWord expr)
		{
			AddOrFindColumn(new ColumnWord(SelectQuery, expr));
			return this;
		}

		public SelectClause ExprNew(IExpWord expr)
		{
			Columns.content.Add(new ColumnWord(SelectQuery, expr));
			return this;
		}

		public SelectClause Expr(IExpWord expr, string alias)
		{
			AddOrFindColumn(new ColumnWord(SelectQuery, expr, alias));
			return this;
		}

		public SelectClause Expr(string expr, params IExpWord[] values)
		{
			AddOrFindColumn(new ColumnWord(SelectQuery, new ExpressionWord(null, expr, values)));
			return this;
		}

		public SelectClause Expr(Type systemType, string expr, params IExpWord[] values)
		{
			AddOrFindColumn(new ColumnWord(SelectQuery, new ExpressionWord(systemType, expr, values)));
			return this;
		}

		public SelectClause Expr(string expr, int priority, params IExpWord[] values)
		{
			AddOrFindColumn(new ColumnWord(SelectQuery, new ExpressionWord(null, expr, priority, values)));
			return this;
		}

		public SelectClause Expr(Type systemType, string expr, int priority, params IExpWord[] values)
		{
			AddOrFindColumn(new ColumnWord(SelectQuery, new ExpressionWord(systemType, expr, priority, values)));
			return this;
		}

		public SelectClause Expr(string alias, string expr, int priority, params IExpWord[] values)
		{
			AddOrFindColumn(new ColumnWord(SelectQuery, new ExpressionWord(null, expr, priority, values)));
			return this;
		}

		public SelectClause Expr(Type systemType, string alias, string expr, int priority, params IExpWord[] values)
		{
			AddOrFindColumn(new ColumnWord(SelectQuery, new ExpressionWord(systemType, expr, priority, values)));
			return this;
		}

		public SelectClause Expr<T>(IExpWord expr1, string operation, IExpWord expr2)
		{
			AddOrFindColumn(new ColumnWord(SelectQuery, new BinaryWord(typeof(T), expr1, operation, expr2)));
			return this;
		}

		public SelectClause Expr<T>(IExpWord expr1, string operation, IExpWord expr2, int priority)
		{
			AddOrFindColumn(new ColumnWord(SelectQuery, new BinaryWord(typeof(T), expr1, operation, expr2, priority)));
			return this;
		}

		public SelectClause Expr<T>(string alias, IExpWord expr1, string operation, IExpWord expr2, int priority)
		{
			AddOrFindColumn(new ColumnWord(SelectQuery, new BinaryWord(typeof(T), expr1, operation, expr2, priority), alias));
			return this;
		}

		public int Add(IExpWord expr)
		{
			if (expr is ColumnWord column && column.Parent == SelectQuery)
				throw new InvalidOperationException();

			return AddOrFindColumn(new ColumnWord(SelectQuery, expr));
		}

		public ColumnWord AddColumn(IExpWord expr)
		{
			return SelectQuery.Select.Columns[Add(expr)];
		}

		public int AddNew(IExpWord expr, string? alias = default)
		{
			if (expr is ColumnWord column && column.Parent == SelectQuery)
				throw new InvalidOperationException();

			Columns.Add(new ColumnWord(SelectQuery, expr, alias));
			return Columns.Count - 1;
		}

		public ColumnWord AddNewColumn(IExpWord expr)
		{
			return Columns[AddNew(expr)];
		}

		public int Add(IExpWord expr, string? alias)
		{
			return AddOrFindColumn(new ColumnWord(SelectQuery, expr, alias));
		}

		/// <summary>
		/// Adds column if it is not added yet.
		/// </summary>
		/// <returns>Returns index of column in Columns list.</returns>
		int AddOrFindColumn(ColumnWord col)
		{
			//var colUnderlying = col.UnderlyingExpression();
			var colExpression = col.Expression;

			for (var i = 0; i < Columns.Count; i++)
			{
				var column           = Columns[i];
				var columnExpression = column.Expression;
				//var underlying       = column.UnderlyingExpression();

				if (column.Expression.Equals(colExpression))
				{
					if (column.NodeType == ClauseType.SqlValue &&
						colExpression.NodeType == ClauseType.Column)
					{
						// avoid suppressing constant columns
						continue;
					}

					return i;
				}
			}

#if DEBUG

			switch (col.Expression.NodeType)
			{
				case ClauseType.SqlField :
					{
						var table = ((FieldWord)col.Expression).Table;

						//if (SqlQuery.From.GetFromTables().Any(_ => _ == table))
						//	throw new InvalidOperationException("Wrong field usage.");

						break;
					}

				case ClauseType.Column :
					{
						var query = ((ColumnWord)col.Expression).Parent;

						//if (!SqlQuery.From.GetFromQueries().Any(_ => _ == query))
						//	throw new InvalidOperationException("Wrong column usage.");

						if (SelectQuery.HasSetOperators)
						{
							if (SelectQuery.SetOperators.Any(u => u.SelectQuery == query))
							{

							}
						}

						break;
					}

				case ClauseType.SqlQuery :
					{
						if (col.Expression == SelectQuery)
							throw new InvalidOperationException("Wrong query usage.");
						break;
					}
			}

#endif
			Columns.Add(col);

			return Columns.Count - 1;
		}



		#endregion
	}
}
