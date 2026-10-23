using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace mooSQL.linq.clause
{
	using mooSQL.linq.builder;
    using mooSQL.data;
    using mooSQL.data.model;

	using mooSQL.linq.provider;
	/// <summary>
	/// ClauseValidatorVisitor type.
	/// </summary>
	/// <summary>
	/// ClauseValidatorVisitor type.
	/// </summary>
	public class ClauseValidatorVisitor : ClauseVisitor
	{
        SelectQueryClause?     _parentQuery;
        JoinTableWord?  _fakeJoin;
		//SelectQuery?     _joinQuery;
		SQLProviderFlags _providerFlags = default!;
		int?             _columnSubqueryLevel;

		bool    _isValid;
		string? _errorMessage;
		/// <summary>
		/// IsValid
		/// </summary>

		public bool IsValid
		{
			get => _isValid;
		}
		/// <summary>
		/// ErrorMessage
		/// </summary>

		public string? ErrorMessage
		{
			get => _errorMessage;
		}

		private VisitMode VisitingMode;
		/// <summary>
		/// ClauseValidatorVisitor type.
		/// </summary>

		public ClauseValidatorVisitor() 
		{
			this.VisitingMode = VisitMode.ReadOnly;

        }
		/// <summary>
		/// Cleanup
		/// </summary>

		public void Cleanup()
		{
			_parentQuery         = null;
			//_joinQuery           = null;
			_providerFlags       = default!;
			_isValid             = true;
			_columnSubqueryLevel = default;
			_errorMessage        = default!;
		}
		/// <summary>
		/// SetInvalid
		/// </summary>

		public void SetInvalid(string errorMessage)
		{
			_isValid      = false;
			_errorMessage = errorMessage;
		}

		bool IsSubquery(SelectQueryClause selectQuery)
		{
			if (_parentQuery == null)
				return false;
			if (selectQuery == _parentQuery) 
				return false;

			return true;
		}
		/// <summary>
		/// IsValidQuery
		/// </summary>

		public bool IsValidQuery(Clause element,
            SelectQueryClause?                       parentQuery,
            JoinTableWord?                    fakeJoin,
			bool                               forColumn,
			SQLProviderFlags                   providerFlags,
			out string?                        errorMessage)
		{
			_isValid             = true;
			_errorMessage        = default!;
			_parentQuery         = parentQuery;
			_fakeJoin            = fakeJoin;
			_providerFlags       = providerFlags;
			_columnSubqueryLevel = forColumn ? 0 : null;

			Visit(element);

			errorMessage = _errorMessage;

			return IsValid;
		}
		/// <summary>
		/// IsValidSubQuery
		/// </summary>

		public bool IsValidSubQuery(SelectQueryClause selectQuery, [NotNullWhen(false)] out string? errorMessage)
		{
			bool? isDependedOnOuterSources = null;

			bool IsDependsOnOuterSources()
			{
				isDependedOnOuterSources ??= QueryHelper.IsDependsOnOuterSources(selectQuery);

				return isDependedOnOuterSources.Value;
			}

			if (!_providerFlags.IsCorrelatedSubQueryTakeSupported && selectQuery.Select.TakeValue != null)
			{
				if (_columnSubqueryLevel != null && IsDependsOnOuterSources())
				{
					errorMessage = ErrorHelper.Error_Take_in_Correlated_Subquery;
					return false;
				}
			}

			if (_columnSubqueryLevel != null)
			{
				if (_providerFlags.DoesNotSupportCorrelatedSubquery)
				{
					if (IsDependsOnOuterSources())
					{
						var isValied = false;
						if (_providerFlags.IsSupportedSimpleCorrelatedSubqueries && IsSimpleCorrelatedSubquery(selectQuery))
						{
							isValied = true;
						}

						if (!isValied)
						{
							errorMessage = ErrorHelper.Error_Correlated_Subqueries;
							return false;
						}
					}
				}

				if (!_providerFlags.IsSubQueryTakeSupported && selectQuery.Select.TakeValue != null)
				{
					if (_parentQuery?.From.Tables.Count > 0 || IsDependsOnOuterSources())
					{
						errorMessage = ErrorHelper.Error_Take_in_Subquery;
						return false;
					}
				}

				if (!_providerFlags.IsSubQuerySkipSupported && selectQuery.Select.SkipValue != null)
				{
					if (_parentQuery?.From.Tables.Count > 0 || IsDependsOnOuterSources())
					{
						errorMessage = ErrorHelper.Error_Skip_in_Subquery;
						return false;
					}
				}

				if (!_providerFlags.IsSubQueryOrderBySupported && !selectQuery.OrderBy.IsEmpty)
				{
					if (_parentQuery?.From.Tables.Count > 0 || IsDependsOnOuterSources())
					{
						errorMessage = ErrorHelper.Error_OrderBy_in_Subquery;
						return false;
					}
				}

				if (!_providerFlags.IsSubqueryWithParentReferenceInJoinConditionSupported)
				{
					var current = QueryHelper.EnumerateAccessibleSources(selectQuery).ToList();

					foreach (var innerJoin in QueryHelper.EnumerateJoins(selectQuery))
					{
						if (QueryHelper.IsDependsOnOuterSources(innerJoin.Condition, currentSources: current))
						{
							errorMessage = ErrorHelper.Error_Join_ParentReference_Condition;
							return false;
						}
					}
				}

				if (_providerFlags.IsColumnSubqueryShouldNotContainParentIsNotNull)
				{
					if (HasIsNotNullParentReference(selectQuery))
					{
						errorMessage = ErrorHelper.Error_ColumnSubqueryShouldNotContainParentIsNotNull;
						return false;
					}
				}

				var shouldCheckNesting = _columnSubqueryLevel            > 0     && !_providerFlags.IsColumnSubqueryWithParentReferenceSupported
				                         || selectQuery.Select.TakeValue != null && !_providerFlags.IsColumnSubqueryWithParentReferenceAndTakeSupported;

				if (shouldCheckNesting)
				{
					if (IsDependsOnOuterSources())
					{
						errorMessage = ErrorHelper.Error_Correlated_Subqueries;
						return false;
					}
				}

			}
			else
			{
				if (!_providerFlags.IsDerivedTableOrderBySupported && !selectQuery.OrderBy.IsEmpty)
				{
					errorMessage = ErrorHelper.Error_OrderBy_in_Derived;
					return false;
				}
			}

			errorMessage = null;
			return true;
		}

		static bool IsSimpleCorrelatedSubquery(SelectQueryClause selectQuery)
		{
			if (selectQuery.Where.SearchCondition.IsOr)
				return false;

			if (selectQuery.Where.SearchCondition.Predicates.Any((object p) => p is SearchConditionWord))
				return false;

			if (QueryHelper.IsDependsOnOuterSources(selectQuery, elementsToIgnore : new[] { selectQuery.Where }))
				return false;

			return true;
		}

		static bool HasIsNotNullParentReference(SelectQueryClause selectQuery)
		{
			var visitor = new ValidateThatQueryHasNoIsNotNullParentReferenceVisitor();

			visitor.Visit(selectQuery);

			return visitor.ContainsNotNullExpr;
		}

		class ValidateThatQueryHasNoIsNotNullParentReferenceVisitor : SentenceVisitor
		{
			public Stack<ITableNode> _currentSources = new Stack<ITableNode>();

			public ValidateThatQueryHasNoIsNotNullParentReferenceVisitor() : base(VisitMode.ReadOnly, null)
			{
			}

			public bool ContainsNotNullExpr {get; private set; }

			public override Clause VisitTableSource(TableSourceWord element)
			{
				_currentSources.Push(element.Source);

				base.VisitTableSource(element);

				_currentSources.Pop();

				return element;
			}

			public override Clause? Visit(Clause? element)
			{
				if (ContainsNotNullExpr)
					return element;

				return base.Visit(element);
			}

			public override Clause VisitAffirmIsNull(data.model.affirms.IsNull predicate)
			{
				if (predicate.IsNot)
				{
					var para= new List<ITableNode>();
					foreach (var i in _currentSources) {
						para.Add(i);
					}
					if (QueryHelper.IsDependsOnOuterSources(predicate, currentSources : para))
					{
						ContainsNotNullExpr = true;
					}
				}

				return base.VisitAffirmIsNull(predicate);
			}
		}
/// <summary>
/// VisitSearchCondition
/// </summary>
	

		public override Clause VisitSearchCondition(SearchConditionWord element)
		{
			var saveColumnSubqueryLevel = _columnSubqueryLevel;
			_columnSubqueryLevel = null;

			var result = base.VisitSearchCondition(element);

			_columnSubqueryLevel = saveColumnSubqueryLevel;
			return result;
		}
/// <summary>
/// Visit
/// </summary>
	

		public override Clause? Visit(Clause? element)
		{
			if (!IsValid)
				return element;

			return base.Visit(element);
		}
/// <summary>
/// VisitJoinedTable
/// </summary>
	

		public override Clause VisitJoinedTable(JoinTableWord element)
		{
			if (!_providerFlags.IsApplyJoinSupported)
			{
				// No apply joins are allowed
				if (element.JoinType == JoinKind.CrossApply ||
				    element.JoinType == JoinKind.OuterApply ||
				    element.JoinType == JoinKind.FullApply  ||
				    element.JoinType == JoinKind.RightApply)
				{
					if (_providerFlags.DoesNotSupportCorrelatedSubquery)
					{
						SetInvalid(ErrorHelper.Error_Correlated_Subqueries);
					}
					else
					{
						SetInvalid(ErrorHelper.Error_OUTER_Joins);
					}
					return element;
				}
			}

			if (element != _fakeJoin)
			{
				if (!_providerFlags.IsSupportsJoinWithoutCondition && element.JoinType is JoinKind.Left or JoinKind.Inner)
				{
					if (element.Condition.IsTrue() || element.Condition.IsFalse())
					{
						SetInvalid(ErrorHelper.Error_Join_Without_Condition);
						return element;
					}
				}
			}

			if (_providerFlags.IsJoinDerivedTableWithTakeInvalid && element.Table.FindISrc() is SelectQueryClause { Select.TakeValue: not null })
			{
                SetInvalid(ErrorHelper.Error_JoinToDerivedTableWithTakeInvalid);
				return element;
			}

			//_joinQuery = element.Table.Source as SelectQuery;

			var result = base.VisitJoinedTable(element);

			//_joinQuery = null;

			return result;
		}
/// <summary>
/// VisitTableSource
/// </summary>
	

		public override Clause VisitTableSource(TableSourceWord element)
		{
			base.VisitTableSource(element);

			return element;
		}
/// <summary>
/// VisitSelectQuery
/// </summary>
 

        public override Clause VisitSelectQuery(SelectQueryClause selectQuery)
		{
			if (IsSubquery(selectQuery))
			{
				string? errorMessage;
				if (!IsValidSubQuery(selectQuery, out errorMessage))
				{
					SetInvalid(errorMessage);
					return selectQuery;
				}
			}
			else if (selectQuery.Select.SkipValue != null
			         && selectQuery.Select.TakeValue == null
			         && !_providerFlags.GetIsSkipSupportedFlag(null, selectQuery.Select.SkipValue))
			{
				SetInvalid("Provider does not support Skip without Take.");
				return selectQuery;
			}

			var saveParent = _parentQuery;
			_parentQuery = selectQuery;

			base.VisitSelectQuery(selectQuery);

			_parentQuery = saveParent;

			return selectQuery;
		}
/// <summary>
/// VisitFromClause
/// </summary>
	

		public override Clause VisitFromClause(FromClause element)
		{
			if (_columnSubqueryLevel != null)
				_columnSubqueryLevel += 1;

			base.VisitFromClause(element);

			if (_columnSubqueryLevel != null)
				_columnSubqueryLevel -= 1;

			return element;
		}
		/// <summary>
		/// VisitSqlColumnExpression
		/// </summary>

		protected  IExpWord VisitSqlColumnExpression(ColumnWord column, IExpWord expression)
		{
			var saveLevel = _columnSubqueryLevel;

			_columnSubqueryLevel = 0;

			//base.Visit(column, expression);

			_columnSubqueryLevel = saveLevel;

			return expression;
		}
	}
}
