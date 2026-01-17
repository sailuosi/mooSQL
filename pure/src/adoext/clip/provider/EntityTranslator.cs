using mooSQL.data.model;
using mooSQL.data.utils;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 实体转译
    /// </summary>
    public partial class EntityTranslator
    {

        private Func<EntityInfo,string> _onParseTableName;

        private Action<SQLBuilder, EntityInfo, EntityTranslator> _onBuildFromTable;
        private Action<SQLBuilder, EntityInfo, EntityTranslator> _onBuildFromPart;
        /// <summary>
        /// 构建条件部分事件
        /// </summary>
        public event Action<SQLBuilder, EntityInfo, EntityTranslator, QueryAction> onBuildWherePart;
        private List<string> ignoreUpdateFields;

        private List<string> includeUpdateFields;

        private List<string> ignoreInsertFields;

        private List<string> includeInsertField;
        /// <summary>
        /// 实体翻页器
        /// </summary>
        public EntityTranslator()
        {
            this.ignoreUpdateFields = new List<string>();
            this.ignoreInsertFields = new List<string>();
            this.includeInsertField = new List<string>();
            this.includeUpdateFields = new List<string>();

        }

        internal EntityTranslator BeforeBuildWhere(SQLBuilder kit,EntityInfo en,QueryAction qa) { 
            if(this.onBuildWherePart != null) { 

                this.onBuildWherePart(kit,en,this,qa);
            }
            return this;
        }

        private string parseTableName(EntityInfo en) {
            if (this._onParseTableName != null) { 
                return this._onParseTableName(en);
            }
            return en.DbTableName;
        }

        private string parseTableName(EntityInfo en,object row)
        {
            if (this._onParseTableName != null)
            {
                return this._onParseTableName(en);
            }
            if (en.LiveName == true) {
                foreach (var cepter in en.NameParses) { 
                    var name= cepter.Value.Parse(row);
                    if (!string.IsNullOrWhiteSpace(name)) { 
                        return name;
                    }
                }
            
            }
            return en.DbTableName;
        }
        /// <summary>
        /// 构建插入语句  
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="entity"></param>
        /// <param name="EntityType"></param>
        /// <param name="en"></param>
        /// <returns></returns>
        public StatusResult prepareInsert(SQLBuilder builder, object entity, Type EntityType,EntityInfo en=null)
        {
            if (en == null) {
                en = builder.DBLive.client.EntityCash.getEntityInfo(EntityType);
            }
            
            if (en.Insertable == false)
            {
                return new StatusResult(false, "实体类未标记为可插入！");
            }

            builder.setTable(parseTableName(en,entity));
            foreach (var col in en.Columns)
            {
                if (col.IsIgnore || col.IsOnlyIgnoreInsert || col.PropertyInfo == null) continue;
                //未设置或者基础表字段才进行更新
                if ((col.Kind == FieldKind.Base || col.Kind == FieldKind.None) == false) continue;
                //配置为忽略时，则忽略执行
                if (this.ignoreInsertFields.Count > 0 && this.ignoreInsertFields.Contains(col.PropertyName)) {
                    continue;
                }
                //配置了包含时，则只操作包含的字段
                if (this.includeInsertField.Count > 0 && this.ignoreInsertFields.Contains(col.PropertyName) == false) {
                    continue;
                }
                if (CheckEdition(builder.DBLive,col)==false) {
                    continue;
                }
                builder.set(col.DbColumnName, col.PropertyInfo.GetValue(entity));
            }
            return new StatusResult(true, "");
        }

        private bool CheckEdition(DBInstance DB, EntityColumn col) {
            if (DB.config.edition.HasText() == false)
            {//连接位未配置版本，视为通过
                return true;
            }
            var eds = col.Editions;
            if (eds == null || eds.Count == 0 )
            {//字段未配置版本，视为全版本可用
                return true;
            }
            if (eds.Contains(DB.config.edition) == false) {
                return false;
            }
            return true;
        }
        /// <summary>
        /// 构建更新语句
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="entity"></param>
        /// <param name="EntityType"></param>
        /// <param name="en"></param>
        /// <returns></returns>
        public StatusResult prepareUpdate(SQLBuilder builder, object entity, Type EntityType, EntityInfo en = null)
        {
            if (en == null)
            {
                en = builder.DBLive.client.EntityCash.getEntityInfo(EntityType);
            }
            if (en.Updatable == false)
                return new StatusResult(false, "实体类未标记为可更新！");
            if (string.IsNullOrWhiteSpace(en.DbTableName))
            {
                return new StatusResult(false, "实体类未标记对应的数据库表！");
            }

            builder.setTable(parseTableName(en,entity));
            bool gotWhere = false;
            foreach (var col in en.Columns)
            {
                if (col.IsIgnore || col.IsOnlyIgnoreUpdate) continue;
                //未设置或者基础表字段才进行更新
                if ((col.Kind== FieldKind.Base || col.Kind== FieldKind.None)==false) continue;
                //配置为忽略时，则忽略执行
                if (this.ignoreUpdateFields.Count > 0 && this.ignoreUpdateFields.Contains(col.PropertyName))
                {
                    continue;
                }
                //配置了包含时，则只操作包含的字段
                if (this.includeUpdateFields.Count > 0 && this.includeUpdateFields.Contains(col.PropertyName) == false)
                {
                    continue;
                }
                if (CheckEdition(builder.DBLive, col) == false)
                {
                    continue;
                }
                var val = col.PropertyInfo.GetValue(entity);
                if (col.IsPrimarykey)
                {
                    if (val == null)
                    {
                        return new StatusResult(false, "无法更新！要更新的实体属性其where条件字段值为空！");
                    }
                    builder.where(col.DbColumnName, val);
                    gotWhere = true;
                    continue;
                }

                builder.set(col.DbColumnName, val);
            }
            if (gotWhere == false && builder.ConditionCount == 0)
            {
                return new StatusResult(false, "无法更新！未找到主键或者where条件未定义！");
            }

            return new StatusResult(true, "");
        }

        public  void setPKWhere(SQLBuilder builder, object entity, EntityInfo en)
        {
            bool gotWhere = false;
            var pks = en.GetPK();
            foreach (var col in pks)
            {
                var val = col.PropertyInfo.GetValue(entity);
                if (val == null)
                {
                    throw new Exception("设置主键失败！要操作的实体属性其where条件字段值为空！");
                }
                builder.where(col.DbColumnName, val);
            }
            if (gotWhere == false && builder.ConditionCount == 0)
            {
                throw new Exception("设置主键失败！未找到主键或者where条件未定义！");
            }
        }

        public void setPKWhere(SQLBuilder builder, IEnumerable<object> entitys, EntityInfo en)
        {
            bool gotWhere = false;
            var pks = en.GetPK();
            if (pks.Count == 1)
            {
                var pkValues = new List<object>();
                var prop = pks[0].PropertyInfo;
                foreach (var row in entitys)
                {
                    var val = prop.GetValue(row);
                    if (val != null)
                    {
                        pkValues.Add(val);
                    }

                }
                builder.whereIn(pks[0].DbColumnName, pkValues);
            }
            else if (pks.Count > 1) {
                //联合主键，需要用or处理
                builder.sinkOR();
                foreach (var row in entitys) {
                    builder.sink();
                    foreach (var col in pks) {

                        var val = col.PropertyInfo.GetValue(row);
                        if (val != null)
                        {
                            builder.where(col.DbColumnName, val);
                        }
                    }
                    builder.rise();
                }
                builder.rise();
            
            }

            if (gotWhere == false && builder.ConditionCount == 0)
            {
                throw new Exception("设置主键失败！未找到主键或者where条件未定义！");
            }
        }

        /// <summary>
        /// 构建删除语句
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <param name="entity"></param>
        public void prepareDelete<T>(SQLBuilder builder, T entity)
        {
            var en = builder.DBLive.client.EntityCash.getEntityInfo(typeof(T));
            builder.setTable(parseTableName(en,entity));
            setPKWhere(builder, entity, en);

        }
        /// <summary>
        /// 构建删除语句
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="entity"></param>
        /// <param name="type"></param>
        public void prepareDelete(SQLBuilder builder, object entity, Type type)
        {
            var en = builder.DBLive.client.EntityCash.getEntityInfo(type);
            builder.setTable(parseTableName(en, entity));
            setPKWhere(builder, entity, en);

        }

        public void prepareDelete(SQLBuilder builder, EntityInfo en, IEnumerable ids) {

            var pks = en.GetPK();
            if (pks.Count != 1)
            {
                throw new NotSupportedException("当前实体的主键信息不匹配！");
            }
            var pk = pks[0];
            builder.setTable(en.DbTableName)
                .whereIn(pk.DbColumnName, ids);
        }


        /// <summary>
        /// 执行更新
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <param name="entity"></param>
        /// <param name="updateKey"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public int updateByFieild<T>(SQLBuilder builder, T entity, string updateKey)
        {
            var en = builder.DBLive.client.EntityCash.getEntityInfo(typeof(T));
            builder.setTable(parseTableName(en,entity));
            bool gotWhere = false;
            foreach (var col in en.Columns)
            {
                if (col.IsIgnore || col.IsOnlyIgnoreUpdate) continue;
                //未设置或者基础表字段才进行更新
                if ((col.Kind == FieldKind.Base || col.Kind == FieldKind.None) == false) continue;
                var val = col.PropertyInfo.GetValue(entity);
                if (col.PropertyName == updateKey)
                {
                    if (val == null)
                    {
                        throw new Exception("无法更新！要更新的实体属性其where条件字段值为空！");
                    }
                    builder.where(col.DbColumnName, val);
                    gotWhere = true;
                    continue;
                }
                //配置为忽略时，则忽略执行
                if (this.ignoreUpdateFields.Count > 0 && this.ignoreUpdateFields.Contains(col.PropertyName))
                {
                    continue;
                }
                //配置了包含时，则只操作包含的字段
                if (this.includeUpdateFields.Count > 0 && this.includeUpdateFields.Contains(col.PropertyName) == false)
                {
                    continue;
                }
                if (CheckEdition(builder.DBLive, col) == false)
                {
                    continue;
                }
                builder.set(col.DbColumnName, val);
            }
            if (gotWhere == false && builder.ConditionCount == 0)
            {
                throw new Exception("无法更新！未找到指定的实体属性或者where条件未定义！");
            }
            return builder.doUpdate();
        }

        /// <summary>
        /// 执行from部分的构建
        /// </summary>
        /// <param name="kit"></param>
        /// <param name="en"></param>
        private void BuildFromInner(SQLBuilder kit, EntityInfo en)
        {
            if (this._onBuildFromPart != null)
            {
                this._onBuildFromPart(kit, en, this);
                return;
            }
            if (this._onBuildFromTable != null)
            {
                this._onBuildFromTable(kit, en, this);
                //接着进行join构建
            }
            else if (string.IsNullOrWhiteSpace(en.Alias))
            {
                //直接使用表名，无别名
                kit.from(parseTableName(en));
            }
            else
            {
                kit.from(string.Format("{0} as {1}", parseTableName(en), en.Alias));
            }
            if (en.Joins != null) { 
                //构建join
                foreach (var jo in en.Joins)
                {
                    var joinStr = BuildJoinType(jo.Type);
                    var t = new StringBuilder();
                    t.Append(joinStr);
                    t.Append(" ");
                    t.Append(jo.To);
                    t.Append(" ");
                    if (!string.IsNullOrWhiteSpace(jo.As))
                    {
                        t.Append(" as ");
                        t.Append(jo.As);
                    }
                    if (!string.IsNullOrWhiteSpace(jo.OnA) && !string.IsNullOrWhiteSpace(jo.OnB))
                    {
                        t.Append(" on ");
                        t.Append(jo.OnA);
                        t.Append("=");
                        t.Append(jo.OnB);
                    }
                    else if (!string.IsNullOrWhiteSpace(jo.On))
                    {
                        t.Append(" on ");
                        t.Append(jo.On);
                    }
                    if (t.Length > 0)
                        kit.join(t.ToString());
                }            
            }

        }
        /// <summary>
        /// 创建实体的基础select和from部分
        /// </summary>
        /// <param name="kit"></param>
        /// <param name="en"></param>
        /// <returns></returns>
        public SQLBuilder BuildSelectFrom(SQLBuilder kit, EntityInfo en)
        {
            var exp = kit.Dialect.expression;
            if (en.DType == DBTableType.Table)
            {
                BuildFromInner(kit, en);
                return kit;
            }
            if (en.DType == DBTableType.Select)
            {
                //复合查询表，则需要获取所有的字段，并构建基础from 

                var nick = en.Alias;
                if (string.IsNullOrWhiteSpace(nick))
                {
                    nick = en.DbTableName;
                }
                nick = exp.wrapTable(nick);

                foreach (var field in en.Columns)
                {
                    //
                    if (CheckEdition(kit.DBLive, field) == false)
                    {
                        continue;
                    }
                    var fAlias = field.Alias;
                    if (!string.IsNullOrWhiteSpace(fAlias)) { 
                        fAlias=exp.wrapField(fAlias);
                    }
                    if (field.Kind == model.FieldKind.Base)
                    {
                        var fie = string.Format("{0}.{1}", nick,exp.wrapField(field.DbColumnName));
                        if (!string.IsNullOrWhiteSpace(fAlias))
                        {
                            fie = string.Format("{0} as {1}", fie, fAlias);
                        }
                        kit.select(fie);
                    }
                    else if (field.Kind == model.FieldKind.Join)
                    {
                        
                        //如果字段为空，则忽略该字段
                        if (string.IsNullOrWhiteSpace(field.SrcField))
                        {
                            continue;
                        }
                        var fie = exp.wrapField(field.SrcField);
                        if (!string.IsNullOrWhiteSpace(field.SrcTable))
                        {
                            fie = string.Format("{0}.{1}", field.SrcTable, fie);
                        }

                        if (!string.IsNullOrWhiteSpace(fAlias))
                            fie = string.Format("{0} as {1}", fie, fAlias);
                        kit.select(fie);
                    }
                    else if (field.Kind == model.FieldKind.Free)
                    {
                        var fie = field.FreeSQL;
                        if (!string.IsNullOrWhiteSpace(fAlias))
                            fie = string.Format("{0} as {1}", fie, fAlias);
                        kit.select(fie);
                    }
                }

                //构建from
                BuildFromInner(kit, en);


                return kit;

            }

            if (en.DType == DBTableType.View)
            {
                kit.from(en.DbTableName);
                return kit;

            }

            return kit;
        }

        public SQLBuilder BuildFromPart(SQLBuilder kit, EntityInfo en)
        {
            var exp = kit.Dialect.expression;
            if (en.DType == DBTableType.Table)
            {
                BuildFromInner(kit, en);
                return kit;
            }
            if (en.DType == DBTableType.Select)
            {
                //构建from
                BuildFromInner(kit, en);
                return kit;

            }

            if (en.DType == DBTableType.View)
            {
                kit.from(en.DbTableName);
                return kit;

            }

            return kit;
        }

        private string BuildJoinType(model.JoinKind joinType)
        {
            switch (joinType)
            {
                case model.JoinKind.Right:
                    return "right join";
                case model.JoinKind.RightApply:
                    return "right apply";
                case model.JoinKind.Inner:
                    return "inner join";
                case model.JoinKind.Left:
                    return "left join";
                case model.JoinKind.Full:
                    return "full join";
                case model.JoinKind.FullApply:
                    return "full apply";
                case model.JoinKind.Cross:
                    return "cross join";
                case model.JoinKind.CrossApply:
                    return "cross apply";
                default:
                    return "join";
            }
        }
        /// <summary>
        /// 根据查询参数，构建SQL语句
        /// </summary>
        /// <param name="para"></param>
        /// <param name="kit"></param>
        /// <param name="en"></param>
        public void PatchSQLByQueryPara(QueryPara para, SQLBuilder kit, EntityInfo en)
        {
            BuildSelectFrom(kit, en);
            BeforeBuildWhere(kit, en, QueryAction.QueryList);
            if (para.onBuildingSQL != null)
            {
                para.onBuildingSQL(kit);
            }

            if (para.pageSize != null && para.pageNum != null)
            {
                kit.setPage(para.pageSize.Value, para.pageNum.Value);
            }
            //放入条件部分
            this.BuildQueryCondition(en, kit, para);

            //处理排序部分
            var usedOrders = new List<string>();
            if (para.orderBy != null)
            {

                //对参数进行排序
                para.orderBy = para.orderBy.OrderBy(x => x.idx).ToList();

                foreach (var ob in para.orderBy)
                {
                    var field = this.MatchQueryField(en, ob.field, kit);
                    if (field == null)
                    {
                        continue;
                    }
                    usedOrders.Add(field);
                    if (string.IsNullOrWhiteSpace(ob.order))
                    {
                        //默认升序排序
                        kit.orderBy(field + " ASC ");
                        continue;
                    }
                    var des = ob.order.Trim().ToLower();
                    if (des == "desc")
                    {
                        kit.orderBy(field + " DESC ");
                        continue;
                    }
                    kit.orderBy(field + " ASC ");
                    continue;
                }


            }
            //除了参数排序外，还需对实体中定义的排序字段进行排序
            if (en.OrderBy != null)
            {
                foreach (var ob in en.OrderBy)
                {
                    var fie = ob.Field;
                    if (string.IsNullOrWhiteSpace(fie))
                    {
                        continue;
                    }
                    if (!string.IsNullOrWhiteSpace(ob.Nick))
                    {
                        fie = ob.Nick + "." + fie;
                    }
                    //如果已经使用了该字段，则不再重复使用
                    if (usedOrders.Contains(fie))
                    {
                        continue;
                    }
                    if (ob.OType == OrderType.None)
                    {
                        continue;
                    }
                    else if (ob.OType == OrderType.ASC)
                    {
                        kit.orderBy(fie + " ASC ");
                    }
                    else if (ob.OType == OrderType.DESC)
                    {
                        kit.orderBy(fie + " DESC ");
                    }
                }
            }
        }

        /// <summary>
        /// 构建查询条件部分，支持简单条件和in条件。不支持复杂条件（如：a=b and c>d）
        /// </summary>
        /// <param name="en"></param>
        /// <param name="kit"></param>
        /// <param name="para"></param>
        public void BuildQueryCondition(EntityInfo en, SQLBuilder kit, QueryPara para)
        {
            if (para.conditions == null)
            {
                return;
            }
            //放入条件部分

            var mtb = en.Alias;
            if (string.IsNullOrWhiteSpace(mtb))
            {
                mtb = parseTableName(en);
            }

            if (para.suckWheres != null)
            {
                foreach (var wh in para.suckWheres)
                {
                    //此处由服务端定义，信赖其可靠性，不做强校验
                    if (wh.Sink != null)
                    {
                        this.PatchSink(wh.Sink.Value, kit);
                    }
                    //写入条件

                    this.PatchEnWhere(wh, en, kit);

                    //处理右开关符
                    if (wh.Rise == true)
                    {
                        kit.rise();
                    }
                }
            }

            if (para.conditions != null)
            {
                foreach (var cond in para.conditions)
                {
                    //处理左开关符
                    if (cond.sink != null && Enum.IsDefined(typeof(SinkType), cond.sink.Value))
                    {
                        var st = (SinkType)cond.sink;
                        PatchSink(st, kit);
                    }

                    //特殊处理，字段名必须在查询实体的字段中，否则忽略
                    this.PatchConditionWhere(cond, en, kit);

                    //处理右开关符
                    if (cond.rise == true)
                    {
                        kit.rise();
                    }
                }
            }


        }

        public void PatchEnWhere(EntityWhere wh, EntityInfo en, SQLBuilder kit)
        {
            //自由WHERE
            if (wh.IsFree == true && wh.OnBuildFree != null)
            {
                var sql = wh.OnBuildFree(kit);
                if (!string.IsNullOrWhiteSpace(sql))
                {
                    kit.where(sql);
                    return;
                }
            }
            //函数取值
            if (wh.IsFuncVal == true)
            {
                var val = wh.OnLoadValue();
                if (val != null)
                {
                    var op = wh.Op;
                    if (!string.IsNullOrWhiteSpace(op))
                    {
                        kit.where(wh.Field, val, op);
                        return;
                    }
                    kit.where(wh.Field, val);
                }
                return;
            }
            //普通取值 固定值
            //特殊处理，没有操作符时，如果字段内容不为空，则直接当作条件使用。否则，必须同时存在操作符、字段、值。
            if (string.IsNullOrWhiteSpace(wh.Op) && !string.IsNullOrWhiteSpace(wh.Field))
            {
                kit.where(wh.Field);
                return;
            }
            if (string.IsNullOrWhiteSpace(wh.Op) || string.IsNullOrWhiteSpace(wh.Field))
            {
                return;
            }
            var fie = wh.Field;
            if (!string.IsNullOrWhiteSpace(wh.Bind))
            {
                fie = wh.Bind + "." + wh.Field;
            }
            kit.where(fie, wh.Value, wh.Op);
        }

        public void PatchSink(SinkType st, SQLBuilder kit)
        {
            switch (st)
            {
                case SinkType.And:
                    kit.sink(); break;
                case SinkType.Or:
                    kit.sinkOR(); break;
                case SinkType.AndNot:
                    kit.sink("AND NOT"); break;
                case SinkType.OrNot:
                    kit.sink("OR NOT"); break;
            }
        }

        public int PatchConditionWhere(QueryCondition cond, EntityInfo en, SQLBuilder kit)
        {
            var cc = 0;
            var simpleOps = new List<string> {
                "=",
                "<>",
                ">",
                "<",
                ">=",
                "<="
            };

            //特殊处理，字段名必须在查询实体的字段中，否则忽略
            if (string.IsNullOrWhiteSpace(cond.op) || cond.value == null || string.IsNullOrWhiteSpace(cond.field))
            {
                return cc;
            }
            var field = MatchQueryField(en, cond.field,kit);
            if (field == null) { return cc; }

            //解析子条件范围参数


            var op = cond.op.Trim().ToLower();

            if (simpleOps.Contains(cond.op))
            {
                kit.where(field, cond.value, cond.op);
                cc++;
            }

            else if (cond.op == "in")
            {
                if (cond.value is string valstr)
                {
                    kit.whereIn(field, Regex.Split(valstr, @"[,;|]"));
                }
                else if (cond.value is IEnumerable<string> vals)
                {
                    kit.whereIn(field, vals);
                }
                else if (cond.value is IEnumerable vals2)
                {
                    kit.whereIn(field, vals2);
                }
                return cc;
            }
            else if (cond.op == "not in")
            {
                if (cond.value is string valstr)
                {
                    kit.whereNotIn(field, Regex.Split(valstr, @"[,;|]"));
                }
                else if (cond.value is IEnumerable<string> vals)
                {
                    kit.whereNotIn(field, vals);
                }
                else if (cond.value is IEnumerable vals2)
                {
                    kit.whereNotIn(field, vals2);
                }
                return cc;
            }
            if (cond.op == "like")
            {
                //只支持字符型值参数
                if (cond.value is string valstr)
                {
                    kit.whereLike(field, valstr);
                }
                return cc;
            }
            if (cond.op == "not like")
            {
                if (cond.value is string valstr)
                {
                    kit.whereNotLike(field, valstr);
                }
                return cc;
            }
            if (cond.op == "likeLeft")
            {
                //只支持字符型值参数
                if (cond.value is string valstr)
                {
                    kit.whereLikeLeft(field, valstr);
                }
                return cc;
            }

            return cc;
        }

        public string MatchQueryField(EntityInfo en, string field,SQLBuilder kit)
        {
            var mtb = en.Alias;
            if (string.IsNullOrWhiteSpace(mtb))
            {
                mtb = parseTableName(en);
            }
            foreach (var x in en.Columns)
            {
                if (CheckEdition(kit.DBLive, x) == false)
                {
                    continue;
                }
                if (x.Kind == model.FieldKind.Base)
                {
                    if (x.DbColumnName == field || x.Alias == field)
                    {
                        return string.Format("{0}.{1}", mtb, x.DbColumnName);
                    }
                }
                else if (x.Kind == model.FieldKind.Join)
                {
                    if (x.Alias == field)
                    {
                        if (string.IsNullOrWhiteSpace(x.SrcTable))
                        {
                            return x.SrcField;
                        }
                        return string.Format("{0}.{1}", x.SrcTable, x.SrcField);
                    }
                }
                else if (x.Kind == model.FieldKind.Free && x.Alias == field)
                {
                    return x.FreeSQL;
                }

            }
            return null;
        }
    }
}
