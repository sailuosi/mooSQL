using mooSQL.data.builder;
using mooSQL.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// DDL语句的编制功能
    /// </summary>
    public abstract partial class SQLExpression
    {
        /// <summary>
        /// 创建表的语句构建
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        public virtual string buildCreateTable(DDLFragSQL frag) {

            var sb = new StringBuilder();
            sb.Append("CREATE TABLE ");
            sb.Append(frag.Table);
            sb.Append(" ");

            sb.Append("( ");
            //内容定义部分
            var contents= new List<string>();
            //列定义部分
            foreach (var fie in frag.Columns) {
                var fieSQL = this.buildDDLField(fie);
                if (!string.IsNullOrWhiteSpace(fieSQL)) {
                    contents.Add(fieSQL);
                }
                
            }

            //处理约束
            var strick = this.buildConstraint(frag);
            if (!string.IsNullOrWhiteSpace(strick)) {
                contents.Add(strick);
            }
            //处理索引
            var indexSQL = this.buildTableIndex(frag);
            if (!string.IsNullOrWhiteSpace(indexSQL)) { 
                contents.Add(indexSQL);
            }
            //处理表选项
            var optSQL = buildTableOption(frag);
            if (!string.IsNullOrWhiteSpace(optSQL)) { 
                contents.Add(optSQL);
            }
            sb.Append(string.Join(",", contents));
            sb.Append(")");
            
            var t0 = buildDDLSoloCaptions(frag);
            if (!string.IsNullOrWhiteSpace(t0)) {
                sb.Append(t0);
            }
            //处理独立表注释的
            var t1 = buildDDLFieldsCaption(frag);
            if (!string.IsNullOrWhiteSpace(t1)) { 
                sb.Append(t1);
            }
            var t2 = buildCreateTableAfter(frag);
            if (!string.IsNullOrWhiteSpace(t2)) { 
                sb.Append(t2);
            }

            return sb.ToString();
        }
        protected virtual string buildCreateTableAfter(DDLFragSQL frag) { 
            return string.Empty;
        }
        /// <summary>
        /// 设置表的字段
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        protected virtual string buildDDLField(DDLField field) {
            var sb = new StringBuilder();
            sb.Append(field.FieldName);
            sb.Append(" ");
            sb.Append(field.TextType);
            sb.Append(" ");
            //可空性
            if (field.Nullable == false)
            {
                sb.Append("NOT NULL ");
            }
            else { 
                sb.Append("NULL ");
            }
            //默认值
            if (!string.IsNullOrWhiteSpace(field.DefaultValue))
            {
                sb.Append(field.DefaultValue);
                sb.Append(" ");
            }
            sb.Append(this.buildDDLFieldCaption(field));
            return sb.ToString();
        }
        /// <summary>
        /// 设置表字段的注释
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        protected virtual string buildDDLFieldCaption(DDLField field) {
            return string.Empty;
        }
        /// <summary>
        /// 字段循环后的表注释
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        protected virtual string buildDDLFieldsCaption(DDLFragSQL frag) {
            return string.Empty;
        }

        protected string buildDDLSoloCaptions(DDLFragSQL frag)
        {
            var sb = new StringBuilder();
            foreach (var fie in frag.Columns)
            {
                if (!string.IsNullOrWhiteSpace(fie.Caption))
                {
                    sb.Append(this.buildSoloFieldCaption(frag, fie));
                }
            }
            if (!string.IsNullOrWhiteSpace(frag.TableCaption))
            {
                sb.Append(buildSoloTableCaption(frag));
            }
            return sb.ToString();
        }
        public virtual string buildSoloFieldCaption(DDLFragSQL frag, DDLField fild) {
            return string.Empty;
        }
        public virtual string buildSoloTableCaption(DDLFragSQL frag)
        {
            return string.Empty;
        }

        /// <summary>
        /// 构造字段约束SQL，如主键、外键等
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        protected virtual string buildConstraint(DDLFragSQL frag)
        {
            return string.Empty;
        }
        /// <summary>
        /// 获取主键字段
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        protected List<string> getDDLPK(DDLFragSQL frag) {
            var res = new List<string>();
            foreach (var fie in frag.Columns) {
                if (fie.IsPrimary) { 
                    res.Add(fie.FieldName);
                }
            }
            return res;
        }
        /// <summary>
        /// 处理索引
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        protected virtual string buildTableIndex(DDLFragSQL frag)
        {
            var res = new List<string>();
            //创建主键约束
            var pk = getDDLPK(frag);
            if (pk.Count > 0) {
                var pkSQL = this.getFieldToSQL(pk);
                var pkname = string.Format("pk_{0}_{1}", frag.Table, RandomUtils.NextString(5));
                var pksetSQL= buildConstrainPK(pkname, pkSQL);
                if (!string.IsNullOrWhiteSpace(pksetSQL)) { 
                    res.Add(pksetSQL);
                }
            }
            if (frag.Indexes != null) { 
                foreach (var index in frag.Indexes) { 
            
                    var indexSQL = this.buildFieldIndexCreating(index);
                    if (!string.IsNullOrWhiteSpace(indexSQL)) { 
                        res.Add(indexSQL);
                    }
                }            
            }


            return string.Join(",", res) ;
        }
        /// <summary>
        /// 在建表期间的索引部分。此时位置在表语句的括号内部。
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        protected virtual string buildFieldIndexCreating(DDLIndex index) { 
            return string.Empty;
        }

        protected virtual string buildConstrainPK(string pkname,string fields)
        {
            return string.Empty;
        }
        /// <summary>
        /// 表选项部分，以及一些特殊部分
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        protected virtual string buildTableOption(DDLFragSQL frag)
        {
            return string.Empty;
        }

        /// <summary>
        /// 添加表字段SQL
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        public virtual string buildAddColumn(DDLFragSQL frag) {
            var sb = new StringBuilder();
            if (frag.Columns == null || frag.Columns.Count == 0) {
                return string.Empty;
            }
            var res = new List<string>();
            foreach (var col in frag.Columns) {
                var sql = this.buildAddColumn(frag, col);
                if (!string.IsNullOrWhiteSpace(sql)) {
                    res.Add(sql);
                }
            }
            return string.Join(this.SentenceSeprator, res);
        }
        /// <summary>
        /// 创建一个字段的添加SQL
        /// </summary>
        /// <param name="frag"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public virtual string buildAddColumn(DDLFragSQL frag, DDLField field)
        {
            var sb = new StringBuilder();
            sb.Append("ALTER TABLE ")
                .Append(frag.Table)
                .Append(" ADD ");

            var col = this.buildDDLField(field);

            sb.Append(col);
            return sb.ToString();

        }

        /// <summary>
        /// 添加表字段SQL
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        public virtual string buildAlterColumn(DDLFragSQL frag)
        {
            var sb = new StringBuilder();
            if (frag.Columns == null || frag.Columns.Count == 0)
            {
                return string.Empty;
            }
            var res = new List<string>();
            foreach (var col in frag.Columns)
            {
                var sql = this.buildAlterColumn(frag, col);
                if (!string.IsNullOrWhiteSpace(sql))
                {
                    res.Add(sql);
                }
            }
            return string.Join(this.SentenceSeprator, res);
        }
        /// <summary>
        /// 创建一个字段的添加SQL
        /// </summary>
        /// <param name="frag"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public virtual string buildAlterColumn(DDLFragSQL frag, DDLField field)
        {
            var sb = new StringBuilder();
            sb.Append("ALTER TABLE ")
                .Append(frag.Table)
                .Append(" ALTER COLUMN ");
            var col = this.buildDDLField(field);

            sb.Append(col);
            return sb.ToString();

        }

        /// <summary>
        /// 添加表字段SQL
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        public virtual string buildDropColumn(DDLFragSQL frag)
        {
            var sb = new StringBuilder();
            if (frag.Columns == null || frag.Columns.Count == 0)
            {
                return string.Empty;
            }
            var res = new List<string>();
            foreach (var col in frag.Columns)
            {
                var sql = this.buildDropColumn(frag, col);
                if (!string.IsNullOrWhiteSpace(sql))
                {
                    res.Add(sql);
                }
            }
            return string.Join(this.SentenceSeprator, res);
        }
        /// <summary>
        /// 创建一个字段的添加SQL
        /// </summary>
        /// <param name="frag"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public virtual string buildDropColumn(DDLFragSQL frag, DDLField field)
        {
            var sb = new StringBuilder();
            sb.Append("ALTER TABLE ")
                .Append(frag.Table)
                .Append(" DROP COLUMN ");
            var col = this.buildDDLField(field);

            sb.Append(col);
            return sb.ToString();

        }

        /// <summary>
        /// 表删除
        /// </summary>
        /// <returns></returns>
        public virtual string buildDropTable(DDLFragSQL frag)
        {
            return string.Format("DROP TABLE {0}", frag.Table);
        }

        /// <summary>
        /// 构造创建视图的语句
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        public virtual string buildCreateView(DDLFragSQL frag)
        {
            var sb = new StringBuilder();
            sb.Append("CREATE VIEW ")
                .Append(frag.Table)
                .Append(" AS ");
            sb.Append(frag.SelectSQL);
            return sb.ToString();
        }

        /// <summary>
        /// 构造修改视图的语句
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        public virtual string buildAlterView(DDLFragSQL frag)
        {
            var sb = new StringBuilder();
            sb.Append("ALTER VIEW ")
                .Append(frag.Table)
                .Append(" AS ");
            sb.Append(frag.SelectSQL);
            return sb.ToString();
        }
        /// <summary>
        /// 创建视图删除语句
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        public virtual string buildDropView(DDLFragSQL frag)
        {
            return string.Format("DROP VIEW {0}", frag.Table);
        }

        /// <summary>
        /// 复制表
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        public virtual string buildCopyTableSchema(DDLFragSQL frag) {
            return string.Empty;
        }
        /// <summary>
        /// 复制表数据到新表
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        public virtual string buildCopyTable(DDLFragSQL frag)
        {
            return string.Empty;
        }
        /// <summary>
        /// 创建清空重置表
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        public virtual string buildTruncateTable(DDLFragSQL frag) {
            var sb = new StringBuilder();
            sb.Append("TRUNCATE TABLE ")
                .Append(frag.Table);
            return sb.ToString();
        }
        /// <summary>
        /// 创建所有索引
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        public virtual string buildCreateIndex(DDLFragSQL frag)
        {
            if (frag == null || frag.Indexes==null) return string.Empty;
            var res= new List<string>();
            foreach (var index in frag.Indexes) { 
                res.Add(buildCreateIndex(frag, index));
            }
            var tar= string.Join(this.SentenceSeprator, res);
            return tar;
        }
        /// <summary>
        /// 创建单个索引
        /// </summary>
        /// <param name="frag"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public virtual string buildCreateIndex(DDLFragSQL frag,DDLIndex index)
        {
            var sb = new StringBuilder();
            sb.Append("CREATE INDEX ")
                .Append(index.IndexName)
                .Append(" ON ")
                .Append(frag.Table);
            var fieds = this.getFieldToSQL(index.MapedFields);
            //过滤无效构建结果
            if (string.IsNullOrWhiteSpace(fieds)) { 
                return string.Empty;
            }
            sb.Append(" ( ").Append(fieds).Append(" ) ");
            return sb.ToString();
        }

        public virtual string buildDropIndex(string indexName, string tableName = null) {
            return string.Empty;
        }

        protected string getFieldToSQL(List<string> fieldList)
        {
            var fields = new List<string>();
            foreach (var field in fieldList)
            {
                if (string.IsNullOrWhiteSpace(field)) continue;
                var fie = field.Trim();
                fie= this.wrapField(fie);
                if(!fields.Contains(fie)) fields.Add(fie);
            }

            return string.Join(",", fields);

        }

        public virtual string getStringColumnType(int length) {
            return string.Format("VARCHAR({0})", length);
        }
        public virtual string getIntColumnType(int length)
        {
            return "INT";
        }
        public virtual string getDateTimeColumnType(int length)
        {
            return string.Empty;
        }
        public virtual string getNumberColumnType(int precision,int scale)
        {
            return string.Format("NUMERIC({0}, {1})", precision, scale);
        }
        public virtual string getBoolColumnType()
        {
            return string.Empty;
        }
        public virtual string getGuidColumnType()
        {
            return string.Empty;
        }
        /// <summary>
        /// 创建表的自增字段的SQL
        /// </summary>
        /// <returns></returns>
        public virtual string getTableAutoIdSQL() {
            return string.Empty;
        }

        public virtual string CreateDataBaseBy (string database)
        {
            return string.Empty;
        }

        public virtual string CreateIndexBy (string indexName, string tableName, string columnName,string unique)
        {
            return string.Empty;
        }

        public virtual string IsAnyIndexBy (string indexName)
        {
            return string.Empty;
        }

        public virtual string AddColumnToTableBy (string tableName, string columnName, string dataType, string defval, string nullable, string p2, string p3)
        {
            return string.Empty;
        }

        public virtual string AlterColumnToTableby (string tableName, string columnName, string dataType, string defval, string nullable, string p2, string p3)
        {
            return string.Empty;
        }

        public virtual string CreateTableBy (string tableName, string detail)
        {
            return string.Empty;
        }

        public virtual string CreateTableColumnBy (string columnName, string dataType, string defval, string nullable, string p2, string p3)
        {
            return string.Empty;
        }

        //protected abstract string TruncateTableSql ();

        //protected abstract string DropTableSql ();

        public virtual string DropColumnToTableBy (string tableName, string columnName)
        {
            return string.Empty;
        }

        public virtual string DropConstraintBy (string tableName, string constraintName)
        {
            return string.Empty;
        }

        public virtual string AddPrimaryKeyBy (string tableName, string columnName, string indexName)
        {
            return string.Empty;
        }

        public virtual string RenameColumnBy (string tableName, string oldName, string newName)
        {
            return string.Empty;
        }

        public virtual string AddColumnCaptionBy (string tableName, string columnName, string caption)
        {
            return string.Empty;
        }

        public virtual string DeleteColumnCaptionBy (string tableName, string columnName)
        {
            return string.Empty;
        }

        public virtual string IsAnyColumnCaptionBy (string columnName, string tableNam)
        {
            return string.Empty;
        }

        public virtual string AddTableCaptionBy (string tableName, string caption)
        {
            return string.Empty;
        }

        public virtual string DeleteTableCaptionBy (string tableName)
        {
            return string.Empty;
        }

        public virtual string IsAnyTableCaptionBy (string tableName)
        {
            return string.Empty;
        }

        public virtual string RenameTableBy (string oldTableName, string newTableName)
        {
            return string.Empty;
        }

        public virtual string CheckSystemTablePermissionsBy ()
        {
            return string.Empty;
        }

        public virtual string CreateTableNullBy ()
        {
            return string.Empty;
        }

        public virtual string CreateTableNotNullBy ()
        {
            return string.Empty;
        }

        public virtual string CreateTablePirmaryKeyBy ()
        {
            return string.Empty;
        }

    }
}
