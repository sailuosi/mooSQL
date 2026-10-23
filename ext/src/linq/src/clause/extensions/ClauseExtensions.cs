using mooSQL.data;
using mooSQL.data.model;
using mooSQL.data.model.affirms;
using mooSQL.linq.mapping;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


namespace mooSQL.linq.clause
{
    /// <summary>
    /// ClauseExtensions type.
    /// </summary>
    /// <summary>
    /// ClauseExtensions type.
    /// </summary>
    public static class ClauseExtensions
    {
        /// <summary>
        /// FindTables
        /// </summary>

        public static List<TableSourceWord> FindTables(this ITableNode table) { 
            throw new NotImplementedException();    
        }
        /// <summary>
        /// Gets GetJoins.
        /// </summary>


        public static List<JoinTableWord> GetJoins(this ITableNode table) { 
        
            var joins = new List<JoinTableWord>();
            return joins;
        }
        /// <summary>
        /// FindSrc
        /// </summary>

        public static TableSourceWord FindSrc(this ITableNode table) {
            if (table is TableSourceWord srcTable) {
                return srcTable.Source as TableSourceWord;
            }
            return null;
        }
        /// <summary>
        /// FindISrc
        /// </summary>
        /// <summary>
        /// FindISrc
        /// </summary>
        public static ITableNode FindISrc(this ITableNode table)
        {
            if (table is TableSourceWord srcTable)
            {
                return srcTable.Source;
            }
            return null;
        }
        /// <summary>
        /// FindTableSrc
        /// </summary>


        public static TableSourceWord FindTableSrc(this FromClause fromClause, ITableNode table) { 
            throw new NotImplementedException();
        }
        /// <summary>
        /// CanBeNullable
        /// </summary>

        public static bool CanBeNullable(this IExpWord expWord,NullabilityContext context) {
            if (expWord is FieldWord field) {
                if (field.ColumnDescriptor != null) {
                    return field.ColumnDescriptor.IsNullable;
                }
            }


                return true;
            throw new NotImplementedException();
        }
        /// <summary>
        /// HasUniqueKeys
        /// </summary>

        public static bool HasUniqueKeys(this ITableNode tableNode) { 
            throw new NotImplementedException(); 
        }
        /// <summary>
        /// FindUniqueKeys
        /// </summary>

        public static List<IExpWord[]> FindUniqueKeys(this ITableNode tableNode)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// IsSimple
        /// </summary>

        public static bool IsSimple(this SelectQueryClause selectQuery) {
            throw new NotImplementedException();
        }
        /// <summary>
        /// IsSimpleOrSet
        /// </summary>
        /// <summary>
        /// IsSimpleOrSet
        /// </summary>
        public static bool IsSimpleOrSet(this SelectQueryClause selectQuery)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// FindIdentityFields
        /// </summary>
        /// <summary>
        /// FindIdentityFields
        /// </summary>
        public static IReadOnlyList<FieldWord> FindIdentityFields(this ITableNode selectQuery)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// FindAlias
        /// </summary>


        public static string? FindAlias(this ITableNode selectQuery)
        {
            return selectQuery switch
            {
                DerivatedTableWord derivated => derivated.Name,
                TableWord table => table.Alias,
                TableSourceWord source => source.Alias,
                _ => null
            };
        }
        /// <summary>
        /// setAlias
        /// </summary>
        /// <summary>
        /// setAlias
        /// </summary>
        public static string setAlias(this ITableNode selectQuery,string alias)
        {
            switch (selectQuery)
            {
                case DerivatedTableWord derivated:
                    return derivated.Name = alias;
                case TableWord table:
                    return table.Alias = alias;
                case TableSourceWord source:
                    return source.Alias = alias;
                default:
                    throw new NotImplementedException($"setAlias not supported for {selectQuery?.GetType().Name}");
            }
        }
        /// <summary>
        /// FindSystemType
        /// </summary>


        public static Type FindSystemType(this ITableNode sentence)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// CanInvert
        /// </summary>
        

