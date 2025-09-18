
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using mooSQL.data.builder;


namespace mooSQL.data
{
    /// <summary>
    /// 数据库方言类：主要是 SQL表达式的构建器，处理数据库方言中的SQL语法方言
    /// </summary>
    public abstract partial class SQLExpression
    {

        /// <summary>
        /// 根方言实例
        /// </summary>
        public Dialect dialect;
        /// <summary>
        /// 构造器
        /// </summary>
        /// <param name="parent"></param>
        public SQLExpression(Dialect parent) { 
            this.dialect = parent;
        }
        /// <summary>
        /// 数据库配置
        /// </summary>
        public DataBase DB
        {
            get
            {
                return dialect.db;
            }
        }
        /// <summary>
        /// 数据库核心可有实例
        /// </summary>
        public DBInstance DBLive {
            get { 
                return dialect.dbInstance;
            }
        }


        /// <summary>
        /// 构建常规的 select语句
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        public abstract string buildSelect(FragSQL frag);
        /// <summary>
        /// 构建非翻页语句的from到order by 部分
        /// </summary>
        /// <param name="frag"></param>
        /// <param name="sb"></param>
        protected void buildSelectFromToOrderPart(FragSQL frag, StringBuilder sb) {
            sb.Append(" FROM ");
            sb.Append(frag.fromInner);
            sb.Append(" ");
            if (!string.IsNullOrWhiteSpace(frag.whereInner))
            {
                sb.Append("WHERE ");
                sb.Append(frag.whereInner);
                sb.Append(" ");
            }

            if (!string.IsNullOrWhiteSpace(frag.groupByInner))
            {
                sb.Append("GROUP BY ");
                sb.Append(frag.groupByInner);
                sb.Append(" ");
            }
            if (!string.IsNullOrWhiteSpace(frag.havingInner))
            {
                sb.Append("HAVING ");
                sb.Append(frag.havingInner);
                sb.Append(" ");
            }

            if (!string.IsNullOrWhiteSpace(frag.orderbyInner))
            {
                sb.Append("ORDER BY ");
                sb.Append(frag.orderbyInner);
                sb.Append(" ");
            }
        }
        /// <summary>
        /// 构建select count
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        public virtual string buildSelectCount(FragSQL frag) {

            string res = "select count(*) as cn ";
            //如果包含了distinct，则不能省略列信息
            if (frag.distincted)
            {

                res = "select distinct " + frag.selectInner;
            }
            //未设置from部分时，直接使用表名
            res += " from ";
            res += frag.fromInner;
            //检查where部分

            if (!string.IsNullOrWhiteSpace(frag.whereInner))
            {
                res += " where ";
                res += frag.whereInner;
            }

            if (frag.distincted)
            {
                res = "select count(*) as cn from (" + res + ") ditnumwrap";
            }

            return res;
            

        }

        /// <summary>
        /// 构建分页模式的查询SQL
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        public virtual string buildPagedSelect(FragSQL frag) {
            return this.buildPagedByRowNumber(frag);
        }
        /// <summary>
        /// 通过romNumber() 来实现分页
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        public string buildPagedByRowNumber(FragSQL frag)
        {
            //如果没有设置排序列，则自动设置

            //先移除排序来构建
            var orderby = frag.orderbyInner;
            frag.orderbyInner = "";

            //默认实现下，翻页必定要有
            var asName = frag.rowNumberFieldName;
            if (string.IsNullOrWhiteSpace(asName))
            {
                asName = "rowoonum";
            }

            var nfie = "ROW_NUMBER() over (";
            if (!string.IsNullOrWhiteSpace(frag.rowNumberOrderBy))
            {
                nfie += "order by " + frag.rowNumberOrderBy;
            }
            else if (!string.IsNullOrWhiteSpace(orderby)) {
                nfie += "order by " + orderby;
                //如果外置的order by 被翻页使用，则替换后缀的order by 为翻页参数
                orderby = asName + " ASC ";
            }
            nfie += (") as " + asName);

            //追加到select语句中
            if (string.IsNullOrWhiteSpace(frag.selectInner))
            {
                frag.selectInner = "*";
            }
            frag.selectInner += "," + nfie;
            frag.hasRowNumber = false;
            string cksqlSimple = this.buildSelect(frag);
            var cksql = wrapPaged(asName, cksqlSimple, frag.pageSize, frag.pageNum, orderby);


            return cksql;
        }
        /// <summary>
        /// 构造在
        /// </summary>
        /// <param name="frag"></param>
        /// <param name="onBuildPagePart"></param>
        /// <returns></returns>
        protected string buildPagedSelectTail(FragSQL frag,Action<StringBuilder> onBuildPagePart)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT ");
            if (frag.distincted)
            {
                sb.Append("distinct ");
            }
            
