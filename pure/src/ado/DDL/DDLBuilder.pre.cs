using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mooSQL.data.builder;
using mooSQL.data.model;
using mooSQL.utils;


namespace mooSQL.data
{
    public partial class DDLBuilder
    {

        string? _targetTable;
        string? _tableCaption;
        /// <summary>
        /// 设置目标表
        /// </summary>
        /// <param name="targetTable"></param>
        /// <returns></returns>
        public DDLBuilder setTable(string targetTable,string caption=null) {
            this._targetTable = targetTable;
            if (!string.IsNullOrEmpty(caption)) {
                this._tableCaption = caption;
            }
            
            return this;
        }

        string? _targetView;
        /// <summary>
        /// 设置目标视图
        /// </summary>
        /// <param name="targetView"></param>
        /// <returns></returns>
        public DDLBuilder setView(string targetView)
        {
            this._targetView = targetView;
            return this;
        }

        List<DDLField> _ddlFields= new List<DDLField>();
        /// <summary>
        /// 添加字段
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="columnType"></param>
        /// <param name="caption"></param>
        /// <param name="nullable"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public DDLBuilder set(string columnName, string columnType,string caption, bool nullable = true, string defaultValue = null) {
            var tar = new DDLField()
            {
                Mode="set",
                FieldName = columnName,
                TextType = columnType,
                Caption = caption,
                Nullable = nullable,
                DefaultValue = defaultValue
            };
            set(tar);
            return this;
        }
        /// <summary>
        /// 添加字段
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="columnType"></param>
        /// <param name="caption"></param>
        /// <param name="nullable"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public DDLBuilder set(string columnName, DbDataType columnType, string caption,bool nullable=true,string defaultValue=null,bool isPK=false)
        {
            var tar = new DDLField()
            {
                Mode = "set",
                FieldName = columnName,
                DbType = columnType,
                Caption = caption,
                Nullable=nullable,
                DefaultValue=defaultValue,
                IsPrimary=isPK
            };
            tar.TextType= this.DBLive.dialect.mapping.DbDataTypeToSQL(columnType);
            set(tar);
            return this;
        }
        public DDLColumnBuilder set(string columnName)
        {
            var tar = new DDLField()
            {
                Mode = "set",
                FieldName = columnName
            };
            var cb = new DDLColumnBuilder(tar, this);
            return cb;
        }
        public DDLBuilder setField(string columnName)
        {
            var tar = new DDLField()
            {
                Mode = "drop",
                FieldName = columnName
            };
            return this;
        }

        internal DDLBuilder set(DDLField field)
        {
            for (int i = 0; i < _ddlFields.Count; i++) { 
                if (_ddlFields[i].FieldName == field.FieldName) {
                    _ddlFields[i] = field;
                    return this;
                }
            }
            _ddlFields.Add(field);
            return this;
        }
        private List<DDLIndex> _ddlIndexes ;
        /// <summary>
        /// 设置索引名和对应的字段
        /// </summary>
        /// <param name="indexName"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        public DDLBuilder setIndex( List<string> fields,string indexName="") {
            if (fields==null || fields.Count==0)
            {
                return this;
            }
            if (string.IsNullOrWhiteSpace(indexName))
            {
                indexName = string.Format("index_{0}_{1}", "fieds"+fields.Count, RandomUtils.NextString(4));
            }
            if (this._ddlIndexes == null) {
                _ddlIndexes = new List<DDLIndex>();
            }
            var t = new DDLIndex()
            {
                IndexName = indexName,
                MapedFields = fields
            };
            _ddlIndexes.Add(t);
            return this;
        }
        /// <summary>
        /// 设置索引名和字段
        /// </summary>
        /// <param name="indexName"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public DDLBuilder setIndex(string field, string indexName="")
        {
            if (string.IsNullOrWhiteSpace(field)) { 
                return this;
            }
            if (string.IsNullOrWhiteSpace(indexName)) {
                indexName = string.Format("index_{0}_{1}", field,RandomUtils.NextString(4));
            }
            if (this._ddlIndexes == null)
            {
                _ddlIndexes = new List<DDLIndex>();
            }
            foreach (var ix in _ddlIndexes) {
                if (ix.IndexName == indexName) {
                    ix.Add(field);
                    return this;
                }
            }
            //此处没找到
            var t = new DDLIndex()
            {
                IndexName = indexName,
                MapedFields = new List<string>() { field}
            };
            _ddlIndexes.Add(t);
            return this;
        }
    }
}
