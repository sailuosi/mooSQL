// 基础功能说明：

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{

    /// 查询方面的SQL

    public partial class SQLBuilder
    {
        /// <summary>
        /// 设置一个CTE表达式，可设置多个
        /// </summary>
        /// <param name="name"></param>
        /// <param name="doselect"></param>
        /// <returns></returns>
        public SQLBuilder withSelect(string name, Action<SQLBuilder> doselect) {

            var kit = this.getBrotherBuilder();
            doselect(kit);

            var item = new SqlCTEItem();
            item.builder = kit;
            item.type= SqlCTEType.Select;
            item.asName = name;

            CTECollection.add(item);
            return this;
        }



        /// <summary>
        /// 插入一段 with tabletmp as ( ... ) 的SQL语句到 后续执行的SQL之前。 将自动调用委托的 toSelect 方法获取SQL语句编织的结果。
        /// </summary>
        /// <param name="name"></param>
        /// <param name="selectBuilder"></param>
        /// <returns></returns>
        public SQLBuilder withAs(string name, Action<SQLBuilder> selectBuilder)
        {
            this.withSelect(name, selectBuilder);
            //var kit = this.getBrotherBuilder();

            //selectBuilder(kit);
            //var sql = kit.toSelect();
            //var withSQL = string.Format("with {0} as ({1})", name, sql.sql);
            //current.prefix(withSQL);
            return this;
        }

        public RecurCTEBuilder withRecurTo(string name)
        {
            var rec = new RecurCTEBuilder();
            rec.setWithAsName(name);
            rec.useBuilder(this);
            return rec;
        }

        public SQLBuilder withRecur(string name, Action<RecurCTEBuilder> buildRecur)
        {

            var rec = new RecurCTEBuilder();
            rec.setWithAsName(name);
            rec.useBuilder(this);
            buildRecur(rec);
            rec.apply();
            return this;
        }

        /// <summary>
        /// 创建一个CTE，可以多个
        /// </summary>
        /// <param name="name"></param>
        /// <param name="selectSQL"></param>
        /// <returns></returns>
        public SQLBuilder withSelect(string name, string selectSQL)
        {

            var item = new SqlCTEItem();
            item.type = SqlCTEType.SolidSQL;
            item.solidSQL = selectSQL;
            item.asName = name;

            CTECollection.add(item);
            return this;
        }

        /// <summary>
        /// 设置 select部分的SQL，不设置时为 *，多次调用自动累积。
        /// </summary>
        /// <param name="columns"></param>
        /// <returns></returns>

        public SQLBuilder select(string columns)
        {
            current.select(columns);
            return this;
        }
        /// <summary>
        /// SQL语句列定义
        /// </summary>
        /// <param name="asName"></param>
        /// <param name="doColSelect"></param>
        /// <returns></returns>
        public SQLBuilder select(string asName, Action<SQLBuilder> doColSelect) 
        {
            var ckit = this.getBrotherBuilder();
            doColSelect(ckit);
            var selectSQL = ckit.toSelect();
            var fromPart = string.Format("({0}) as {1} ", selectSQL.sql, asName);
            current.select(fromPart);
            return this;
        }

        /// <summary>
        /// 对unnion 的包裹最外层select语句进行select赋值。
        /// </summary>
        /// <param name="columns"></param>
        /// <returns></returns>
        public SQLBuilder selectUnioned(string columns)
        {
            unionHolder.unitedWraper.select(columns);
            return this;
        }

        /// <summary>
        /// 默认不唯一，调用则设置为distinct。
        /// </summary>
        /// <returns></returns>
        public SQLBuilder distinct()
        {
            current.distinct();
            return this;
        }
        /// <summary>
        /// 选取前几条记录，自动根据数据库使用top或limit 
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public SQLBuilder top(int num)
        {
            current.top(num);
            return this;
        }
        /// <summary>
        /// 设置查询语句的from部分，不设置时为 构造器的tableName。用于select语句、delete语句或insert from 语句。连续from时，中间会用逗号连接，否则需要使用join时，请用join 方法。
        /// </summary>
        /// <param name="fromPart"></param>
        /// <returns></returns>

        public SQLBuilder from(string fromPart)
        {
            current.from(fromPart);
            return this;
        }
        /// <summary>
        /// 注意！不会自动添加left join这样的前缀字符，请写全 join语句，包含 on 部分。
        /// </summary>
        /// <param name="joinSQLString"></param>
        /// <returns></returns>
        public SQLBuilder join(string joinSQLString)
        {
            current.fromAppend(joinSQLString);
            return this;
        }
        /// <summary>
        /// 注意！不会自动添加left join这样的前缀字符，请写全 join语句，包含 on 部分。
        /// </summary>
        /// <param name="joinSQLString"></param>
        /// <param name="childFromPart">(select的查询语句) as {childFromPart}</param>
        /// <returns></returns>
        public SQLBuilder join(string joinKey,string joinSQLString, Action<SQLBuilder> childFromPart)
        {
            var ckit = this.getBrotherBuilder();
            childFromPart(ckit);
            var selectSQL = ckit.toSelect();
            var fromPart = string.Format(" {0} ({1}) as {2} ",joinKey ,selectSQL.sql, joinSQLString);
            current.fromAppend(fromPart);
            return this;
        }
        /// <summary>
        /// 左连接
        /// </summary>
        /// <param name="joinSQLString"></param>
        /// <param name="childFromPart"></param>
        /// <returns></returns>
        public SQLBuilder leftJoin( string joinSQLString, Action<SQLBuilder> childFromPart)
        {
            return this.join("LEFT JOIN", joinSQLString, childFromPart);
        }
        public SQLBuilder leftJoin(string joinSQLString)
        {
            return this.join("LEFT JOIN "+ joinSQLString);
        }
        /// <summary>
        /// 内连接
        /// </summary>
        /// <param name="joinSQLString"></param>
        /// <param name="childFromPart"></param>
        /// <returns></returns>
        public SQLBuilder innerJoin(string joinSQLString, Action<SQLBuilder> childFromPart)
        {
            return this.join("INNER JOIN", joinSQLString, childFromPart);
        }
        public SQLBuilder innerJoin(string joinSQLString)
        {
            return this.join("INNER JOIN "+ joinSQLString);
        }
        /// <summary>
        /// 右连接
        /// </summary>
        /// <param name="joinSQLString"></param>
        /// <param name="childFromPart"></param>
        /// <returns></returns>
        public SQLBuilder rightJoin(string joinSQLString, Action<SQLBuilder> childFromPart)
        {
            return this.join("RIGHT JOIN", joinSQLString, childFromPart);
        }
        /// <summary>
        /// 使用子查询来构建 from布局 ，子查询可配置所有select配置。
        /// </summary>
        /// <param name="childFromPart"></param>
        /// <param name="asName"></param>
        /// <returns></returns>
        public SQLBuilder from(string asName, Action<SQLBuilder> childFromPart)
        {
            var ckit = this.getBrotherBuilder();
            childFromPart(ckit);
            var selectSQL = ckit.toSelect();
            var fromPart = string.Format("({0}) as {1} ", selectSQL.sql, asName);
            current.from(fromPart);
            return this;
        }

        /// <summary>
        /// 配置行转列的
        /// </summary>
        /// <param name="SQLString"></param>
        /// <returns></returns>
        public SQLBuilder pivot(PivotItem SQLString)
        {
            current.pivot(SQLString);
            return this;
        }
        /// <summary>
        /// 配置列转行的转置部分
        /// </summary>
        /// <param name="SQLString"></param>
        /// <returns></returns>
        public SQLBuilder unpivot(UnpivotItem SQLString)
        {
            current.unpivot(SQLString);
            return this;
        }
        /// <summary>
        /// 配置行转列的 SQL部分 ，注意：Mysql下慎用
        /// </summary>
        /// <param name="aggregation"></param>
        /// <param name="field"></param>
        /// <param name="values"></param>
        /// <param name="asName"></param>
        /// <returns></returns>
        public SQLBuilder pivot(string aggregation, string field, List<string> values, string asName)
        {
            current.pivot(new PivotItem(aggregation, field, values, asName));
            return this;
        }
        /// <summary>
        /// 配置列转行的转置部分
        /// </summary>
        /// <param name="valueName"></param>
        /// <param name="fieldName"></param>
        /// <param name="fields"></param>
        /// <param name="asName"></param>
        /// <returns></returns>
        public SQLBuilder unpivot(string valueName, string fieldName, List<string> fields, string asName)
        {
            current.unpivot(new UnpivotItem(valueName, fieldName, fields, asName));
            return this;
        }

        /// <summary>
        /// group by 后面跟随的内容，不用带关键字
        /// </summary>
        /// <param name="groupField"></param>
        /// <returns></returns>
        public SQLBuilder groupBy(string groupField)
        {
            current.groupBy(groupField);
            return this;
        }
        /// <summary>
        /// having 跟随的内容，当设置了groupby 才会生效
        /// </summary>
        /// <param name="havingStr"></param>
        /// <returns></returns>
        public SQLBuilder having(string havingStr)
        {
            current.having(havingStr);
            return this;
        }


        /// <summary>
        /// union All
        /// </summary>
        /// <param name="wrapSelect"></param>
        /// <param name="wrapAsName"></param>
        /// <returns></returns>
        public SQLBuilder unionAll(bool wrapSelect = true, string wrapAsName = "tmpunioned") {
            this.union(true, wrapSelect, wrapAsName);
            return this;
        }

        /// <summary>
        /// 设置是否使用 union all,以及union 外层是否需要自动用一层select包裹
        /// </summary>
        /// <param name="isUnionAll"></param>
        /// <param name="wrapSelect"></param>
        /// <returns></returns>
        public SQLBuilder union( bool isUnionAll = false, bool wrapSelect = true, string wrapAsName = "tmpunioned")
        {
            unionHolder.union(this, isUnionAll, wrapSelect, wrapAsName);
            return this;
        }
        /// <summary>
        /// 对unsion的执行器进行配置
        /// </summary>
        /// <param name="dogroup"></param>
        /// <returns></returns>
        public SQLBuilder unionAs(Action<SqlGoup> dogroup)
        {
            //this._unionAll = isUnionAll;
            //this._unionWrap = wrapSelect;
            dogroup(this.unionHolder.unitedWraper);
            return this;
        }
        /// <summary>
        /// 将当前的语句配置焦点移动到 union 的包裹层SQL分组
        /// </summary>
        /// <returns></returns>
        public SQLBuilder toggleToUnionOutor()
        {
            if (this.unionHolder.Count == 0)
            {
                return this;
            }
            //if (!this.unionHolder.Contains(current) && this.current != this.unitedExecutor)
            //{
            //    this.united.Add(current);
            //}

            this.current = this.unionHolder.unitedWraper;
            return this;
        }


        /// <summary>
        /// union 一个新的查询，不影响当前的SQL分组
        /// </summary>
        /// <param name="doUnion"></param>
        /// <returns></returns>
        public SQLBuilder union(Action<SQLBuilder> doUnion)
        {
            //当开始时，把当前的放入到union列表。并创建一个新的
            var cur = current;

            union();
            doUnion(this);
            current = cur;
            return this;
        }


        /// <summary>
        /// 设置排序部分
        /// </summary>
        /// <param name="orderByPart"></param>
        /// <returns></returns>

        public SQLBuilder orderBy(string orderByPart)
        {
            if (this.unionHolder.Count>0 )
            {
                unionHolder.orderby(orderByPart);
                //联合模式下，只有最后的SQL执行排序。
                return this;
            }
            current.orderby(orderByPart);
            return this;
        }
        /// <summary>
        /// 设置排序部分，规范化后废弃，请使用 orderBy 方法代替
        /// </summary>
        /// <param name="orderByPart"></param>
        /// <returns></returns>
        [Obsolete("规范化后废弃，请使用 orderBy 方法代替")]
        public SQLBuilder orderby(string orderByPart) { 
            return orderBy(orderByPart);
        }

        /// <summary>
        /// 设置翻页排序的依据
        /// </summary>
        /// <returns></returns>

        public SQLBuilder rowNumber()
        {
            current.rowNumber();
            return this;
        }
        /// <summary>
        /// 使用一个自行定义的好的序号字段作为翻页依据
        /// </summary>
        /// <param name="numFieldName"></param>
        /// <returns></returns>
        public SQLBuilder rowNumberUse(string numFieldName)
        {
            current.rowNumberUse(numFieldName);
            return this;
        }
        /// <summary>
        /// 行号开窗函数
        /// </summary>
        /// <param name="orderPart"></param>
        /// <returns></returns>
        public SQLBuilder rowNumber(string orderPart)
        {
            if (this.unionHolder.Count>1)
            {
                unionHolder.unitedWraper.rowNumber(orderPart);
                return this;
            }
            current.rowNumber(orderPart);
            return this;
        }
        /// <summary>
        /// 行号开窗函数
        /// </summary>
        /// <param name="orderPart"></param>
        /// <param name="asName"></param>
        /// <returns></returns>
        public SQLBuilder rowNumber(string orderPart, string asName)
        {

            if (this.unionHolder.Count > 1)
            {
                unionHolder.unitedWraper.rowNumber(asName, orderPart);
                return this;
            }
            current.rowNumber(asName, orderPart);
            return this;
        }
        /// <summary>
        /// 设置翻页的参数
        /// </summary>
        /// <param name="size"></param>
        /// <param name="num"></param>
        /// <returns></returns>
        public SQLBuilder setPage(int size, int num)
        {

            if (this.unionHolder.Count > 1)
            {
                unionHolder.unitedWraper.setPage(size, num);
                return this;
            }
            current.setPage(size, num);
            return this;
        }
    }
}
