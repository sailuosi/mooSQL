using System;
using System.Collections.Generic;

namespace mooSQL.linq.SqlQuery
{
	using Common;

	using mooSQL.data.model;
    using mooSQL.data.model.affirms;
    using Visitors;

	public class SqlQueryColumnUsageCollector : SqlQueryVisitor
	{
		SelectQueryClause?                _parentSelectQuery;
		readonly HashSet<ColumnWord> _usedColumns = new(Utils.ObjectReferenceEqualityComparer<ColumnWord>.Default);

		public SqlQueryColumnUsageCollector() : base(VisitMode.ReadOnly, null)
		{
		}

		public override void Cleanup()
		{
			base.Cleanup();

			_parentSelectQuery = null;
			_usedColumns.Clear();
		}

		public ICollection<ColumnWord> UsedColumns => _usedColumns;

		public Clause CollectUsedColumns(Clause element)
		{
			Cleanup();
			var result = Visit(element);
			return result;
		}

		void RegisterColumn(ColumnWord column)
		{
			if (!_usedColumns.Add(column))
				return;

			column.Expression.VisitParentFirst(this, (v, e) =>
			{
				if (e is SelectQueryClause selectQuery)
				{
					foreach(var ec in selectQuery.Select.Columns.content)
					{
						_usedColumns.Add(ec);
					}

					return false;
				}

				if (e is ColumnWord c)
				{
					v.RegisterColumn(c);
				}

				return true;
			});
		}

		public override Clause VisitColumnWord(ColumnWord element)
		{
			RegisterColumn(element);

			return base.VisitColumnWord(element);
		}

        public override Clause VisitGroupByClause(GroupByClause element)
		{
			var saveParentQuery = _parentSelectQuery;
			_parentSelectQuery = null;

			var result = base.VisitGroupByClause(element);

			_parentSelectQuery = saveParentQuery;
			return result;
		}

        public override Clause VisitOrderByClause(OrderByClause element)
		{
			var saveParentQuery = _parentSelectQuery;
			_parentSelectQuery = null;

			var result = base.VisitOrderByClause(element);

			_parentSelectQuery = saveParentQuery;
			return result;
		}

        public override Clause VisitSearchCondition(SearchConditionWord element)
		{
			var saveParentQuery = _parentSelectQuery;
			_parentSelectQuery = null;

			var result = base.VisitSearchCondition(element);

			_parentSelectQuery = saveParentQuery;
			return result;
		}

		protected  IExpWord VisitSqlColumnExpression(ColumnWord column, IExpWord expression)
		{
			if (!_usedColumns.Contains(column))
			{
				return expression;
			}

			var saveParentQuery = _parentSelectQuery;

			_parentSelectQuery = null;

			var newExpression =  VisitIExpWord(expression);

			_parentSelectQuery = saveParentQuery;

			return newExpression as IExpWord;
		}

		public override Clause VisitAffirmExprExpr(ExprExpr predicate)
		{
			base.VisitAffirmExprExpr(predicate);

			if (QueryHelper.UnwrapNullablity(predicate.Expr1).NodeType == ClauseType.SqlRow && QueryHelper.UnwrapNullablity(predicate.Expr2) is SelectQueryClause selectQuery2)
			{
				foreach (var column in selectQuery2.Select.Columns.content)
				{
					RegisterColumn(column);
				}
			}

			if (QueryHelper.UnwrapNullablity(predicate.Expr2).NodeType == ClauseType.SqlRow && QueryHelper.UnwrapNullablity(predicate.Expr1) is SelectQueryClause selectQuery1)
			{
				foreach (var column in selectQuery1.Select.Columns.content)
				{
					RegisterColumn(column);
				}
			}

			return predicate;
		}

		public override Clause VisitSqlQuery(SelectQueryClause selectQuery)
		{
			if (_parentSelectQuery == null || selectQuery.HasSetOperators || selectQuery.Select.IsDistinct || selectQuery.From.Tables.Count == 0)
			{
				foreach (var c in selectQuery.Select.Columns.content)
				{
					RegisterColumn(c);
				}
			}
			else
			{
				if (!selectQuery.GroupBy.IsEmpty)
				{
					if (selectQuery.Select.Columns.Count == 1)
						RegisterColumn(selectQuery.Select.Columns[0]);
				}
				else
				{
					foreach (var column in selectQuery.Select.Columns.content)
					{
						if (QueryHelper.ContainsAggregationOrWindowFunction(column.Expression))
						{
							RegisterColumn(column);
							break;
						}
					}
				}
			}

			if (selectQuery.HasSetOperators)
			{
				foreach (var so in selectQuery.SetOperators)
				{
					foreach (var c in so.SelectQuery.Select.Columns.content)
					{
						RegisterColumn(c);
					}
				}
			}

			var saveParentQuery = _parentSelectQuery;
			_parentSelectQuery  = selectQuery;

			base.VisitSqlQuery(selectQuery);

			_parentSelectQuery  = saveParentQuery;

			return selectQuery;
		}

	}
}
