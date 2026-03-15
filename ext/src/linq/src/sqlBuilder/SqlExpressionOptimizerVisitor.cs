using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace mooSQL.linq.SqlProvider
{
	using Common.Internal;
	using SqlQuery;
	using SqlQuery.Visitors;
	using Mapping;
    using mooSQL.data.model;
    using mooSQL.data.model.affirms;
    using mooSQL.data;

    public class SqlExpressionOptimizerVisitor : SqlQueryVisitor
	{
		EvaluateContext  _evaluationContext  = default!;
		NullabilityContext _nullabilityContext = default!;



		public DBInstance DBLive {  get; set; }

		IAffirmWord?     _allowOptimize;
		bool               _visitQueries;
		bool               _isInsideNot;
		bool               _reduceBinary;

		public SqlExpressionOptimizerVisitor(bool allowModify) : base(allowModify ? VisitMode.Modify : VisitMode.Transform, null)
		{
		}

		public virtual Clause Optimize(
			EvaluateContext           evaluationContext, 
			NullabilityContext          nullabilityContext, 
			IVisitorTransformationInfo? transformationInfo, 
			Clause               element,           
			bool                        visitQueries,       
			bool                        isInsideNot,        
			bool                        reduceBinary)
		{
			Cleanup();
			_evaluationContext  = evaluationContext;
			_nullabilityContext = nullabilityContext;


			_allowOptimize      = default;
			_visitQueries       = visitQueries;
			_isInsideNot        = isInsideNot;
			_reduceBinary       = reduceBinary;
			SetTransformationInfo(transformationInfo);

			return ProcessElement(element);
		}

		public override void Cleanup()
		{
			base.Cleanup();
			_visitQueries       = default;
			_isInsideNot        = default;
			_evaluationContext  = default!;
			_nullabilityContext = default!;

			_allowOptimize      = default;
		}

		[return: NotNullIfNotNull(nameof(element))]
		public override Clause? Visit(Clause? element)
		{
			if (element == null)
				return element;

			var newElement = base.Visit(element);

			return newElement;
		}

		#region Helper functions

		protected bool CanBeEvaluateNoParameters(ISQLNode expr)
		{
			if (expr.HasQueryParameter())
			{
				return false;
			}

			return expr.CanBeEvaluated(_evaluationContext);
		}

		protected bool TryEvaluateNoParameters(ISQLNode expr, out object? result)
		{
			if (expr.HasQueryParameter())
			{
				result = null;
				return false;
			}

			return TryEvaluate(expr, out result);
		}

		protected bool TryEvaluate(ISQLNode expr, out object? result)
		{
			if (expr.TryEvaluateExpression(_evaluationContext, out result))
				return true;

			return false;
		}

		#endregion

		public override Clause VisitAffirmIsTrue(IsTrue predicate)
		{
			var newElement = base.VisitAffirmIsTrue(predicate);

			if (!ReferenceEquals(newElement, predicate))
				return Visit(newElement);

			var optimized = OptimizeIsTruePredicate(predicate);
			if (!ReferenceEquals(optimized, predicate))
				return Visit(optimized as Clause);

			return predicate;
		}

		public override Clause VisitConditionExpression(ConditionWord element)
		{
			var saveAllowOptimize = _allowOptimize;
			_allowOptimize = element.Condition;

			var newExpr = base.VisitConditionExpression(element);

			_allowOptimize = saveAllowOptimize;

			if (!ReferenceEquals(newExpr, element))
				return Visit(newExpr);

			if (TryEvaluate(element.Condition, out var value) && value is bool boolValue)
			{
				return boolValue ? element.TrueValue as Clause : element.FalseValue as Clause;
			}

			if (element.TrueValue is ConditionWord trueConditional)
			{
				if (trueConditional.Condition.Equals(element.Condition, ExpressionWord.DefaultComparer))
				{
					var newConditionExpression = new ConditionWord(element.Condition, trueConditional.TrueValue, element.FalseValue);
					return Visit(newConditionExpression);
				}
			}

			if (element.FalseValue is ConditionWord falseConditional)
			{
				var newCaseExpression = new CaseWord(QueryHelper.GetDbDataType(element.TrueValue, DBLive),
					new CaseWord.CaseItem[]
					{
						new(element.Condition, element.TrueValue),
						new(falseConditional.Condition, falseConditional.TrueValue),
					}, falseConditional.FalseValue);

				return Visit(newCaseExpression);
			}

			if (element.FalseValue is CaseWord falseCase)
			{
				var caseItems = new List<CaseWord.CaseItem>(falseCase.Cases.Count + 1)
				{
					new(element.Condition, element.TrueValue),
				};

				caseItems.AddRange(falseCase.Cases);

				var newCaseExpression = new CaseWord(falseCase.Type, caseItems, falseCase.ElseExpression);

				return Visit(newCaseExpression);
			}

			if (element.Condition is IsNull isNullPredicate)
			{
				var unwrapped = QueryHelper.UnwrapNullablity(isNullPredicate.Expr1);

				if (isNullPredicate.IsNot)
				{
					if (unwrapped.Equals(element.TrueValue, ExpressionWord.DefaultComparer) && element.FalseValue is ValueWord { Value: null })
					{
						return isNullPredicate.Expr1 as Clause;
					}
				}
				else if (unwrapped.Equals(element.FalseValue, ExpressionWord.DefaultComparer) && element.TrueValue is ValueWord { Value: null })
				{
					return isNullPredicate.Expr1 as Clause;
				}

			}

			return element;
		}

		protected CaseWord.CaseItem VisitCaseItem(CaseWord.CaseItem element)
		{
            var newElement = element.Update((IAffirmWord)VisitAffirmWord(element.Condition), (IExpWord)VisitIExpWord(element.ResultExpression));


            if (TryEvaluate(newElement.Condition, out var result) && result is bool boolValue)
			{
				return new CaseWord.CaseItem(AffirmWord.MakeBool(boolValue), newElement.ResultExpression);
			}

			return newElement;
		}

		public override Clause VisitCaseExpression(CaseWord element)
		{
			var newExpr = base.VisitCaseExpression(element);

			if (!ReferenceEquals(newExpr, element))
				return Visit(newExpr);

			if (GetVisitMode(element) == VisitMode.Modify)
			{
				for (int i = 0; i < element._cases.Count; i++)
				{
					var caseItem = element._cases[i];
					if (caseItem.Condition == AffirmWord.True)
					{
						element._cases.RemoveRange(i, element._cases.Count - i);
						element.Modify(caseItem.ResultExpression);
						break;
					}

					if (caseItem.Condition == AffirmWord.False)
					{
						element._cases.RemoveAt(i);
						--i;
					}
				}
			}
			else
			{
				for (int i = 0; i < element._cases.Count; i++)
				{
					var caseItem = element._cases[i];
					if (caseItem.Condition == AffirmWord.True)
					{
						var newCases = new List<CaseWord.CaseItem>(element._cases.Count - i);
						newCases.AddRange(element._cases.Take(i));

						var newCaseExpression = new CaseWord(element.Type, newCases, caseItem.ResultExpression);
						NotifyReplaced(newCaseExpression, element);

						return Visit(newCaseExpression);
					}

					if (caseItem.Condition == AffirmWord.False)
					{
						var newCases = new List<CaseWord.CaseItem>(element._cases.Count);
						newCases.AddRange(element._cases);

						newCases.RemoveAt(i);

						var newCaseExpression = new CaseWord(element.Type, newCases, element.ElseExpression);
						NotifyReplaced(newCaseExpression, element);

						return Visit(newCaseExpression);
					}
				}
			}

			if (element.Cases.Count == 1)
			{
				var conditionExpression = new ConditionWord(element.Cases[0].Condition, element.Cases[0].ResultExpression, element.ElseExpression ?? new ValueWord(element.Type, null));
				return Visit(conditionExpression);
			}

			if (element.Cases.Count == 0)
			{
				return element.ElseExpression as Clause ?? new ValueWord(element.Type, null) ;
			}

			return element;
		}

		public override Clause VisitSearchCondition(SearchConditionWord element)
		{
			var newElement = base.VisitSearchCondition(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			if (ReferenceEquals(_allowOptimize, element) && element.Predicates.Count == 1)
				return element.Predicates[0] as Clause;

			if (GetVisitMode(element) == VisitMode.Modify)
			{
				for (var i = 0; i < element.Predicates.Count; i++)
				{
					var predicate = element.Predicates[i];
					// unnesting search conditions
					//
					if (predicate is SearchConditionWord sc && (sc.IsOr == element.IsOr || sc.Predicates.Count <= 1))
					{
						element.Predicates.RemoveAt(i);
						element.Predicates.InsertRange(i, sc.Predicates);
						--i;
						continue;
					}

					if (TryEvaluate(predicate, out var value) &&
					    value is bool boolValue)
					{
						if (boolValue)
						{
							if (element.IsAnd)
							{
								// ignore
								if (element.Predicates.Count == 1 && predicate is TrueAffirm)
									break;

								element.Predicates.RemoveAt(i);

								if (element.Predicates.Count == 0)
									element.Predicates.Add(AffirmWord.True);

								continue;
							}

							if (element.Predicates.Count > 1 || predicate is not TrueAffirm)
							{
								element.Predicates.Clear();
								element.Predicates.Add(AffirmWord.True);
								break;
							}
						}
						else
						{
							if (element.IsOr)
							{
								// ignore
								if (element.Predicates.Count == 1 && predicate is FalseAffirm)
									break;

								element.Predicates.RemoveAt(i);
								if (element.Predicates.Count == 0)
									element.Predicates.Add(AffirmWord.False);

								continue;
							}

							if (element.Predicates.Count > 1 || predicate is not FalseAffirm)
							{
								element.Predicates.Clear();
								element.Predicates.Add(AffirmWord.False);
								break;
							}
						}
					}

				}
			}
			else
			{
				List<IAffirmWord>? newPredicates = null;

				void EnsureCopied(int count)
				{
					if (newPredicates == null)
					{
						newPredicates = new List<IAffirmWord>(element.Predicates.Count);
						newPredicates.AddRange(element.Predicates.Take(count));
					}
				}

				void EnsureCleared()
				{
					if (newPredicates == null)
					{
						newPredicates = new List<IAffirmWord>();
					}
					else
					{
						newPredicates.Clear();
					}
				}

				for (var i = 0; i < element.Predicates.Count; i++)
				{
					var predicate = element.Predicates[i];
					// unnesting search conditions
					//
					if (predicate is SearchConditionWord sc && (sc.IsOr == element.IsOr || sc.Predicates.Count <= 1))
					{
						EnsureCopied(i);
						newPredicates!.InsertRange(i, sc.Predicates);
						continue;
					}

					if (TryEvaluate(predicate, out var value) &&
					    value is bool boolValue)
					{
						if (boolValue)
						{
							if (element.IsAnd)
							{
								if (element.Predicates.Count == 1 && predicate is TrueAffirm)
									continue;

								// ignore
								EnsureCopied(i);

								if (element.Predicates.Count == 1)
									newPredicates!.Add(AffirmWord.True);

								continue;
							}

							if (element.Predicates.Count > 1 || predicate is not TrueAffirm)
							{
								EnsureCleared();
								newPredicates!.Add(AffirmWord.True);
								break;
							}
						}
						else
						{
							if (element.IsOr)
							{
								if (element.Predicates.Count == 1 && predicate is FalseAffirm)
									continue;

								// ignore
								EnsureCopied(i);

								if (element.Predicates.Count == 1)
									newPredicates!.Add(AffirmWord.False);

								continue;
							}

							if (element.Predicates.Count > 1 || predicate is not FalseAffirm)
							{
								EnsureCleared();
								newPredicates!.Add(AffirmWord.False);
								break;
							}
						}
					}

					newPredicates?.Add(predicate);
				}

				if (newPredicates != null)
				{
					newElement = new SearchConditionWord(element.IsOr, newPredicates);
					NotifyReplaced(newElement, element);

					return newElement;
				}

			}

			return element;
		}

		public override Clause VisitAffirmIsDistinct(IsDistinct predicate)
		{
			var newElement = base.VisitAffirmIsDistinct(predicate);

			if (!ReferenceEquals(newElement, predicate))
				return Visit(newElement);

			if (_nullabilityContext.IsEmpty)
				return predicate;

			// Here, several optimisations would already have occured:
			// - If both expressions could be evaluated, Sql.IsDistinct would have been evaluated client-side.
			// - If both expressions could not be null, an Equals expression would have been used instead.

			// The only remaining case that we'd like to simplify is when one expression is the constant null.
			if (TryEvaluate(predicate.Expr1, out var value1) && value1 == null)
			{
				return new IsNull(predicate.Expr2, !predicate.IsNot);
			}

			if (TryEvaluate(predicate.Expr2, out var value2) && value2 == null)
			{
				return new IsNull(predicate.Expr1, !predicate.IsNot);
			}

			return predicate;
		}

		public override Clause VisitSqlQuery(SelectQueryClause selectQuery)
		{
			var saveInsideNot = _isInsideNot;
			_isInsideNot = false;

			var result = base.VisitSqlQuery(selectQuery);

			if (!ReferenceEquals(result, selectQuery))
			{
				_nullabilityContext.RegisterReplacement(selectQuery, (SelectQueryClause)result);
			}

			_isInsideNot = saveInsideNot;

			return result;
		}

		public override Clause VisitTableSource(TableSourceWord element)
		{
			if (!_visitQueries)
				return element;

			return base.VisitTableSource(element);
		}

		public override Clause VisitAffirmNot(Not predicate)
		{
			if (predicate.Predicate.CanInvert(_nullabilityContext))
			{
				return Visit(predicate.Predicate.Invert(_nullabilityContext) as Clause);
			}

			var saveInsideNot = _isInsideNot;
			var saveAllow     = _allowOptimize;

			_isInsideNot     = true;
			_allowOptimize = predicate.Predicate;
			var newInnerPredicate = (IAffirmWord)Visit(predicate.Predicate as Clause);
			_isInsideNot     = saveInsideNot;
			_allowOptimize = saveAllow;

			if (newInnerPredicate.CanInvert(_nullabilityContext))
			{
				return Visit(newInnerPredicate.Invert(_nullabilityContext) as Clause);
			}

			if (!ReferenceEquals(newInnerPredicate, predicate.Predicate))
			{
				if (GetVisitMode(predicate) == VisitMode.Transform)
				{
					return new Not(newInnerPredicate);
				}

				predicate.Modify(newInnerPredicate);
			}

			return predicate;
		}

		public override Clause VisitBinaryExpression(BinaryWord element)
		{
			var newElement = base.VisitBinaryExpression(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			newElement = element switch
			{
				(var e, "+", BinaryWord { Operation: "*", Expr1: ValueWord { Value: -1 } } binary) => new BinaryWord(element.SystemType!, e, "-", binary.Expr2, PrecedenceLv.Subtraction),
				(var e, "+", BinaryWord { Operation: "*", Expr2: ValueWord { Value: -1 } } binary) => new BinaryWord(e.SystemType!, e, "-", binary.Expr1, PrecedenceLv.Subtraction),
				(var e, "-", BinaryWord { Operation: "*", Expr1: ValueWord { Value: -1 } } binary) => new BinaryWord(element.SystemType!, e, "+", binary.Expr2, PrecedenceLv.Subtraction),
				(var e, "-", BinaryWord { Operation: "*", Expr2: ValueWord { Value: -1 } } binary) => new BinaryWord(e.SystemType!, e, "+", binary.Expr1, PrecedenceLv.Subtraction),

				_ => element
			};

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			if (TryEvaluateNoParameters(element, out var evaluatedValue))
				return new ValueWord(element.SystemType, evaluatedValue);

			switch (element.Operation)
			{
				case "+":
				{
					var v1 = TryEvaluateNoParameters(element.Expr1, out var value1);
					if (v1)
					{
						switch (value1)
						{
							case short   h when h == 0  :
							case int     i when i == 0  :
							case long    l when l == 0  :
							case decimal d when d == 0  :
							case string  s when s.Length == 0: return element.Expr2 as Clause;
						}
					}

					var v2 = TryEvaluateNoParameters(element.Expr2, out var value2);
					if (v2)
					{
						switch (value2)
						{
							case int vi when vi == 0 : return element.Expr1 as Clause;
							case int vi when
								element.Expr1    is BinaryWord be1                             &&
								TryEvaluateNoParameters(be1.Expr2, out var be1v2) &&
								                        be1v2 is int be1v2i :
							{
								switch (be1.Operation)
								{
									case "+":
									{
										var value = be1v2i + vi;
										var oper  = be1.Operation;

										if (value < 0)
										{
											value = -value;
											oper  = "-";
										}

										return new BinaryWord(element.SystemType, be1.Expr1, oper, QueryHelper.CreateSqlValue(value, element, DBLive), element.Precedence);
									}

									case "-":
									{
										var value = be1v2i - vi;
										var oper  = be1.Operation;

										if (value < 0)
										{
											value = -value;
											oper  = "+";
										}

										return new BinaryWord(element.SystemType, be1.Expr1, oper, QueryHelper.CreateSqlValue(value, element, DBLive), element.Precedence);
									}
								}

								break;
							}

							case string vs when vs.Length == 0 : return element.Expr1 as Clause;
							case string vs when
								element.Expr1    is BinaryWord be1 &&
								//be1.Operation == "+"                   &&
								TryEvaluateNoParameters(be1.Expr2, out var be1v2) &&
								be1v2 is string be1v2s :
							{
								return new BinaryWord(
									be1.SystemType,
									be1.Expr1,
									be1.Operation,
									new ValueWord(string.Concat(be1v2s, vs)));
							}
						}
					}

					if (v1 && v2)
					{
						if (value1 is int i1 && value2 is int i2) return QueryHelper.CreateSqlValue(i1 + i2, element, DBLive) as Clause;
						if (value1 is string || value2 is string) return QueryHelper.CreateSqlValue($"{value1}{value2}", element, DBLive) as Clause;
					}

					break;
				}

				case "-":
				{
					var v2 = TryEvaluateNoParameters(element.Expr2, out var value2);
					if (v2)
					{
						switch (value2)
						{
							case int vi when vi == 0 : return element.Expr1 as Clause;
							case int vi when
								element.Expr1 is BinaryWord be1 &&
								TryEvaluateNoParameters(be1.Expr2, out var be1v2) &&
								be1v2 is int be1v2i :
							{
								switch (be1.Operation)
								{
									case "+":
									{
										var value = be1v2i - vi;
										var oper  = be1.Operation;

										if (value < 0)
										{
											value = -value;
											oper  = "-";
										}

										return new BinaryWord(element.SystemType, be1.Expr1, oper, QueryHelper.CreateSqlValue(value, element, DBLive), element.Precedence);
									}

									case "-":
									{
										var value = be1v2i + vi;
										var oper  = be1.Operation;

										if (value < 0)
										{
											value = -value;
											oper  = "+";
										}

										return new BinaryWord(element.SystemType, be1.Expr1, oper, QueryHelper.CreateSqlValue(value, element, DBLive), element.Precedence);
									}
								}

								break;
							}
						}
					}

					if (v2 && TryEvaluateNoParameters(element.Expr1, out var value1))
					{
						if (value1 is int i1 && value2 is int i2) return QueryHelper.CreateSqlValue(i1 - i2, element, DBLive) as Clause;
					}

					break;
				}

				case "*":
				{
					var v1 = TryEvaluateNoParameters(element.Expr1, out var value1);
					if (v1)
					{
						switch (value1)
						{
							case int i when i == 0 : return QueryHelper.CreateSqlValue(0, element, DBLive) as Clause;
							case int i when i == 1 : return element.Expr2 as Clause;
							case int i when
								element.Expr2    is BinaryWord be2 &&
								be2.Operation == "*"                   &&
								TryEvaluateNoParameters(be2.Expr1, out var be2v1)  &&
								be2v1 is int bi :
							{
								return new BinaryWord(be2.SystemType, QueryHelper.CreateSqlValue(i * bi, element, DBLive), "*", be2.Expr2);
							}
						}
					}

					var v2 = TryEvaluateNoParameters(element.Expr2, out var value2);
					if (v2)
					{
						switch (value2)
						{
							case int i when i == 0 : return QueryHelper.CreateSqlValue(0, element, DBLive) as Clause;
							case int i when i == 1 : return element.Expr1 as Clause;
						}
					}

					if (v1 && v2)
					{
						switch (value1)
						{
							case int    i1 when value2 is int    i2 : return QueryHelper.CreateSqlValue(i1 * i2, element, DBLive) as Clause;
							case int    i1 when value2 is double d2 : return QueryHelper.CreateSqlValue(i1 * d2, element, DBLive) as Clause;
							case double d1 when value2 is int    i2 : return QueryHelper.CreateSqlValue(d1 * i2, element, DBLive) as Clause;
							case double d1 when value2 is double d2 : return QueryHelper.CreateSqlValue(d1 * d2, element, DBLive) as Clause;
						}
					}

					break;
				}
			}

			return element;
		}

		public override Clause VisitCastExpression(CastWord element)
		{
			if (!element.IsMandatory)
			{
				var from = element.FromType?.Type ?? QueryHelper.GetDbDataType(element.Expression, DBLive);

				if (element.SystemType == typeof(object) || from.EqualsDbOnly(element.Type))
					return element.Expression as Clause;

				if (element.Expression is CastWord { IsMandatory: false } castOther)
				{
					var dbType = QueryHelper.GetDbDataType(castOther.Expression, DBLive);
					if (element.Type.EqualsDbOnly(dbType))
						return castOther.Expression as Clause;
				}
			}

			if (element.Expression is SelectQueryClause selectQuery && selectQuery.Select.Columns.Count == 1)
			{
				if (GetVisitMode(selectQuery) == VisitMode.Modify)
				{
					var columnExpression = selectQuery.Select.Columns[0].Expression;
					selectQuery.Select.Columns[0].Expression = (IExpWord)Visit(new CastWord(columnExpression, element.ToType, element.FromType, isMandatory: element.IsMandatory));

					return selectQuery;
				}
			}

			return base.VisitCastExpression(element);
		}

		public override Clause VisitAffirmFuncLike(FuncLike element)
		{
			var funcElement = Visit(element.Function);

			if (ReferenceEquals(funcElement, element.Function))
				return element;

			if (funcElement is AffirmWord)
				return funcElement;

			if (funcElement is FunctionWord function)
				return element.Update(function);

			throw new InvalidCastException($"Converted FuncLikePredicate expression expected to be a Predicate expression but got {funcElement.GetType()}.");
		}

		public override Clause VisitFunctionWord(FunctionWord element)
		{
			var newElement = base.VisitFunctionWord(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			if (element.DoNotOptimize)
				return element;

			if (TryEvaluate(element, out var value))
			{
				return QueryHelper.CreateSqlValue(value, QueryHelper.GetDbDataType(element, DBLive), element.Parameters) as Clause;
			}

			switch (element.Name)
			{
				case PseudoFunctions.COALESCE:
				{
					var parms = element.Parameters;
					if (parms.Length == 2)
					{
						if (parms[0] is ValueWord val1 && parms[1] is not ValueWord)
							return new FunctionWord(element.SystemType, element.Name, element.IsAggregate, element.Precedence, QueryHelper.CreateSqlValue(val1.Value, QueryHelper.GetDbDataType(parms[1], DBLive), parms[0]), parms[1])
							{
								DoNotOptimize = true,
								CanBeNull     = element.CanBeNull
							};
						else if (parms[1] is ValueWord val2 && parms[0] is not ValueWord)
							return new FunctionWord(element.SystemType, element.Name, element.IsAggregate, element.Precedence, parms[0], QueryHelper.CreateSqlValue(val2.Value, QueryHelper.GetDbDataType(parms[0], DBLive), parms[1]))
							{
								DoNotOptimize = true,
								CanBeNull     = element.CanBeNull
							};
					}

					break;
				}

				case "EXISTS":
				{
					if (element.Parameters.Length == 1 && element.Parameters[0] is SelectQueryClause query && query.Select.Columns.Count > 0)
					{
						if (query.GroupBy.IsEmpty)
						{
							var isAggregateQuery = query.Select.Columns.content.All(static c => QueryHelper.IsAggregationOrWindowFunction(c.Expression));

							if (isAggregateQuery)
								return AffirmWord.True;
						}
					}

					break;
				}
			}

			return element;
		}

		public override Clause VisitAffirmIsNull(IsNull predicate)
		{
			var newPredicate = base.VisitAffirmIsNull(predicate);

			if (!ReferenceEquals(newPredicate, predicate))
				return Visit(newPredicate);

			if (_nullabilityContext.IsEmpty)
				return predicate;

			if (!predicate.Expr1.CanBeNullable(_nullabilityContext))
			{
				return ClauseExtensions.MakeBool(predicate.IsNot);
			}

			if (TryEvaluate(predicate.Expr1, out var value))
			{
				return ClauseExtensions.MakeBool((value == null) != predicate.IsNot);
			}

			var unwrapped = QueryHelper.UnwrapNullablity(predicate.Expr1);
			if (unwrapped is BinaryWord binaryExpression)
			{
				IAffirmWord? result = null;

				if (binaryExpression.Operation is "+" or "-" or "*" or "/" or "%" or "&")
				{
					if (binaryExpression.Expr1.CanBeNullable(_nullabilityContext) && !binaryExpression.Expr2.CanBeNullable(_nullabilityContext))
					{
						result = new IsNull(NullabilityWord.ApplyNullability(binaryExpression.Expr1, true), predicate.IsNot);
					}
					else if (binaryExpression.Expr2.CanBeNullable(_nullabilityContext) && !binaryExpression.Expr1.CanBeNullable(_nullabilityContext))
					{
						result = new IsNull(NullabilityWord.ApplyNullability(binaryExpression.Expr2, true), predicate.IsNot);
					}
				}

				if (result != null)
					return Visit(result as Clause) ;
			}

			return predicate;
		}

		public override Clause VisitNullabilityExpression(NullabilityWord element)
		{
			var newNode = base.VisitNullabilityExpression(element);

			if (!ReferenceEquals(newNode, element))
				return Visit(newNode);

			if (element.SqlExpression is NullabilityWord nullabilityExpression)
			{
				return NullabilityWord.ApplyNullability(nullabilityExpression.SqlExpression,
					element.CanBeNullable(_nullabilityContext)) as Clause;
			}

			if (element.SqlExpression is SearchConditionWord)
			{
				return element.SqlExpression as Clause;
			}

			return element;
		}

		public override Clause VisitAffirmExpr(Expr predicate)
		{
			var result = base.VisitAffirmExpr(predicate);

			if (!ReferenceEquals(result, predicate))
				return Visit(result);

			if (predicate.Expr1 is IAffirmWord inner)
				return inner as Clause;

			return predicate;
		}

		public override Clause VisitAffirmExprExpr(ExprExpr predicate)
		{
			var newElement = base.VisitAffirmExprExpr(predicate);

			if (!ReferenceEquals(newElement, predicate))
				return Visit(newElement);

			if (TryEvaluateNoParameters(predicate, out var value) && value is bool boolValue)
			{
				return ClauseExtensions.MakeBool(boolValue);
			}

			if (_reduceBinary)
			{
				var reduced = predicate.Reduce(_nullabilityContext, _evaluationContext, _isInsideNot);

				if (!ReferenceEquals(reduced, predicate))
				{
					return Visit(reduced as Clause);
				}
			}

			var expr = predicate;

			if (expr.Operator is AffirmWord.Operator.Equal or AffirmWord.Operator.NotEqual)
			{
				if (expr.WithNull == null)
				{
					if (expr.Expr2 is IAffirmWord expr2Predicate)
					{
						var boolValue1 = QueryHelper.GetBoolValue(expr.Expr1, _evaluationContext);
						if (boolValue1 != null)
						{
							var isNot       = boolValue1.Value != (expr.Operator == AffirmWord.Operator.Equal);
							var transformed = expr2Predicate.MakeNot(isNot);

							return transformed as Clause;
						}
					}

					if (expr.Expr1 is IAffirmWord expr1Predicate)
					{
						var boolValue2 = QueryHelper.GetBoolValue(expr.Expr2, _evaluationContext);
						if (boolValue2 != null)
						{
							var isNot       = boolValue2.Value != (expr.Operator == AffirmWord.Operator.Equal);
							var transformed = expr1Predicate.MakeNot(isNot);
							return transformed as Clause;
						}
					}
				}

				if (QueryHelper.UnwrapNullablity(predicate.Expr1) is ValueWord { Value: null })
				{
					return Visit(new IsNull(predicate.Expr2, expr.Operator == AffirmWord.Operator.NotEqual));
				}

				if (QueryHelper.UnwrapNullablity(predicate.Expr2) is ValueWord { Value: null })
				{
					return Visit(new IsNull(predicate.Expr1, expr.Operator == AffirmWord.Operator.NotEqual));
				}

			}

			switch (expr.Operator)
			{
				case AffirmWord.Operator.Equal          :
				case AffirmWord.Operator.NotEqual       :
				case AffirmWord.Operator.Greater        :
				case AffirmWord.Operator.GreaterOrEqual :
				case AffirmWord.Operator.Less           :
				case AffirmWord.Operator.LessOrEqual    :
				{
					var newPredicate = OptimizeExpExprPredicate(expr);
					if (!ReferenceEquals(newPredicate, expr))
						return Visit(newPredicate as Clause);

					break;
				}
			}

			return predicate;
		}

		public override Clause VisitWhereClause(WhereClause element)
		{
			var newElement = base.VisitWhereClause(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			// Ensure top level is always AND

			if (element.SearchCondition.IsOr)
			{
				var p = element.SearchCondition.Predicates;

				SearchConditionWord newSearchCondition;
				if (p.Count == 0) {
					newSearchCondition = new SearchConditionWord(false);

				}
				else if (p.Count == 1)
				{
					newSearchCondition = new SearchConditionWord(false, p[0]);

				}
				else {
					newSearchCondition = new SearchConditionWord(false, element.SearchCondition);

                }


				if (GetVisitMode(element) == VisitMode.Modify)
				{
					element.SearchCondition = newSearchCondition;
				}
				else
				{
					return new WhereClause(newSearchCondition);
				}
			}

			return element;
		}

		public override Clause VisitAffirmInSubQuery(InSubQuery predicate)
		{
			var optmimized = base.VisitAffirmInSubQuery(predicate);

			if (!ReferenceEquals(optmimized, predicate))
				return Visit(optmimized);

			var p = predicate.SubQuery.Where.SearchCondition.Predicates;

            if (p.Count==1 && p[0] is FalseAffirm firstPredicate)
			{
				return firstPredicate.MakeNot(predicate.IsNot) as Clause;
			}

			return predicate;
		}

		public override Clause VisitAffirmInList(InList predicate)
		{
			var newElement = base.VisitAffirmInList(predicate);

			if (!ReferenceEquals(newElement, predicate))
				return Visit(newElement);

			if (_evaluationContext.ParameterValues == null)
			{
				return predicate;
			}

			if (predicate.Values.Count==1 && predicate.Values[0] is ParameterWord valuesParam && _evaluationContext.ParameterValues!.TryGetValue(valuesParam, out var parameterValue))
			{
				switch (parameterValue.ProviderValue)
				{
					case null:
						return ClauseExtensions.MakeBool(predicate.IsNot);

					// Be careful that string is IEnumerable, we don't want to handle x.In(string) here
					case string:
						break;
					case IEnumerable items:
					{
						if (predicate.Expr1 is not ITableNode)
						{
							bool noValues = !items.Cast<object?>().Any();
							if (noValues)
								return ClauseExtensions.MakeBool(predicate.IsNot);
						}

						break;
					}
				}
			}

			return predicate;
		}

		#region OptimizeExpExprPredicate

		static bool Compare(int v1, int v2, AffirmWord.Operator op)
		{
			switch (op)
			{
				case AffirmWord.Operator.Equal:          return v1 == v2;
				case AffirmWord.Operator.NotEqual:       return v1 != v2;
				case AffirmWord.Operator.Greater:        return v1 >  v2;
				case AffirmWord.Operator.NotLess:
				case AffirmWord.Operator.GreaterOrEqual: return v1 >= v2;
				case AffirmWord.Operator.Less:           return v1 <  v2;
				case AffirmWord.Operator.NotGreater:
				case AffirmWord.Operator.LessOrEqual:    return v1 <= v2;
			}

			throw new InvalidOperationException();
		}

		static bool Compare(object? value1, object? value2, AffirmWord.Operator op, out bool result)
		{
			if (op is AffirmWord.Operator.NotEqual)
			{
				if (value1 is null && value2 is not null)
				{
					result = false;
					return true;
				}

				if (value2 is null && value1 is not null)
				{
					result = false;
					return true;
				}
			}

			if (value1 is IComparable comparable1 && value1.GetType() == value2?.GetType())
			{
				switch (op)
				{
					case AffirmWord.Operator.Equal:          result = comparable1.CompareTo(value2) == 0; break;
					case AffirmWord.Operator.NotEqual:       result = comparable1.CompareTo(value2) != 0; break;
					case AffirmWord.Operator.Greater:        result = comparable1.CompareTo(value2) >  0; break;
					case AffirmWord.Operator.GreaterOrEqual: result = comparable1.CompareTo(value2) >= 0; break;
					case AffirmWord.Operator.Less:           result = comparable1.CompareTo(value2) <  0; break;
					case AffirmWord.Operator.LessOrEqual:    result = comparable1.CompareTo(value2) <= 0; break;

					default:
					{
						result = false;
						return false;
					}
				}

				return true;
			}

			result = false;
			return false;
		}

		static void CombineOperator(ref AffirmWord.Operator? current, AffirmWord.Operator additional)
		{
			if (current == null)
			{
				current = additional;
				return;
			}

			if (current == additional)
				return;

			if (current == AffirmWord.Operator.Equal && additional == AffirmWord.Operator.Greater)
				current = AffirmWord.Operator.GreaterOrEqual;
			else if (current == AffirmWord.Operator.Equal && additional == AffirmWord.Operator.Less)
				current = AffirmWord.Operator.LessOrEqual;
			else if (current == AffirmWord.Operator.Greater && additional == AffirmWord.Operator.Equal)
				current = AffirmWord.Operator.GreaterOrEqual;
			else if (current == AffirmWord.Operator.Less && additional == AffirmWord.Operator.Equal)
				current = AffirmWord.Operator.LessOrEqual;
			else if (current == AffirmWord.Operator.Greater && additional == AffirmWord.Operator.Less)
				current = AffirmWord.Operator.NotEqual;
			else if (current == AffirmWord.Operator.Less && additional == AffirmWord.Operator.Greater)
				current = AffirmWord.Operator.NotEqual;
			else
				throw new NotImplementedException();
		}

		static AffirmWord.Operator SwapOperator(AffirmWord.Operator op)
		{
			return op switch
			{
				AffirmWord.Operator.Equal => op,
				AffirmWord.Operator.NotEqual => op,
				AffirmWord.Operator.Greater => AffirmWord.Operator.Less,
				AffirmWord.Operator.NotLess => AffirmWord.Operator.NotGreater,
				AffirmWord.Operator.GreaterOrEqual => AffirmWord.Operator.LessOrEqual,
				AffirmWord.Operator.Less => AffirmWord.Operator.Greater,
				AffirmWord.Operator.NotGreater => AffirmWord.Operator.NotLess,
				AffirmWord.Operator.LessOrEqual => AffirmWord.Operator.GreaterOrEqual,
				_ => throw new InvalidOperationException()
			};
		}

		IAffirmWord? ProcessComparisonWithCase(IExpWord other, IExpWord valueExpression, AffirmWord.Operator op)
		{
			var unwrappedOther = QueryHelper.UnwrapNullablity(other);
			var unwrappedValue = QueryHelper.UnwrapNullablity(valueExpression);

			var isNot = op == AffirmWord.Operator.NotEqual;

			if (unwrappedOther is ConditionWord sqlConditionExpression)
			{
				if (op is AffirmWord.Operator.Equal or AffirmWord.Operator.NotEqual)
				{
					if (sqlConditionExpression.TrueValue.Equals(unwrappedValue))
						return sqlConditionExpression.Condition.MakeNot(isNot);

					if (sqlConditionExpression.FalseValue.Equals(unwrappedValue))
						return sqlConditionExpression.Condition.MakeNot(!isNot);
				}

				if (TryEvaluateNoParameters(unwrappedValue, out _))
				{
					if (TryEvaluateNoParameters(sqlConditionExpression.TrueValue, out _) || TryEvaluateNoParameters(sqlConditionExpression.FalseValue, out _))
					{
						var sc = new SearchConditionWord(true)
							.AddAnd( sub => 
								sub
									.Add(new ExprExpr(sqlConditionExpression.TrueValue, op, valueExpression,DBLive.dialect.Option.CompareNullsAsValues ? true : null))
									.Add(sqlConditionExpression.Condition)
							)
							.AddAnd( sub => 
								sub
									.Add(new ExprExpr(sqlConditionExpression.FalseValue, op, valueExpression, DBLive.dialect.Option.CompareNullsAsValues ? true : null))
									.Add(sqlConditionExpression.Condition.MakeNot())
								);

						return sc;
					}
				}
			}
			else if (unwrappedOther is CaseWord sqlCaseExpression)
			{
				// Try comparing by values

				if (TryEvaluateNoParameters(unwrappedValue, out var value))
				{
					var caseMatch      = new bool [sqlCaseExpression._cases.Count];
					var allEvaluatable = true;
					var elseMatch      = false;

					for (var index = 0; index < sqlCaseExpression._cases.Count; index++)
					{
						var caseItem = sqlCaseExpression._cases[index];
						if (TryEvaluateNoParameters(caseItem.ResultExpression, out var caseItemValue)
						    && Compare(caseItemValue, value, op, out var result))
						{
							caseMatch[index] = result;
						}
						else
						{
							allEvaluatable = false;
							break;
						}
					}

					object? elseValue = null;

					if ((sqlCaseExpression.ElseExpression == null || sqlCaseExpression.ElseExpression.TryEvaluateExpression(_evaluationContext, out elseValue))
					    && Compare(elseValue, value, op, out var compareResult))
					{
						elseMatch = compareResult;
					}
					else
						allEvaluatable = false;

					if (allEvaluatable)
					{
						if (caseMatch.All(c => !c) && !elseMatch)
							return AffirmWord.False;

						var resultCondition = new SearchConditionWord(true);

						var notMatches = new List<IAffirmWord>();
						for (int index = 0; index < caseMatch.Length; index++)
						{
							if (caseMatch[index])
							{
								var condition = new SearchConditionWord(false)
									.Add(sqlCaseExpression._cases[index].Condition);

								if (notMatches.Count > 0)
									condition.Add(new SearchConditionWord(true, notMatches).MakeNot());

								resultCondition.Add(condition);
							}
							else
							{
								notMatches.Add(sqlCaseExpression._cases[index].Condition);
							}
						}

						if (elseMatch)
						{
							if (notMatches.Count == 0)
								return AffirmWord.True;

							resultCondition.Add(new SearchConditionWord(true, notMatches).MakeNot());
						}

						return resultCondition;
					}
				}

			}

			return null;
		}

		IAffirmWord OptimizeIsTruePredicate(IsTrue isTrue)
		{
			if (TryEvaluateNoParameters(isTrue.Expr1, out var result) && result is bool boolValue)
			{
				return AffirmWord.MakeBool(boolValue == isTrue.IsNot);
			}

			if (isTrue.Expr1 is ConditionWord or CaseWord)
			{
				if (!isTrue.IsNot)
				{
					var predicate = ProcessComparisonWithCase(isTrue.TrueValue, isTrue.Expr1, AffirmWord.Operator.Equal);
					if (predicate != null)
						return predicate.MakeNot(isTrue.IsNot);
				}
			}

			if (_reduceBinary)
			{
				var reduced = isTrue.Reduce(_nullabilityContext, _isInsideNot);

				if (!ReferenceEquals(reduced, isTrue))
				{
					return (IAffirmWord)Visit(reduced as Clause);
				}
			}

			return isTrue;
		}

		IAffirmWord OptimizeExpExprPredicate(ExprExpr exprExpr)
		{
			var unwrapped1 = QueryHelper.UnwrapNullablity(exprExpr.Expr1);

			if (unwrapped1 is CompareToWord compareTo1)
			{
				if (TryEvaluateNoParameters(exprExpr.Expr2, out var result) && result is int intValue)
				{
					AffirmWord.Operator? current = null;

					if (Compare(1, intValue, exprExpr.Operator))
						CombineOperator(ref current, AffirmWord.Operator.Greater);

					if (Compare(0, intValue, exprExpr.Operator))
						CombineOperator(ref current, AffirmWord.Operator.Equal);

					if (Compare(-1, intValue, exprExpr.Operator))
						CombineOperator(ref current, AffirmWord.Operator.Less);

					if (current == null)
						return AffirmWord.False;

					return new ExprExpr(compareTo1.Expression1, current.Value, compareTo1.Expression2, DBLive.dialect.Option.CompareNullsAsValues ? true : null);
				}
			}

			var unwrapped2 = QueryHelper.UnwrapNullablity(exprExpr.Expr2);

			if (unwrapped2 is CompareToWord compareTo2)
			{
				if (TryEvaluateNoParameters(exprExpr.Expr1, out var result) && result is int intValue)
				{
					AffirmWord.Operator? current = null;

					if (Compare(1, intValue, exprExpr.Operator))
						CombineOperator(ref current, AffirmWord.Operator.Less);

					if (Compare(0, intValue, exprExpr.Operator))
						CombineOperator(ref current, AffirmWord.Operator.Equal);

					if (Compare(-1, intValue, exprExpr.Operator))
						CombineOperator(ref current, AffirmWord.Operator.Greater);

					if (current == null)
						return AffirmWord.False;

					return new ExprExpr(compareTo2.Expression1, current.Value, compareTo2.Expression2, DBLive.dialect.Option.CompareNullsAsValues ? true : null);
				}
			}

			var processed = ProcessComparisonWithCase(exprExpr.Expr1, exprExpr.Expr2, exprExpr.Operator)
			                ?? ProcessComparisonWithCase(exprExpr.Expr2, exprExpr.Expr1, SwapOperator(exprExpr.Operator));

			if (processed != null)
				return processed;

			var left  = QueryHelper.UnwrapNullablity(exprExpr.Expr1);
			var right = QueryHelper.UnwrapNullablity(exprExpr.Expr2);

			if (!exprExpr.Expr1.CanBeNullable(_nullabilityContext) && left.Equals(right))
			{
				if (exprExpr.Operator is AffirmWord.Operator.Equal or AffirmWord.Operator.GreaterOrEqual or AffirmWord.Operator.LessOrEqual or AffirmWord.Operator.NotGreater or AffirmWord.Operator.NotLess)
				{
					return AffirmWord.True;
				}

				if (exprExpr.Operator is AffirmWord.Operator.NotEqual or AffirmWord.Operator.Greater or AffirmWord.Operator.Less)
				{
					return AffirmWord.False;
				}
			}

			if (!_nullabilityContext.IsEmpty                       &&
			    !exprExpr.Expr1.CanBeNullable(_nullabilityContext) &&
			    !exprExpr.Expr2.CanBeNullable(_nullabilityContext) &&
			    exprExpr.Expr1.SystemType.IsSignedType()           &&
			    exprExpr.Expr2.SystemType.IsSignedType())
			{
				var unwrapped = (left, exprExpr.Operator, right);

				var newExpr = unwrapped switch
				{
					(BinaryWord binary, var op, var v) when CanBeEvaluateNoParameters(v) =>

						// binary < v
						binary switch
						{
							// e + some < v ===> some < v - e
							(var e, "+", var some) when CanBeEvaluateNoParameters(e) => new ExprExpr(some, op, new BinaryWord(v.SystemType!, v, "-", e), null),
							// e - some < v ===>  e - v < some
							(var e, "-", var some) when CanBeEvaluateNoParameters(e) => new ExprExpr(new BinaryWord(v.SystemType!, e, "-", v), op, some, null),

							// some + e < v ===> some < v - e
							(var some, "+", var e) when CanBeEvaluateNoParameters(e) => new ExprExpr(some, op, new BinaryWord(v.SystemType!, v, "-", e), null),
							// some - e < v ===> some < v + e
							(var some, "-", var e) when CanBeEvaluateNoParameters(e) => new ExprExpr(some, op, new BinaryWord(v.SystemType!, v, "+", e), null),

							_ => null
						},

					(BinaryWord binary, var op, var v) when CanBeEvaluateNoParameters(v) =>

						// binary < v
						binary switch
						{
							// e + some < v ===> some < v - e
							(var e, "+", var some) when CanBeEvaluateNoParameters(e) => new ExprExpr(some, op, new BinaryWord(v.SystemType!, v, "-", e), null),
							// e - some < v ===>  e - v < some
							(var e, "-", var some) when CanBeEvaluateNoParameters(e) => new ExprExpr(new BinaryWord(v.SystemType!, e, "-", v), op, some, null),

							// some + e < v ===> some < v - e
							(var some, "+", var e) when CanBeEvaluateNoParameters(e) => new ExprExpr(some, op, new BinaryWord(v.SystemType!, v, "-", e), null),
							// some - e < v ===> some < v + e
							(var some, "-", var e) when CanBeEvaluateNoParameters(e) => new ExprExpr(some, op, new BinaryWord(v.SystemType!, v, "+", e), null),

							_ => null
						},

					(var v, var op, BinaryWord binary) when CanBeEvaluateNoParameters(v) =>

						// v < binary
						binary switch
						{
							// v < e + some ===> v - e < some
							(var e, "+", var some) when CanBeEvaluateNoParameters(e) => new ExprExpr(new BinaryWord(v.SystemType!, v, "-", e), op, some, null),
							// v < e - some ===> some < e - v
							(var e, "-", var some) when CanBeEvaluateNoParameters(e) => new ExprExpr(some, op, new BinaryWord(v.SystemType!, e, "-", v), null),

							// v < some + e ===> v - e < some
							(var some, "+", var e) when CanBeEvaluateNoParameters(e) => new ExprExpr(new BinaryWord(v.SystemType!, v, "-", e), op, some, null),
							// v < some - e ===> v + e < some
							(var e, "-", var some) when CanBeEvaluateNoParameters(e) => new ExprExpr(new BinaryWord(v.SystemType!, v, "+", e), op, some, null),

							_ => null
						},

					_ => null
				};

				exprExpr = newExpr ?? exprExpr;
			}

			return exprExpr;
		}

		#endregion

	}
}
