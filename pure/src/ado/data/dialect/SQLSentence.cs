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

        /// <summary>
        /// 获得table的主键
        /// </summary>
        /// <param name="table">表名称</param>
        /// <returns>主键名称、没有返回为空.</returns>
        public virtual string GetTablePKName(string table)
        {
            return string.Empty;
            //Paras ps = new Paras();
            //string sql = "";
            //switch (AppCenterDBType)
            //{
            //    case DBType.Access:
            //        return null;
            //    case DBType.Oracle:
            //    case DBType.KingBaseR3:
            //    case DBType.KingBaseR6:
            //    case DBType.KingBaseR8:
            //    case DBType.DM:
            //    case DBType.GBASE8CByOracle:
            //        sql = "SELECT constraint_name, constraint_type,search_condition, r_constraint_name  from user_constraints WHERE table_name = upper(:tab) AND constraint_type = 'P'";
            //        ps.Add("Tab", table);
            //        break;
            //    case DBType.MySQL:
            //        sql = "SELECT CONSTRAINT_NAME , column_name, table_name CONSTRAINT_NAME from INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE table_name =@Tab and table_schema='" + BP.Difference.SystemConfig.AppCenterDBDatabase + "' ";
            //        ps.Add("Tab", table);
            //        break;
            //    case DBType.Informix:
            //        sql = "SELECT * FROM sysconstraints c inner join systables t on c.tabid = t.tabid where t.tabname = lower(?) and constrtype = 'P'";
            //        ps.Add("Tab", table);
            //        break;
            //    case DBType.PostgreSQL:
            //    case DBType.UX:
            //    case DBType.HGDB:
            //        sql = " SELECT ";
            //        sql += " pg_constraint.conname AS pk_name ";
            //        sql += " FROM ";
            //        sql += " pg_constraint ";
            //        sql += " INNER JOIN pg_class ON pg_constraint.conrelid = pg_class.oid ";
            //        sql += " WHERE ";
            //        sql += " pg_class.relname =:Tab ";
            //        sql += " AND pg_constraint.contype = 'p' ";
            //        ps.Add("Tab", table.ToLower());
            //        break;
            //    default:
            //        throw new Exception("@GetTablePKName没有判断的数据库类型.");
            //}

            //DataTable dt = DBAccess.RunSQLReturnTable(sql, ps);
            //if (dt.Rows.Count == 0)
            //    return null;
            //return dt.Rows[0][0].ToString();
        }

        /// <summary>
        /// 是否是view
        /// </summary>
        /// <param name="tabelOrViewName"></param>
        /// <returns></returns>
        public virtual bool? IsView(string tabelOrViewName, string dbName=null)
        {
            return null;

            //string sql = "";
            //switch (dbType)
            //{
            //    case DBType.Oracle:
            //    case DBType.KingBaseR3:
            //    case DBType.KingBaseR6:
            //    case DBType.KingBaseR8:
            //    case DBType.GBASE8CByOracle:
            //        sql = "Select count(*) as nm From user_objects Where object_type='VIEW' and object_name=:v";
            //        DataTable Oracledt = DBAccess.RunSQLReturnTable(sql, "v", tabelOrViewName.ToUpper());
            //        if (Oracledt.Rows[0]["nm"].ToString().Equals("1"))
            //            return true;
            //        else
            //            return false;
            //    case DBType.DM:
            //        sql = "SELECT VIEW_NAME FROM USER_VIEWS WHERE VIEW_NAME=:v";
            //        DataTable oradt = DBAccess.RunSQLReturnTable(sql, "v", tabelOrViewName.ToUpper());
            //        if (oradt.Rows.Count == 0)
            //            return false;

            //        if (oradt.Rows[0][0].ToString().ToUpper().Trim().Equals("V"))
            //            return true;
            //        else
            //            return false;

            //    case DBType.MSSQL:
            //        sql = "select xtype from sysobjects WHERE name =" + BP.Difference.SystemConfig.AppCenterDBVarStr + "v";
            //        DataTable dt1 = DBAccess.RunSQLReturnTable(sql, "v", tabelOrViewName);
            //        if (dt1.Rows.Count == 0)
            //            return false;

            //        if (dt1.Rows[0][0].ToString().ToUpper().Trim().Equals("V") == true)
            //            return true;
            //        else
            //            return false;
            //    case DBType.PostgreSQL:
            //        sql = "select relkind from pg_class WHERE relname ='" + tabelOrViewName.ToLower() + "'";
            //        DataTable dt4 = DBAccess.RunSQLReturnTable(sql);
            //        if (dt4.Rows.Count == 0)
            //            return false;

            //        //如果是个表.
            //        if (dt4.Rows[0][0].ToString().ToLower().Trim().Equals("r") == true)
            //            return false;
            //        else
            //            return true;
            //    case DBType.UX:
            //    case DBType.HGDB:
            //        sql = "select relkind from pg_class WHERE relname ='" + tabelOrViewName + "'";
            //        DataTable dt3 = DBAccess.RunSQLReturnTable(sql);
            //        if (dt3.Rows.Count == 0)
            //            return false;

            //        //如果是个表.
            //        if (dt3.Rows[0][0].ToString().ToLower().Trim().Equals("r") == true)
            //            return false;
            //        else
            //            return true;
            //    case DBType.Informix:
            //        sql = "select tabtype from systables where tabname = '" + tabelOrViewName.ToLower() + "'";
            //        DataTable dtaa = DBAccess.RunSQLReturnTable(sql);
            //        if (dtaa.Rows.Count == 0)
            //            throw new Exception("@表不存在[" + tabelOrViewName + "]");

            //        if (dtaa.Rows[0][0].ToString().ToUpper().Trim().Equals("V"))
            //            return true;
            //        else
            //            return false;
            //    case DBType.MySQL:
            //        sql = "SELECT Table_Type FROM information_schema.TABLES WHERE table_name='" + tabelOrViewName + "' and table_schema='" + BP.Difference.SystemConfig.AppCenterDBDatabase + "'";
            //        DataTable dt2 = DBAccess.RunSQLReturnTable(sql);
            //        if (dt2.Rows.Count == 0)
            //            return false;

            //        if (dt2.Rows[0][0].ToString().ToUpper().Trim().Equals("VIEW"))
            //            return true;
            //        else
            //            return false;
            //    case DBType.Access:
            //        sql = "select   Type   from   msysobjects  WHERE  UCASE(name)='" + tabelOrViewName.ToUpper() + "'";
            //        DataTable dtw = DBAccess.RunSQLReturnTable(sql);
            //        if (dtw.Rows.Count == 0)
            //            return false;

            //        if (dtw.Rows[0][0].ToString().Trim().Equals("5"))
            //            return true;
            //        else
            //            return false;
            //    default:
            //        throw new Exception("@没有做的判断。");
            //}

        }

        /// <summary>
        /// 表中是否存在指定的列
        /// </summary>
        /// <param name="table">表名</param>
        /// <param name="col">列名</param>
        /// <returns>是否存在</returns>
        public virtual bool? IsExitsTableCol(string table, string col)
        {
            return null;
            //Paras ps = new Paras();
            //ps.Add("tab", table);
            //ps.Add("col", col);

            //int i = 0;
            //switch (DBAccess.AppCenterDBType)
            //{
            //    case DBType.MSSQL:
            //        i = DBAccess.RunSQLReturnValInt("SELECT COUNT(*) FROM information_schema.COLUMNS  WHERE TABLE_NAME='" + table + "' AND COLUMN_NAME='" + col + "'", 0);
            //        break;
            //    case DBType.MySQL:
            //        string sql = "select count(*) FROM information_schema.columns WHERE TABLE_SCHEMA='" + BP.Difference.SystemConfig.AppCenterDBDatabase + "' AND table_name ='" + table + "' and column_Name='" + col + "'";
            //        i = DBAccess.RunSQLReturnValInt(sql);
            //        break;
            //    case DBType.PostgreSQL:
            //    case DBType.UX:
            //    case DBType.HGDB:
            //        string sql1 = "select count(*) from information_schema.columns where   table_name ='" + table.ToLower() + "' and  column_name='" + col.ToLower() + "'";
            //        i = DBAccess.RunSQLReturnValInt(sql1);
            //        break;
            //    case DBType.Oracle:
            //    case DBType.KingBaseR3:
            //    case DBType.KingBaseR6:
            //    case DBType.KingBaseR8:
            //    case DBType.DM:
            //    case DBType.GBASE8CByOracle:
            //        if (table.IndexOf(".") != -1)
            //            table = table.Split('.')[1];
            //        i = DBAccess.RunSQLReturnValInt("SELECT COUNT(*) from user_tab_columns  WHERE table_name= upper(:tab) AND column_name= upper(:col) ", ps);
            //        break;
            //    //case DBType.Informix:
            //    //    i = DBAccess.RunSQLReturnValInt("select count(*) from syscolumns c where tabid in (select tabid	from systables	where tabname = lower('" + table + "')) and c.colname = lower('" + col + "')", 0);
            //    //    break;
            //    //case DBType.Access:
            //    //    return false;
            //    //    break;
            //    default:
            //        throw new Exception("err@IsExitsTableCol没有判断的数据库类型.");
            //}

            //if (i == 1)
            //    return true;
            //else
            //    return false;
        }
        /// <summary>
        ///是否存在索引？
        /// </summary>
        /// <param name="table">表名称</param>
        /// <param name="indexName">索引名称</param>
        /// <returns>是否存在索引？</returns>
        public virtual bool IsExitsTableIndex(string table, string indexName)
        {
            string sql = "";

            int i = 0;
            //switch (DBAccess.AppCenterDBType)
            //{
            //    case DBType.MSSQL:
            //        i = DBAccess.RunSQLReturnValInt("SELECT COUNT(*) FROM sys.indexes  WHERE object_id=OBJECT_ID('" + table + "', N'U') and NAME='" + indexName + "'", 0);
            //        break;
            //    case DBType.MySQL:
            //        sql = "SELECT count(*) FROM information_schema.statistics WHERE table_schema='" + BP.Difference.SystemConfig.AppCenterDBDatabase + "' AND table_name = '" + table + "' AND index_name = '" + indexName + "' ";
            //        i = DBAccess.RunSQLReturnValInt(sql);
            //        break;
            //    case DBType.PostgreSQL:
            //    case DBType.UX:
            //    case DBType.HGDB:
            //        sql = "SELECT count(*) FROM pg_indexes WHERE  tablename = '" + table.ToLower() + "' and indexname = '" + indexName.ToLower() + "'";
            //        //string sql1 = "select count(*) from information_schema.statistics where   table_name ='" + table.ToLower() + "' and  index_name='" + indexName.ToLower() + "'";
            //        i = DBAccess.RunSQLReturnValInt(sql);

            //        break;
            //    case DBType.DM:
            //        if (table.IndexOf(".") != -1)
            //            table = table.Split('.')[1];
            //        i = DBAccess.RunSQLReturnValInt("SELECT COUNT(*) from user_indexes   WHERE table_name= upper('" + table + "') ");
            //        break;
            //    case DBType.Oracle:
            //    case DBType.KingBaseR3:
            //    case DBType.KingBaseR6:
            //    case DBType.KingBaseR8:
            //    case DBType.GBASE8CByOracle:
            //        if (table.IndexOf(".") != -1)
            //            table = table.Split('.')[1];
            //        i = DBAccess.RunSQLReturnValInt("SELECT COUNT(*) from user_indexes   WHERE table_name= upper('" + table + "') ");
            //        break;

            //    default:
            //        throw new Exception("err@IsExitsTableCol没有判断的数据库类型.");
            //}

            return i >= 1;
        }
    }
}