        public static bool CanInvert(this IAffirmWord affirmWord, NullabilityContext nullability) {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Invert
        /// </summary>
        /// <summary>
        /// Invert
        /// </summary>
        public static IAffirmWord Invert(this IAffirmWord affirmWord, NullabilityContext nullability)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Reduce
        /// </summary>



        public static IAffirmWord Reduce(this IsTrue isTrue,NullabilityContext nullability, bool insideNot)
        {
            if (isTrue.Expr1.NodeType == ClauseType.SearchCondition)
            {
                return ((IAffirmWord)isTrue.Expr1).MakeNot(isTrue.IsNot);
            }

            var predicate = new ExprExpr(isTrue.Expr1, AffirmWord.Operator.Equal, isTrue.IsNot ? isTrue.FalseValue : isTrue.TrueValue, null);

            if (isTrue.WithNull == null || !isTrue.Expr1.ShouldCheckForNull(nullability))
                return predicate;

            if (!insideNot)
            {
                if (isTrue.WithNull == false)
                    return predicate;
            }

            var search = new SearchConditionWord(isTrue.WithNull.Value);

            search.Predicates.Add(predicate);
            search.Predicates.Add(new IsNull(isTrue.Expr1, !isTrue.WithNull.Value));

            if (search.IsOr)
            {
                search = new SearchConditionWord(false, search);
            }

            return search;

        }
        /// <summary>
        /// Reduce
        /// </summary>

        public static IAffirmWord Reduce(this ExprExpr expr, NullabilityContext nullability, EvaluateContext context, bool insideNot)
        {
            IAffirmWord MakeWithoutNulls()
            {
                return new ExprExpr(expr.Expr1, expr.Operator, expr.Expr2, null);
            }

            if (expr.Operator ==  AffirmWord.Operator.Equal || expr.Operator == AffirmWord.Operator.NotEqual)
            {
                if (expr.Expr1.TryEvaluateExpression(context, out var value1))
                {
                    if (value1 == null)
                        return new IsNull(expr.Expr2, expr.Operator != AffirmWord.Operator.Equal);

                }
                else if (expr.Expr2.TryEvaluateExpression(context, out var value2))
                {
                    if (value2 == null)
                        return new IsNull(expr.Expr1, expr.Operator != AffirmWord.Operator.Equal);
                }
            }

            if (expr.WithNull == null || nullability.IsEmpty)
                return expr;
            if (!nullability.CanBeNull(expr.Expr1) && !nullability.CanBeNull(expr.Expr2))
                return MakeWithoutNulls();

            if (expr.WithNull.Value)
            {
                if (expr.Operator == AffirmWord.Operator.Greater || expr.Operator == AffirmWord.Operator.Less)
                    return expr;

                if (expr.Operator == AffirmWord.Operator.NotEqual)
                {
                    var search = new SearchConditionWord(true)
                        .Add(MakeWithoutNulls())
                    .AddAnd(sc => sc
                            .Add(new IsNull(expr.Expr1, false))
                            .Add(new IsNull(expr.Expr2, true)))
                    .AddAnd(sc => sc
                            .Add(new IsNull(expr.Expr1, true))
                            .Add(new IsNull(expr.Expr2, false))
                        );

                    return search;
                }
                else
                {
                    var search = new SearchConditionWord(true)
                        .Add(MakeWithoutNulls())
                    .AddAnd(sc => sc
                            .Add(new IsNull(expr.Expr1, false))
                            .Add(new IsNull(expr.Expr2, false))
                        );

                    return search;
                }
            }
            else
            {
                if (expr.Operator == AffirmWord.Operator.Equal)
                    return expr;

                if (expr.Operator == AffirmWord.Operator.NotEqual)
                {
                    var search = new SearchConditionWord(true)
                        .Add(MakeWithoutNulls())
                    .AddAnd(sc => sc
                            .Add(new IsNull(expr.Expr1, false))
                            .Add(new IsNull(expr.Expr2, true)))
                        .AddAnd(sc => sc
                            .Add(new IsNull(expr.Expr1, true))
                            .Add(new IsNull(expr.Expr2, false)));

                    return search;
                }
                else
                {
                    if (insideNot)
                        return expr;

                    var search = new SearchConditionWord(true)
                        .Add(MakeWithoutNulls())
                        .Add(new IsNull(expr.Expr1, false))
                        .Add(new IsNull(expr.Expr2, false));

                    return search;
                }
            }
        }
        /// <summary>
        /// CloneQuery
        /// </summary>


        public static SelectQueryClause CloneQuery(this SelectQueryClause clause)
        {
            return clause.Clone(e => ReferenceEquals(e, clause));
        }
        /// <summary>
        /// IsComplex
        /// </summary>

        public static bool IsComplex(this PropertyInfo propertyInfo) {
            if (propertyInfo.Name.Contains("."))
            {
                return true;
            }
            else { 
                return false;
            }
            //throw new NotImplementedException();
        }
        /// <summary>
        /// MakeBool
        /// </summary>


        public static Clause MakeBool(bool isTrue)
        {
            return isTrue ? AffirmWord.True : AffirmWord.False;
        }
    }
}
