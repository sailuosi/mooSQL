using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace mooSQL.linq.SqlProvider
{
	using Common;
	using Extensions;
	using Linq;
	using Mapping;

	using mooSQL.data;
	using mooSQL.data.model;
	using mooSQL.data.model.affirms;
    using mooSQL.utils;
    using SqlQuery;
	using SqlQuery.Visitors;
    using static mooSQL.data.model.AffirmWord;

    public class SqlExpressionConvertVisitor : SqlQueryVisitor
	{
		protected bool            IsInsideNot;
		protected IExpWord? IsForPredicate;
		protected bool            VisitQueries;

		protected OptimizationContext OptimizationContext = default!;
		protected NullabilityContext  NullabilityContext  = default!;

		protected EvaluateContext EvaluationContext => OptimizationContext.EvaluationContext;
		protected SQLProviderFlags? SqlProviderFlags  => OptimizationContext.SqlProviderFlags;

		public SqlExpressionConvertVisitor(bool allowModify) : base(allowModify ? VisitMode.Modify : VisitMode.Transform, null)
		{
		}

		protected virtual bool SupportsBooleanInColumn    => false;
		protected virtual bool SupportsNullInColumn       => true;

		public virtual ISQLNode Convert(OptimizationContext optimizationContext, NullabilityContext nullabilityContext, Clause element, bool visitQueries, bool isInsideNot)
		{
			Cleanup();

			IsInsideNot         = isInsideNot;
			OptimizationContext = optimizationContext;
			NullabilityContext  = nullabilityContext;
			VisitQueries        = visitQueries;
			SetTransformationInfo(optimizationContext.TransformationInfoConvert);

			var newElement = ProcessElement(element);

			return newElement;
		}

		public override void Cleanup()
		{
			base.Cleanup();

			OptimizationContext = default!;
			NullabilityContext  = default!;
			IsInsideNot         = default;
			VisitQueries        = default;
		}

		public override Clause VisitColumnWord(ColumnWord column)
		{
			var newElement = base.VisitColumnWord(column);

			newElement = WrapBooleanExpression(newElement as IExpWord, includeFields: false) as Clause;
			if (!ReferenceEquals(newElement, column.Expression))
                column.Expression = (IExpWord)base.Visit(Optimize(newElement));

			newElement = WrapColumnExpression(column.Expression ) as Clause;
			if (!ReferenceEquals(newElement, column.Expression))
			{
                column.Expression = (IExpWord)base.Visit(Optimize(newElement));
			}

			return column.Expression as Clause;
		}

		public override Clause VisitOutputClause(OutputClause element)
		{
			var result = (OutputClause)base.VisitOutputClause(element);

			if (result.OutputColumns != null)
			{
				var newElements = result.OutputColumns.VisitElements( GetVisitMode(element), e => (ExpWordBase)WrapBooleanExpression(e, includeFields : false));
				if (!ReferenceEquals(newElements, result.OutputColumns))
				{
					return new OutputClause()
					{
						DeletedTable = result.DeletedTable, 
						InsertedTable = result.InsertedTable,
						OutputTable = result.OutputTable,
						OutputItems = result.OutputItems,
						OutputColumns = newElements
					};
				}
			}

			return result;
		}

		public override Clause VisitConditionExpression(ConditionWord element)
		{
			var newElement = base.VisitConditionExpression(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			newElement = ConvertSqlCondition(element ) as Clause;

			if (!ReferenceEquals(newElement, element))
			{
				return Visit(NotifyReplaced(newElement, element));
			}

			return element;
		}

        public CaseWord.CaseItem VisitCaseItem(CaseWord.CaseItem element)
		{
			var newElement = element.Update((IAffirmWord)VisitAffirmWord(element.Condition), (IExpWord)VisitIExpWord(element.ResultExpression));

            newElement = ConvertCaseItem(newElement);

			return newElement;
		}

		public override Clause VisitCaseExpression(CaseWord element)
		{
			var newElement = base.VisitCaseExpression(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			newElement = ConvertSqlCaseExpression(element) as Clause;

			if (!ReferenceEquals(newElement, element))
			{
				return Visit(NotifyReplaced(newElement, element));
			}

			return element;
		}

		public override Clause VisitSelectQuery(SelectQueryClause selectQuery)
		{
			if (!VisitQueries)
				return selectQuery;

			var saveIsInsideNot = IsInsideNot;
			IsInsideNot = false;

			var newQuery = base.VisitSqlQuery(selectQuery);

			IsInsideNot = saveIsInsideNot;
			return newQuery;
		}

		public override Clause VisitAffirmExpr(mooSQL.data.model.affirms.Expr predicate)
		{
			var saveIsForPredicate = IsForPredicate;
			IsForPredicate = predicate.Expr1;

			var result = base.VisitAffirmExpr(predicate);

			IsForPredicate = saveIsForPredicate;
			return result;
		}

		public override Clause VisitFieldWord(FieldWord element)

        {
			var newElement = base.VisitFieldWord(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			if (element.SystemType?.UnwrapNullable() == typeof(bool))
			{
				if (ReferenceEquals(element, IsForPredicate))
					return ConvertToBooleanSearchCondition(element);
			}

			return element;
		}

		//public override Clause VisitColumnWord(ColumnWord element)
		//{
		//	var newElement = base.VisitColumnWord(element);

		//	if (!ReferenceEquals(newElement, element))
		//		return Visit(newElement);

		//	if (element.SystemType?.ToUnderlying() == typeof(bool))
		//	{
		//		if (ReferenceEquals(element, IsForPredicate))
		//			return ConvertToBooleanSearchCondition(element);
		//	}

		//	return element;
		//}

		public override Clause VisitAffirmNot(Not predicate)
		{
			var saveIsInsideNot = IsInsideNot;
			IsInsideNot = true;

			var newPredicate = base.VisitAffirmNot(predicate);

			IsInsideNot = saveIsInsideNot;

			return newPredicate;
		}

        public override Clause VisitValueWord(ValueWord element)
		{
			var newElement = base.VisitValueWord(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			if (element.Value is Sql.SqlID)
				return element;

			if (!DBLive.dialect.mapping.CanConvertToSql(element.Value))
			{
				// we cannot generate SQL literal, so just convert to parameter
				var param = OptimizationContext.SuggestDynamicParameter(element.ValueType, element.Value);
				return param;
			}

			return element;
		}

		protected Clause Optimize(Clause element)
		{
			var res= OptimizationContext.OptimizerVisitor.Optimize(EvaluationContext, NullabilityContext, OptimizationContext.TransformationInfo, element, VisitQueries, IsInsideNot, reduceBinary : false);
			return res as Clause;
		}

		public override Clause VisitAffirmExprExpr(mooSQL.data.model.affirms.ExprExpr predicate)
		{
			var newElement = base.VisitAffirmExprExpr(predicate);

			if (!ReferenceEquals(newElement, predicate))
			{
				return Visit(Optimize(newElement));
			}

			var newPredicate = ConvertExprExprPredicate(predicate);

			if (!ReferenceEquals(newPredicate, predicate))
			{
				newPredicate = Optimize(newPredicate as Clause);
				newPredicate = Visit(newPredicate as Clause);
			}

			return newPredicate as Clause;
		}

		public override Clause VisitCompareToExpression(CompareToWord element)
		{
			var newElement = base.VisitCompareToExpression(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			var caseExpression = new CaseWord(new DbDataType(typeof(int)),
				new CaseWord.CaseItem[]
				{
					new(new SearchConditionWord().AddGreater(element.Expression1, element.Expression2,DBLive.dialect.Option.CompareNullsAsValues), new ValueWord(1)),
					new(new SearchConditionWord().AddEqual(element.Expression1, element.Expression2, DBLive.dialect.Option.CompareNullsAsValues), new ValueWord(0))
				},
				new ValueWord(-1));

			return Visit(Optimize(caseExpression));
		}

		public virtual ISQLNode ConvertExprExprPredicate(mooSQL.data.model.affirms.ExprExpr predicate)
		{
			var unwrapped = QueryHelper.UnwrapNullablity(predicate.Expr1);
			if (unwrapped.NodeType == ClauseType.SqlRow)
			{
				// Do not convert for remote context
				if (SqlProviderFlags == null)
					return predicate;

				var newPredicate = ConvertRowExprExpr(predicate, EvaluationContext);
				if (!ReferenceEquals(newPredicate, predicate))
				{
					return Visit(Optimize(newPredicate as Clause));
				}
			}

			if (SqlProviderFlags is { SupportsBooleanComparison: false })
			{
				if (QueryHelper.UnwrapNullablity(predicate.Expr2) is not (ValueWord or ParameterWord) && QueryHelper.UnwrapNullablity(predicate.Expr1) is not (ValueWord or ParameterWord))
				{
					var expr1 = WrapBooleanExpression(predicate.Expr1, includeFields : true);
					var expr2 = WrapBooleanExpression(predicate.Expr2, includeFields : true);

					if (!ReferenceEquals(expr1, predicate.Expr1) || !ReferenceEquals(expr2, predicate.Expr2))
					{
						return new mooSQL.data.model.affirms.ExprExpr(expr1, predicate.Operator, expr2, predicate.WithNull);
					}
				}
			}

			return predicate;
		}

		static FieldWord ExpectsUnderlyingField(IExpWord expr)
		{
			var result = QueryHelper.GetUnderlyingField(expr);
			if (result == null)
				throw new InvalidOperationException($"Cannot retrieve underlying field for '{expr.ToDebugString()}'.");
			return result;
		}

		public override Clause VisitAffirmInList(InList predicate)
		{
			var newElement = base.VisitAffirmInList(predicate);

			if (!ReferenceEquals(newElement, predicate))
				return Visit(newElement);

			if (predicate.Expr1.NodeType == ClauseType.SqlRow)
			{
				var converted = ConvertRowInList(predicate) as Clause;
				if (!ReferenceEquals(converted, predicate))
				{
					converted = Optimize(converted );
					converted = base.Visit(converted);
					return converted;
				}
			}

			if (predicate.Values.Count == 0)
				return ClauseExtensions.MakeBool(predicate.IsNot);
            //[SqlParameter parameter]
            if (predicate.Values.Count==1 && predicate.Values[0] is ParameterWord parameter)
			{
				var paramValue = parameter.GetParameterValue(EvaluationContext.ParameterValues);

				if (paramValue.ProviderValue == null)
					return ClauseExtensions.MakeBool(predicate.IsNot);

				if (paramValue.ProviderValue is IEnumerable items)
				{
					if (predicate.Expr1 is ITableNode table)
					{
						var keys  = table.GetKeys(true);

						if (keys == null || keys.Count == 0)
							throw new Exception("Cant create IN expression.");

						if (keys.Count == 1)
						{
							var values = new List<IExpWord>();
							var field  = ExpectsUnderlyingField(keys[0]);
							var cd     = field.ColumnDescriptor;

							foreach (var item in items)
							{
								values.Add(DBLive.GetSqlValueFromObject(cd, item!));
							}

							if (values.Count == 0)
								return ClauseExtensions.MakeBool(predicate.IsNot);

							return new InList(keys[0], null, predicate.IsNot, values);
						}

						{
							var sc = new SearchConditionWord(true);

							foreach (var item in items)
							{
								var itemCond = new SearchConditionWord();

								foreach (var key in keys)
								{
									var field    = ExpectsUnderlyingField(key);
									var cd       = field.ColumnDescriptor;
									var sqlValue = DBLive.GetSqlValueFromObject(cd, item!);
                                    //TODO: review
                                    IAffirmWord p = sqlValue.Value == null ?
										new IsNull  (field, false) :
										new mooSQL.data.model.affirms.ExprExpr(field, AffirmWord.Operator.Equal, sqlValue, null);

									itemCond.Add(p);
								}

								sc.Add(itemCond);
							}

							if (sc.Predicates.Count == 0)
								return ClauseExtensions.MakeBool(predicate.IsNot);

							return Optimize(sc.MakeNot(predicate.IsNot) as Clause);
						}
					}

					if (predicate.Expr1 is ObjectWord expr)
					{
						var parameters = expr.InfoParameters;
						if (parameters.Length == 1)
						{
							var values = new List<IExpWord>();

							foreach (var item in items)
								values.Add(DBLive.GetSqlValue (item.GetType(),item!,null ));

							if (values.Count == 0)
								return ClauseExtensions.MakeBool(predicate.IsNot);

							return new InList(parameters[0].Sql, null, predicate.IsNot, values);
						}

						var sc = new SearchConditionWord(true);

						foreach (var item in items)
						{
							var itemCond = new SearchConditionWord();

							for (var i = 0; i < parameters.Length; i++)
							{
								var sql   = parameters[i].Sql;
								var value = DBLive.GetSqlValue(item.GetType(), item!, null);
                                IAffirmWord cond  = value == null ?
									new IsNull  (sql, false) :
									new mooSQL.data.model.affirms.ExprExpr(sql, AffirmWord.Operator.Equal, value, null);

								itemCond.Predicates.Add(cond);
							}

							sc.Add(itemCond);
						}

						if (sc.Predicates.Count == 0)
							return ClauseExtensions.MakeBool(predicate.IsNot);

						return Optimize(sc.MakeNot(predicate.IsNot) as Clause);
					}
				}
			}

			return predicate;
		}

		public override Clause VisitSearchStringPredicate(mooSQL.data.model.affirms.SearchString predicate)
		{
			var newElement = base.VisitSearchStringPredicate(predicate);

			if (!ReferenceEquals(newElement, predicate))
				return Visit(newElement);

			var newPredicate = ConvertSearchStringPredicate(predicate) as Clause;
			if (!ReferenceEquals(newPredicate, predicate))
			{
				newPredicate = Optimize(newPredicate);
				newPredicate = Visit(newPredicate);
			}

			return newPredicate;
		}

		public virtual IAffirmWord ConvertSearchStringPredicate(mooSQL.data.model.affirms.SearchString predicate)
		{
			if (predicate.CaseSensitive.EvaluateBoolExpression(EvaluationContext) == false)
			{
				predicate = new mooSQL.data.model.affirms.SearchString(
                    PseudoFunctions.MakeToLower(predicate.Expr1),
					predicate.IsNot,
                    PseudoFunctions.MakeToLower(predicate.Expr2),
					predicate.Kind,
					new ValueWord(false));
			}

			return ConvertSearchStringPredicateViaLike(predicate);
		}

		#region LIKE support

		/// <summary>
		/// Escape sequence/character to escape special characters in LIKE predicate (defined by <see cref="LikeCharactersToEscape"/>).
		/// Default: <c>"~"</c>.
		/// </summary>
		public virtual string LikeEscapeCharacter         => "~";
		public virtual string LikeWildcardCharacter       => "%";
		public virtual bool   LikePatternParameterSupport => true;
		public virtual bool   LikeValueParameterSupport   => true;
		/// <summary>
		/// Should be <c>true</c> for provider with <c>LIKE ... ESCAPE</c> modifier support.
		/// Default: <c>true</c>.
		/// </summary>
		public virtual bool   LikeIsEscapeSupported       => true;

		protected static string[] StandardLikeCharactersToEscape = {"%", "_", "?", "*", "#", "[", "]"};

		/// <summary>
		/// Characters with special meaning in LIKE predicate (defined by <see cref="LikeCharactersToEscape"/>) that should be escaped to be used as matched character.
		/// Default: <c>["%", "_", "?", "*", "#", "[", "]"]</c>.
		/// </summary>
		public virtual string[] LikeCharactersToEscape => StandardLikeCharactersToEscape;

		public virtual string EscapeLikeCharacters(string str, string escape)
		{
			var newStr = str;

			newStr = newStr.Replace(escape, escape + escape);

			var toEscape = LikeCharactersToEscape;
			foreach (var s in toEscape)
			{
				newStr = newStr.Replace(s, escape + s);
			}

			return newStr;
		}

		static IExpWord GenerateEscapeReplacement(IExpWord expression, IExpWord character, IExpWord escapeCharacter)
		{
			var result = PseudoFunctions.MakeReplace(expression, character, new BinaryWord(typeof(string), escapeCharacter, "+", character, PrecedenceLv.Additive));
			return result;
		}

		public static IExpWord GenerateEscapeReplacement(IExpWord expression, IExpWord character)
		{
			var result = PseudoFunctions.MakeReplace(
				expression,
				character,
				new BinaryWord(typeof(string), new ValueWord("["), "+",
					new BinaryWord(typeof(string), character, "+", new ValueWord("]"), PrecedenceLv.Additive),
                    PrecedenceLv.Additive));
			return result;
		}

		/// <summary>
		/// Implements LIKE pattern escaping logic for provider without ESCAPE clause support (<see cref="LikeIsEscapeSupported"/> is <c>false</c>).
		/// Default logic prefix characters from <see cref="LikeCharactersToEscape"/> with <see cref="LikeEscapeCharacter"/>.
		/// </summary>
		/// <param name="str">Raw pattern value.</param>
		/// <returns>Escaped pattern value.</returns>
		protected virtual string EscapeLikePattern(string str)
		{
			foreach (var s in LikeCharactersToEscape)
				str = str.Replace(s, LikeEscapeCharacter + s);

			return str;
		}

		public virtual IExpWord EscapeLikeCharacters(IExpWord expression, ref IExpWord? escape)
		{
			var newExpr = expression;

			escape ??= new ValueWord(LikeEscapeCharacter);

			newExpr = GenerateEscapeReplacement(newExpr, escape, escape);

			var toEscape = LikeCharactersToEscape;
			foreach (var s in toEscape)
			{
				newExpr = GenerateEscapeReplacement(newExpr, new ValueWord(s), escape);
			}

			return newExpr;
		}

		protected IAffirmWord ConvertSearchStringPredicateViaLike(mooSQL.data.model.affirms.SearchString predicate)
		{
			if (predicate.Expr2.TryEvaluateExpression(EvaluationContext, out var patternRaw)
				&& Converter.TryConvertToString(patternRaw, out var patternRawValue))
			{
				if (patternRawValue == null)
					return new	IsTrue(new ValueWord(true), new ValueWord(true), new ValueWord(false), null, predicate.IsNot);

				var patternValue = LikeIsEscapeSupported
					? EscapeLikeCharacters(patternRawValue, LikeEscapeCharacter)
					: EscapeLikePattern(patternRawValue);

				patternValue = predicate.Kind switch
				{
                    mooSQL.data.model.affirms.SearchString.SearchKind.StartsWith => patternValue + LikeWildcardCharacter,
                    mooSQL.data.model.affirms.SearchString.SearchKind.EndsWith   => LikeWildcardCharacter + patternValue,
                    mooSQL.data.model.affirms.SearchString.SearchKind.Contains   => LikeWildcardCharacter + patternValue + LikeWildcardCharacter,
					_ => throw new InvalidOperationException($"Unexpected predicate kind: {predicate.Kind}")
				};

				var patternExpr = LikePatternParameterSupport
                    ? QueryHelper.CreateSqlValue(patternValue, QueryHelper.GetDbDataType(predicate.Expr2, DBLive), predicate.Expr2)
					: new ValueWord(patternValue);

				var valueExpr = predicate.Expr1;
				if (!LikeValueParameterSupport)
				{
					var c = predicate.Expr1 as Clause;

                    c.VisitAll(static e =>
					{
						if (e is ParameterWord p)
							p.IsQueryParameter = false;
					});
				}

				return new mooSQL.data.model.affirms.Like(valueExpr, predicate.IsNot, patternExpr,
                    LikeIsEscapeSupported && (patternValue != patternRawValue) ? new ValueWord(LikeEscapeCharacter) : null);
			}
			else
			{
                IExpWord? escape = null;

				var patternExpr = EscapeLikeCharacters(predicate.Expr2, ref escape);

				var anyCharacterExpr = new ValueWord(LikeWildcardCharacter);

				patternExpr = predicate.Kind switch
				{
                    mooSQL.data.model.affirms.SearchString.SearchKind.StartsWith => new BinaryWord(typeof(string), patternExpr, "+", anyCharacterExpr, PrecedenceLv.Additive),
                    mooSQL.data.model.affirms.SearchString.SearchKind.EndsWith   => new BinaryWord(typeof(string), anyCharacterExpr, "+", patternExpr, PrecedenceLv.Additive),
                    mooSQL.data.model.affirms.SearchString.SearchKind.Contains   => new BinaryWord(typeof(string), new BinaryWord(typeof(string), anyCharacterExpr, "+", patternExpr, PrecedenceLv.Additive), "+", anyCharacterExpr, PrecedenceLv.Additive),
					_ => throw new InvalidOperationException($"Unexpected predicate kind: {predicate.Kind}")
				};

				return new mooSQL.data.model.affirms.Like(predicate.Expr1, predicate.IsNot, patternExpr, LikeIsEscapeSupported ? escape : null);
			}
		}

		#endregion

		#region Visitor overrides

		public override Clause VisitAffirmIsNull(IsNull predicate)
		{
			var newElement = base.VisitAffirmIsNull(predicate);

			if (!ReferenceEquals(newElement, predicate))
				return Visit(newElement);

			if (NullabilityContext.IsEmpty)
				return predicate;

			if (QueryHelper.UnwrapNullablity(predicate.Expr1) is RowWord sqlRow)
			{
				if (ConvertRowIsNullPredicate(sqlRow, predicate.IsNot, out var rowIsNullFallback))
				{
					return Visit(rowIsNullFallback as Clause);
				}
			}

			return predicate;
		}

		public override Clause VisitFunctionWord(FunctionWord element)
		{
			var newElement = base.VisitFunctionWord(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			newElement = ConvertSqlFunction(element);
			if (!ReferenceEquals(newElement, element))
				return Visit(Optimize(newElement));

			return element;
		}

		public override Clause VisitExpression(ExpressionWord element)
		{
			var newElement = base.VisitExpression(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			newElement = ConvertSqlExpression(element);
			if (!ReferenceEquals(newElement, element))
			{
				newElement = Visit(Optimize(newElement));
			}

			return newElement;
		}

		public override Clause VisitAffirmLike(mooSQL.data.model.affirms.Like predicate)
		{
			var newElement = base.VisitAffirmLike(predicate);

			if (!ReferenceEquals(newElement, predicate))
				return Visit(newElement);

			newElement = ConvertLikePredicate(predicate);
			if (!ReferenceEquals(newElement, predicate))
			{
				newElement = Visit(Optimize(newElement));
			}
			return newElement;
		}

		public override Clause VisitBinaryExpression(BinaryWord element)
		{
			var newElement = base.VisitBinaryExpression(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			newElement = ConvertSqlBinaryExpression(element);
			if (!ReferenceEquals(newElement, element))
			{
				newElement = Visit(Optimize(newElement));
			}
			return newElement;
		}

		public override Clause VisitInlinedSqlExpression(InlinedSqlWord element)
		{
			var newElement = base.VisitInlinedSqlExpression(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			newElement = element.GetSqlExpression(EvaluationContext) as Clause;
			if (!ReferenceEquals(newElement, element))
			{
				newElement = Visit(Optimize(newElement));
			}
			return newElement;
		}

        public override Clause VisitInlinedToSqlExpression(InlinedToSqlWord element)
		{
			var newElement = base.VisitInlinedToSqlExpression(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			newElement = element.GetSqlExpression(EvaluationContext) as Clause;
			if (!ReferenceEquals(newElement, element))
			{
				newElement = Visit(Optimize(newElement));
			}
			return newElement;
		}

        public override Clause VisitAffirmBetween(Between predicate)
		{
			var newElement = base.VisitAffirmBetween(predicate);

			if (!ReferenceEquals(newElement, predicate))
				return Visit(newElement);

			if (SqlProviderFlags != null)
			{
				if (!SqlProviderFlags.RowConstructorSupport.HasFlag(RowFeature.Between) && QueryHelper.UnwrapNullablity(predicate.Expr1) is RowWord)
				{
					return Visit(Optimize(ConvertBetweenPredicate(predicate)));
				}
			}

			return newElement;
		}

		public override Clause VisitAffirmInSubQuery(InSubQuery predicate)
		{
			if (predicate.DoNotConvert)
				return base.VisitAffirmInSubQuery(predicate);

			var newPredicate = base.VisitAffirmInSubQuery(predicate);

			// preparing for remoting
			if (SqlProviderFlags == null)
				return newPredicate;

			if (!ReferenceEquals(newPredicate, predicate))
				return Visit(newPredicate);

			var doNotSupportCorrelatedSubQueries = SqlProviderFlags.DoesNotSupportCorrelatedSubquery;

			var testExpression  = predicate.Expr1;
			var valueExpression = predicate.SubQuery.Select.Columns[0].Expression;

			if (NullabilityContext.CanBeNull(testExpression) && NullabilityContext.CanBeNull(valueExpression))
			{
				if (doNotSupportCorrelatedSubQueries)
				{
					newPredicate = EmulateNullability(predicate) as Clause;

					if (!ReferenceEquals(newPredicate, predicate))
						return Visit(newPredicate);
				}
				else
				{
					return Visit(ConvertToExists(predicate));
				}
			}

			if (!doNotSupportCorrelatedSubQueries && (DBLive.dialect.Option.PreferExistsForScalar || SqlProviderFlags.IsExistsPreferableForContains))
			{
				return Visit(ConvertToExists(predicate));
			}

			if (NullabilityContext.CanBeNull(testExpression) && !NullabilityContext.CanBeNull(valueExpression) && predicate.IsNot)
			{
				var withoutNull = new InSubQuery(testExpression, predicate.IsNot, predicate.SubQuery, true);

				var sc = new SearchConditionWord(predicate.IsNot)
					.Add(new IsNull(testExpression, false))
					.Add(withoutNull);

				return Visit(sc);
			}

			return predicate;
		}

		public override Clause VisitOrderByItem(OrderByWord element)
		{
			var newElement = (OrderByWord)base.VisitOrderByItem(element);

			var wrapped = WrapBooleanExpression(newElement.Expression, includeFields : false);

			if (!ReferenceEquals(wrapped, newElement.Expression))
			{
				if (GetVisitMode(newElement) == VisitMode.Modify)
				{
					newElement.Expression = wrapped;
				}
				else
				{
					newElement = new OrderByWord(wrapped, newElement.IsDescending, newElement.IsPositioned);
				}
			}

			return newElement;
		}

		public override Clause VisitSetWord(SetWord element)
		{
			var newElement = (SetWord)base.VisitSetWord(element);

			var wrapped = newElement.Expression == null ? null : WrapBooleanExpression(newElement.Expression, includeFields : false);

			if (!ReferenceEquals(wrapped, newElement.Expression))
			{
				if (GetVisitMode(newElement) == VisitMode.Modify)
				{
					newElement.Expression = wrapped;
				}
				else
				{
					newElement = new SetWord(newElement.Column, wrapped);
				}
			}

			return newElement;
		}

		public Clause VisitSqlGroupByItem(Clause element)
		{
			var newItem = base.Visit(element);

			return WrapBooleanExpression(newItem as IExpWord, includeFields: false) as Clause;
		}

        public override Clause VisitCastExpression(CastWord element)
		{
			var newElement = base.VisitCastExpression(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			var converted = ConvertConversion(element);
			if (!ReferenceEquals(converted, element))
			{
				return Visit(Optimize(converted as Clause));
			}

			return element;
		}

		#endregion Visitor overrides

		public virtual Clause ConvertSqlExpression(ExpressionWord element)
		{
			return element;
		}

		public virtual Clause ConvertSqlFunction(FunctionWord func)
		{
			switch (func.Name)
			{
				case "MAX":
				case "MIN":
				{
					if (func.SystemType == typeof(bool) || func.SystemType == typeof(bool?))
					{
						if (func.Parameters[0] is not IAffirmWord predicate)
						{
							predicate = new mooSQL.data.model.affirms.Expr(func.Parameters[0]);
						}

						return new FunctionWord(typeof(int), func.Name, new ConditionWord(predicate, new ValueWord(1), new ValueWord(0)));
					}

					break;
				}

				case PseudoFunctions.CONVERT_FORMAT:
				{
					return new FunctionWord(func.SystemType, "Convert", func.Parameters[0], func.Parameters[2], func.Parameters[3]);
				}

				case PseudoFunctions.TO_LOWER: return func.WithName("Lower");
				case PseudoFunctions.TO_UPPER: return func.WithName("Upper");
				case PseudoFunctions.REPLACE:  return func.WithName("Replace");
				case PseudoFunctions.COALESCE: return func.WithName("Coalesce");
			}

			return func;
		}

		public virtual Clause ConvertLikePredicate(mooSQL.data.model.affirms.Like predicate)
		{
			return predicate;
		}

        IAffirmWord EmulateNullability(InSubQuery inPredicate)
		{
			var sc = new SearchConditionWord(true);

			var testExpr = inPredicate.Expr1;

			var intTestSubQuery = inPredicate.SubQuery.Clone();
			intTestSubQuery = WrapIfNeeded(intTestSubQuery);
			var inSubqueryExpr = intTestSubQuery.Select.Columns[0].Expression;

			intTestSubQuery.Select.Columns.Clear();
			intTestSubQuery.Select.AddNewColumn(new ValueWord(1));
			intTestSubQuery.Where.SearchCondition.AddIsNull(inSubqueryExpr);

			sc.AddAnd((SearchConditionWord sub) => sub
					.AddIsNull(testExpr)
					.Add(new InSubQuery(new ValueWord(1), false, intTestSubQuery, doNotConvert: true))
				)
				.AddAnd((SearchConditionWord sub) => sub
					.AddIsNotNull(testExpr)
					.Add(new InSubQuery(testExpr, false, inPredicate.SubQuery, doNotConvert: true))
				);

			var result = Optimize(sc.MakeNot(inPredicate.IsNot) as Clause);

			return (IAffirmWord)result;
		}

		static SelectQueryClause WrapIfNeeded(SelectQueryClause selectQuery)
		{
			if (selectQuery.Select.HasModifier || !selectQuery.GroupBy.IsEmpty || selectQuery.Select.Columns.content.Any(c => QueryHelper.IsAggregationOrWindowFunction(c.Expression)))
			{
				var newQuery = new SelectQueryClause();
				newQuery.From.Tables.Add(new TableSourceWord(selectQuery, null));

				foreach (var column in selectQuery.Select.Columns.content)
				{
					newQuery.Select.AddNew(column);
				}

				selectQuery = newQuery;
			}

			return selectQuery;
		}

        Clause ConvertToExists(InSubQuery inPredicate)
		{
            IExpWord[] testExpressions;
			if (inPredicate.Expr1 is RowWord sqlRow)
			{
				testExpressions = sqlRow.Values;
			}
			else
			{
				testExpressions = new IExpWord[] { inPredicate.Expr1 };
			}

			var subQuery = inPredicate.SubQuery;

			if (inPredicate.SubQuery.Where.SearchCondition.IsOr)
				throw new InvalidOperationException("Not expected root SearchCondition.");

			if (GetVisitMode(subQuery) == VisitMode.Transform || subQuery.Where.SearchCondition.IsOr)
			{
				subQuery = subQuery.CloneQuery();
				subQuery.Where.EnsureConjunction();
			}

			subQuery = WrapIfNeeded(subQuery);

			var predicates = new List<IAffirmWord>(testExpressions.Length);

			var sc = new SearchConditionWord(false);

			for (int i = 0; i < testExpressions.Length; i++)
			{
				var testValue = testExpressions[i];
				var expr      = subQuery.Select.Columns[i].Expression;

				predicates.Add(new mooSQL.data.model.affirms.ExprExpr(testValue, AffirmWord.Operator.Equal, expr, DBLive.dialect.Option.CompareNullsAsValues ? true : null));
			}

			subQuery.Select.Columns.Clear();
			subQuery.Where.SearchCondition.AddRange(predicates);

			sc.AddExists(subQuery, inPredicate.IsNot);

			var result = Optimize(sc);

			result = Visit(result);

			return result;
		}

		public virtual Clause ConvertBetweenPredicate(	Between between)
		{
			var newPredicate = new SearchConditionWord()
				.AddGreaterOrEqual(between.Expr1, between.Expr2, false)
				.AddLessOrEqual(between.Expr1, between.Expr3, false)
				.MakeNot(between.IsNot);

			return newPredicate as Clause;
		}

		public virtual Clause ConvertSqlBinaryExpression(BinaryWord element)
		{
			switch (element.Operation)
			{
				case "+":
				{
					if (element.Expr1.SystemType == typeof(string) && element.Expr2.SystemType != typeof(string))
					{
						var len = element.Expr2.SystemType == null ? 100 : DataTypeWord.GetMaxDisplaySize(DBLive.dialect.mapping.GetDbDataType(element.Expr2.SystemType).DataType);

						if (len == null || len <= 0)
							len = 100;

						return new BinaryWord(
							element.SystemType,
							element.Expr1,
							element.Operation,
							(IExpWord)Visit(PseudoFunctions.MakeCast(element.Expr2, new DbDataType(typeof(string), DataFam.VarChar, null, len.Value))),
							element.Precedence);
					}

					if (element.Expr1.SystemType != typeof(string) && element.Expr2.SystemType == typeof(string))
					{
						var len = element.Expr1.SystemType == null ? 100 : DataTypeWord.GetMaxDisplaySize(DBLive.dialect.mapping.GetDbDataType(element.Expr2.SystemType).DataType);

						if (len == null || len <= 0)
							len = 100;

						return new BinaryWord(
							element.SystemType,
							(IExpWord)Visit(PseudoFunctions.MakeCast(element.Expr1, new DbDataType(typeof(string), DataFam.VarChar, null, len.Value))),
							element.Operation,
							element.Expr2,
							element.Precedence);
					}

					break;
				}
			}

			return element;
		}

		protected virtual IExpWord ConvertSqlCondition(ConditionWord element)
		{
			var trueValue  = WrapBooleanExpression(element.TrueValue, includeFields : true);
			var falseValue = WrapBooleanExpression(element.FalseValue, includeFields : true);

			if (!ReferenceEquals(trueValue, element.TrueValue) || !ReferenceEquals(falseValue, element.FalseValue))
			{
				return new ConditionWord(element.Condition, trueValue, falseValue);
			}

			return element;
		}

		protected virtual IExpWord ConvertSqlCaseExpression(CaseWord element)
		{
			if (element.ElseExpression != null)
			{
				var elseExpression = WrapBooleanExpression(element.ElseExpression, includeFields : true);

				if (!ReferenceEquals(elseExpression, element.ElseExpression))
				{
					return new CaseWord(element.Type, element.Cases, elseExpression);
				}
			}

			return element;
		}

		protected virtual CaseWord.CaseItem ConvertCaseItem(CaseWord.CaseItem newElement)
		{
			var resultExpr = WrapBooleanExpression(newElement.ResultExpression, includeFields : true);

			if (!ReferenceEquals(resultExpr, newElement.ResultExpression))
			{
				newElement = new CaseWord.CaseItem(newElement.Condition, resultExpr);
			}

			return newElement;
		}

		protected virtual IExpWord WrapBooleanExpression(IExpWord expr, bool includeFields)
		{
			if (SqlProviderFlags == null)
				return expr;

			if (expr.SystemType == typeof(bool))
			{
				var unwrapped = QueryHelper.UnwrapNullablity(expr);
				if (unwrapped is IAffirmWord || includeFields && unwrapped.NodeType is ClauseType.Column or ClauseType.SqlField)
				{
					var predicate = unwrapped as IAffirmWord ?? ConvertToBooleanSearchCondition(expr);

					var trueValue  = new ValueWord(true);
					var falseValue = new ValueWord(false);

					if (expr.CanBeNullable(NullabilityContext))
					{
						var conditionExpr = new ConditionWord(predicate, trueValue, falseValue);
						expr = new ConditionWord(new IsNull(expr, false), new ValueWord(QueryHelper.GetDbDataType(expr, DBLive), null), conditionExpr);
					}
					else
					{
						expr = new ConditionWord(predicate, trueValue, falseValue);
					}

					expr = (IExpWord)base.VisitIExpWord(expr);
				}
			}

			return expr;
		}

		protected virtual IExpWord WrapColumnExpression(IExpWord expr)
		{
			if (!SupportsNullInColumn && QueryHelper.UnwrapNullablity(expr) is ValueWord sqlValue && sqlValue.Value == null)
			{
				return new CastWord(sqlValue, QueryHelper.GetDbDataType(sqlValue, DBLive), null, true);
			}

			return expr;
		}

		#region DataTypes

		protected virtual int? GetMaxLength(DbDataType      type) { return DataTypeWord.GetMaxLength(type.DataType); }
		protected virtual int? GetMaxPrecision(DbDataType   type) { return DataTypeWord.GetMaxPrecision(type.DataType); }
		protected virtual int? GetMaxScale(DbDataType       type) { return DataTypeWord.GetMaxScale(type.DataType); }
		protected virtual int? GetMaxDisplaySize(DbDataType type) { return DataTypeWord.GetMaxDisplaySize(type.DataType); }

		/// <summary>
		/// Implements <see cref="CastWord"/> conversion.
		/// </summary>
		protected virtual IExpWord ConvertConversion(CastWord cast)
		{
			var toDataType = cast.ToType;

			if (cast.SystemType == typeof(string) && cast.Expression is ValueWord value)
			{
				if (value.Value is char charValue)
					return new ValueWord(cast.Type, charValue.ToString());
			}

			var fromDbType = QueryHelper.GetDbDataType(cast.Expression, DBLive);

			if (toDataType.Length > 0)
			{
				var maxLength = toDataType.SystemType == typeof(string) ? GetMaxDisplaySize(fromDbType) : GetMaxLength(fromDbType);
				var newLength = maxLength != null && maxLength >= 0 ? Math.Min(toDataType.Length ?? 0, maxLength.Value) : fromDbType.Length;

				var newDataType = toDataType.WithLength(newLength);
				if (!newDataType.Equals(toDataType))
				{
					return new CastWord(cast.Expression, newDataType, cast.FromType);
				}
			}
			else if (!cast.IsMandatory && fromDbType.SystemType == typeof(short) && toDataType.SystemType == typeof(int))
			{
				return cast.Expression;
			}

			if (SqlProviderFlags?.SupportsBooleanComparison == false)
			{
				if (NullTypeExtensions.UnwrapNullable(cast.SystemType) == typeof(bool))
				{
					if (ReferenceEquals(cast, IsForPredicate))
						return ConvertToBooleanSearchCondition(cast.Expression);
				}
			}

			return cast;
		}

		#endregion

		#region SqlRow

		protected IAffirmWord ConvertRowExprExpr(mooSQL.data.model.affirms.ExprExpr predicate, EvaluateContext context)
		{
			if (SqlProviderFlags == null)
				return predicate;

			var op = predicate.Operator;
			var feature = op is AffirmWord.Operator.Equal or AffirmWord.Operator.NotEqual
				? RowFeature.Equality
				: op is AffirmWord.Operator.Overlaps
                    ? RowFeature.Overlaps
					: RowFeature.Comparisons;

			var expr2 = QueryHelper.UnwrapNullablity(predicate.Expr2);

			switch (expr2)
			{
				// ROW(a, b) IS [NOT] NULL
				case ValueWord { Value: null }:
				{
					if (op is not (AffirmWord.Operator.Equal or AffirmWord.Operator.NotEqual))
						throw new LinqException("Null SqlRow is only allowed in equality comparisons");

					if (ConvertRowIsNullPredicate((RowWord)predicate.Expr2, op is AffirmWord.Operator.NotEqual, out var rowIsNullFallback))
					{
						return rowIsNullFallback;
					}

					break;
				}

				// ROW(a, b) operator ROW(c, d)
				case RowWord rhs:
				{
					if (!SqlProviderFlags.RowConstructorSupport.HasFlag(feature))
						return RowComparisonFallback(op, (RowWord)predicate.Expr1, rhs, context);
					break;
				}

				// ROW(a, b) operator (SELECT c, d)
				case SelectQueryClause:
				{
					if (!SqlProviderFlags.RowConstructorSupport.HasFlag(feature) ||
					    !SqlProviderFlags.RowConstructorSupport.HasFlag(RowFeature.CompareToSelect))
						throw new LinqException("SqlRow comparisons to SELECT are not supported by this DB provider");
					break;
				}

				default:
					throw new LinqException("Inappropriate SqlRow expression, only Sql.Row() and sub-selects are valid.");
			}

			// Default ExprExpr translation is ok
			// We always disable CompareNullsAsValues behavior when comparing SqlRow.
			return predicate.WithNull == null
				? predicate
				: new mooSQL.data.model.affirms.ExprExpr(predicate.Expr1, predicate.Operator, expr2, withNull: null);
		}

		bool ConvertRowIsNullPredicate(RowWord sqlRow, bool IsNot, [NotNullWhen(true)] out IAffirmWord? rowIsNullFallback)
		{
			if (SqlProviderFlags != null && !SqlProviderFlags!.RowConstructorSupport.HasFlag(RowFeature.IsNull))
			{
				rowIsNullFallback = RowIsNullFallback(sqlRow, IsNot);
				return true;
			}

			rowIsNullFallback = null;
			return false;
		}

		protected virtual IAffirmWord ConvertRowInList(InList predicate)
		{
			if (SqlProviderFlags == null)
				return predicate;

			if (!SqlProviderFlags.RowConstructorSupport.HasFlag(RowFeature.In))
			{
				var left    = predicate.Expr1;
				var op      = predicate.IsNot ? AffirmWord.Operator.NotEqual : AffirmWord.Operator.Equal;
				var isOr    = !predicate.IsNot;
				var rewrite = new SearchConditionWord(isOr);
				foreach (var item in predicate.Values)
					rewrite.Predicates.Add(new mooSQL.data.model.affirms.ExprExpr(left, op, item, withNull: null));
				return rewrite;
			}

			// Default InList translation is ok
			// We always disable CompareNullsAsValues behavior when comparing SqlRow.
			return predicate.WithNull == null
				? predicate
				: new InList(predicate.Expr1, withNull: null, predicate.IsNot, predicate.Values);
		}

		protected IAffirmWord RowIsNullFallback(RowWord row, bool isNot)
		{
			var rewrite = new SearchConditionWord();
			// (a, b) is null     => a is null     and b is null
			// (a, b) is not null => a is not null and b is not null
			foreach (var value in row.Values)
				rewrite.Predicates.Add(new IsNull(value, isNot));
			return rewrite;
		}

		protected IAffirmWord RowComparisonFallback(Operator op, RowWord row1, RowWord row2, EvaluateContext context)
		{
			if (op is AffirmWord.Operator.Equal or AffirmWord.Operator.NotEqual)
			{
				// (a1, a2) =  (b1, b2) => a1 =  b1 and a2 = b2
				// (a1, a2) <> (b1, b2) => a1 <> b1 or  a2 <> b2
				bool isOr = op == AffirmWord.Operator.NotEqual;

				var rewrite = new SearchConditionWord(isOr);

				var compares = row1.Values.Zip(row2.Values, (a, b) =>
				{
                    // There is a trap here, neither `a` nor `b` should be a constant null value,
                    // because ExprExpr reduces `a == null` to `a is null`,
                    // which is not the same and not equivalent to the Row expression.
                    // We use `a >= null` instead, which is equivalent (always evaluates to `unknown`) but is never reduced by ExprExpr.
                    // Reducing to `false` is an inaccuracy that causes problems when composed in more complicated ways,
                    // e.g. the NOT IN SqlRow tests fail.
                    AffirmWord.Operator nullSafeOp = a.TryEvaluateExpression(context, out var val) && val == null ||
					                                   b.TryEvaluateExpression(context, out     val) && val == null
						? AffirmWord.Operator.GreaterOrEqual
						: op;
					return new mooSQL.data.model.affirms.ExprExpr(a, nullSafeOp, b, withNull: null);
				});

				foreach (var comp in compares)
					rewrite.Predicates.Add(comp);

				return rewrite;
			}

			if (op is AffirmWord.Operator.Greater or AffirmWord.Operator.GreaterOrEqual or AffirmWord.Operator.Less or AffirmWord.Operator.LessOrEqual)
			{
				var rewrite = new SearchConditionWord(true);

				// (a1, a2, a3) >  (b1, b2, b3) => a1 > b1 or (a1 = b1 and a2 > b2) or (a1 = b1 and a2 = b2 and a3 >  b3)
				// (a1, a2, a3) >= (b1, b2, b3) => a1 > b1 or (a1 = b1 and a2 > b2) or (a1 = b1 and a2 = b2 and a3 >= b3)
				// (a1, a2, a3) <  (b1, b2, b3) => a1 < b1 or (a1 = b1 and a2 < b2) or (a1 = b1 and a2 = b2 and a3 <  b3)
				// (a1, a2, a3) <= (b1, b2, b3) => a1 < b1 or (a1 = b1 and a2 < b2) or (a1 = b1 and a2 = b2 and a3 <= b3)
				var strictOp = op is AffirmWord.Operator.Greater or AffirmWord.Operator.GreaterOrEqual ? AffirmWord.Operator.Greater : AffirmWord.Operator.Less;
				var values1 = row1.Values;
				var values2 = row2.Values;

				for (int i = 0; i < values1.Length; ++i)
				{
					var sub = new SearchConditionWord();
					for (int j = 0; j < i; j++)
					{
						sub.Add(new mooSQL.data.model.affirms.ExprExpr(values1[j], AffirmWord.Operator.Equal, values2[j], withNull : null));
					}

					sub.Add(new mooSQL.data.model.affirms.ExprExpr(values1[i], i == values1.Length - 1 ? op : strictOp, values2[i], withNull: null));

					rewrite.Add(sub);
				}

				return rewrite;
			}

			if (op is AffirmWord.Operator.Overlaps)
			{
				//TODO:: retest

				/*if (row1.Values.Length == 2 && row2.Values.Length == 2)
				{
					var rewrite = new SearchConditionWord(true);

					static void AddCase(SqlSearchCondition condition, (ISqlExpression start, ISqlExpression end) caseRow1, (ISqlExpression start, ISqlExpression end) caseRow2)
					{
						// (s1 <= e1) and (s2 <= e2) and ((s2 < e1 and e2 > s1) or (s1 < e2 and e1 > s2))

						condition.AddAnd(subCase =>
							subCase
								.AddLessOrEqual(caseRow1.start, caseRow1.end, false)
								.AddLessOrEqual(caseRow2.start, caseRow2.end, false)
								.AddOr(x =>
									x
										.AddAnd(sub =>
											sub
												.AddLess(caseRow2.start, caseRow1.end, false)
												.AddGreater(caseRow2.end, caseRow1.start, false)
										)
										.AddAnd(sub =>
											sub
												.AddLess(caseRow1.start, caseRow2.end, false)
												.AddGreater(caseRow1.end, caseRow2.start, false)
										)
								));
					}

					// add possible permutations

					AddCase(rewrite, (row1.Values[0], row1.Values[1]), (row2.Values[0], row2.Values[1]));
					AddCase(rewrite, (row1.Values[0], row1.Values[1]), (row2.Values[1], row2.Values[0]));
					AddCase(rewrite, (row1.Values[1], row1.Values[0]), (row2.Values[0], row2.Values[1]));
					AddCase(rewrite, (row1.Values[1], row1.Values[0]), (row2.Values[1], row2.Values[0]));

					return rewrite;
				}*/
			}

			throw new LinqException("Unsupported SqlRow operator: " + op);
		}

		#endregion

		#region Helper functions

		public IExpWord Add(IExpWord expr1, IExpWord expr2, Type type)
		{
			return new BinaryWord(type, expr1, "+", expr2, PrecedenceLv.Additive);
		}

		public IExpWord Add<T>(IExpWord expr1, IExpWord expr2)
		{
			return Add(expr1, expr2, typeof(T));
		}

		public IExpWord Add(IExpWord expr1, int value)
		{
			return Add<int>(expr1, new ValueWord(value));
		}

		public IExpWord Inc(IExpWord expr1)
		{
			return Add(expr1, 1);
		}

		public Clause Sub(IExpWord expr1, IExpWord expr2, Type type)
		{
			return new BinaryWord(type, expr1, "-", expr2, PrecedenceLv.Subtraction);
		}

		public Clause Sub<T>(IExpWord expr1, IExpWord expr2)
		{
			return Sub(expr1, expr2, typeof(T));
		}

		public Clause Sub(IExpWord expr1, int value)
		{
			return Sub<int>(expr1, new ValueWord(value));
		}

		public Clause Dec(IExpWord expr1)
		{
			return Sub(expr1, 1);
		}

		public IExpWord Mul(IExpWord expr1, IExpWord expr2, Type type)
		{
			return new BinaryWord(type, expr1, "*", expr2, PrecedenceLv.Multiplicative);
		}

		public IExpWord Mul<T>(IExpWord expr1, IExpWord expr2)
		{
			return Mul(expr1, expr2, typeof(T));
		}

		public IExpWord Mul(IExpWord expr1, int value)
		{
			return Mul<int>(expr1, new ValueWord(value));
		}

		public Clause Div(IExpWord expr1, IExpWord expr2, Type type)
		{
			return new BinaryWord(type, expr1, "/", expr2, PrecedenceLv.Multiplicative);
		}

		public Clause Div<T>(IExpWord expr1, IExpWord expr2)
		{
			return Div(expr1, expr2, typeof(T));
		}

		public Clause Div(IExpWord expr1, int value)
		{
			return Div<int>(expr1, new ValueWord(value));
		}

		protected SearchConditionWord ConvertToBooleanSearchCondition(IExpWord expression)
		{
			var sc = new SearchConditionWord();

            IAffirmWord predicate;
			if (expression.SystemType?.UnwrapNullable() == typeof(bool))
			{
				predicate = new IsTrue(expression, new ValueWord(true), new ValueWord(false), DBLive.dialect.Option.CompareNullsAsValues ? false : null, false);
			}
			else
			{
				predicate = new mooSQL.data.model.affirms.ExprExpr(expression, AffirmWord.Operator.Equal, new ValueWord(0), DBLive.dialect.Option.CompareNullsAsValues ? true : null)
					.MakeNot();
			}

			sc.Add(predicate);

			return sc;
		}

		protected IExpWord ConvertBooleanToCase(IExpWord expr, DbDataType toType)
		{
			var caseExpr = new CaseWord(toType,
				new CaseWord.CaseItem[]
				{
					new(new IsNull(expr, false), new ValueWord(toType, null)),
					new(new mooSQL.data.model.affirms.ExprExpr(expr, AffirmWord.Operator.NotEqual, new ValueWord(0), null), new ValueWord(toType, true))
				}, new ValueWord(toType, false));
			

			return caseExpr;
		}

		protected IExpWord ConvertCoalesceToBinaryFunc(FunctionWord func, string funcName, bool supportsParameters = true)
		{
			var last = func.Parameters[func.Parameters.Length - 1];
			if (!supportsParameters && last is ParameterWord p1)
				p1.IsQueryParameter = false;

			for (int i = func.Parameters.Length - 2; i >= 0; i--)
			{
				var param = func.Parameters[i];
				if (!supportsParameters && param is ParameterWord p2)
					p2.IsQueryParameter = false;

				last = new FunctionWord(func.SystemType, funcName, param, last);
			}
			return last;
		}

		protected static bool IsDateDataType(DbDataType dataType, string typeName)
		{
			return dataType.DataType == DataFam.Date || dataType.DbType == typeName;
		}

		protected static bool IsSmallDateTimeType(DbDataType dataType, string typeName)
		{
			return dataType.DataType == DataFam.SmallDateTime || dataType.DbType == typeName;
		}

		protected static bool IsDateTime2Type(DbDataType dataType, string typeName)
		{
			return dataType.DataType == DataFam.DateTime2 || dataType.DbType == typeName;
		}

		protected static bool IsDateTimeType(DbDataType dataType, string typeName)
		{
			return dataType.DataType == DataFam.DateTime2 || dataType.DbType == typeName;
		}

		protected static bool IsDateDataOffsetType(DbDataType dataType)
		{
			return dataType.DataType == DataFam.DateTimeOffset;
		}

		protected static bool IsTimeDataType(DbDataType dataType)
		{
			return dataType.DataType == DataFam.Time || dataType.DbType == "Time";
		}

		protected CastWord FloorBeforeConvert(CastWord cast)
		{
			if (cast.Expression.SystemType!.IsFloatType() && cast.SystemType.IsIntegerType())
			{
				if (cast.Expression is FunctionWord { Name: "Floor" })
					return cast;

				return cast.WithExpression(new FunctionWord(cast.Expression.SystemType!, "Floor", cast.Expression));
			}

			return cast;
		}

		protected IExpWord TryConvertToValue(IExpWord expr, EvaluateContext context)
		{
			if (expr.NodeType != ClauseType.SqlValue)
			{
				if (expr.TryEvaluateExpression(context, out var value))
					expr = new ValueWord(QueryHelper.GetDbDataType(expr, DBLive), value);
			}

			return expr;
		}

		#endregion
	}
}
