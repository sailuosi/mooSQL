// 基础功能说明：


using mooSQL.data.model;
using mooSQL.utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{ /// <summary>
  /// 数据库的语句方言集合，主要是数据库查询方面。基础DDL功能在DDLBuilder下。
  /// </summary>
    public abstract partial class SQLSentence
    {
        /// <summary>
        /// 根方言实例
        /// </summary>
        public Dialect dialect;

        public SQLSentence(Dialect parent)
        {
            this.dialect = parent;
        }

        public DBInstance DBLive
        {
            get
            {
                return dialect.dbInstance;
            }
        }

        public SQLExpression WordBuilder
        {
            get {
                return dialect.expression;
            }
        }
        /// <summary>
        /// 获取DDL语句编织器
        /// </summary>
        /// <returns></returns>
        public DDLBuilder useDDL() {
            return DBLive.useDDL();
        }
        /// <summary>
        /// 获取数据库中一张表的所有列信息。columnName columnType columnLen columnDesc
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="builder">注入一个别名为t的列子查询</param>
        /// <returns></returns>

        public virtual SQLBuilder getTableColumns(string tableName, SQLBuilder builder)
        {
            /*
            case MSSQL:
            sql = "SELECT column_name as FName, data_type as FType, CHARACTER_MAXIMUM_LENGTH as FLen , column_name as FDesc FROM information_schema.columns where table_name='"
                    + tableName + "'";
            break;
            case Oracle:
            case KingBaseR3:
            case KingBaseR6:
            case DM:
            sql = "SELECT COLUMN_NAME as FName,DATA_TYPE as FType,DATA_LENGTH as FLen,COLUMN_NAME as FDesc FROM all_tab_columns WHERE table_name = upper('"
                    + tableName + "')";
            break;
            case MySQL:
            sql = "SELECT COLUMN_NAME FName,DATA_TYPE FType,CHARACTER_MAXIMUM_LENGTH FLen,COLUMN_COMMENT FDesc FROM information_schema.columns WHERE table_name='"
                    + tableName + "' and TABLE_SCHEMA='" + SystemConfig.getAppCenterDBDatabase() + "'";
            break;

            default:
            break;
            */
            return null;
        }

        public virtual string GetReserveSequenceValuesSql(int count, string sequenceName)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// 获取数据库列表
        /// </summary>
        /// <returns></returns>
        public virtual List<string> GetDataBaseList()
        {
            var dt = this.DBLive.ExeQuery(GetDataBaseSql);
            return dt.getFieldValues("NAME");
        }
        /// <summary>
        /// 获取数据库的表列表
        /// </summary>
        /// <returns></returns>
        public virtual List<DbTableInfo> GetDbTableList()
        {
            var dt = this.DBLive.ExeQuery<DbTableInfo>(GetTableInfoListSql);
            return dt.ToList();
        }
        /// <summary>
        /// 获取数据库的视图列表
        /// </summary>
        /// <returns></returns>
        public virtual List<DbTableInfo> GetDbViewList()
        {
            var dt = this.DBLive.ExeQuery<DbTableInfo>(GetViewInfoListSql);
            return dt.ToList();
        }
        /// <summary>
        /// 获取表的字段列表
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public virtual List<DbColumnInfo> GetDbColumnsByTableName(string tableName)
        {
            var sql = string.Format(GetColumnInfosByTableNameSql, tableName);
            var res = this.DBLive.ExeQuery<DbColumnInfo>(sql);
            var tar= res.ToList();
            foreach (var it in tar) {
                if (!string.IsNullOrWhiteSpace(it.DbTypeText)) { 
                    it.FieldType = this.dialect.mapping.GetDataType(it.DbTypeText);
                }
            }
            return tar;
        }
        public virtual bool CreateDatabase(string DatabaseName, string databaseDirectory = null)
        {
            bool result = false;
            if (databaseDirectory != null && Directory.Exists(databaseDirectory))
            {
                Directory.CreateDirectory(databaseDirectory);
            }

            if (!GetDataBaseList().Any((string it) => it.Equals(DatabaseName, StringComparison.CurrentCultureIgnoreCase)))
            {
                var sql = GetCreateDataBaseSQL(DatabaseName, databaseDirectory);
                DBLive.ExeNonQuery(sql);
                result = true;
            }
            return result;
        }
        protected virtual SQLCmd GetCreateDataBaseSQL(string DatabaseName, string databaseDirectory = null)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// 查询数据库是否有某个表
        /// </summary>
        /// <param name="TableName"></param>
        /// <returns></returns>
        public virtual SQLCmd buildHasTable(string TableName) {
            return null;
        }

        public virtual List<TableInfo> GetTables(GetSchemaOptions options) {
            return null;
        }
        public virtual IReadOnlyCollection<PrimaryKeyInfo> GetPrimaryKeys(IEnumerable<TableSchema> tables, GetSchemaOptions options) {
            return null;
        }
        public virtual List<ColumnInfo> GetColumns(GetSchemaOptions options) {
            return null;
        }
        public virtual IReadOnlyCollection<ForeignKeyInfo> GetForeignKeys(IEnumerable<TableSchema> tables, GetSchemaOptions options) {
            return null;
        }

        public virtual List<ProcedureInfo>? GetProcedures(GetSchemaOptions options) => null;
        public virtual List<ProcedureParameterInfo>? GetProcedureParameters(IEnumerable<ProcedureInfo> procedures, GetSchemaOptions options) => null;
        /// <summary>
        /// 从数据库中读取版本信息。
        /// </summary>
        /// <returns></returns>
        public virtual DBVersion LoadDBVersion()
        {
            return null;
        }
    }
}
