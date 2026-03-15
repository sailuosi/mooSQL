using System;
using System.Globalization;

using mooSQL.data.model;
using mooSQL.data.model.affirms;
using mooSQL.utils;

namespace mooSQL.linq.SqlQuery
{
	partial class QueryHelper
	{
		public static SQLParameterValue GetParameterValue(this ParameterWord parameter, IReadOnlyParaValues? parameterValues)
		{
			if (parameterValues != null && parameterValues.TryGetValue(parameter, out var value))
			{
				return value;
			}
			return new SQLParameterValue(parameter.Value, parameter.Type);
		}

		public static bool TryEvaluateExpression(this ISQLNode expr, EvaluateContext context, out object? result)
		{
			var res = expr.TryEvaluateExpression(context);
			result=res.value;
			return res.success;
		}

		public static bool IsMutable(this ISQLNode expr)
		{
			if (expr.CanBeEvaluated(false))
				return false;

			if (expr is IExpWord sqlExpression)
			{
				sqlExpression = UnwrapNullablity(sqlExpression);
				if (sqlExpression is ParameterWord)
					return true;
			}

			var isMutable = false;
			expr.VisitParentFirst(a =>
			{
				if (a is BinaryWord binary)
				{
					var expr1 = UnwrapNullablity(binary.Expr1);
					var expr2 = UnwrapNullablity(binary.Expr2);
					if ((expr1 is ParameterWord || expr1.CanBeEvaluated(false)) && (expr2 is ParameterWord || expr2.CanBeEvaluated(false)))
						isMutable = true;
				}

				return !isMutable;
			});

			return isMutable;
		}

		public static bool CanBeEvaluated(this ISQLNode expr, bool withParameters)
		{
			return expr.TryEvaluateExpression(new EvaluateContext(withParameters ? SqlParameterValues.Empty : null), out _);
		}

		public static bool CanBeEvaluated(this ISQLNode expr, EvaluateContext context)
		{
			return expr.TryEvaluateExpression(context, out _);
		}

		static EvaluateResult TryEvaluateExpression(this ISQLNode expr, EvaluateContext context)
		{
            //(object? value, bool success)
            if (!context.TryGetValue(expr, out var info))
			{
				if (TryEvaluateExpressionInternal(expr, context, out var result))
				{
					context.Register(expr, result);
					return new EvaluateResult(result, true);
				}
				else
				{
					context.RegisterError(expr);
					return new EvaluateResult(result, false);
				}
			}

			return info;
		}

