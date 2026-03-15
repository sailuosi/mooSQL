
using mooSQL.data.call;

using mooSQL.data.model;
using mooSQL.linq.Expressions;
using mooSQL.linq.Linq.Builder;
using mooSQL.linq.SqlQuery;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Linq.Expressions;
using System.Text;




namespace mooSQL.linq.translator
{
    /// <summary>
    /// 用来构建SQL 表值表达式 的翻译器
    /// </summary>
    internal class ValueWordTranslator : BaseTranslateVisitor
    {
        public ValueWordTranslator(MethodVisitor visitor) : base(visitor)
        {
        }

        IBuildContext? context;
        string alias;

        /// <summary>
        /// 二元表达式的计算
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitBinary(BinaryExpression node)
        {

            var op = "";

            var leftExp = Visit(node.Left);
            var rightExp = Visit(node.Right);

            if (leftExp is SqlPlaceholderExpression leftSQL && rightExp is SqlPlaceholderExpression rightSQL) {
                var l = leftSQL.Sql;
                var r = rightSQL.Sql;
                var t = node.Type;

                IExpWord word =null;
                if (node.NodeType == ExpressionType.Add || node.NodeType == ExpressionType.AddChecked)
                {
                    word = new BinaryWord(t, l, "+", r, PrecedenceLv.Additive);
                }
                else if (node.NodeType == ExpressionType.And) {
                    word = new BinaryWord(t, l, "&", r, PrecedenceLv.Bitwise);
                }
                else if (node.NodeType == ExpressionType.Divide)
                {
                    word = new BinaryWord(t, l, "/", r, PrecedenceLv.Multiplicative);
                }
                else if (node.NodeType == ExpressionType.ExclusiveOr)
                {
                    word = new BinaryWord(t, l, "^", r, PrecedenceLv.Bitwise);
                }
                else if (node.NodeType == ExpressionType.Modulo)
                {
                    word = new BinaryWord(t, l, "%", r, PrecedenceLv.Multiplicative);
                }
                else if (node.NodeType == ExpressionType.Multiply||node.NodeType== ExpressionType.MultiplyChecked)
                {
                    word = new BinaryWord(t, l, "*", r, PrecedenceLv.Multiplicative);
                }
                else if (node.NodeType == ExpressionType.Or)
                {
                    word = new BinaryWord(t, l, "|", r, PrecedenceLv.Bitwise);
                }
                else if (node.NodeType == ExpressionType.Power)
                {
                    word = new FunctionWord(t, "Power",l, r);
                }
                else if (node.NodeType == ExpressionType.Subtract|| node.NodeType == ExpressionType.SubtractChecked)
                {
                    word = new BinaryWord(t, l, "-", r, PrecedenceLv.Subtraction);
                }
                if (node.NodeType == ExpressionType.Coalesce) {
                    if (QueryHelper.UnwrapExpression(r, checkNullability: true) is FunctionWord c)
                    {
                        if (c.Name is "Coalesce" or PseudoFunctions.COALESCE)
                        {
                            var parms = new IExpWord[c.Parameters.Length + 1];

                            parms[0] = l;
                            c.Parameters.CopyTo(parms, 1);

                            word=  PseudoFunctions.MakeCoalesce(t, parms);
                        }
                    }
                    if (word == null) {
                        word = PseudoFunctions.MakeCoalesce(t, l, r);
                    }
                }

                return CreatePlaceholder(context, word,node,alias:this.alias);

            }


            return base.VisitBinary(node);
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.UnaryPlus:
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:

                    var o = Visit(node.Operand) as SqlPlaceholderExpression;


                    switch (node.NodeType)
                    {
                        case ExpressionType.UnaryPlus: return CreatePlaceholder(context, o.Sql, node);
                        case ExpressionType.Negate:
                        case ExpressionType.NegateChecked:
                            return CreatePlaceholder(context, new BinaryWord(node.Type, new ValueWord(-1), "*", o.Sql, PrecedenceLv.Multiplicative), node, alias: alias);
                    }
                    break;

                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:

                    var op = Visit(node.Operand);
                    if (!SequenceHelper.IsSqlReady(op))
                        return node;


                    break;

            }


  
            return base.VisitUnary(node);
        }


        public SqlPlaceholderExpression CreatePlaceholder(IBuildContext? context, IExpWord sqlExpression,
    System.Linq.Expressions.Expression path, Type? convertType = null, string? alias = null, int? index = null, Expression? trackingPath = null)
        {
            var placeholder = new SqlPlaceholderExpression(context?.SelectQuery, sqlExpression, path, convertType, alias, index, trackingPath ?? path);
            return placeholder;
        }
    }
}
