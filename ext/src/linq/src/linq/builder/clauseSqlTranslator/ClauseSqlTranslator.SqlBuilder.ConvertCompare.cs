using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace mooSQL.linq.Linq.Builder
{
	using Common;
	using Common.Internal;
	using Data;
	using Extensions;
	using Translation;
	using mooSQL.linq.Expressions;
	using Mapping;
	using Reflection;
	using SqlQuery;
	using DataProvider;
	using mooSQL.data.model;
	using mooSQL.data;
	using mooSQL.data.model.affirms;
    using mooSQL.data.Mapping;
    using mooSQL.utils;
    using mooSQL.data.mapping;

    partial class ClauseSqlTranslator
	{
		#region ConvertCompare

		static LambdaExpression BuildMemberPathLambda(Expression path)
		{
			var memberPath = new List<MemberInfo>();

			var current = path;
			do
			{
				if (current is MemberExpression me)
				{
					current = me.Expression!;
					memberPath.Add(me.Member);
				}
				else
					break;

			} while (true);

			var        param = Expression.Parameter(current.Type, "o");
			Expression body  = param;
			for (int i = memberPath.Count - 1; i >= 0; i--)
			{
				body = Expression.MakeMemberAccess(body, memberPath[i]);
			}

			return Expression.Lambda(body, param);
		}

		public SearchConditionWord? TryGenerateComparison(
			IClauseContext? context,
			Expression     left,
			Expression     right,
			ProjectFlags   flags = ProjectFlags.SQL)
		{
			var expr = ConvertCompareExpression(context, ExpressionType.Equal, left, right, flags);
			if (expr is SqlPlaceholderExpression { Sql: SearchConditionWord sc })
				return sc;

			return null;
		}

		public SearchConditionWord GenerateComparison(
			IClauseContext? context,
			Expression     left,
			Expression     right,
			ProjectFlags   flags = ProjectFlags.SQL)
		{
			var expr = ConvertCompareExpression(context, ExpressionType.Equal, left, right, flags);
			if (expr is SqlPlaceholderExpression { Sql: SearchConditionWord sc })
				return sc;
			if (expr is SqlErrorExpression error)
				throw error.CreateException();

			throw new SqlErrorExpression($"Could not compare '{left}' with {right}", typeof(bool)).CreateException();
		}

		Expression ConvertCompareExpression(IClauseContext? context, ExpressionType nodeType, Expression left, Expression right, ProjectFlags flags, Expression? originalExpression = null)
		{
			Expression GetOriginalExpression()
			{
				if (originalExpression != null)
					return originalExpression;

				var rightExpr = right;
				var leftExpr  = left;
				if (rightExpr.Type != leftExpr.Type)
				{
					if (rightExpr.Type.CanConvertTo(leftExpr.Type))
						rightExpr = Expression.Convert(rightExpr, leftExpr.Type);
					else if (left.Type.CanConvertTo(leftExpr.Type))
						leftExpr = Expression.Convert(leftExpr, right.Type);
				}
				else
				{
					if (nodeType == ExpressionType.Equal || nodeType == ExpressionType.NotEqual)
					{
						// Fore generating Path for SqlPlaceholderExpression
						if (!rightExpr.Type.IsPrimitive)
						{
							return new SqlPathExpression(
								new[] { leftExpr, Expression.Constant(nodeType), rightExpr },
								typeof(bool));
						}
					}
				}

				return Expression.MakeBinary(nodeType, leftExpr, rightExpr);
			}

			Expression GenerateNullComparison(Expression placeholdersExpression, bool isNot)
			{
				List<Expression> expressions = new();
				if (!CollectNullCompareExpressions(context, placeholdersExpression, expressions) || expressions.Count == 0)
					return GetOriginalExpression();

				List<SqlPlaceholderExpression> placeholders = new(expressions.Count);
				List<SqlPlaceholderExpression>? notNull      = null;

				var nullability = NullabilityContext.NonQuery;

				foreach (var expression in expressions)
				{
					var predicateExpr = ConvertToSqlExpr(context, expression, flags.SqlFlag());
					if (predicateExpr is SqlPlaceholderExpression placeholder)
					{
						if (!placeholder.Sql.CanBeNullable(nullability))
						{
							placeholders.Clear();
							placeholders.Add(placeholder);
							notNull = placeholders;
							break;
						}
						else
						{
							placeholders.Add(placeholder);
						}
					}
				}

				if (placeholders.Count == 0)
					return GetOriginalExpression();

				if (notNull == null)
					notNull = placeholders;

				var searchCondition = new SearchConditionWord(isNot);
				foreach (var placeholder in notNull)
				{
					var sql = placeholder.Sql;
					searchCondition.Predicates.Add(new IsNull(sql, isNot));
				}

				return CreatePlaceholder(context, searchCondition, GetOriginalExpression());
			}

			Expression GeneratePathComparison(Expression leftOriginal, Expression leftParsed, Expression rightOriginal, Expression rightParsed)
			{
				var predicateExpr = GeneratePredicate(leftOriginal, leftParsed, rightOriginal, rightParsed);
				if (predicateExpr == null)
					return GetOriginalExpression();

				var converted = ConvertToSqlExpr(context, predicateExpr, flags);
				if (converted is not SqlPlaceholderExpression)
					converted = GetOriginalExpression();

				return converted;
			}

			Expression? GeneratePredicate(Expression leftOriginal, Expression leftParsed, Expression rightOriginal, Expression rightParsed)
			{
				Expression? predicateExpr = null;

				if (leftParsed is SqlGenericConstructorExpression genericLeft)
				{
					predicateExpr = BuildPredicateExpression(genericLeft, null, rightOriginal);
				}

				if (predicateExpr != null)
					return predicateExpr;

				if (rightParsed is SqlGenericConstructorExpression genericRight)
				{
					predicateExpr = BuildPredicateExpression(genericRight, null, leftOriginal);
				}

				if (predicateExpr != null)
					return predicateExpr;

				if (leftParsed is ConditionalExpression condLeft)
				{
					if (condLeft.IfTrue is SqlGenericConstructorExpression genericTrue)
					{
						predicateExpr = BuildPredicateExpression(genericTrue, leftOriginal, rightOriginal);
					}
					else if (condLeft.IfFalse is SqlGenericConstructorExpression genericFalse)
					{
						predicateExpr = BuildPredicateExpression(genericFalse, leftOriginal, rightOriginal);
					}

					if (predicateExpr == null)
						predicateExpr = GeneratePredicate(leftOriginal, condLeft.IfTrue, rightOriginal, rightParsed);
					if (predicateExpr == null)
						predicateExpr = GeneratePredicate(leftOriginal, condLeft.IfFalse, rightOriginal, rightParsed);
				}

				if (predicateExpr != null)
					return predicateExpr;

				if (rightParsed is ConditionalExpression condRight)
				{
					if (condRight.IfTrue is SqlGenericConstructorExpression genericTrue)
					{
						predicateExpr = BuildPredicateExpression(genericTrue, leftOriginal, rightOriginal);
					}
					else if (condRight.IfFalse is SqlGenericConstructorExpression genericFalse)
					{
						predicateExpr = BuildPredicateExpression(genericFalse, leftOriginal, rightOriginal);
					}

					if (predicateExpr == null)
						predicateExpr = GeneratePredicate(leftOriginal, leftParsed, condRight.IfTrue, rightParsed);
					if (predicateExpr == null)
						predicateExpr = GeneratePredicate(leftOriginal, leftParsed, condRight.IfFalse, rightParsed);
				}

				if (predicateExpr != null)
					return predicateExpr;

				return predicateExpr;
			}

			Expression? BuildPredicateExpression(SqlGenericConstructorExpression genericConstructor, Expression? rootLeft, Expression rootRight)
			{
				if (genericConstructor.Assignments.Count == 0)
					return null;

				var operations = genericConstructor.Assignments
					.Select(a => Expression.Equal(
						rootLeft == null ? a.Expression : Expression.MakeMemberAccess(rootLeft, a.MemberInfo),
						Expression.MakeMemberAccess(rootRight, a.MemberInfo))
					);

				var result = (Expression)operations.Aggregate(Expression.AndAlso);
				if (nodeType == ExpressionType.NotEqual)
					result = Expression.Not(result);

				return result;
			}

			Expression GenerateConstructorComparison(SqlGenericConstructorExpression leftConstructor, SqlGenericConstructorExpression rightConstructor)
			{
				var strict = leftConstructor.ConstructType  == SqlGenericConstructorExpression.CreateType.Full ||
							 rightConstructor.ConstructType == SqlGenericConstructorExpression.CreateType.Full ||
							 (leftConstructor.ConstructType  == SqlGenericConstructorExpression.CreateType.New &&
							  rightConstructor.ConstructType == SqlGenericConstructorExpression.CreateType.New) ||
							 (leftConstructor.ConstructType  == SqlGenericConstructorExpression.CreateType.MemberInit &&
							  rightConstructor.ConstructType == SqlGenericConstructorExpression.CreateType.MemberInit);

				var isNot           = nodeType == ExpressionType.NotEqual;
				var searchCondition = new SearchConditionWord(isNot);
				var usedMembers     = new HashSet<MemberInfo>(MemberInfoEqualityComparer.Default);

				foreach (var leftAssignment in leftConstructor.Assignments)
				{
					var found = rightConstructor.Assignments.FirstOrDefault(a =>
						MemberInfoEqualityComparer.Default.Equals(a.MemberInfo, leftAssignment.MemberInfo));

					if (found == null && strict)
					{
						// fail fast and prepare correct error expression
						return CreateSqlError(context, Expression.MakeMemberAccess(right, leftAssignment.MemberInfo));
					}

					var rightExpression = found?.Expression;
					if (rightExpression == null)
					{
						rightExpression = Expression.Default(leftAssignment.Expression.Type);
					}
					else
					{
						usedMembers.Add(found!.MemberInfo);
					}

					var predicateExpr = ConvertCompareExpression(context, nodeType, leftAssignment.Expression, rightExpression, flags);
					if (predicateExpr is not SqlPlaceholderExpression { Sql: SearchConditionWord sc })
					{
						if (strict)
							return GetOriginalExpression();
						continue;
					}

					searchCondition.Predicates.Add(sc.MakeNot(isNot));
				}

				foreach (var rightAssignment in rightConstructor.Assignments)
				{
					if (usedMembers.Contains(rightAssignment.MemberInfo))
						continue;

					if (strict)
					{
						// fail fast and prepare correct error expression
						return CreateSqlError(context, Expression.MakeMemberAccess(left, rightAssignment.MemberInfo));
					}

					var leftExpression = Expression.Default(rightAssignment.Expression.Type);

					var predicateExpr = ConvertCompareExpression(context, nodeType, leftExpression, rightAssignment.Expression, flags);
					if (predicateExpr is not SqlPlaceholderExpression { Sql: SearchConditionWord sc })
					{
						if (strict)
							return predicateExpr;
						continue;
					}

					searchCondition.Predicates.Add(sc.MakeNot(isNot));
				}

				if (usedMembers.Count == 0)
				{
					if (leftConstructor.Parameters.Count > 0 && leftConstructor.Parameters.Count == rightConstructor.Parameters.Count)
					{
						for (var index = 0; index < leftConstructor.Parameters.Count; index++)
						{
							var leftParam  = leftConstructor.Parameters[index];
							var rightParam = rightConstructor.Parameters[index];

							var predicateExpr = ConvertCompareExpression(context, nodeType, leftParam.Expression, rightParam.Expression, flags);
							if (predicateExpr is not SqlPlaceholderExpression { Sql: SearchConditionWord sc })
							{
								if (strict)
									return GetOriginalExpression();
								continue;
							}

							searchCondition.Predicates.Add(sc.MakeNot(isNot));
						}

					}
					else
						return GetOriginalExpression();
				}

				return CreatePlaceholder(context, searchCondition, GetOriginalExpression());
			}

			if (!RestoreCompare(ref left, ref right))
				RestoreCompare(ref right, ref left);

			if (context == null)
				throw new InvalidOperationException();

            IExpWord? l = null;
            IExpWord? r = null;

			var nullability = NullabilityContext.GetContext(context.SelectQuery);

			var keysFlag         = (flags & ~ProjectFlags.ForExtension) | ProjectFlags.Keys;
			var columnDescriptor = SuggestColumnDescriptor(context, left, right, keysFlag);
			var leftExpr         = ConvertToSqlExpr(context, left,  keysFlag, columnDescriptor : columnDescriptor);
			var rightExpr        = ConvertToSqlExpr(context, right, keysFlag, columnDescriptor : columnDescriptor);

			var compareNullsAsValues = CompareNullsAsValues;

			//SQLRow case when needs to add Single
			//
			if (leftExpr is SqlPlaceholderExpression { Sql: RowWord } && rightExpr is not SqlPlaceholderExpression)
			{
				var elementType = TypeHelper.GetEnumerableElementType(rightExpr.Type);
				var singleCall  = Expression.Call(Methods.Enumerable.Single.MakeGenericMethod(elementType), right);
				rightExpr = ConvertToSqlExpr(context, singleCall, keysFlag, columnDescriptor : columnDescriptor);
			}
			else if (rightExpr is SqlPlaceholderExpression { Sql: RowWord } &&
			         leftExpr is not SqlPlaceholderExpression)
			{
				var elementType = TypeHelper.GetEnumerableElementType(leftExpr.Type);
				var singleCall  = Expression.Call(Methods.Enumerable.Single.MakeGenericMethod(elementType), left);
				leftExpr = ConvertToSqlExpr(context, singleCall, keysFlag, columnDescriptor : columnDescriptor);
			}

			leftExpr  = RemoveNullPropagation(leftExpr, true);
			rightExpr = RemoveNullPropagation(rightExpr, true);

			if (leftExpr is SqlErrorExpression leftError)
				return leftError.WithType(typeof(bool));

			if (rightExpr is SqlErrorExpression rightError)
				return rightError.WithType(typeof(bool));

			if (leftExpr is SqlPlaceholderExpression placeholderLeft)
			{
				l = placeholderLeft.Sql;
			}

			if (rightExpr is SqlPlaceholderExpression placeholderRight)
			{
				r = placeholderRight.Sql;
			}

			switch (nodeType)
			{
				case ExpressionType.Equal:
				case ExpressionType.NotEqual:

					var isNot = nodeType == ExpressionType.NotEqual;

					if (l != null && r != null)
						break;

					leftExpr  = ParseGenericConstructor(leftExpr, flags, columnDescriptor, true);
					rightExpr = ParseGenericConstructor(rightExpr, flags, columnDescriptor, true);

					if (SequenceHelper.UnwrapDefaultIfEmpty(leftExpr) is SqlGenericConstructorExpression leftGenericConstructor &&
					    SequenceHelper.UnwrapDefaultIfEmpty(rightExpr) is SqlGenericConstructorExpression rightGenericConstructor)
					{
						return GenerateConstructorComparison(leftGenericConstructor, rightGenericConstructor);
					}

					if (l is ValueWord lv && lv.Value == null || left.IsNullValue())
					{
						rightExpr = BuildSqlExpression(context, rightExpr, flags);

						if (rightExpr is ConditionalExpression { Test: SqlPlaceholderExpression { Sql: SearchConditionWord rightSearchCond } } && rightSearchCond.Predicates.Count == 1)
						{
							var rightPredicate  = rightSearchCond.Predicates[0];
							var localIsNot = isNot;

							if (rightPredicate is IsNull isnull)
							{
								if (isnull.IsNot == localIsNot)
									return CreatePlaceholder(context, (IExpWord)new SearchConditionWord(false, isnull), GetOriginalExpression());

								return CreatePlaceholder(context, (IExpWord)new SearchConditionWord(false, new IsNull(isnull.Expr1, !isnull.IsNot)), GetOriginalExpression());
							}
						}

						return GenerateNullComparison(rightExpr, isNot);
					}

					if (r is ValueWord rv && rv.Value == null || right.IsNullValue())
					{
						leftExpr = BuildSqlExpression(context, leftExpr, flags);

						if (leftExpr is ConditionalExpression { Test: SqlPlaceholderExpression { Sql: SearchConditionWord leftSearchCond } } && leftSearchCond.Predicates.Count == 1)
						{
							var leftPredicate  = leftSearchCond.Predicates[0];
							var localIsNot = isNot;

							if (leftPredicate is IsNull isnull)
							{
								if (isnull.IsNot == localIsNot)
									return CreatePlaceholder(context, (IExpWord)new SearchConditionWord(false, isnull), GetOriginalExpression());

								return CreatePlaceholder(context, (IExpWord)new SearchConditionWord(false, new IsNull(isnull.Expr1, !isnull.IsNot)), GetOriginalExpression());
							}
						}

						return GenerateNullComparison(leftExpr, isNot);
					}

					if (l == null || r == null)
					{
						var pathComparison = GeneratePathComparison(left, SequenceHelper.UnwrapDefaultIfEmpty(leftExpr), right, SequenceHelper.UnwrapDefaultIfEmpty(rightExpr));

						return pathComparison;
					}

					break;
			}

			var op = nodeType switch
			{
                ExpressionType.Equal              => AffirmWord.Operator.Equal,
                ExpressionType.NotEqual           => AffirmWord.Operator.NotEqual,
                ExpressionType.GreaterThan        => AffirmWord.Operator.Greater,
                ExpressionType.GreaterThanOrEqual => AffirmWord.Operator.GreaterOrEqual,
                ExpressionType.LessThan           => AffirmWord.Operator.Less,
                ExpressionType.LessThanOrEqual    => AffirmWord.Operator.LessOrEqual,
				_                                 => throw new InvalidOperationException(),
			};

			if ((left.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked || right.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked) && (op == AffirmWord.Operator.Equal || op == AffirmWord.Operator.NotEqual))
			{
				var p = ConvertEnumConversion(context!, left, op, right);
				if (p != null)
					return CreatePlaceholder(context, new SearchConditionWord(false, p), GetOriginalExpression());
			}

			if (l is null)
			{
				if (!TryConvertToSql(context, left, flags, columnDescriptor : columnDescriptor, out var lConverted, out _))
					return GetOriginalExpression();
				l = lConverted;
			}

			if (r is null)
			{
				if (!TryConvertToSql(context, right, flags, columnDescriptor : columnDescriptor, out var rConverted, out _))
					return GetOriginalExpression();
				r = rConverted;
			}

			var lOriginal = l;
			var rOriginal = r;

			l = QueryHelper.UnwrapExpression(l, checkNullability: true);
			r = QueryHelper.UnwrapExpression(r, checkNullability: true);

			if (l is ValueWord lValue)
				lValue.ValueType = GetDataType(r, lValue.ValueType, DBLive);

			if (r is ValueWord rValue)
				rValue.ValueType = GetDataType(l, rValue.ValueType, DBLive);

			switch (nodeType)
			{
				case ExpressionType.Equal:
				case ExpressionType.NotEqual:

					if (!context!.SelectQuery.IsParameterDependent &&
						(l is ParameterWord && lOriginal.CanBeNullable(nullability) || r is ParameterWord && r.CanBeNullable(nullability)))
					{
						context.SelectQuery.IsParameterDependent = true;
					}

					break;
			}

            IAffirmWord? predicate = null;

			var isEquality = op == AffirmWord.Operator.Equal || op == AffirmWord.Operator.NotEqual
				? op == AffirmWord.Operator.Equal
				: (bool?)null;

			// TODO: maybe remove
			if (l is SearchConditionWord lsc)
			{
				if (isEquality != null & IsBooleanConstant(rightExpr, out var boolRight) && boolRight != null)
				{
					predicate = lsc.MakeNot(boolRight != isEquality);
				}
			}

			// TODO: maybe remove
			if (r is SearchConditionWord rsc)
			{
				if (isEquality != null & IsBooleanConstant(rightExpr, out var boolLeft) && boolLeft != null)
				{
					predicate = rsc.MakeNot(boolLeft != isEquality);
				}
			}

			if (predicate == null)
			{
				if (isEquality != null)
				{
					bool?           value;
                    IExpWord? expression  = null;

					if (IsBooleanConstant(left, out value))
					{
						if (l.NodeType != ClauseType.SqlParameter)
						{
							expression = rOriginal;
						}
					}
					else if (IsBooleanConstant(right, out value))
					{
						if (r.NodeType != ClauseType.SqlParameter)
						{
							expression = lOriginal;
						}
					}

					if (value != null
						&& expression != null
						&& !(expression.NodeType == ClauseType.SqlValue && ((ValueWord)expression).Value == null))
					{
						var isNot = !value.Value;
						var withNull = false;
						if (op == AffirmWord.Operator.NotEqual)
						{
							isNot = !isNot;
							withNull = true;
						}
						var descriptor = QueryHelper.GetColumnDescriptor(expression);
						var trueValue  = ConvertToSql(context, ExpressionInstances.True,  unwrap: false, columnDescriptor: descriptor);
						var falseValue = ConvertToSql(context, ExpressionInstances.False, unwrap: false, columnDescriptor: descriptor);

						if (trueValue.NodeType  == ClauseType.SqlValue &&
						    falseValue.NodeType == ClauseType.SqlValue)
						{
							var withNullValue = compareNullsAsValues
								? withNull
								: (bool?)null;
							predicate = new IsTrue(expression, trueValue, falseValue, withNullValue, isNot);
						}
					}
				}

				// Force nullability (constant null may become ParameterWord when CompareNullsAsValues)
				if (QueryHelper.IsNullValue(lOriginal) || left.IsNullValue())
				{
					rOriginal = NullabilityWord.ApplyNullability(rOriginal, true);
					predicate = new IsNull(rOriginal, op == AffirmWord.Operator.NotEqual);
				}
				else if (QueryHelper.IsNullValue(rOriginal) || right.IsNullValue())
				{
					lOriginal = NullabilityWord.ApplyNullability(lOriginal, true);
					predicate = new IsNull(lOriginal, op == AffirmWord.Operator.NotEqual);
				}

				if (predicate == null)
				{
					if (compareNullsAsValues)
					{
						if (lOriginal is ColumnWord colLeft)
							lOriginal = NullabilityWord.ApplyNullability(lOriginal, NullabilityContext.GetContext(colLeft.Parent));
						else if (lOriginal is FieldWord)
							lOriginal = NullabilityWord.ApplyNullability(lOriginal, NullabilityContext.NonQuery);

						if (rOriginal is ColumnWord colRight)
							rOriginal = NullabilityWord.ApplyNullability(rOriginal, NullabilityContext.GetContext(colRight.Parent));
						else if (rOriginal is FieldWord)
							rOriginal = NullabilityWord.ApplyNullability(rOriginal, NullabilityContext.NonQuery);

						lOriginal = NullabilityWord.ApplyNullability(lOriginal, nullability);
						rOriginal = NullabilityWord.ApplyNullability(rOriginal, nullability);
					}

					predicate = new ExprExpr(lOriginal, op, rOriginal,
						compareNullsAsValues
							? true
							: null);
				}
			}

			return CreatePlaceholder(context, new SearchConditionWord(false, predicate), GetOriginalExpression());
		}

		public static List<SqlPlaceholderExpression> CollectPlaceholders(Expression expression)
		{
			var result = new List<SqlPlaceholderExpression>();

			expression.Visit(result, static (list, e) =>
			{
				if (e is SqlPlaceholderExpression placeholder)
				{
					list.Add(placeholder);
				}
			});

			return result;
		}

		public static IEnumerable<(SqlPlaceholderExpression placeholder, MemberInfo[] path)> CollectPlaceholders2(
			Expression expression, List<MemberInfo> currentPath)
		{
			IEnumerable<(SqlPlaceholderExpression placeholder, MemberInfo[] path)> Collect(Expression expr, Stack<MemberInfo> current)
			{
				if (expr is SqlPlaceholderExpression placeholder)
					yield return (placeholder, current.ToArray());

				if (expr is SqlGenericConstructorExpression generic)
				{
					foreach (var assignment in generic.Assignments)
					{
						current.Push(assignment.MemberInfo);
						foreach (var found in Collect(assignment.Expression, current))
							yield return found;
						current.Pop();
					}

					foreach (var parameter in generic.Parameters)
					{
						if (parameter.MemberInfo == null)
							throw new LinqException("Parameters which are not mapped to field are not supported.");

						current.Push(parameter.MemberInfo);
						foreach (var found in Collect(parameter.Expression, current))
							yield return found;
						current.Pop();
					}
				}
			}

			foreach (var found in Collect(expression, new (currentPath)))
				yield return found;
		}

		public static List<SqlPlaceholderExpression> CollectDistinctPlaceholders(Expression expression)
		{
			var result = new List<SqlPlaceholderExpression>();

			expression.Visit(result, static (list, e) =>
			{
				if (e is SqlPlaceholderExpression placeholder)
				{
					if (!list.Contains(placeholder))
						list.Add(placeholder);
				}
			});

			return result;
		}

		public bool CollectNullCompareExpressions(IClauseContext context, Expression expression, List<Expression> result)
		{
			switch (expression.NodeType)
			{
				case ExpressionType.Constant:
				case ExpressionType.Default:
				{
					result.Add(expression);
					return true;
				}
			}

			if (expression is SqlPlaceholderExpression or DefaultValueExpression)
			{
				result.Add(expression);
				return true;
			}

			if (expression is SqlGenericConstructorExpression generic)
			{
				foreach (var assignment in generic.Assignments)
				{
					if (!CollectNullCompareExpressions(context, assignment.Expression, result))
						return false;
				}

				foreach (var parameter in generic.Parameters)
				{
					if (!CollectNullCompareExpressions(context, parameter.Expression, result))
						return false;
				}

				return true;
			}

			if (expression is SqlDefaultIfEmptyExpression defaultIfEmptyExpression)
			{
				result.AddRange(defaultIfEmptyExpression.NotNullExpressions);
				return true;
			}

			if (expression is SqlEagerLoadExpression)
				return true;

			return false;
		}

		private static bool IsBooleanConstant(Expression expr, out bool? value)
		{
			value = null;
			if (expr.Type == typeof(bool) || expr.Type == typeof(bool?))
			{
				expr = expr.Unwrap();
				if (expr is ConstantExpression c)
				{
					value = c.Value as bool?;
					return true;
				}
				else if (expr is DefaultExpression)
				{
					value = expr.Type == typeof(bool) ? false : null;
					return true;
				}
				else if (expr is SqlPlaceholderExpression palacehoder)
				{
					if (palacehoder.Sql is ValueWord sqlValue)
					{
						value = sqlValue.Value as bool?;
						return true;
					}
					return false;
				}
			}
			return false;
		}

		// restores original types, lost due to C# compiler optimizations (issue #2041)
		private static bool RestoreCompare(ref Expression op1, ref Expression op2)
		{
			if (op1.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
			{
				var op1conv = (UnaryExpression)op1;

				// handle char replaced with int
				// (int)chr op CONST
				if (op1.Type == typeof(int) && op1conv.Operand.Type == typeof(char)
					&& (op2.NodeType is ExpressionType.Constant or ExpressionType.Convert or ExpressionType.ConvertChecked))
				{
					op1 = op1conv.Operand;
					op2 = op2.NodeType == ExpressionType.Constant
						? Expression.Constant(ConvertTo<char>.From(((ConstantExpression)op2).Value))
						: ((UnaryExpression)op2).Operand;
					return true;
				}
				// (int?)chr? op CONST
				else if (op1.Type == typeof(int?) && op1conv.Operand.Type == typeof(char?)
					&& (op2.NodeType == ExpressionType.Constant
						|| (op2.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked && ((UnaryExpression)op2).Operand.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)))
				{
					op1 = op1conv.Operand;
					op2 = op2.NodeType == ExpressionType.Constant
						? Expression.Constant(ConvertTo<char>.From(((ConstantExpression)op2).Value))
						: ((UnaryExpression)((UnaryExpression)op2).Operand).Operand;
					return true;
				}
				// handle enum replaced with integer
				// here byte/short values replaced with int, int+ values replaced with actual underlying type
				// (int)enum op const
				else if (op1conv.Operand.Type.IsEnum
					&& op2.NodeType == ExpressionType.Constant
						&& (op2.Type == Enum.GetUnderlyingType(op1conv.Operand.Type) || op2.Type == typeof(int)))
				{
					op1 = op1conv.Operand;
					op2 = Expression.Constant(Enum.ToObject(op1conv.Operand.Type, ((ConstantExpression)op2).Value!), op1conv.Operand.Type);
					return true;
				}
				// here underlying type used
				// (int?)enum? op (int?)enum
				else if (op1conv.Operand.Type.IsNullable() && Nullable.GetUnderlyingType(op1conv.Operand.Type)!.IsEnum
					&& op2.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked
					&& op2 is UnaryExpression op2conv2
					&& op2conv2.Operand.NodeType == ExpressionType.Constant
					&& op2conv2.Operand.Type == Nullable.GetUnderlyingType(op1conv.Operand.Type))
				{
					op1 = op1conv.Operand;
					op2 = Expression.Convert(op2conv2.Operand, op1conv.Operand.Type);
					return true;
				}
				// byte, sbyte and ushort comparison operands upcasted to int (issue #2039)
				else if (op2.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked
					&& op2 is UnaryExpression op2conv1
					&& op1conv.Operand.Type == op2conv1.Operand.Type
					&& op1conv.Operand.Type != typeof(object))
				{
					op1 = op1conv.Operand;
					op2 = op2conv1.Operand;
					return true;
				}

				// nullable enum vs underlying type compare (issue #2166)
				// Convert(member, int) == const(value, int)
				// we must replace it with:
				// member == const(value, member_type)
				if (op2 is ConstantExpression const2
					&& const2.Type == typeof(int)
					&& ConvertUtils.TryConvert(const2.Value, op1conv.Operand.Type, out var convertedValue))
				{
					op1 = op1conv.Operand;
					op2 = Expression.Constant(convertedValue, op1conv.Operand.Type);
					return true;
				}
			}

			return false;
		}

        #endregion
	}
}
