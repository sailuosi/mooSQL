/*
 * * 由于 searchCondition 不一定是放在 where下。
 * 因此，在刻意转译非where情况时，需要虚拟一个builder来执行。
 */
using mooSQL.data;
using mooSQL.data.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace mooSQL.linq
{
    public partial class ClauseTranslateVisitor
    {

        /// <summary>
        /// 构建不是where子句的条件项
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        public virtual string BuildContionNotWhere(SearchConditionWord search) {

            var noew = this.builder;
            var havingBuilder = this.builder.getBrotherBuilder();
            this.builder = havingBuilder;


            this.VisitSearchCondition(search);
            //提取构造结果
            var wh = builder.buildWhereContent();
            //还原编织器
            this.builder = noew;
            return wh;
        }
        /// <summary>
        /// 转译列
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public override Clause VisitColumnWord(ColumnWord clause)
        {
            var fname = this.TranslateValue(clause.Alias, ConvertType.NameToQueryField);
            if (DisableAlias) {
                
                return new SQLFragClause(fname);
            }
            var res = "";
            var tb = clause.BelongTable;
            if (tb != null)
            {
                var tbname = tb.Name;
                tbname = this.TranslateValue(tbname, ConvertType.NameToQueryTableAlias);
                res += tbname + ".";
            }

            var val = clause.FieldValue;
            if (val != null) { 
                var valRes= VisitIExpWord(val);
                if (valRes is SQLFragClause valFrag) {
                    res += valRes.ToString();
                }
            }

            res += fname;

            return new SQLFragClause(res);


        }

        public virtual bool DisableAlias { get; set; }

        protected TableAliasVisitor TableAliasVisitor = new TableAliasVisitor();
        /// <summary>
        /// 字段
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public override Clause VisitFieldWord(FieldWord field)
        {
            var fname = this.TranslateValue(field.PhysicalName, ConvertType.NameToQueryField);
            if (DisableAlias) {
                
                return new SQLFragClause(fname);
            }

            var res = "";
            var tb = field.BelongTable;
            if (tb != null) { 
                var tbname = tb.Name;
                var tbRes=TableAliasVisitor.VisitTableNode(tb);
                if (tbRes is SQLFragClause tbFrag) { 
                    tbname = tbRes.ToString();
                }
                tbname = this.TranslateValue(tbname, ConvertType.NameToQueryTableAlias);
                res += tbname + ".";
            }
            res += fname;

            return new SQLFragClause(res);
        }

        protected string FindTableAlias(ColumnWord column) {
            var sentence = column.Parent;
            var t = sentence.From;

            return "";
        }
        /// <summary>
        /// 转译 join类型
        /// </summary>
        /// <param name="join"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        protected virtual string TranslateJoinType(JoinKind join)
        {
            switch (join)
            {
                case JoinKind.Cross: return "CROSS JOIN";
                case JoinKind.Inner: return "INNER JOIN";
                case JoinKind.Left: return "LEFT JOIN";
                case JoinKind.CrossApply: return "CROSS APPLY";
                case JoinKind.OuterApply: return "OUTER APPLY";
                case JoinKind.Right: return "RIGHT JOIN";
                case JoinKind.Full: return "FULL JOIN";
                default: throw new InvalidOperationException("未知的Join类型");
            }
        }
        /// <summary>
        /// join 是否有ON的条件部分
        /// </summary>
        /// <param name="join"></param>
        /// <returns></returns>
        protected bool JoinHasOn(JoinKind join) {
            if (join == JoinKind.Inner|| join == JoinKind.Left|| join == JoinKind.Right|| join == JoinKind.Full) { 
                return true;
            }
            return false;
        }
        /// <summary>
        /// join 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public override Clause VisitJoinedTable(JoinTableWord clause)
        {
            
            

            var joinStr= TranslateJoinType(clause.JoinType);
            var table= VisitTableNode( clause.Table);
            var str = joinStr + " " + table;
            if (JoinHasOn(clause.JoinType)){
                var condition = BuildContionNotWhere(clause.Condition);
                str = str +" ON " + condition;
            }
                
            return new SQLFragClause(str);
        }
        /// <summary>
        /// groupby的类型转译
        /// </summary>
        /// <param name="groupingType"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        protected virtual string TranslateGroupingType(GroupingType groupingType) {
            switch (groupingType)
            {
                case GroupingType.Default:
                    return "";
                case GroupingType.GroupBySets:
                    return "GROUPING SETS";
                case GroupingType.Rollup:
                    return "ROLLUP";
                case GroupingType.Cube:
                    return"CUBE";
                default:
                    throw new InvalidOperationException($"Unexpected grouping type: {groupingType}");
            }
        }
        /// <summary>
        /// 逗号
        /// </summary>
        protected virtual string Comma
        {
            get {
                return ",";
            }
        }


        public override Clause VisitCteClause(CTEClause clause)
        {
            //需要临时切换编织器上下文
            var noew = this.builder;
            builder.withSelect(clause.Name, (kit) =>
            {
                this.builder = kit;
                this.VisitSelectQuery(clause.Body);
            });
            this.builder= noew;
            return null;
        }
        /// <summary>
        /// 转译select的入口
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public override Clause VisitSelectSentence(SelectSentence clause)
        {
            var tag = VisitComment(clause.Tag);
            if (tag != null)
            {
                builder.prefix(tag.ToString());
            }
            if (clause.With != null)
            {
                VisitWithClause(clause.With);
            }

            if (clause.SelectQuery !=null)
            {
                VisitSelectClause(clause.SelectQuery.Select);
                VisitFromClause(clause.SelectQuery.From);
                VisitWhereClause(clause.SelectQuery.Where);
                VisitGroupByClause(clause.SelectQuery.GroupBy);
                VisitHavingClause(clause.SelectQuery.Having);
                VisitOrderByClause(clause.SelectQuery.OrderBy);
            }
            


            return new SQLBuilderClause(builder, (tar) => {
                return tar.Builder.toSelect();
            });
        }
        /// <summary>
        /// select从句
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public override Clause VisitSelectClause(SelectClause clause)
        {
            if (clause == null) { return clause; }
            if (clause.TakeValue != null)
            {
                var TakeValue = VisitIExpWord(clause.TakeValue);
            }
            if (clause.SkipValue != null)
            {
                var SkipValue = VisitIExpWord(clause.SkipValue);
            }
            //遍历明细
            if (clause.Columns != null && clause.Columns.Count > 0)
            {
                for (int i = 0; i < clause.Columns.Count; i++)
                {
                    var col = VisitColumnWord(clause.Columns[i]);
                    if (col is SQLFragClause colfrag) {
                        builder.select(colfrag.ToString());
                    }
                }
            }
            return null;
        }



        /// <summary>
        /// from从句
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public override Clause VisitFromClause(FromClause clause)
        {
            if (clause.focus != null) {
                var tar = VisitBoxTable(clause.focus);
                //由于selectQuery也注册为了ItableNode，这里进行过滤
                if (tar is SQLFragClause frag) {
                    var str = frag.ToString();
                    builder.from(str);
                }
                
            }
            return null;
        }
        /// <summary>
        /// 访问盒表
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public override Clause VisitBoxTable(BoxTable clause)
        {
            if (clause.Content == null) return null;

            var tar= clause.Content;
            Func<LinkBox<ITableNode, JoinKind, JoinOnWord>, string> func=null;
            func = (LinkBox<ITableNode, JoinKind, JoinOnWord> he) =>
            {
                var res = "";
                if (he.isBox)
                {
                    var chids= new List<string>();
                    
                    foreach (var child in he.children)
                    {
                        var tar = child.Visit(func);
                        if (!string.IsNullOrWhiteSpace(tar)) {
                            chids.Add(tar);
                        }
                    }
                    if (chids.Count == 1)
                    {
                        res += chids[0];
                    }
                    else if (chids.Count > 1) {
                        res += "(";
                        res += string.Join(" ", chids);
                        res += ")";
                    }
                    
                }
                else {
                    res += " ";
                    if (he.Prefix != JoinKind.Auto) {
                        res += TranslateJoinType(he.Prefix);
                    }
                    var tb= VisitTableNode(he.value);
                    res += " " + tb.ToString();
                    if (he.Subfix != null) {
                        var onstr = BuildContionNotWhere(he.Subfix.condition);
                        if (!string.IsNullOrWhiteSpace(onstr)) { 
                            res +=" "+ onstr;
                        }
                    }
                }
                return res;
            };

            var sql = tar.Visit(func);
            return new SQLFragClause(sql);
        }
        /// <summary>
        /// SelectQueryClause是作为查询使用的，只需构造它的where 内容
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public override Clause VisitSelectQuery(SelectQueryClause clause)
        {
            //如果他拥有where部分，也构造他的条件
            if (clause.Where != null) {
                var condi = clause.Where.SearchCondition;
                VisitSearchCondition(condi);
            }

            var content = clause.From.focus;
            var tar = VisitBoxTable(content);
            if (tar is SQLFragClause frag)
            {
                //var sql=tar.ToString();
                return tar;
            }
            return clause;
        }

        /// <summary>
        /// 代理表
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public override Clause VisitDerivatedTable(DerivatedTableWord clause)
        {
            var res = "";
            var tb = clause.src;
            var tbRes = VisitTableNode(tb);
            if (tbRes == null) { return null; }
            //访问他可能也是在访问 SelectQuery
            bool gotTbSQL = false;
            if (tbRes is SQLFragClause frag) { 
                var tbstr= tbRes.ToString();
                if (!string.IsNullOrWhiteSpace(tbstr)) {
                    res += tbstr;
                    gotTbSQL = true;
                }            
            }

            if (!string.IsNullOrWhiteSpace(clause.Name)) {
                if (gotTbSQL)
                {
                    res += " as ";
                }
                res += clause.Name;
            }
            return new SQLFragClause (res);
        }

        public override Clause VisitTableWord(TableWord clause)
        {

            var name=clause.Name;
            return new SQLFragClause (name);
        }


        public override Clause VisitWhereClause(WhereClause clause)
        {
            VisitSearchCondition(clause.SearchCondition);
            return null;
        }

        public override Clause VisitSearchCondition(SearchConditionWord clause)
        {
            if (clause.IsOr)
            {
                builder.sinkOR();
                foreach (var i in clause.Predicates)
                {
                    this.VisitAffirmWord(i);
                }
                builder.rise();
            }
            else {
                builder.sink();
                foreach (var i in clause.Predicates)
                {
                    this.VisitAffirmWord(i);
                }
                builder.rise();
            }
            return null;
        }

        /// <summary>
        /// 构建group by的内容
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public override Clause VisitGroupByClause(GroupByClause clause)
        {
            var fixStr = this.TranslateGroupingType(clause.GroupingType);

            var content = new List<string>();
            for (int i = 0; i < clause.Items.Count; i++)
            {
                var item = clause.Items[i];
                var str = this.VisitIExpWord(item).ToString();
                if (!string.IsNullOrWhiteSpace(str))
                {
                    content.Add(str);
                }
            }

            var inner = string.Join(Comma, content);
            if (!string.IsNullOrWhiteSpace(inner))
            {
                var res = fixStr + " " + inner;
                builder.groupBy(res);
                return null;
            }
            return null;
        }
        /// <summary>
        /// group 的成员
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public override Clause VisitGroupingSet(GroupingSetWord clause)
        {
            var content = new List<string>();
            for (int i = 0; i < clause.Items.Count; i++)
            {
                var item = clause.Items[i];
                var str = this.VisitIExpWord(item).ToString();
                if (!string.IsNullOrWhiteSpace(str))
                {
                    content.Add(str);
                }
            }

            var inner = string.Join(Comma, content);
            if (!string.IsNullOrWhiteSpace(inner))
            {
                return new SQLFragClause(inner);
            }
            return new SQLFragClause("");
        }

        /// <summary>
        /// having语句的处理
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public override Clause VisitHavingClause(HavingClause clause)
        {

            //提取构造结果
            var wh = BuildContionNotWhere(clause.SearchCondition);
            //编织
            builder.having(wh);
            return null;
        }

        /// <summary>
        /// 构建order by的内容体
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public override Clause VisitOrderByClause(OrderByClause clause)
        {
            if (clause.Items == null || clause.Items.Count == 0)
            {
                return null;
            }

            var content = new List<string>();
            for (int i = 0; i < clause.Items.Count; i++)
            {
                var item = clause.Items[i];
                var str = this.VisitOrderByItem(item).ToString();
                if (!string.IsNullOrWhiteSpace(str))
                {
                    content.Add(str);
                }
            }

            var inner = string.Join(Comma, content);
            if (!string.IsNullOrWhiteSpace(inner))
            {
                var res = " " + inner;
                builder.orderBy(res);
                return null;
            }
            return null;
        }
        /// <summary>
        /// orderby 的某个条件
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public override Clause VisitOrderByItem(OrderByWord clause)
        {
            var val = clause.Expression;

            var tar = this.VisitIExpWord(val).ToString();

            if (!string.IsNullOrWhiteSpace(tar))
            {

                if (clause.IsDescending)
                {
                    tar += " DESC";
                }
                else
                {
                    tar += " ASC";
                }

                return new SQLFragClause(tar);
            }
            return null;
        }
        /// <summary>
        /// 转译数据对象的全称
        /// </summary>
        /// <param name="name"></param>
        /// <param name="objectType"></param>
        /// <param name="escape"></param>
        /// <param name="tableOptions"></param>
        /// <param name="withoutSuffix"></param>
        /// <returns></returns>
        public virtual string TranslateObjectName(SqlObjectName name, ConvertType objectType = ConvertType.NameToQueryTable, bool escape = true, TableOptions tableOptions = TableOptions.NotSet, bool withoutSuffix = false)
        {
            var res = "";
            if (name.Database != null)
            {
                if (escape)
                {
                    res += TranslateValue(name.Database, ConvertType.NameToDatabase);
                }
                else {
                    res += name.Database;
                }
                res += ".";

                if (name.Schema == null)
                    res +=('.');
            }

            if (name.Schema != null)
            {
                if (escape)
                {
                    res += TranslateValue(name.Schema, ConvertType.NameToSchema);
                }
                else
                {
                    res += name.Schema;
                }
                res += ".";

            }

            if (escape)
            {
                res += TranslateValue(name.Name, objectType);
            }
            else {
                res += name.Name;
            }

            return res;
        }
    }
}
