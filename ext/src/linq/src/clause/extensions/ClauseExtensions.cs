using DocumentFormat.OpenXml.Bibliography;
using mooSQL.data;
using mooSQL.data.model;
using mooSQL.data.model.affirms;
using mooSQL.linq.Mapping;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


namespace mooSQL.linq.SqlQuery
{
    public static class ClauseExtensions
    {


        public static void RefineDbParameter(this SetWord setWord, IExpWord column, IExpWord? value)
        {
            if (value is ParameterWord p && column is FieldWord field)
            {
                if (field.ColumnDescriptor != null && p.Type.SystemType != typeof(object))
                {
                    if (field.ColumnDescriptor.DataType != DataFam.Undefined && p.Type.DataType == DataFam.Undefined)
                        p.Type = p.Type.WithDataType(field.ColumnDescriptor.DataType);

                    if (field.ColumnDescriptor.DbType != null && p.Type.DbType == null)
                        p.Type = field.ColumnDescriptor.DbType;
                    if (field.ColumnDescriptor.Length != null && p.Type.Length == null)
                        p.Type = p.Type.WithLength(field.ColumnDescriptor.Length);
                    if (field.ColumnDescriptor.Precision != null && p.Type.Precision == null)
                        p.Type = p.Type.WithPrecision(field.ColumnDescriptor.Precision);
                    if (field.ColumnDescriptor.Scale != null && p.Type.Scale == null)
                        p.Type = p.Type.WithScale(field.ColumnDescriptor.Scale);
                }
            }
        }





        public static TableSourceWord? CheckSource(this TableSourceWord src,ITableNode table,string alias){
            foreach (var tj in src.Joins)
			{
				//var t = CheckTableSource(tj.Table, table, alias);

				//if (t != null)
				//	return t;
			}
            return null;
        }



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
        public static FieldWord FindIdentityField(this ITableNode selectQuery)
        {
            throw new NotImplementedException();
        }

        public static string FindAlias(this ITableNode selectQuery)
        {
            if (selectQuery is DerivatedTableWord derivated) {
                return derivated.Name;
            }
            throw new NotImplementedException();
        }
        public static string setAlias(this ITableNode selectQuery,string alias)
        {
            if (selectQuery is DerivatedTableWord derivated)
            {
                return derivated.Name= alias;
            }
            throw new NotImplementedException();
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


        public static void Add(this ValuesTableWord valuesTable, FieldWord field, MemberInfo? memberInfo, Func<object, IExpWord> valueBuilder)
        {
            if (field.Table != null) throw new InvalidOperationException("Invalid parent table.");

            field.Table = valuesTable;
            valuesTable.Fields.Add(field);

            if (memberInfo != null)
                valuesTable.FieldsLookup!.Add(memberInfo, field);

            valuesTable.ValueBuilders ??= new List<Func<object, IExpWord>>();
            valuesTable.ValueBuilders.Add(valueBuilder);
        }

        public static Clause MakeBool(bool isTrue)
        {
            return isTrue ? AffirmWord.True : AffirmWord.False;
        }
    }
}
