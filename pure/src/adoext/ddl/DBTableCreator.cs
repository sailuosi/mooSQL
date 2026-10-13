using mooSQL.data.model;
using mooSQL.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 创建表的帮助类，直接根据注解，创建表
    /// </summary>
    public class DBTableCreator
    {

        /// <summary>
        /// 属性 DBLive（DBInstance）。
        /// </summary>
        public DBInstance DBLive {  get; set; }

        /// <summary>
        /// 属性 CreateMode（string）。
        /// </summary>
        public string CreateMode { get; set; }

        /// <summary>
        /// 实体落到 DDL 时是否严格保留标识符大小写（经方言 wrap 加引号）。
        /// null 未定义时回退 <see cref="DataBase.StrictCase"/>，最终默认 false。
        /// </summary>
        public bool? StrictCase { get; set; }

        /// <summary>
        /// 自动建表模式，如果表不存在则创建，如果存在则新增字段
        /// </summary>
        /// <returns></returns>
        public DBTableCreator autoMode() {
            this.CreateMode = "createAuto";
            EnsureProgress();
            this.WorkingProgress.Clear();
            return this;
        }

        public DBTableCreator preview()
        {
            this.CreateMode = "preview";
            EnsureProgress();
            this.WorkingProgress.Clear();
            return this;
        }

        /// <summary>
        /// 设置实体→DDL 标识符严格大小写。
        /// </summary>
        public DBTableCreator strictCase(bool enable = true)
        {
            StrictCase = enable;
            return this;
        }

        /// <summary>
        /// 解析生效的 StrictCase：本层 → 连接配置 → false。
        /// </summary>
        public bool ResolveStrictCase() => StrictCase ?? DBLive?.config?.StrictCase ?? false;

        /// <summary>
        /// 实体标识符落入 DDLBuilder 前的命名：严格时按方言加引号，否则原样。
        /// </summary>
        string LandIdent(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || !ResolveStrictCase())
                return name;
            return DBLive.dialect.expression.wrapField(name);
        }

        /// <summary>
        /// 属性 WorkingProgress（StringBuilder）。
        /// </summary>
        public StringBuilder WorkingProgress { get; set; }

        /// <summary>
        /// 累计过程结果（preview 下为建表/加列 SQL；执行模式下为状态文案）。
        /// </summary>
        public string Result => WorkingProgress?.ToString() ?? string.Empty;

        /// <summary>
        /// 泛型方法 createTable（返回 DBTableCreator）。
        /// </summary>
        public DBTableCreator createTable<T>(string mode = null) { 
            if(mode == null) {
                mode = this.CreateMode;
            }
            EnsureProgress();
            var en = DBLive.client.EntityCash.getEntityInfo<T>();
            var res = CreateTable(en, mode);
            this.WorkingProgress.Append(res);
            return this;
        }

        /// <summary>
        /// 按指定物理表名创建分表（结构同实体定义）。
        /// </summary>
        public DBTableCreator createTable<T>(string physicalTableName, string mode = null)
        {
            if (mode == null)
                mode = this.CreateMode;
            EnsureProgress();
            var en = DBLive.client.EntityCash.getEntityInfo<T>();
            var res = CreateTable(en, physicalTableName, mode);
            this.WorkingProgress.Append(res);
            return this;
        }

        private int defaultFieldLength = 50;
        /// <summary>
        /// 清空配置，恢复到初始化状态
        /// </summary>
        /// <returns></returns>
        public DBTableCreator clear() {
            this.CreateMode = "createAuto";
            EnsureProgress();
            this.WorkingProgress.Clear();
            this.defaultFieldLength = 50;
            this.StrictCase = null;
            return this;
        }

        void EnsureProgress()
        {
            if (WorkingProgress == null)
                WorkingProgress = new StringBuilder();
        }

        /// <summary>
        /// 创建表
        /// </summary>
        /// <param name="en"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public string CreateTable(EntityInfo en, string mode)
        {
            return CreateTable(en, en.DbTableName, mode);
        }

        /// <summary>
        /// 创建Table。
        /// </summary>
        public string CreateTable(EntityInfo en, string physicalTableName, string mode)
        {
            var ddl = DBLive.useDDL();

            var tbname = physicalTableName ?? en.DbTableName;
            if (string.IsNullOrWhiteSpace(tbname))
            {
                throw new Exception("未知表名");
            }

            // preview 不访问真实库，始终按建表脚本生成
            var isPreview = string.Equals(mode, "preview", StringComparison.OrdinalIgnoreCase);
            var isExist = false;
            if (!isPreview)
            {
                // 元数据探测用物理名（不加引号）
                isExist = ddl.hasTable(tbname);
                if (isExist)
                {
                    return string.Format("", en.EntityName);
                }
            }

            if (isExist == false)
            {
                // StrictCase 仅在实体→DDLBuilder 落点加引号；DDLBuilder 本身原样拼接
                ddl.setTable(LandIdent(tbname), en.TableDescription);

                foreach (var field in en.Columns)
                {
                    if (string.IsNullOrWhiteSpace(field.DbColumnName)) continue;
                    // None 与 Base 同为可落库列（与 EntityTranslator 一致）
                    if (field.Kind == FieldKind.Base || field.Kind == FieldKind.None) {
                        setDDLField(field, ddl);
                    }
                }

                if (isPreview)
                {
                    var sql = ddl.toCreateTable();
                    var txt = sql.toRawSQL(DBLive.dialect.expression.paraPrefix);
                    return txt;
                }
                else if (mode == "create" || mode == "createAuto")
                {
                    var cc = ddl.doCreateTable();
                    return string.Format("初始化{0}成功；",en.EntityName);
                }
            }
            else
            {
                var exFields = ddl.loadColumns(tbname);

                var exFNames = exFields.map(x => x.Name);
                ddl.setTable(LandIdent(tbname), en.TableDescription);

                foreach (var field in en.Columns)
                {
                    if (string.IsNullOrWhiteSpace(field.DbColumnName)) continue;
                    if (exFNames.Contains(field.DbColumnName))
                    {
                        continue;
                    }
                    if (field.Kind == FieldKind.Base || field.Kind == FieldKind.None)
                    {
                        setDDLField(field, ddl);
                    }
                }

                if (mode == "preview")
                {
                    var sql = ddl.toAddColumn();
                    var txt = sql.toRawSQL(DBLive.dialect.expression.paraPrefix);
                    return txt;
                }
                else if (mode == "create" || mode == "createAuto")
                {
                    var cc = ddl.doAddColumn();
                    return string.Format("初始化{0}成功；", en.EntityName);
                }

            }

            return string.Format("{0}未执行处理；", en.EntityName);
        }
        private void setDDLField(EntityColumn field, DDLBuilder ddl) {
            var dt = transDtype(field);
            if ((dt.DataType == DataFam.VarChar || dt.DataType ==DataFam.NVarChar) && (dt.Length == 0|| dt.Length == 50)) {
                dt.Length = this.defaultFieldLength; 
            }
            ddl.set(LandIdent(field.DbColumnName), dt, field.ColumnDescription, field.IsNullable, field.DefaultValue, field.IsPrimarykey);
            if (!string.IsNullOrWhiteSpace(field.ColumnDescription)) { 
                
            }
        }

        private DbDataType transDtype(EntityColumn field)
        {
            if (field.DbType != DbDataType.Undefined && field.DbType.DataType != DataFam.Undefined) { 
                return field.DbType;
            }
            if (field.DataType == DataFam.Undefined) {
                var valType = field.PropertyInfo.PropertyType.UnwrapNullable();
                if (valType == typeof(string)) {
                    if (field.Length == 0 || field.Length == null) {
                        field.Length = this.defaultFieldLength;
                    }
                    return new DbDataType(DataFam.VarChar, field.Length);
                }
                else if (valType == typeof(Guid))
                {
                    return new DbDataType(DataFam.Guid);
                }
                else if (valType == typeof(DateTime))
                {
                    return new DbDataType(DataFam.DateTime);
                }
                else if (valType == typeof(bool))
                {
                    return new DbDataType(DataFam.Boolean);
                }
                else if (valType == typeof(int))
                {
                    return new DbDataType(DataFam.Int32);
                }
                else if (valType == typeof(long))
                {
                    return new DbDataType(DataFam.Long);
                }
                else if (valType == typeof(short))
                {
                    return new DbDataType(DataFam.Int16);
                }
                else if (valType == typeof(byte[]))
                {
                    return new DbDataType(DataFam.Binary);
                }
                else if (valType == typeof(double)|| valType == typeof(decimal) || valType == typeof(float))
                {
                    return new DbDataType(DataFam.Decimal, field.Length, field.Precision, field.Scale);
                }
                if (field.Length == 0)
                {
                    field.Length = this.defaultFieldLength;
                }
                return getDefaultField(field);
            }
            switch (field.DataType)
            {
                case DataFam.Char:
                case DataFam.NChar:
                    return new DbDataType(field.DataType, field.Length);
                case DataFam.VarChar:
                case DataFam.NVarChar:
                    return new DbDataType(field.DataType, field.Length);
                case DataFam.Decimal:
                case DataFam.VarNumeric:
                case DataFam.Money:
                    return new DbDataType(field.DataType, field.Length, field.Precision, field.Scale);
                case DataFam.Vector:
                    return new DbDataType(field.DataType, field.Length > 0 ? field.Length : 1536);
                case DataFam.Text:
                case DataFam.LongText:
                case DataFam.NText:
                case DataFam.Xml:
                case DataFam.Json:
                case DataFam.BinaryJson:
                case DataFam.Binary:
                case DataFam.VarBinary:
                case DataFam.Blob:
                case DataFam.Image:
                case DataFam.DateTime:
                case DataFam.DateTime2:
                case DataFam.SmallDateTime:
                case DataFam.DateTimeOffset:
                case DataFam.Date:
                case DataFam.Time:
                case DataFam.Long:
                case DataFam.Int16:
                case DataFam.Int32:
                case DataFam.Int64:
                case DataFam.Byte:
                case DataFam.Boolean:
                case DataFam.Guid:
                case DataFam.Double:
                case DataFam.Single:
                    return new DbDataType(field.DataType);
            }

            return getDefaultField(field);
        }
        private DbDataType getDefaultField(EntityColumn field) {
            if (field.Length == 0)
            {
                field.Length = this.defaultFieldLength;
            }
            return new DbDataType(DataFam.VarChar, field.Length);

        }
    }
}
