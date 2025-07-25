using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using mooSQL.data.model;
using mooSQL.data.utils;

namespace mooSQL.data
{
    /// <summary>
    /// 实体转译
    /// </summary>
    public class EntityTranslator
    {

        public StatusResult prepareInsert(SQLBuilder builder, object entity, Type EntityType)
        {
            var en = builder.DBLive.client.EntityCash.getEntityInfo(EntityType);
            if (en.Insertable == false)
            {
                return new StatusResult(false, "实体类未标记为可插入！");
            }

            builder.setTable(en.DbTableName);
            foreach (var col in en.Columns)
            {
                if (col.IsIgnore || col.IsOnlyIgnoreInsert || col.PropertyInfo == null) continue;

                builder.set(col.DbColumnName, col.PropertyInfo.GetValue(entity));
            }
            return new StatusResult(true, "");
        }

        public StatusResult prepareUpdate(SQLBuilder builder, object entity, Type EntityType)
        {
            var en = builder.DBLive.client.EntityCash.getEntityInfo(EntityType);
            if (en.Updatable == false)
                return new StatusResult(false, "实体类未标记为可更新！");
            if (string.IsNullOrWhiteSpace(en.DbTableName))
            {
                return new StatusResult(false, "实体类未标记对应的数据库表！");
            }

            builder.setTable(en.DbTableName);
            bool gotWhere = false;
            foreach (var col in en.Columns)
            {
                if (col.IsIgnore || col.IsOnlyIgnoreUpdate) continue;
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
        public void prepareDelete<T>(SQLBuilder builder, T entity)
        {
            var en = builder.DBLive.client.EntityCash.getEntityInfo(typeof(T));
            builder.setTable(en.DbTableName);
            setPKWhere(builder, entity, en);

        }

        public void prepareDelete(SQLBuilder builder, object entity, Type type)
        {
            var en = builder.DBLive.client.EntityCash.getEntityInfo(type);
            builder.setTable(en.DbTableName);
            setPKWhere(builder, entity, en);

        }



        public int updateByFieild<T>(SQLBuilder builder, T entity, string updateKey)
        {
            var en = builder.DBLive.client.EntityCash.getEntityInfo(typeof(T));
            builder.setTable(en.DbTableName);
            bool gotWhere = false;
            foreach (var col in en.Columns)
            {
                if (col.IsIgnore || col.IsOnlyIgnoreUpdate) continue;
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
                builder.set(col.DbColumnName, val);
            }
            if (gotWhere == false && builder.ConditionCount == 0)
            {
                throw new Exception("无法更新！未找到指定的实体属性或者where条件未定义！");
            }
            return builder.doUpdate();
        }


        public SQLBuilder BuildSelectFrom(SQLBuilder kit, EntityInfo en)
        {
            if (en.DType == DBTableType.Table)
            {
                if (string.IsNullOrWhiteSpace(en.Alias))
                {
                    //直接使用表名，无别名
                    kit.from(en.DbTableName);
                }
                else
                {
                    kit.from(string.Format("{0} as {1}", en.DbTableName, en.Alias));
                }
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

                foreach (var field in en.Columns)
                {
                    //
                    if (field.Kind == model.FieldKind.Base)
                    {
                        var fie = string.Format("{0}.{1}", nick, field.DbColumnName);
                        if (!string.IsNullOrWhiteSpace(field.Alias))
                        {
                            fie = string.Format("{0} as {1}", fie, field.Alias);
                        }
                        kit.select(fie);
                    }
                    else if (field.Kind == model.FieldKind.Join)
                    {
                        var fie = field.SrcField;
                        //如果字段为空，则忽略该字段
                        if (string.IsNullOrWhiteSpace(fie))
                        {
                            continue;
                        }
                        if (!string.IsNullOrWhiteSpace(field.SrcTable))
                        {
                            fie = string.Format("{0}.{1}", field.SrcTable, field.SrcField);
                        }

                        if (!string.IsNullOrWhiteSpace(field.Alias))
                            fie = string.Format("{0} as {1}", fie, field.Alias);
                        kit.select(fie);
                    }
                    else if (field.Kind == model.FieldKind.Free)
                    {
                        var fie = field.FreeSQL;
                        if (!string.IsNullOrWhiteSpace(field.Alias))
                            fie = string.Format("{0} as {1}", fie, field.Alias);
                        kit.select(fie);
                    }
                }

                //构建from
                if (string.IsNullOrWhiteSpace(en.Alias))
                {
                    //直接使用表名，无别名
                    kit.from(en.DbTableName);
                }
                else
                {
                    kit.from(string.Format("{0} as {1}", en.DbTableName, en.Alias));
                }
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
                    var field = this.MatchQueryField(en, ob.field);
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
                mtb = en.DbTableName;
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
            var field = MatchQueryField(en, cond.field);
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

        public string MatchQueryField(EntityInfo en, string field)
        {
            var mtb = en.Alias;
            if (string.IsNullOrWhiteSpace(mtb))
            {
                mtb = en.DbTableName;
            }
            foreach (var x in en.Columns)
            {
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
