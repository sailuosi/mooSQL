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
    public static class ClauseExtensions
    {

        public static List<TableSourceWord> FindTables(this ITableNode table) { 
            throw new NotImplementedException();    
        }


        public static List<JoinTableWord> GetJoins(this ITableNode table) { 
        
            var joins = new List<JoinTableWord>();
            return joins;
        }

        public static TableSourceWord FindSrc(this ITableNode table) {
            if (table is TableSourceWord srcTable) {
                return srcTable.Source as TableSourceWord;
            }
            return null;
        }
        public static ITableNode FindISrc(this ITableNode table)
        {
            if (table is TableSourceWord srcTable)
            {
                return srcTable.Source;
            }
            return null;
        }


        public static TableSourceWord FindTableSrc(this FromClause fromClause, ITableNode table) { 
            throw new NotImplementedException();
        }

        public static bool CanBeNullable(this IExpWord expWord,NullabilityContext context) {
            if (expWord is FieldWord field) {
                if (field.ColumnDescriptor != null) {
                    return field.ColumnDescriptor.IsNullable;
                }
            }


                return true;
            throw new NotImplementedException();
        }

        public static bool HasUniqueKeys(this ITableNode tableNode) { 
            throw new NotImplementedException(); 
        }

        public static List<IExpWord[]> FindUniqueKeys(this ITableNode tableNode)
        {
            throw new NotImplementedException();
        }

        public static bool IsSimple(this SelectQueryClause selectQuery) {
            throw new NotImplementedException();
        }
        public static bool IsSimpleOrSet(this SelectQueryClause selectQuery)
        {
            throw new NotImplementedException();
        }
        public static IReadOnlyList<FieldWord> FindIdentityFields(this ITableNode selectQuery)
        {
            throw new NotImplementedException();
        }


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


        public static Type FindSystemType(this ITableNode sentence)
        {
            throw new NotImplementedException();
        }
        

        public static bool CanInvert(this IAffirmWord affirmWord, NullabilityContext nullability) {
            throw new NotImplementedException();
        }
        public static IAffirmWord Invert(this IAffirmWord affirmWord, NullabilityContext nullability)
        {
            throw new NotImplementedException();
        }



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


        public static SelectQueryClause CloneQuery(this SelectQueryClause clause)
        {
            return clause.Clone(e => ReferenceEquals(e, clause));
        }

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


        public static Clause MakeBool(bool isTrue)
        {
            return isTrue ? AffirmWord.True : AffirmWord.False;
        }
    }
}
