using System.Linq;
using mooSQL.data;
using mooSQL.data.model;
using mooSQL.data.model.affirms;
using mooSQL.linq.Common;
using mooSQL.linq.SqlProvider;
using mooSQL.linq.SqlQuery.Visitors;

namespace mooSQL.linq.SqlQuery
{
	public class SqlQueryOrderByOptimizer : SqlQueryVisitor
	{
		SQLProviderFlags _providerFlags = default!;
		bool             _disableOrderBy;
		bool             _insideSetOperator;
		bool             _optimized;

		public bool IsOptimized => _optimized;

		public SqlQueryOrderByOptimizer() : base(VisitMode.Modify, null)
		{
		}

		public override void Cleanup()
		{
			base.Cleanup();

			_disableOrderBy    = false;
			_insideSetOperator = false;
			_optimized         = false;
			_providerFlags       = default!;
		}

		public void OptimizeOrderBy(Clause element, SQLProviderFlags providerFlags)
		{
			_disableOrderBy    = false;
			_optimized         = false;
			_insideSetOperator = false;
			_providerFlags     = providerFlags;

			ProcessElement(element);
		}

		void CorrectOrderBy(SelectQueryClause selectQuery, bool disable)
		{
			if (!selectQuery.OrderBy.IsEmpty)
			{
				if (!selectQuery.IsLimited)
				{
					if (disable || 
					    selectQuery.Select.Columns.Count > 0 && selectQuery.Select.Columns.content.All(c => QueryHelper.IsAggregationOrWindowFunction(c.Expression))
					   )
					{
						selectQuery.OrderBy.Items.Clear();
						_optimized = true;
					}
				}

				if (!selectQuery.OrderBy.IsEmpty)
				{
					Utils.RemoveDuplicates(selectQuery.OrderBy.Items, item => item.Expression);
				}
			}
		
		}

		public override Clause VisitSetOperator(SetOperatorWord element)
		{
			var saveDisableOrderBy    = _disableOrderBy;
			var saveInsideSetOperator = _insideSetOperator;
			_insideSetOperator = true;
			
			_disableOrderBy = _disableOrderBy                                ||
			                  element.Operation == SetOperation.Except       ||
			                  element.Operation == SetOperation.ExceptAll    ||
			                  element.Operation == SetOperation.Intersect    ||
			                  element.Operation == SetOperation.IntersectAll ||
			                  element.Operation == SetOperation.Union;

			var newElement = base.VisitSetOperator(element);

			_disableOrderBy     = saveDisableOrderBy;
			_insideSetOperator  = saveInsideSetOperator;

			return newElement;
		}

		public override Clause VisitAffirmFuncLike(FuncLike element)
		{
			var saveDisableOrderBy = _disableOrderBy;

			_disableOrderBy = true;

			var newElement = base.VisitAffirmFuncLike(element);

			_disableOrderBy = saveDisableOrderBy;

			return newElement;
		}

        public override Clause VisitJoinedTable(JoinTableWord element)
		{
			var saveDisableOrderBy = _disableOrderBy;

			_disableOrderBy = true;

			var newElement = base.VisitJoinedTable(element);

			_disableOrderBy = saveDisableOrderBy;

			return newElement;
		}

		public override Clause VisitSelectQuery(SelectQueryClause selectQuery)
		{
			var saveDisableOrderBy = _disableOrderBy;
			
			if (selectQuery.HasSetOperators)
			{
				var setOperator = selectQuery.SetOperators[0];
				if (setOperator.Operation == SetOperation.Union     || 
				    setOperator.Operation == SetOperation.Except    || 
				    setOperator.Operation == SetOperation.Intersect || 
				    setOperator.Operation == SetOperation.IntersectAll)
				{
					_disableOrderBy = true;
				}

				var saveInsideSetOperator = _insideSetOperator;
				_insideSetOperator = true;

				Visit(selectQuery.From);

				_insideSetOperator = saveInsideSetOperator;
			}
			else
			{
				Visit(selectQuery.From);
			}

			CorrectOrderBy(selectQuery, _disableOrderBy);

			Visit(selectQuery.Select );
			Visit(selectQuery.Where  );
			Visit(selectQuery.GroupBy);
			Visit(selectQuery.Having );
			Visit(selectQuery.OrderBy);

			if (selectQuery.HasSetOperators)
                selectQuery.SetOperators.VisitElements(VisitMode.Modify,(t)=> VisitSetOperator(t));

			if (selectQuery.HasUniqueKeys)
                selectQuery.UniqueKeys.VisitListOfArrays(VisitMode.Modify,(t)=>VisitIExpWord(t));

            selectQuery.SqlQueryExtensions.VisitElements(VisitMode.Modify,(t)=>VisitQueryExtension(t));

			_disableOrderBy = saveDisableOrderBy;

			return selectQuery;
		}

        public override Clause VisitTableSource(TableSourceWord element)
		{
			var saveDisableOrderBy = _disableOrderBy;

			if (!_insideSetOperator)
			{
				_disableOrderBy = true;
			}

			var newElement = base.VisitTableSource(element);

			_disableOrderBy = saveDisableOrderBy;

			return newElement;
		}

		public override Clause VisitWhereClause(WhereClause element)
		{
			var saveDisableOrderBy = _disableOrderBy;
			_disableOrderBy = false;

			var newElement = base.VisitWhereClause(element);

			_disableOrderBy = saveDisableOrderBy;

			return newElement;
		}

        public override Clause VisitGroupByClause(GroupByClause element)
		{
			var saveDisableOrderBy = _disableOrderBy;
			_disableOrderBy = false;

			var newElement = base.VisitGroupByClause(element);

			_disableOrderBy = saveDisableOrderBy;

			return newElement;
		}

  //      public override Clause VisitColumnWord(ColumnWord column, IExpWord expression)
		//{
		//	var saveDisableOrderBy = _disableOrderBy;
		//	_disableOrderBy = false;

		//	expression = base.VisitColumnWord(column, expression);

		//	_disableOrderBy = saveDisableOrderBy;

		//	return expression;
		//}

        public override Clause VisitCteClause(CTEClause element)
		{
			var saveDisableOrderBy = _disableOrderBy;

			_disableOrderBy = !_providerFlags.IsCTESupportsOrdering;

			var newElement = base.VisitCteClause(element);

			_disableOrderBy = saveDisableOrderBy;

			return newElement;
		}

        public override Clause VisitAffirmInSubQuery(InSubQuery predicate)
		{
			var saveDisableOrderBy = _disableOrderBy;

			_disableOrderBy = true;

			var newElement = base.VisitAffirmInSubQuery(predicate);

			_disableOrderBy = saveDisableOrderBy;

			return newElement;
		}
	}
}