		static bool TryEvaluateExpressionInternal(this ISQLNode expr, EvaluateContext context, out object? result)
		{
			result = null;
			switch (expr.NodeType)
			{
				case ClauseType.SqlValue           :
				{
					var sqlValue = (ValueWord)expr;
					result = sqlValue.Value;
					return true;
				}

				case ClauseType.SqlParameter       :
				{
					var sqlParameter = (ParameterWord)expr;

					if (context.ParameterValues == null)
					{
						return false;
					}

					var parameterValue = sqlParameter.GetParameterValue(context.ParameterValues);

					result = parameterValue.ProviderValue;
					return true;
				}

				case ClauseType.IsNullPredicate:
				{
					var isNullPredicate = (IsNull)expr;
					if (!isNullPredicate.Expr1.TryEvaluateExpression(context, out var value))
						return false;
					result = isNullPredicate.IsNot == (value != null);
					return true;
				}

				case ClauseType.ExprExprPredicate:
				{
					var exprExpr = (ExprExpr)expr;
					/*
					var reduced = exprExpr.Reduce(context, TODO);
					if (!ReferenceEquals(reduced, expr))
						return TryEvaluateExpression(reduced, context, out result);
						*/

					if (!exprExpr.Expr1.TryEvaluateExpression(context, out var value1) ||
					    !exprExpr.Expr2.TryEvaluateExpression(context, out var value2))
						return false;

					if (value1 != null && value2 != null)
					{
						if (value1.GetType().IsEnum != value2.GetType().IsEnum)
						{
							return false;
						}
					}

					switch (exprExpr.Operator)
					{
						case AffirmWord.Operator.Equal:
						{
							if (value1 == null)
							{
								result = value2 == null;
							}
							else
							{
								result = (value2 != null) && value1.Equals(value2);
							}
							break;
						}
						case AffirmWord.Operator.NotEqual:
						{
							if (value1 == null)
							{
								result = value2 != null;
							}
							else
							{
								result = value2 == null || !value1.Equals(value2);
							}
							break;
						}
						default:
						{
							if (!(value1 is IComparable comp1) || !(value2 is IComparable comp2))
							{
								result = false;
								return true;
							}

							switch (exprExpr.Operator)
							{
								case AffirmWord.Operator.Greater:
									result = comp1.CompareTo(comp2) > 0;
									break;
								case AffirmWord.Operator.GreaterOrEqual:
									result = comp1.CompareTo(comp2) >= 0;
									break;
								case AffirmWord.Operator.NotGreater:
									result = !(comp1.CompareTo(comp2) > 0);
									break;
								case AffirmWord.Operator.Less:
									result = comp1.CompareTo(comp2) < 0;
									break;
								case AffirmWord.Operator.LessOrEqual:
									result = comp1.CompareTo(comp2) <= 0;
									break;
								case AffirmWord.Operator.NotLess:
									result = !(comp1.CompareTo(comp2) < 0);
									break;

								default:
									return false;

							}
							break;
						}
					}

					return true;
				}

				case ClauseType.NotPredicate:
				{
					var notPredicate = (Not)expr;
					if (notPredicate.Predicate.TryEvaluateExpression(context, out var value) && value is bool boolValue)
					{
						result = !boolValue;
						return true;
					}

					return false;
				}

				case ClauseType.TruePredicate:
				{
					result = true;
					return true;
				}

				case ClauseType.FalsePredicate:
				{
					result = false;
					return true;
				}

				case ClauseType.IsTruePredicate:
				{
					var isTruePredicate = (IsTrue)expr;
					if (!isTruePredicate.Expr1.TryEvaluateExpression(context, out var value))
						return false;

					if (value == null)
					{
						if (isTruePredicate.WithNull != null)
							result = isTruePredicate.WithNull;
						else
							result = false;
						return true;
					}

					if (value is bool boolValue)
					{
						result = boolValue != isTruePredicate.IsNot;
						return true;
					}

					return false;
				}

				case ClauseType.SqlCast:
				{
					var cast = (CastWord)expr;
					if (!cast.Expression.TryEvaluateExpression(context, out var value))
						return false;

					result = value;

					if (result != null)
					{
						if (cast.SystemType == typeof(string))
						{
							if (result.GetType().IsNumeric())
							{
								if (result is int intValue)
									result = intValue.ToString(CultureInfo.InvariantCulture);
								else if (result is long longValue)
									result = longValue.ToString(CultureInfo.InvariantCulture);
								else if (result is short shortValue)
									result = shortValue.ToString(CultureInfo.InvariantCulture);
								else if (result is byte byteValue)
									result = byteValue.ToString(CultureInfo.InvariantCulture);
								else if (result is uint uintValue)
									result = uintValue.ToString(CultureInfo.InvariantCulture);
								else if (result is ulong ulongValue)
									result = ulongValue.ToString(CultureInfo.InvariantCulture);
								else if (result is ushort ushortValue)
									result = ushortValue.ToString(CultureInfo.InvariantCulture);
								else if (result is sbyte sbyteValue)
									result = sbyteValue.ToString(CultureInfo.InvariantCulture);
								else if (result is char charValue)
									result = charValue.ToString(CultureInfo.InvariantCulture);
								else if (result is decimal decimalValue)
									result = decimalValue.ToString(CultureInfo.InvariantCulture);
								else if (result is float floatValue)
									result = floatValue.ToString(CultureInfo.InvariantCulture);
								else if (result is double doubleValue)
									result = doubleValue.ToString(CultureInfo.InvariantCulture);
							}
						}
						else
						{
							if (result.GetType() != cast.SystemType)
							{
								try
								{
									result = Convert.ChangeType(result, cast.SystemType, CultureInfo.InvariantCulture);
								}
								catch (InvalidCastException)
								{
									return false;
								}
							}
						}
					}

					return true;
				}

				case ClauseType.SqlBinaryExpression:
				{
					var binary = (BinaryWord)expr;
					if (!binary.Expr1.TryEvaluateExpression(context, out var leftEvaluated))
						return false;
					if (!binary.Expr2.TryEvaluateExpression(context, out var rightEvaluated))
						return false;
					dynamic? left  = leftEvaluated;
					dynamic? right = rightEvaluated;
					if (left == null || right == null)
						return false;

					try
					{
						switch (binary.Operation)
						{
							case "+":  result = left + right; break;
							case "-":  result = left - right; break;
							case "*":  result = left * right; break;
							case "/":  result = left / right; break;
							case "%":  result = left % right; break;
							case "^":  result = left ^ right; break;
							case "&":  result = left & right; break;
							case "<":  result = left < right; break;
							case ">":  result = left > right; break;
							case "<=": result = left <= right; break;
							case ">=": result = left >= right; break;
							default:
								return false;
						}
					}
					catch
					{
						return false;
					}

					return true;
				}

				case ClauseType.SqlFunction        :
				{
					var function = (FunctionWord)expr;

					switch (function.Name)
					{
						case "Length":
						{
							if (function.Parameters[0]
								.TryEvaluateExpression(context, out var strValue))
							{
								if (strValue == null)
									return true;
								if (strValue is string str)
								{
									result = str.Length;
									return true;
								}
							}

							return false;
						}

						case PseudoFunctions.TO_LOWER:
						{
							if (function.Parameters[0]
								.TryEvaluateExpression(context, out var strValue))
							{
								if (strValue == null)
									return true;
								if (strValue is string str)
								{
									result = str.ToLower(CultureInfo.InvariantCulture);
									return true;
								}
							}

							return false;
						}

						case PseudoFunctions.TO_UPPER:
						{
							if (function.Parameters[0]
								.TryEvaluateExpression(context, out var strValue))
							{
								if (strValue == null)
									return true;
								if (strValue is string str)
								{
									result = str.ToUpper(CultureInfo.InvariantCulture);
									return true;
								}
							}

							return false;
						}

						default:
							return false;
					}
				}

				case ClauseType.SearchCondition    :
				{
					var cond = (SearchConditionWord)expr;

					if (cond.Predicates.Count == 0)
					{
						result = true;
						return true;
					}

					for (var i = 0; i < cond.Predicates.Count; i++)
					{
						var predicate = cond.Predicates[i];
						if (predicate.TryEvaluateExpression(context, out var evaluated))
						{
							if (evaluated is bool boolValue)
							{
								if (boolValue)
								{
									if (cond.IsOr)
									{
										result = true;
										return true;
									}
								}
								else
								{
									if (!cond.IsOr)
									{
										result = false;
										return true;
									}
								}
							}
						}
					}

					return false;
				}

				case ClauseType.SqlCase:
				{
					var caseExpr = (CaseWord)expr;

					foreach (var caseItem in caseExpr.Cases)
					{
						if (caseItem.Condition.TryEvaluateExpression(context, out var evaluatedCondition) && evaluatedCondition is bool boolValue)
						{
							if (boolValue)
							{
								if (caseItem.ResultExpression.TryEvaluateExpression(context, out var resultValue))
								{
									result = resultValue;
									return true;
								}
							}
						}
						else
						{
							return false;
						}
					}

					if (caseExpr.ElseExpression == null)
					{
						result = null;
						return true;
					}

					if (caseExpr.ElseExpression.TryEvaluateExpression(context, out var elseValue))
					{
						result = elseValue;
						return true;
					}

					return false;
				}

				case ClauseType.ExprPredicate      :
				{
					var predicate = (Expr)expr;
					if (!predicate.Expr1.TryEvaluateExpression(context, out var value))
						return false;

					result = value;
					return true;
				}

				case ClauseType.SqlNullabilityExpression:
				{
					var nullability = (NullabilityWord)expr;
					if (nullability.SqlExpression.TryEvaluateExpression(context, out var evaluated))
					{
						result = evaluated;
						return true;
					}

					return false;
				}

				case ClauseType.SqlExpression:
				{
					var sqlExpression = (ExpressionWord)expr;

					if (sqlExpression .Expr== "{0}"&& sqlExpression.Parameters.Length==1 )
					{
						var p=sqlExpression.Parameters[0];
						if (p.TryEvaluateExpression(context, out var evaluated))
						{
							result = evaluated;
							return true;
						} 
					}

					return false;
				}

				case ClauseType.CompareTo:
				{
					var compareTo = (CompareToWord)expr;
					if (!compareTo.Expression1.TryEvaluateExpression(context, out var value1) || !compareTo.Expression2.TryEvaluateExpression(context, out var value2))
						return false;

					if (value1 == null || value2 == null)
					{
						result = false;
						return true;
					}

					if (value1 is IComparable comp1 && value2 is IComparable comp2)
					{
						result = comp1.CompareTo(comp2);
						return true;
					}

					return false;
				}

				case ClauseType.SqlCondition:
				{
					var compareTo = (ConditionWord)expr;

					if (compareTo.Condition.TryEvaluateExpression(context, out var conditionValue) && conditionValue is bool boolCondition)
					{
						if (boolCondition)
						{
							if (compareTo.TrueValue.TryEvaluateExpression(context, out var trueValue))
							{
								result = trueValue;
								return true;
							}
						}
						else
						{
							if (compareTo.FalseValue.TryEvaluateExpression(context, out var falseValue))
							{
								result = falseValue;
								return true;
							}
						}
					}

					return false;
				}

				default:
				{
					return false;
				}
			}
		}

		public static object? EvaluateExpression(this ISQLNode expr, EvaluateContext context)
		{
			var etar = expr.TryEvaluateExpression(context);
			if (!etar.success)
				throw new LinqToDBException($"Cannot evaluate expression: {expr}");

			return etar.value;
		}

		public static bool? EvaluateBoolExpression(this ISQLNode expr, EvaluateContext context, bool? defaultValue = null)
		{
			var evaluated = expr.EvaluateExpression(context);

			if (evaluated is bool boolValue)
				return boolValue;

			return defaultValue;
		}

		public static void ExtractPredicate(IAffirmWord predicate, out IAffirmWord underlying, out bool isNot)
		{
			underlying = predicate;
			isNot      = false;

			if (predicate is Not notPredicate)
			{
				underlying = notPredicate.Predicate;
				isNot      = true;
			}
		}
	}
}
