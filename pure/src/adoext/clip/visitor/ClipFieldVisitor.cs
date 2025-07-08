using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using mooSQL.data;
using mooSQL.linq;
using mooSQL.utils;

namespace mooSQL.data.clip
{

    internal class ClipFieldVisitor : EntityMemberVisitor
    {

        private SQLClip clip;
        public ClipFieldVisitor(FastCompileContext context, bool srcNick,SQLClip clipBuilder) : base(context)
        {
            this.srcNick = srcNick;
            this.clip = clipBuilder;
            this.ClipFields = new List<ClipExpField>();
        }
        /// <summary>
        /// 步骤，1--查找表，2--查找字段
        /// </summary>
        private int step;
        internal ClipTable table;

        internal List<ClipExpField> ClipFields;
        ClipExpField parsingField;

        public List<ClipExpField> GetFields()
        {
            return ClipFields;
        }

        /// <summary>
        ///返回格式为 a.field格式的字段集合，逗号分隔。
        /// </summary>
        /// <param name="needAlias"></param>
        /// <returns></returns>
        public string GetFieldCondtionSQL(bool needAlias= true) { 
        
            var sb = new StringBuilder();
            bool isFirst = true;
            foreach (var item in ClipFields) { 
                if(!isFirst)
                {
                    sb.Append(",");
                }
                if (needAlias)
                {
                    sb.Append(item.SQLAlias);
                    sb.Append(".");
                    sb.Append(item.SQLField);
                }
                else { 
                    sb.Append(item.SQLField);
                }
                isFirst = false;

            }
            return sb.ToString();
        }


        protected override Expression VisitMember(MemberExpression node)
        {

            this.step = 1;
            //this.Visit(node.Expression);
            parsingField = new ClipExpField();
            parsingField.SrcExp = node;
            this.step = 2;
            var prop = node.Member;
            this.member = prop;
            parsingField.Member = prop;
            var fie = this.GetFieldCol(prop, node);
            if (fie == null)
            {
                return node;
            }
            this.field = fie;
            var t = "";
            if ( !string.IsNullOrWhiteSpace(fie.DbColumnName))
            {
                t = fie.DbColumnName;
            }
            if (srcNick)
            {
                var nick = this.table.Alias;
                if (nick != null)
                {
                    t = nick + "." + t;
                }
            }
            this.ClipFields.Add(parsingField);
            //return new StringExpression(t);
            return Expression.Constant(parsingField);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            //if (node.NodeType == ExpressionType.Assign) {
            //    var left = node.Left;
            //    var right= node.Right;
            //    var rightDo= this.Visit(right);

            //}
            return base.VisitBinary(node);
        }
        protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding node)
        {
            var mem= node.Member;
            var val = node.Bindings;
            foreach (var item in val) {
                //var bindField= this.Visit(item);
                var a= 1;
            }
            return base.VisitMemberMemberBinding(node);
        }
        protected override MemberBinding VisitMemberBinding(MemberBinding node)
        {
            /*
             -		Expression	{value(HHNY.NET.Core.Service.SysTenantService+<>c__DisplayClass13_0).t.Id}	System.Linq.Expressions.Expression {System.Linq.Expressions.PropertyExpression}

             */

            if (node is MemberAssignment ass) { 
                
                var field =this.Visit( ass.Expression);
                if (field is ConstantExpression t && t.Value is ClipExpField fieldExp)
                {
                    var mem = node.Member;
                    fieldExp.AsName = mem.Name;
                    fieldExp.AsToMember = mem;
                    
                }
                return node;

            }
            
            return base.VisitMemberBinding(node);
        }

        protected override Expression VisitNew(NewExpression node)
        {
            Visit(node.Arguments);
            return node;
        }

        private ClipTable findTargetTable( ConstantExpression exp, string name) {
            //一般此时的参数为闭包类，因此首先进行判断。
            if (ClosureInspector.IsClosureClass(exp.Type)) { 
                var v= ClosureInspector.GetFieldValueN(exp.Value, name);
                if (v != null) {
                    if (clip.Context.BindTables.ContainsKey(v))
                    {
                        return clip.Context.BindTables[v];
                    }
                }
            }

            var  val = exp.Value;
            if (clip.Context.BindTables.ContainsKey(val))
            {
                return clip.Context.BindTables[val];
            }
            return null;
        }

        protected override EntityColumn GetFieldCol(MemberInfo prop, Expression expression)
        {
            var type = prop.DeclaringType;
            var name = prop.Name;
            //表定位阶段
            if(expression is MemberExpression expr)
            {

                var mName= expr.Member.Name;
                //获取它的调用方，应该是clip中的声明的表变量
                var find = new ExpressionFindVisitor<ConstantExpression>();
                var constExpr = find.Find(expr.Expression);

                var callerFind = new ExpressionFindVisitor<MemberExpression>();
                var callerExpr = callerFind.Find(expr.Expression);
                if (constExpr !=null && callerExpr != null) {
                    parsingField.MemExp = callerExpr;
                    var caller = expr.Expression;
                    var callerName= callerExpr.Member.Name;
                    //检查clip中是否已注册了这个表变量
                    var tb=this.findTargetTable(constExpr, callerName);
                    if (tb != null) {
                        //此时，如果表信息中的别名未注册，则注册别名
                        if (string.IsNullOrWhiteSpace(tb.Alias)) { 
                            tb.Alias= callerName;
                        }

                        this.table = tb;
                        parsingField.CTable = tb;
                        parsingField.EnTable = tb.TableInfo;
                        parsingField.SQLAlias = tb.Alias;
                        var col= tb.TableInfo.GetColumn(name);
                        if (col != null) { 
                            parsingField.SQLField = col.DbColumnName;
                            parsingField.EnColumn = col;                        
                        }

                        return col;
                    }

                }
            }
            if(step==2 && table!=null)
            {
                var col = this.table.TableInfo.GetColumn(name);
                return col;
            }
            //var dic = typeMap[type];
            //if (dic.Fields.ContainsKey(name))
            //{
            //    var col = dic.Fields[name];

            //    return col;

            //}
            return null;
        }
    }
}
