using mooSQL.linq;
using mooSQL.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace mooSQL.data.clip
{
    internal class ClipProvider
    {

        internal SQLClip clip {  get; set; }

        private ClipWhereVisitor WhereVisitor { get; set; }
        public void PatchJoin<T>(Expression<Func<bool>> joinCondition, ClipJoin<T> clipJoin)
        {
            //暂时只考虑条件的两端都是成员访问的情况。

            var runner = new ClipJoinVisitor(clip.DBLive, clip);
            var data = runner.Visit(joinCondition);
            //将解析的数据，组织为sql
            data.JoinType = clipJoin.JoinType;
            //复制参数
            if (data.paras != null) { 
                clip.Context.Builder.ps.Copy(data.paras);
            }

            //如果此时from第一表是空的，则将当前表设置为第一表
            if (data.JoinBy == null) { 
                foreach (var item in data.ParsedTables) {
                    if (item.BindValue == clipJoin.JoinTarget) { 
                        data.JoinBy = item;
                        break;
                    }
                }
            }
            //检查from是否已解析，但理论上不应该出现这种情况。
            var fromtb = clip.Context.getFromTable();
            if (string.IsNullOrWhiteSpace(fromtb.Alias)) {
                foreach (var item in data.ParsedTables)
                {
                    if (item.BindValue == fromtb.BindValue)
                    {
                        fromtb.Alias = item.Alias;
                        break;
                    }
                }
            }

            //放入暂存区
            if(clip.Context.Joins == null) {
                clip.Context.Joins = new List<ClipJoinData>();
            }
            clip.Context.Joins.Add(data);
        }

        private void checkWhere() { 
            if (WhereVisitor == null) { 
                WhereVisitor = new ClipWhereVisitor(clip.DBLive, clip);
            }
        }

        public void PatchWhere(Expression<Func<bool>> joinCondition)
        {
            //直接应用到builder上
            this.checkWhere();

            this.WhereVisitor.Visit(joinCondition);
        }

        private string TranslateField(Expression expression)
        {
            var context = new FastCompileContext();
            context.initByBuilder(clip.Context.Builder);
            var fidv = new ClipFieldVisitor(context, true, clip);
            var fie = fidv.Visit(expression);
            bool needAlias = true;
            if (clip.Context.BType == BuildSQLType.Edit) { 
                needAlias = false;
            }
            var sql = fidv.GetFieldCondtionSQL(needAlias);
            if (!string.IsNullOrWhiteSpace(sql)) { 
                return sql;
            }
            return null;
        }

        private string TranslateFieldToSelect(Expression expression,out int fieldCount)
        {
            var context = new FastCompileContext();
            context.initByBuilder(clip.Context.Builder);
            var fidv = new ClipFieldVisitor(context, true, clip);
            var fie = fidv.Visit(expression);
            fieldCount = fidv.ClipFields.Count;
            var db = context.DB;
            foreach (var item in fidv.ClipFields)
            {
                var sb = new StringBuilder();
                if (!string.IsNullOrWhiteSpace(item.SQLAlias)) { 
                    sb.Append(item.SQLAlias);
                    sb.Append(".");                
                }
                
                sb.Append(db.dialect.expression.wrapField(item.SQLField));
                if (!string.IsNullOrWhiteSpace(item.AsName)) { 
                    sb.Append(" AS ");
                    sb.Append(item.AsName);
                }
                clip.Context.Builder.select(sb.ToString());
            }

            this.clip.Context.FieldCount = fieldCount;
            return null;
        }

        public void PatchOrderBy<R>(Expression<Func<R>> keySelector, bool isDesc = false)
        {
            //直接应用到builder上

            var context = new FastCompileContext();
            context.initByBuilder(clip.Context.Builder);
            var fidv = new ClipFieldVisitor(context, true, clip);
            var fie = fidv.Visit(keySelector);

            if (fidv.ClipFields != null) {
                foreach (var item in fidv.ClipFields) {
                    var fid = item.toSQLField(true);
                    var sql = string.Format(" {0} {1}", fid, isDesc ? "DESC" : "ASC");
                    clip.Context.Builder.orderBy(sql);
                }
            }
        }

        public string PatchOutField(Expression expression) {
            var field = this.TranslateField(expression);
            return field;
        }

        public void PatchGroupBy(Expression exp) {
            var field = this.TranslateField(exp);

            if (string.IsNullOrWhiteSpace(field))
            {
                clip.Context.Builder.groupBy(field);
            }
        }

        public void PatchHaving(Expression havingExp) {
            var runner = new ClipJoinVisitor(clip.DBLive, clip);
            var data = runner.Visit(havingExp);
            //复制参数
            if (data.paras != null)
            {
                clip.Context.Builder.ps.Copy(data.paras);
            }
            var sql = data.onSQL;
            if (!string.IsNullOrWhiteSpace(sql)) { 
                clip.Context.Builder.having(sql);
            }
        }

        public void PatchSelect(Expression exp) {
            if (exp is LambdaExpression lmd) { 
                var body=   lmd.Body;
                var obj = lmd.Type.UnwrapNullable();
                //如果类型是当前一个选取表，则不再需要解析。
                foreach( var tb in clip.Context.BindTables) {
                    if (tb.Value.EnityType == obj) {
                        var name = tb.Value.Alias;
                        if (string.IsNullOrWhiteSpace(name)) { 
                            name = tb.Value.TableInfo.DbTableName;
                        }
                        var fromsql= string.Format("{0}.*", name);
                        clip.Context.Builder.select(fromsql);

                        clip.Context.FieldCount= tb.Value.TableInfo.Columns.Count;
                        checkJoin();
                        return;
                    }
                }
            }


            var field = this.TranslateFieldToSelect(exp,out int fcc);

            //运行到select后，将之前的join数据应用到builder上

            checkJoin();


        }

        public void PatchSetTable<T>() {
            var setTb = clip.Context.getSetTable();
            if (setTb != null)
            {
                var sql = setTb.TableInfo.DbTableName;
                clip.Context.Builder.setTable(sql);
            }
        }

        private void checkJoin() {
            //检查from

            if (clip.Context.FromBinded == false)
            {
                var fromtb = clip.Context.getFromTable();
                if (fromtb != null)
                {
                    var sql = string.Format("{0} AS {1}", fromtb.TableInfo.DbTableName, fromtb.Alias);
                    clip.Context.Builder.from(sql);
                }

            }
            if (clip.Context.Joins != null)
            {
                foreach (var item in clip.Context.Joins)
                {
                    var sb = new StringBuilder();
                    sb.Append(item.JoinType);
                    var ti = item.JoinBy.TableInfo;
                    sb.Append(" ");
                    var tbsql= clip.Context.Builder.DBLive.dialect.expression.wrapTableAsSQL(ti.DbTableName, item.JoinBy.Alias,ti.SchemaName);
                    sb.Append(tbsql);

                    if (string.IsNullOrWhiteSpace(item.onSQL))
                    {
                        throw new Exception("SQL语句的join语句条件部分为空！");
                    }
                    sb.Append(" ON ");
                    sb.Append(item.onSQL);
                    clip.Context.Builder.join(sb.ToString());
                }
            }
        }
    }
}
