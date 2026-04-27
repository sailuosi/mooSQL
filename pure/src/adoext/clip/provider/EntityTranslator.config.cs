using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    public partial class EntityTranslator
    {
        /// <summary>
        /// 清空配置
        /// </summary>
        /// <returns></returns>
        public EntityTranslator clear()
        {
            this._onParseTableName = null;
            this._onBuildFromTable = null;
            this.onBuildWherePart = null;
            this._onBuildFromPart = null;
            this._onBeforeInsertEntity = null;
            this._onBeforeUpdateEntity = null;
            this._onBeforeDeleteEntity = null;
            this._onReadyInsertEntity = null;
            this._onReadyUpdateEntity = null;
            this._onReadyDeleteEntity = null;
            this._BeforeBuildQueryCondition = null;

            this.ignoreInsertFields.Clear();
            this.ignoreUpdateFields.Clear();
            this.includeUpdateFields.Clear();
            this.ignoreInsertFields.Clear();
            return this;
        }
        /// <summary>
        /// 注册插入实体前的处理事件
        /// </summary>
        /// <param name="act"></param>
        /// <returns></returns>
        public EntityTranslator OnBeforeInsertEntity(Action<SQLBuilder, object, Type, EntityInfo> act)
        {
            this._onBeforeInsertEntity += act;
            return this;
        }
        /// <summary>
        /// 字段值插入时刻
        /// </summary>
        /// <param name="act"></param>
        /// <returns></returns>
        public EntityTranslator OnInsertField(Func<SQLBuilder,string, object,  EntityInfo,bool> act)
        {
            this._onInsertField += act;
            return this;
        }
        /// <summary>
        /// 字段值的更新时刻
        /// </summary>
        /// <param name="act"></param>
        /// <returns></returns>
        public EntityTranslator OnUpdateField(Func<SQLBuilder, string, object, EntityInfo, bool> act)
        {
            this._onUpdateField += act;
            return this;
        }

        /// <summary>
        /// 注册插入实体后的处理事件
        /// </summary>
        /// <param name="act"></param>
        /// <returns></returns>
        public EntityTranslator OnReadyInsertEntity(Action<SQLBuilder, object, Type, EntityInfo> act){
            this._onReadyInsertEntity += act;
            return this;
        }
        /// <summary>
        /// 注册更新实体前的处理事件
        /// </summary>
        /// <param name="act"></param>
        /// <returns></returns>
        public EntityTranslator OnBeforeUpdateEntity(Action<SQLBuilder, object, Type, EntityInfo> act){
            this._onBeforeUpdateEntity += act;
            return this;
        }
        /// <summary>
        /// 注册更新实体后的处理事件
        /// </summary>
        /// <param name="act"></param>
        /// <returns></returns>
        public EntityTranslator OnReadyUpdateEntity(Action<SQLBuilder, object, Type, EntityInfo> act){
            this._onReadyUpdateEntity += act;
            return this;
        }
        /// <summary>
        /// 注册删除实体前的处理事件
        /// </summary>
        /// <param name="act"></param>
        /// <returns></returns>
        public EntityTranslator OnBeforeDeleteEntity(Action<SQLBuilder, object, Type, EntityInfo> act){
            this._onBeforeDeleteEntity += act;
            return this;
        }
        /// <summary>
        /// 注册删除实体后的处理事件
        /// </summary>
        /// <param name="act"></param>
        /// <returns></returns>
        public EntityTranslator OnReadyDeleteEntity(Action<SQLBuilder, object, Type, EntityInfo> act){
            this._onReadyDeleteEntity += act;
            return this;
        }

        /// <summary>
        /// 注册查询条件构建前的处理事件
        /// </summary>
        /// <param name="act"></param>
        /// <returns></returns>
        public EntityTranslator OnBeforeBuildQueryCondition(Action<QueryCondition, EntityInfo, SQLBuilder> act){
            this._BeforeBuildQueryCondition += act;
            return this;
        }


        /// <summary>
        /// 设置更新的表名
        /// </summary>
        /// <param name="onParseTableName"></param>
        /// <returns></returns>
        public EntityTranslator setTable(Func<EntityInfo, string> onParseTableName)
        {
            _onParseTableName = onParseTableName;
            return this;
        }
        /// <summary>
        /// 设置更新时要包含的字段
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        public EntityTranslator includeUpdate(IEnumerable<string> fields) {
            foreach (var field in fields) {
                if (string.IsNullOrEmpty(field)) continue;
                var f=field.Trim();
                if (includeUpdateFields.Contains(f)) {
                    continue;
                }
                includeUpdateFields.Add(f);
            }
            return this;
        }
        /// <summary>
        /// 包含的更新字段
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        public EntityTranslator includeUpdate(params string[] fields)
        {
            return includeUpdate(fields);
        }
        /// <summary>
        /// 设置更新时要忽略的字段
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        public EntityTranslator ignoreUpdate(IEnumerable<string> fields)
        {
            foreach (var field in fields)
            {
                if (string.IsNullOrEmpty(field)) continue;
                var f = field.Trim();
                if (ignoreUpdateFields.Contains(f))
                {
                    continue;
                }
                ignoreUpdateFields.Add(f);
            }
            return this;
        }
        /// <summary>
        /// 忽略的更新字段
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        public EntityTranslator ignoreUpdate(params string[] fields)
        {
            return includeUpdate(fields);
        }

        /// <summary>
        /// 设置插入时要包含的字段
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        public EntityTranslator includeInsert(IEnumerable<string> fields)
        {
            foreach (var field in fields)
            {
                if (string.IsNullOrEmpty(field)) continue;
                var f = field.Trim();
                if (includeInsertField.Contains(f))
                {
                    continue;
                }
                includeInsertField.Add(f);
            }
            return this;
        }
        /// <summary>
        /// 包含的插入字段
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        public EntityTranslator includeInsert(params string[] fields)
        {
            return includeUpdate(fields);
        }

        /// <summary>
        /// 设置插入时要忽略的字段
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        public EntityTranslator ignoreInsert(IEnumerable<string> fields)
        {
            foreach (var field in fields)
            {
                if (string.IsNullOrEmpty(field)) continue;
                var f = field.Trim();
                if (ignoreInsertFields.Contains(f))
                {
                    continue;
                }
                ignoreInsertFields.Add(f);
            }
            return this;
        }
        /// <summary>
        /// 忽略的插入字段
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        public EntityTranslator ignoreInsert(params string[] fields)
        {
            return includeUpdate(fields);
        }

        /// <summary>
        /// 注册解析表名事件
        /// </summary>
        /// <param name="act"></param>
        /// <returns></returns>
        public EntityTranslator whenParseTable(Func<EntityInfo, string> act)
        {
            this._onParseTableName = act;
            return this;
        }
        /// <summary>
        /// 注册查询语句的from部分的源表构建逻辑(tableName as a)，不包含Join部分，只能注册一次，后者覆盖前者
        /// </summary>
        /// <param name="act"></param>
        /// <returns></returns>
        public EntityTranslator whenBuildFromTable(Action<SQLBuilder, EntityInfo, EntityTranslator> act)
        {
            this._onBuildFromTable = act;
            return this;
        }
        /// <summary>
        /// 注册查询语句的where部分的构建逻辑，注册多次时累计生效
        /// </summary>
        /// <param name="act"></param>
        /// <returns></returns>
        public EntityTranslator whenBuildWhere(Action<SQLBuilder, EntityInfo, EntityTranslator, QueryAction> act)
        {
            this.onBuildWherePart += act;
            return this;
        }
        /// <summary>
        /// 注册自定义的from部分，将忽略表定义和join定义。只能注册一次。
        /// </summary>
        /// <param name="act"></param>
        /// <returns></returns>
        public EntityTranslator whenBuildFromPart(Action<SQLBuilder, EntityInfo, EntityTranslator> act)
        {
            this._onBuildFromPart = act;
            return this;
        }

        private object loadPKValue(EntityColumn col) {
            if (this._onLoadPKValue != null) {
                return _onLoadPKValue(col);
            }
            return null;
        }
        /// <summary>
        /// 注册主键的加载逻辑
        /// </summary>
        /// <param name="act"></param>
        /// <returns></returns>
        public EntityTranslator OnLoadPKValue(Func<EntityColumn, object> act)
        {
            this._onLoadPKValue = act;
            return this;
        }
    }
}