            if (!string.IsNullOrWhiteSpace(frag.rowNumberFieldName) && !string.IsNullOrWhiteSpace(frag.rowNumberOrderBy)){
                var nfie = "ROW_NUMBER() over (";

                    nfie += "order by " + frag.rowNumberOrderBy;
                
   
                nfie += (") as " + frag.rowNumberFieldName);
                if (!string.IsNullOrWhiteSpace(frag.selectInner))
                {
                    frag.selectInner += "," + nfie;
                }
                else { 
                    frag.selectInner = nfie;
                }
                frag.rowNumberOrderBy = null;
                frag.rowNumberFieldName = null;
            }
            sb.Append(frag.selectInner);
            sb.Append(" ");
            sb.Append("from ");
            sb.Append(frag.fromInner);
            sb.Append(" ");
            if (!string.IsNullOrWhiteSpace(frag.whereInner))
            {
                sb.Append("where ");
                sb.Append(frag.whereInner);
                sb.Append(" ");
            }

            if (!string.IsNullOrWhiteSpace(frag.groupByInner))
            {
                sb.Append("group by ");
                sb.Append(frag.groupByInner);
                sb.Append(" ");
            }
            if (!string.IsNullOrWhiteSpace(frag.havingInner))
            {
                sb.Append("having ");
                sb.Append(frag.havingInner);
                sb.Append(" ");
            }


            if (!string.IsNullOrWhiteSpace(frag.orderbyInner))
            {
                sb.Append("order by ");
                sb.Append(frag.orderbyInner);
                sb.Append(" ");
            }

            onBuildPagePart(sb);

            return sb.ToString();
        }
        /// <summary>
        /// 构建行号开窗函数
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        protected string buildRowNumber(FragSQL frag) {

            var asName = frag.rowNumberFieldName;
            if (string.IsNullOrWhiteSpace(asName))
            {
                asName = "rowoonum";
            }

            var nfie = "ROW_NUMBER() over (";
            if (!string.IsNullOrWhiteSpace(frag.rowNumberOrderBy))
            {
                nfie += "order by " + frag.rowNumberOrderBy;
            }
            nfie += (") as " + asName);
            return " "+ nfie+" ";
        }

        public string wrapPaged(string orderColName, string readSql, int pageSize, int pageNum)
        {
            int end = pageSize * (pageNum - 1);
            FragSQL sql = new FragSQL();
            sql.selectInner = "*";
            sql.fromInner = "datares";
            sql.whereInner = orderColName + " > " + end;
            sql.toped = pageSize;

            return "with datares as ( " + readSql + " ) " + buildSelect(sql);
        }
        public string wrapPaged(string orderColName, string readSql, int pageSize, int pageNum, string orderByPart)
        {
            int end = pageSize * (pageNum - 1);
            FragSQL sql = new FragSQL();
            sql.selectInner = "*";
            sql.fromInner = "datares";
            sql.whereInner = orderColName + " > " + end;
            sql.orderbyInner = orderByPart;
            sql.toped = pageSize;
            return "with datares as ( " + readSql + " ) " + buildSelect(sql);
        }


        public string wrapPageOrder(string orderByPart, string readsql, int pageSize, int pageNum)
        {
            //采取直接套一层语句的方式。
            string ressql = "select *,ROW_NUMBER() over (order by " + orderByPart + ") as oonum from (" + readsql + ") as tam";
            return this.wrapPaged("oonum", ressql, pageSize, pageNum);
        }

        /// <summary>
        /// 创建 insert into语句
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        public abstract string buildInsert(FragSQL frag);
        /// <summary>
        /// 创建普通的单行 update 语句
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        public virtual string buildUpdate(FragSQL frag) {
            //SQL Server的语法   UPDATE table_name            SET c1 = v1, c2 = v2, ... cn = vn            [WHERE condition]
            StringBuilder sql = new StringBuilder();
            if (string.IsNullOrEmpty(frag.fromInner))
            {
                //此时是一个单纯的 update set 语句

                sql.Append("UPDATE ");
                sql.Append(frag.updateTo + " ");

                sql.Append(" SET ");
                sql.Append(this.buildSetPart(frag));

                if (!string.IsNullOrWhiteSpace(frag.whereInner))
                {
                    sql.AppendFormat(" where {0}", frag.whereInner);
                }
            }
            else { 
                return this.buildUpdateFrom(frag); 
            }

            return sql.ToString();
        }

        protected virtual string buildSetPart(FragSQL frag) { 
            return this.buildSetPartList(frag.setPart);
        }
        protected virtual string buildSetPartList(List<FragSetPart> setPart)
        {
            bool isFirst = true;
            StringBuilder sb = new StringBuilder();
            foreach (var item in setPart)
            {
                if (!isFirst)
                {
                    sb.Append(",");
                }
                else
                {
                    isFirst = false;
                }
                sb.Append(item.field);
                sb.Append("=");
                sb.Append(item.value);
                sb.Append(" ");
            }
            return sb.ToString();
        }

