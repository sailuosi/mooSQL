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

        /// <inheritdoc />
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

		/// <summary>当前 SELECT 列表中的列数。</summary>
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
        /// <summary>是否在 DISTINCT 下做额外优化（方言/优化器相关）。</summary>
        public bool OptimizeDistinct { get; set; }

        /// <summary>TAKE/TOP/FETCH 等限制行数的表达式。</summary>
        public IExpWord? TakeValue { get;  set; }
        /// <summary>与 TAKE 配套的提示（如 PERCENT、WITH TIES）。</summary>
        public TakeHintType? TakeHints { get;  set; }

        /// <summary>SKIP/OFFSET 等跳过行数的表达式。</summary>
        public IExpWord? SkipValue { get; set; }
        #region Init

        /// <summary>
        /// 挂在指定查询上的 SELECT 子句。
        /// </summary>
        public SelectClause(SelectQueryClause selectQuery, Type type = null) : base(selectQuery, ClauseType.SelectClause, type)
        {
		}

        /// <summary>
        /// 使用列集合及 DISTINCT/TAKE/SKIP 状态构造独立 SELECT 子句（常用于子查询克隆）。
        /// </summary>
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

		/// <summary>设置常量 TAKE 行数及可选提示。</summary>
		public SelectClause Take(int value, TakeHintType? hints)
		{
			TakeValue = new ValueWord(value);
			TakeHints = hints;
			return this;
		}

		/// <summary>使用表达式作为 TAKE 行数。</summary>
		public SelectClause Take(IExpWord? value, TakeHintType? hints)
		{
			TakeHints = hints;
			TakeValue = value;
			return this;
		}



		#endregion

		#region Skip

		/// <summary>设置常量 SKIP 行数。</summary>
		public SelectClause Skip(int value)
		{
			SkipValue = new ValueWord(value);
			return this;
		}

		/// <summary>使用表达式作为 SKIP 行数。</summary>
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

		/// <inheritdoc />
		public override ClauseType NodeType => ClauseType.SelectClause;

		/// <inheritdoc />
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

		/// <summary>清空列、DISTINCT 与 TAKE/SKIP 状态。</summary>
		public void Cleanup()
		{
			IsDistinct = false;
			TakeValue  = null;
			TakeHints  = null;
			SkipValue  = null;
			Columns.Clear();
		}

				#region Columns

		/// <summary>追加一列（字段引用）。</summary>
		public SelectClause Field(FieldWord field)
		{
			AddOrFindColumn(new ColumnWord(SelectQuery, field));
			return this;
		}

		/// <summary>追加一列并指定别名。</summary>
		public SelectClause Field(FieldWord field, string alias)
		{
			AddOrFindColumn(new ColumnWord(SelectQuery, field, alias));
			return this;
		}

		/// <summary>追加一列（子查询作为标量/表列）。</summary>
		public SelectClause SubQuery(SelectQueryClause subQuery)
		{
			AddOrFindColumn(new ColumnWord(SelectQuery, subQuery));
			return this;
		}

		/// <summary>追加子查询列并指定别名。</summary>
		public SelectClause SubQuery(SelectQueryClause selectQuery, string alias)
		{
			AddOrFindColumn(new ColumnWord(SelectQuery, selectQuery, alias));
			return this;
		}

		/// <summary>追加一列（任意表达式）。</summary>
		public SelectClause Expr(IExpWord expr)
		{
			AddOrFindColumn(new ColumnWord(SelectQuery, expr));
			return this;
		}

		/// <summary>追加一列（不做去重合并，始终插入新列）。</summary>
		public SelectClause ExprNew(IExpWord expr)
		{
			Columns.content.Add(new ColumnWord(SelectQuery, expr));
			return this;
		}

		/// <summary>追加表达式列并指定别名。</summary>
		public SelectClause Expr(IExpWord expr, string alias)
		{
			AddOrFindColumn(new ColumnWord(SelectQuery, expr, alias));
			return this;
		}

		/// <summary>使用格式化 SQL 片段与参数表达式追加列。</summary>
		public SelectClause Expr(string expr, params IExpWord[] values)
		{
			AddOrFindColumn(new ColumnWord(SelectQuery, new ExpressionWord(null, expr, values)));
			return this;
		}

		/// <summary>使用 CLR 类型与格式化 SQL 片段追加列。</summary>
		public SelectClause Expr(Type systemType, string expr, params IExpWord[] values)
		{
			AddOrFindColumn(new ColumnWord(SelectQuery, new ExpressionWord(systemType, expr, values)));
			return this;
		}

		/// <summary>使用运算符优先级与格式化 SQL 片段追加列。</summary>
		public SelectClause Expr(string expr, int priority, params IExpWord[] values)
		{
			AddOrFindColumn(new ColumnWord(SelectQuery, new ExpressionWord(null, expr, priority, values)));
			return this;
		}

		/// <inheritdoc cref="Expr(string, int, IExpWord[])" />
		public SelectClause Expr(Type systemType, string expr, int priority, params IExpWord[] values)
		{
			AddOrFindColumn(new ColumnWord(SelectQuery, new ExpressionWord(systemType, expr, priority, values)));
			return this;
		}

		/// <summary>追加带别名与优先级的格式化列。</summary>
		public SelectClause Expr(string alias, string expr, int priority, params IExpWord[] values)
		{
			AddOrFindColumn(new ColumnWord(SelectQuery, new ExpressionWord(null, expr, priority, values)));
			return this;
		}

		/// <inheritdoc cref="Expr(string, string, int, IExpWord[])" />
		public SelectClause Expr(Type systemType, string alias, string expr, int priority, params IExpWord[] values)
		{
			AddOrFindColumn(new ColumnWord(SelectQuery, new ExpressionWord(systemType, expr, priority, values)));
			return this;
		}

		/// <summary>追加二元运算列。</summary>
		/// <typeparam name="T">结果 CLR 类型。</typeparam>
		public SelectClause Expr<T>(IExpWord expr1, string operation, IExpWord expr2)
		{
			AddOrFindColumn(new ColumnWord(SelectQuery, new BinaryWord(typeof(T), expr1, operation, expr2)));
			return this;
		}

		/// <summary>追加二元运算列并指定优先级。</summary>
		/// <typeparam name="T">结果 CLR 类型。</typeparam>
		public SelectClause Expr<T>(IExpWord expr1, string operation, IExpWord expr2, int priority)
		{
			AddOrFindColumn(new ColumnWord(SelectQuery, new BinaryWord(typeof(T), expr1, operation, expr2, priority)));
			return this;
		}

		/// <summary>追加带别名的二元运算列。</summary>
		/// <typeparam name="T">结果 CLR 类型。</typeparam>
		public SelectClause Expr<T>(string alias, IExpWord expr1, string operation, IExpWord expr2, int priority)
		{
			AddOrFindColumn(new ColumnWord(SelectQuery, new BinaryWord(typeof(T), expr1, operation, expr2, priority), alias));
			return this;
		}

		/// <summary>追加列（若已存在等价列则跳过）；返回列索引。</summary>
		public int Add(IExpWord expr)
		{
			if (expr is ColumnWord column && column.Parent == SelectQuery)
				throw new InvalidOperationException();

			return AddOrFindColumn(new ColumnWord(SelectQuery, expr));
		}

		/// <summary>追加列并返回 <see cref="ColumnWord"/> 包装。</summary>
		public ColumnWord AddColumn(IExpWord expr)
		{
			return SelectQuery.Select.Columns[Add(expr)];
		}

		/// <summary>始终插入新列（不合并重复）；返回索引。</summary>
		public int AddNew(IExpWord expr, string? alias = default)
		{
			if (expr is ColumnWord column && column.Parent == SelectQuery)
				throw new InvalidOperationException();

			Columns.Add(new ColumnWord(SelectQuery, expr, alias));
			return Columns.Count - 1;
		}

		/// <summary>追加新列并返回 <see cref="ColumnWord"/>。</summary>
		public ColumnWord AddNewColumn(IExpWord expr)
		{
			return Columns[AddNew(expr)];
		}

		/// <summary>追加列（若已存在则合并）并指定别名。</summary>
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
