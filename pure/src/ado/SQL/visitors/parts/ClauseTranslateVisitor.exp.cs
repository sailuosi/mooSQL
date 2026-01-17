/*
 * * 表达式的转译
 */

using mooSQL.data;
using mooSQL.data.model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;


namespace mooSQL.linq
{
    public partial class ClauseTranslateVisitor
    {
        public override Clause VisitExpression(ExpressionWord field)
        {
            var tar = field.Expr;
            return new SQLFragClause(tar);
        }
        /// <summary>
        /// 转译case
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public override Clause VisitCaseExpression(CaseWord clause)
        {
            if (clause == null) {
                return null;
            }
            var res = new StringBuilder(); 
            res.Append("CASE");
            foreach (var caseItem in clause.Cases)
            {
                res.Append(" ").Append("WHEN ");

                var condi = VisitAffirmWord(caseItem.Condition).ToString();
                res.Append(" ").Append(condi);

                res.Append(" ").Append(" THEN ");
                var resStr = VisitIExpWord(caseItem.ResultExpression);
                res.Append(" ").Append(resStr);

            }

            if (clause.ElseExpression != null)
            {
                res.Append(" ELSE ");
                var elseStr = VisitIExpWord(clause.ElseExpression).ToString();
                res.Append(elseStr);

            }

            res.Append(" END");
            return new SQLFragClause(res.ToString());
        }
        /// <summary>
        /// 一个值
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public override Clause VisitValueWord(ValueWord clause)
        {
            var random= new Random();
            var key = "vw_" + builder.paraSeed + builder.ps.Count+random.Next(100,999);
            var para = new Parameter(key,clause.Value);
            para.dbType = clause.ValueType;
            builder.ps.Add(para);
            var keySQL = builder.DBLive.dialect.expression.paraPrefix + key;

            return new SQLFragClause(keySQL);
        }
        /// <summary>
        /// 函数调用SQL
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public override Clause VisitFunctionWord(FunctionWord field)
        {
            var paras = new List<string>();
            foreach (var param in field.Parameters) { 
                var paraSQL= VisitIExpWord(param);
                if (paraSQL is SQLFragClause parFrag) { 
                    if (!string.IsNullOrWhiteSpace(parFrag.ToString())) { 
                        paras.Add(parFrag.ToString());
                    }                
                }

            }
            var res = string.Format("{0}({1})", field.Name, string.Join(Comma, paras));
            return new SQLFragClause(res);
        }
        /// <summary>
        /// 二元表达式
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public override Clause VisitBinaryExpression(BinaryWord clause)
        {
            var left = VisitIExpWord(clause.Expr1).ToString();
            var right = VisitIExpWord(clause.Expr2).ToString();
            var tar = string.Format("{0} {1} {2}", left, clause.Operation, right);
            return new SQLFragClause(tar);
        }
        /// <summary>
        /// 翻译数据类型
        /// </summary>
        /// <param name="type"></param>
        /// <param name="forCreateTable"></param>
        /// <returns></returns>
        public virtual string TranslateDataType(DbDataType type, bool forCreateTable, bool canBeNull)
        {
            if (!string.IsNullOrEmpty(type.DbType))
                return (type.DbType);
            //var systemType = type.SystemType.FullName;
            //if (type.DataType == DataType.Undefined)
            //    type = MappingSchema.GetDbDataType(type.SystemType);

            //if (!string.IsNullOrEmpty(type.DbType))
            //{
            //    StringBuilder.Append(type.DbType);
            //    return;
            //}

            if (type.DataType == DataFam.Undefined)
                // give some hint to user that it is expected situation and he need to fix something on his side
                throw new Exception($"Database column type cannot be determined automatically and must be specified explicitly for system type {type}");

            switch (type.DataType)
            {
                case DataFam.Double: return "Float";
                case DataFam.Single: return "Real";
                case DataFam.SByte: return "TinyInt";
                case DataFam.UInt16: return "Int";
                case DataFam.UInt32: return "BigInt";
                case DataFam.UInt64: return "Decimal";
                case DataFam.Byte: return "TinyInt";
                case DataFam.Int16: return "SmallInt";
                case DataFam.Int32: return "Int";
                case DataFam.Int64: return "BigInt";
                case DataFam.Boolean: return "Bit";
            }

            var tar=$"{type.DataType}";

            if (type.Length > 0)
                tar+= $"({type.Length})";

            if (type.Precision > 0)
                tar += $"({type.Precision}{Comma}{type.Scale})";
            return tar ;
        }
        /// <summary>
        /// 注释
        /// </summary>
        /// <param name="comment"></param>
        /// <returns></returns>
        public override Clause VisitComment(CommentWord comment)
        {
            if (comment == null) return comment;
            var sb = new StringBuilder();
            sb.Append("/* ");

            for (var i = 0; i < comment.Lines.Count; i++)
            {
                sb.Append(comment.Lines[i].Replace("/*", "").Replace("*/", ""));
                if (i < comment.Lines.Count - 1)
                    sb.AppendLine();
            }

            sb.AppendLine(" */");

            return new SQLFragClause( sb.ToString());
        }
    }
}