        /// <summary>
        /// 构建删除语句
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        public virtual string buildDelete(FragSQL frag) {
            StringBuilder sql = new StringBuilder();
            sql.Append("Delete ");//FROM  

            if (!string.IsNullOrWhiteSpace(frag.fromInner))
            {
                sql.Append(frag.deleteTar);
                sql.Append(" ");
                //含有from的删除语句
                sql.Append("FROM ");
                sql.Append(frag.fromInner);
                if (!string.IsNullOrWhiteSpace(frag.whereInner))
                {
                    sql.AppendFormat(" where {0}", frag.whereInner);
                }
            }
            else {
                //不带from的简单删除
                sql.Append("FROM ");
                sql.Append(frag.deleteTar);
                if (!string.IsNullOrWhiteSpace(frag.whereInner))
                {
                    sql.AppendFormat(" where {0}", frag.whereInner);
                }
            }
            
            return sql.ToString();

        }
        /// <summary>
        /// 创建 update from 语句
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public virtual string buildUpdateFrom(FragSQL frag) {
            throw new Exception("未定义的数据库Update from 语法");
        }
        /// <summary>
        /// 创建 merge into 语句
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public virtual string buildMergeInto(FragMergeInto frag)
        {
            throw new Exception("未定义的数据库merge into 语法");
        }
        protected string buildMergeIntoGeneral(FragMergeInto frag)
        {
            //merge into 目标表 a
            //using 源表 b
            //on a.条件字段1 = b.条件字段1 and a.条件字段2 = b.条件字段2...
            //when matched update set a.字段1 = b.字段1,
            //      a.字段2 = b.字段2
            //when not matched insert values(b.字段1, b.字段2)
            //when not matched by source
            //then delete

            var sb = new StringBuilder();
            sb.AppendFormat("merge into {0} ", frag.intoTable);

            if (!string.IsNullOrWhiteSpace(frag.usingAlias))
            {
                sb.AppendFormat(" using ({0}) as {1} ", frag.usingTable,frag.usingAlias);
            }
            else
            {
                sb.AppendFormat(" using {0}", frag.usingTable);
            }
            sb.AppendFormat(" on ({0}) ", frag.onPart);
            foreach (var when in frag.mergeWhens) {
                var moreWH = "";
                if (when.matched) {
                    moreWH += " when matched ";
                }
                else { 
                    moreWH += " when not matched ";
                }
                if (!string.IsNullOrWhiteSpace(when.whenWhere))
                {
                    moreWH = " AND (" + when.whenWhere + ")";
                }
                if (when.action == MergeAction.update)
                {

                    sb.AppendFormat(" {0} then update set {1} ", moreWH,this.buildSetPartList( when.setInner));
                }
                else if (when.action == MergeAction.insert)
                {
                    sb.AppendFormat(" {0} then insert({1}) values( {2}) "
                        , moreWH, when.fieldInner, when.valueInner);
                }
                else if (when.action == MergeAction.delete) {
                    sb.Append(moreWH);
                    sb.Append(" then delete ");
                }
            }
            sb.Append(";");
            return sb.ToString();
        }
        /// <summary>
        /// 创建CTE表达式
        /// </summary>
        /// <param name="cte"></param>
        /// <returns></returns>
        public virtual string buildCET(SqlCTE cte) {

            if (cte == null) return "";
            if (cte.Empty) return "";
            
            var res= new StringBuilder();
            res.Append("with ");
            int cc = 0;
            foreach (var item in cte.cteList) {
                var cmd = item.getSQL();
                if (cmd != null && !string.IsNullOrWhiteSpace(cmd))
                {
                    if (cc > 0)
                    {
                        res.Append(',');
                    }
                    res.Append(" ");
                    res.Append(item.asName);
                    res.Append(" as (");
                    res.Append(cmd);
                    res.Append(')');
                    res.Append(" ");
                    cc++;
                }
            }
            return res.ToString();
        }

        /// <summary>
        /// 生成UPDATE {0} set {1} where {2};\n格式的update语句
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="kvmap"></param>
        /// <param name="whereString"></param>
        /// <returns></returns>
        public string dealUpdate(string tableName, System.Collections.Generic.Dictionary<string, string> kvmap, string whereString)
        {
            if (kvmap.Count == 0)
            {
                return "";
            }
            var strset = new StringBuilder();
            foreach (var kv in kvmap)
            {
                if (strset.Length > 0)
                {
                    strset.Append(",");
                }
                strset.Append(kv.Key);
                strset.Append("=");
                strset.Append(kv.Value);
            }
            var res = string.Format("UPDATE {0} set {1} where {2}", tableName, strset, whereString);
            return res;
        }


        /// <summary>
        /// 生成INSERT INTO {0}({1}) VALUES({2});\n格式的插入语句
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="kvmap"></param>
        /// <returns></returns>
        public string dealInsert(string tableName, Dictionary<string, string> kvmap)
        {//按值进行插入
            if (kvmap.Count == 0)
            {
                return "";
            }
            var selectstr = new StringBuilder();
            var valuestr = new StringBuilder();
            foreach (var kv in kvmap)
            {
                if (selectstr.Length > 0)
                {
                    selectstr.Append(",");
                }
                if (valuestr.Length > 0)
                {
                    valuestr.Append(",");
                }
                selectstr.Append(kv.Key);
                valuestr.Append(kv.Value);
            }
            var res = string.Format(" INSERT INTO {0}({1}) VALUES({2})", tableName, selectstr, valuestr);
            return res;
        }
    }
}
