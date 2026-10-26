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
        /// 自动建表模式：表不存在则创建；表已存在则跳过（保持兼容，不做增量）。
        /// 需要缺列补齐请用 <see cref="upsertMode"/>；需要强制改列请用 <see cref="coverMode"/>。
        /// </summary>
        public DBTableCreator autoMode() {
            this.CreateMode = "createAuto";
            EnsureProgress();
            this.WorkingProgress.Clear();
            return this;
        }

        /// <summary>
        /// Upsert 模式：表不存在则创建；表已存在则只补齐缺失列，已有列跳过。
        /// </summary>
        public DBTableCreator upsertMode()
        {
            this.CreateMode = "upsert";
            EnsureProgress();
            this.WorkingProgress.Clear();
            return this;
        }

        /// <summary>
        /// Cover 模式：表不存在则创建；表已存在则补齐缺失列，并对已有列执行 ALTER 对齐实体定义。
        /// </summary>
        public DBTableCreator coverMode()
        {
            this.CreateMode = "cover";
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
        /// 实体标识符落入 DDLBuilder 前：经方言 <see cref="SQLExpression.normalizeName"/>（连接 NameCase/NameQuote ⊕ 方言默认）。
        /// </summary>
        string LandIdent(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return name;
            return DBLive.dialect.expression.normalizeName(name);
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
        public DBTableCreator createTableByMode<T>(string mode = null) { 
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
        /// 无参版本
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public DBTableCreator createTable<T>()
        {
            EnsureProgress();
            var en = DBLive.client.EntityCash.getEntityInfo<T>();
            var res = CreateTable(en, this.CreateMode);
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

            mode = string.IsNullOrWhiteSpace(mode) ? (CreateMode ?? "createAuto") : mode;
            var isPreview = string.Equals(mode, "preview", StringComparison.OrdinalIgnoreCase);
            var isUpsert = string.Equals(mode, "upsert", StringComparison.OrdinalIgnoreCase);
            var isCover = string.Equals(mode, "cover", StringComparison.OrdinalIgnoreCase);
            var isCreateOnly = string.Equals(mode, "create", StringComparison.OrdinalIgnoreCase)
                || string.Equals(mode, "createAuto", StringComparison.OrdinalIgnoreCase);

            // preview 不访问真实库，始终按建表脚本生成
            var isExist = false;
            if (!isPreview)
            {
                // 元数据探测用物理名（不加引号）
                isExist = ddl.hasTable(tbname);
                // autoMode / create：表已存在则跳过（兼容旧行为）
                if (isExist && isCreateOnly)
                {
                    return string.Format("", en.EntityName);
                }
            }

            if (!isExist)
            {
                // NameCase/NameQuote 在实体→DDLBuilder 落点规范化；DDLBuilder 本身原样拼接
                ddl.setTable(LandIdent(tbname), en.TableDescription);

                foreach (var field in en.Columns)
                {
                    if (string.IsNullOrWhiteSpace(field.DbColumnName)) continue;
                    // None 与 Base 同为可落库列（与 EntityTranslator 一致）
                    if (field.Kind == FieldKind.Base || field.Kind == FieldKind.None)
                    {
                        setDDLField(field, ddl);
                    }
                }

                if (isPreview)
                {
                    var sql = ddl.toCreateTable();
                    return sql.toRawSQL(DBLive.dialect.expression.paraPrefix);
                }
                if (isCreateOnly || isUpsert || isCover)
                {
                    ddl.doCreateTable();
                    return string.Format("初始化{0}成功；", en.EntityName);
                }
            }
            else if (isUpsert || isCover)
            {
                return SyncExistingTable(ddl, en, tbname, isCover, isPreview: false);
            }

            return string.Format("{0}未执行处理；", en.EntityName);
        }

        /// <summary>
        /// 表已存在时：补齐缺失列；cover 模式下再 ALTER 已有列。
        /// </summary>
        string SyncExistingTable(DDLBuilder ddl, EntityInfo en, string tbname, bool alterExisting, bool isPreview)
        {
            var exFields = ddl.loadColumns(tbname) ?? new List<DbColumnInfo>();
            var exNames = new HashSet<string>(
                exFields.Where(x => !string.IsNullOrWhiteSpace(x?.Name)).Select(x => x.Name),
                StringComparer.OrdinalIgnoreCase);

            var toAdd = new List<EntityColumn>();
            var toAlter = new List<EntityColumn>();
            foreach (var field in en.Columns)
            {
                if (string.IsNullOrWhiteSpace(field.DbColumnName)) continue;
                if (field.Kind != FieldKind.Base && field.Kind != FieldKind.None) continue;

                if (!exNames.Contains(field.DbColumnName))
                {
                    toAdd.Add(field);
                }
                else if (alterExisting)
                {
                    toAlter.Add(field);
                }
            }

            var progress = new StringBuilder();
            var previewSql = new StringBuilder();

            if (toAdd.Count > 0)
            {
                ddl.clear().setTable(LandIdent(tbname), en.TableDescription);
                foreach (var field in toAdd)
                {
                    setDDLField(field, ddl);
                }
                if (isPreview)
                {
                    previewSql.Append(ddl.toAddColumn().toRawSQL(DBLive.dialect.expression.paraPrefix));
                }
                else
                {
                    ddl.doAddColumn();
                    progress.AppendFormat("补齐{0}列；", toAdd.Count);
                }
            }

            if (toAlter.Count > 0)
            {
                ddl.clear().setTable(LandIdent(tbname), en.TableDescription);
                foreach (var field in toAlter)
                {
                    setDDLField(field, ddl);
                }
                if (isPreview)
                {
                    if (previewSql.Length > 0) previewSql.AppendLine();
                    previewSql.Append(ddl.toAlterColumn().toRawSQL(DBLive.dialect.expression.paraPrefix));
                }
                else
                {
                    ddl.doAlterColumn();
                    progress.AppendFormat("更新{0}列；", toAlter.Count);
                }
            }

            if (isPreview)
            {
                return previewSql.Length > 0 ? previewSql.ToString() : string.Empty;
            }
            if (progress.Length == 0)
            {
                return string.Format("表{0}已对齐，无需变更；", en.EntityName);
            }
            return string.Format("更新{0}成功；{1}", en.EntityName, progress);
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
            if (field.DbType != DbDataType.Undefined && field.DbType.DataType != DataFam.Undefined)
            {
                return field.DbType;
            }

            // 实体已标注 DataFam：按字段声明构造尺度
            if (field.DataType != DataFam.Undefined)
            {
                return transDtypeFromDataFam(field);
            }

            // CLR → 方言 MappingPanel（优先）；失败则走本地保底
            Type valType = null;
            if (field.PropertyInfo != null)
            {
                valType = field.PropertyInfo.PropertyType.UnwrapNullable();
                var fromDialect = tryMapFromDialect(valType, field);
                if (fromDialect.DataType != DataFam.Undefined)
                {
                    return fromDialect;
                }
            }

            return transDtypeFallback(field, valType);
        }

        /// <summary>
        /// 优先使用方言 <c>mapping.GetDbDataType</c>，并补齐实体上的 Length/Precision/Scale。
        /// </summary>
        DbDataType tryMapFromDialect(Type valType, EntityColumn field)
        {
            var mapping = DBLive?.dialect?.mapping;
            if (mapping == null)
            {
                return DbDataType.Undefined;
            }

            try
            {
                if (!mapping.SharpToDataType.TryGet(valType, out var mapped)
                    || mapped.DataType == DataFam.Undefined)
                {
                    return DbDataType.Undefined;
                }

                return enrichMappedType(mapped, valType, field);
            }
            catch
            {
                return DbDataType.Undefined;
            }
        }

        /// <summary>
        /// 将 MappingPanel 得到的类型与实体列上的长度/精度合并。
        /// </summary>
        DbDataType enrichMappedType(DbDataType mapped, Type valType, EntityColumn field)
        {
            var fam = mapped.DataType;
            switch (fam)
            {
                case DataFam.Char:
                case DataFam.NChar:
                case DataFam.VarChar:
                case DataFam.NVarChar:
                {
                    var mappedLen = mapped.Length.GetValueOrDefault();
                    var len = field.Length > 0
                        ? field.Length
                        : (mappedLen > 0 ? mappedLen : this.defaultFieldLength);
                    if (field.Length == 0)
                    {
                        field.Length = len;
                    }
                    return new DbDataType(valType, fam, mapped.DbType, len);
                }
                case DataFam.Decimal:
                case DataFam.VarNumeric:
                case DataFam.Money:
                    return new DbDataType(valType, fam, mapped.DbType,
                        field.Length > 0 ? field.Length : mapped.Length,
                        field.Precision > 0 ? field.Precision : mapped.Precision,
                        field.Scale != 0 ? field.Scale : mapped.Scale);
                case DataFam.Vector:
                {
                    var mappedLen = mapped.Length.GetValueOrDefault();
                    var len = field.Length > 0
                        ? field.Length
                        : (mappedLen > 0 ? mappedLen : 1536);
                    return new DbDataType(valType, fam, mapped.DbType, len);
                }
                default:
                    if (!string.IsNullOrEmpty(mapped.DbType)
                        || mapped.Length.GetValueOrDefault() > 0
                        || mapped.Precision.GetValueOrDefault() > 0)
                    {
                        return new DbDataType(valType, fam, mapped.DbType, mapped.Length, mapped.Precision, mapped.Scale);
                    }
                    return new DbDataType(valType, fam);
            }
        }

        /// <summary>
        /// 实体列已声明 <see cref="EntityColumn.DataType"/> 时的尺度构造。
        /// </summary>
        DbDataType transDtypeFromDataFam(EntityColumn field)
        {
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

        /// <summary>
        /// 方言未命中时的本地 CLR→DbDataType 保底（原 transDtype 逻辑）。
        /// </summary>
        DbDataType transDtypeFallback(EntityColumn field, Type? valType)
        {
            if (valType == null && field.PropertyInfo != null)
            {
                valType = field.PropertyInfo.PropertyType.UnwrapNullable();
            }
            if (valType == null)
            {
                return getDefaultField(field);
            }

            if (valType == typeof(string))
            {
                if (field.Length == 0)
                {
                    field.Length = this.defaultFieldLength;
                }
                return new DbDataType(DataFam.VarChar, field.Length);
            }
            if (valType == typeof(Guid))
            {
                return new DbDataType(DataFam.Guid);
            }
            if (valType == typeof(DateTime))
            {
                return new DbDataType(DataFam.DateTime);
            }
            if (valType == typeof(bool))
            {
                return new DbDataType(DataFam.Boolean);
            }
            if (valType == typeof(int))
            {
                return new DbDataType(DataFam.Int32);
            }
            if (valType == typeof(long))
            {
                return new DbDataType(DataFam.Long);
            }
            if (valType == typeof(short))
            {
                return new DbDataType(DataFam.Int16);
            }
            if (valType == typeof(byte[]))
            {
                return new DbDataType(DataFam.Binary);
            }
            if (valType == typeof(double) || valType == typeof(decimal) || valType == typeof(float))
            {
                return new DbDataType(DataFam.Decimal, field.Length, field.Precision, field.Scale);
            }

            var enumFam = EnumColumnMapping.TryMapClrTypeToDataFam(valType);
            if (enumFam != DataFam.Undefined)
            {
                return new DbDataType(enumFam);
            }

            if (field.Length == 0)
            {
                field.Length = this.defaultFieldLength;
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
