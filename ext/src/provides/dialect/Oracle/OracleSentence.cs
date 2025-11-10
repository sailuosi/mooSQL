// 基础功能说明：


using mooSQL.data;
using mooSQL.data.model;
using mooSQL.linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data;
public class OracleSentence : SQLSentence
{
    public OracleSentence(Dialect dia) : base(dia) { }

    private string? _currentUser;
    private string? SchemasFilter { get; set; }
    public override SQLBuilder getTableColumns(string tableName, SQLBuilder builder)
    {
        //sql = "SELECT COLUMN_NAME FName,DATA_TYPE FType,CHARACTER_MAXIMUM_LENGTH FLen,COLUMN_COMMENT FDesc FROM information_schema.columns WHERE table_name='
        builder.from("t", (t) =>
        {
            t.select("co.COLUMN_NAME as columnName, co.DATA_TYPE as columnType, co.CHARACTER_MAXIMUM_LENGTH as columnLen,COLUMN_COMMENT as columnDesc ")
                .from("information_schema.columns co")
                .where("table_name", tableName);
        });

        return builder;
    }

    public override string GetReserveSequenceValuesSql(int count, string sequenceName)
    {
#if NET6_0_OR_GREATER
        return FormattableString.Invariant($"SELECT {dialect.clauseTranslator.TranslateValue(sequenceName, ConvertType.SequenceName)}.nextval ID from DUAL connect by level <= {count}");
#else
        return $"SELECT {dialect.clauseTranslator.TranslateValue(sequenceName, ConvertType.SequenceName)}.nextval ID from DUAL connect by level <= {count}";
#endif

    }



    public override string GetDataBaseSql
    {
        get
        {
            throw new NotSupportedException();
        }
    }

    public override string GetColumnInfosByTableNameSql => "select  " +
        " t1.COLUMN_NAME as Name " +
        " ,t1.DATA_TYPE AS DbTypeText " +
        " ,CASE WHEN t1.DATA_TYPE='NUMBER' then NVL(t1.DATA_PRECISION,0) else t1.CHAR_LENGTH end as MaxLength " +
        " ,t1.DATA_SCALE as Scale " +
        " ,case WHEN t1.NULLABLE='Y' then 1 else 0  end as IsNullable " +
        " ,case WHEN t3.keyname is null then 0 else 1 end as IsPrimary " +
        ",t2.COMMENTS as \"Comment\" " +
        " from all_tab_columns  t1 " +
        " LEFT JOIN all_col_comments t2 on t1.Table_Name = t2.table_name and T1.COLUMN_NAME=T2.COLUMN_NAME " +
        "   left join " +
        "(" +
        "  select distinct cu.COLUMN_name KEYNAME  from user_cons_columns cu, user_constraints au  " +
        " where cu.constraint_name = au.constraint_name " +
        " and au.constraint_type = 'P' and au.table_name=upper('{0}') " +
        ") t3  on t3.keyname = t1.COLUMN_NAME " +
        " where t1.table_name=upper('{0}')";
    //user_tables 
    public override string GetTableInfoListSql => "SELECT  table_name name from user_tables where " +
        " table_name!='HELP' " +
        "  AND table_name NOT LIKE '%$%' " +
        "  AND table_name NOT LIKE 'LOGMNRC_%' " +
        "  AND table_name!='LOGMNRP_CTAS_PART_MAP' " +
        " AND table_name!='LOGMNR_LOGMNR_BUILDLOG' " +
        " AND table_name!='SQLPLUS_PRODUCT_PROFILE' ";

    public override string GetViewInfoListSql => "select view_name name  from user_views \r\n                                                WHERE VIEW_name NOT LIKE '%$%'\r\n                                                AND VIEW_NAME !='PRODUCT_PRIVS'\r\n                        AND VIEW_NAME NOT LIKE 'MVIEW_%' ";






    public override bool CreateDatabase(string DatabaseName, string databaseDirectory = null)
    {
        throw new NotImplementedException();
    }

    public override SQLCmd buildHasTable(string TableName)
    {
        var cmd = DBLive.useSQL()
            .from("user_tables")
            .where("table_name", TableName)
            //.where("owner = 'SCHEMA_NAME'")
            .toSelectCount();
        return cmd;
    }
    private void LoadCurrentUser()
    {
        _currentUser ??= DBLive.ExeQueryScalar<string>("select user from dual", null);
    }
    /// <summary>
    /// 获取数据库表信息列表，不包括视图。
    /// </summary>
    /// <returns></returns>
    public override List<DbTableInfo> GetDbTableList()
    {
        var kit = DBLive.useSQL();

        var dt= kit.select("a.table_name as \"Name\", b.comments as \"Comment\",a.owner as \"Schema\"")
            .from("all_tables a ")
            .join("join all_tab_comments b ON a.table_name = b.table_name")
            .where("a.table_name not like '%$%'")
            .orderBy("a.table_name")
            .query<DbTableInfo>();
        return dt.ToList();
    }

    public override List<TableInfo> GetTables(GetSchemaOptions options)
    {
        LoadCurrentUser();
        //new DataParameter("CurrentUser", _currentUser, DataType.VarChar)
        var para = new Paras();
        para.AddByPrefix("CurrentUser", _currentUser,this.DBLive.dialect.expression.paraPrefix);

        return DBLive.ExeQuery<TableInfo>(
            @"
				SELECT
					:CurrentUser || '.' || d.NAME as TableID,
					:CurrentUser                  as SchemaName,
					d.NAME                        as TableName,
					d.IsView                      as IsView,
					1                             as IsDefaultSchema,
					d.COMMENTS                    as Description
				FROM
				(
					SELECT NAME, ISVIEW, CASE c.MatView WHEN 1 THEN mvc.COMMENTS ELSE tc.COMMENTS END AS COMMENTS
					FROM
					(
						SELECT t.TABLE_NAME NAME, 0 as IsView, 0 as MatView FROM USER_TABLES t
							LEFT JOIN USER_MVIEWS tm ON t.TABLE_NAME = tm.CONTAINER_NAME
							WHERE tm.MVIEW_NAME IS NULL
						UNION ALL
						SELECT v.VIEW_NAME NAME, 1 as IsView, 0 as MatView FROM USER_VIEWS v
						UNION ALL
						SELECT m.MVIEW_NAME NAME, 1 as IsView, 1 as MatView FROM USER_MVIEWS m
					) c
						LEFT JOIN USER_TAB_COMMENTS tc ON c.NAME = tc.TABLE_NAME
						LEFT JOIN USER_MVIEW_COMMENTS mvc ON c.NAME = mvc.MVIEW_NAME
				) d
				ORDER BY TableID, isView
				", para
            )
        .ToList();
        
    }

    public override IReadOnlyCollection<PrimaryKeyInfo> GetPrimaryKeys(IEnumerable<TableSchema> tables, GetSchemaOptions options)
    {
        if (SchemasFilter == null)
            return new List<PrimaryKeyInfo>();

        return
            DBLive.ExeQuery<PrimaryKeyInfo>(@"
					SELECT
						FKCOLS.OWNER || '.' || FKCOLS.TABLE_NAME as TableID,
						FKCOLS.CONSTRAINT_NAME                   as PrimaryKeyName,
						FKCOLS.COLUMN_NAME                       as ColumnName,
						FKCOLS.POSITION                          as Ordinal
					FROM
						ALL_CONS_COLUMNS FKCOLS,
						ALL_CONSTRAINTS FKCON
					WHERE
						FKCOLS.OWNER           = FKCON.OWNER AND
						FKCOLS.TABLE_NAME      = FKCON.TABLE_NAME AND
						FKCOLS.CONSTRAINT_NAME = FKCON.CONSTRAINT_NAME AND
						FKCON.CONSTRAINT_TYPE  = 'P' AND
						FKCOLS.OWNER " + SchemasFilter, null)
            .ToList();
    }



    public override List<ColumnInfo> GetColumns(GetSchemaOptions options)
    {

        var isIdentitySql = "0                                              as IsIdentity,";
        if (this.DBLive.config.versionNumber >= 12)
        {
            isIdentitySql = "CASE c.IDENTITY_COLUMN WHEN 'YES' THEN 1 ELSE 0 END as IsIdentity,";
        }

        string sql;


            // This is significally faster
            sql = @"
					SELECT
						(SELECT USER FROM DUAL) || '.' || c.TABLE_NAME as TableID,
						c.COLUMN_NAME                                  as Name,
						c.DATA_TYPE                                    as DataType,
						CASE c.NULLABLE WHEN 'Y' THEN 1 ELSE 0 END     as IsNullable,
						c.COLUMN_ID                                    as Ordinal,
						c.DATA_LENGTH                                  as Length,
						c.CHAR_LENGTH                                  as CharLength,
						c.DATA_PRECISION                               as Precision,
						c.DATA_SCALE                                   as Scale,
						" + isIdentitySql + @"
						cc.COMMENTS                                    as Description
					FROM USER_TAB_COLUMNS c
						JOIN USER_COL_COMMENTS cc ON
							c.TABLE_NAME  = cc.TABLE_NAME AND
							c.COLUMN_NAME = cc.COLUMN_NAME
					";
        

        return DBLive.ExeQuery(sql, null, rd =>
        {
            // IMPORTANT: reader calls must be ordered to support SequentialAccess
            var tableId = rd.GetString(0);
            var name = rd.GetString(1);
            var dataType = rd.IsDBNull(2) ? null : rd.GetString(2);
            var isNullable = rd.GetInt32(3) != 0;
            var ordinal = rd.IsDBNull(4) ? 0 : rd.GetInt32(4);
            var dataLength = rd.IsDBNull(5) ? (int?)null : rd.GetInt32(5);
            var charLength = rd.IsDBNull(6) ? (int?)null : rd.GetInt32(6);

            return new ColumnInfo
            {
                TableID = tableId,
                Name = name,
                DataType = dataType,
                IsNullable = isNullable,
                Ordinal = ordinal,
                Precision = rd.IsDBNull(7) ? (int?)null : rd.GetInt32(7),
                Scale = rd.IsDBNull(8) ? (int?)null : rd.GetInt32(8),
                IsIdentity = rd.GetInt32(9) != 0,
                Description = rd.IsDBNull(10) ? null : rd.GetString(10),
                Length = dataType == "CHAR" || dataType == "NCHAR" || dataType == "NVARCHAR2" || dataType == "VARCHAR2" || dataType == "VARCHAR"
                                ? charLength : dataLength
            };
        }).ToList();
    }

    public override IReadOnlyCollection<ForeignKeyInfo> GetForeignKeys(IEnumerable<TableSchema> tables, GetSchemaOptions options)
    {

            // This is significally faster
        return
            DBLive.ExeQuery<ForeignKeyInfo>(@"
					SELECT
						FKCON.CONSTRAINT_NAME                    as Name,
						FKCON.OWNER || '.' || FKCON.TABLE_NAME   as ThisTableID,
						FKCOLS.COLUMN_NAME                       as ThisColumn,
						PKCOLS.OWNER || '.' || PKCOLS.TABLE_NAME as OtherTableID,
						PKCOLS.COLUMN_NAME                       as OtherColumn,
						FKCOLS.POSITION                          as Ordinal
					FROM
						USER_CONSTRAINTS FKCON
							JOIN USER_CONS_COLUMNS FKCOLS ON
								FKCOLS.CONSTRAINT_NAME = FKCON.CONSTRAINT_NAME
							JOIN USER_CONS_COLUMNS PKCOLS ON
								PKCOLS.CONSTRAINT_NAME = FKCON.R_CONSTRAINT_NAME
					WHERE
						FKCON.CONSTRAINT_TYPE = 'R' AND
						FKCOLS.POSITION       = PKCOLS.POSITION
					ORDER BY Ordinal, Name
					", null)
                .ToList();
        
    }

    public override List<ProcedureInfo>? GetProcedures(GetSchemaOptions options)
    {

        var    sql = @"SELECT
	        USER                                                                                                                                 AS Owner,
	        1                                                                                                                                    AS IsDefault,
	        p.OVERLOAD                                                                                                                           AS Overload,
	        CASE WHEN p.OBJECT_TYPE = 'PACKAGE' THEN p.OBJECT_NAME ELSE NULL END                                                                 AS PackageName,
	        CASE WHEN p.OBJECT_TYPE = 'PACKAGE' THEN p.PROCEDURE_NAME ELSE p.OBJECT_NAME END                                                     AS ProcedureName,
	        CASE WHEN a.DATA_TYPE IS NULL THEN 'PROCEDURE' WHEN a.DATA_TYPE = 'TABLE' THEN 'TABLE_FUNCTION' ELSE 'FUNCTION' END AS ProcedureType
        FROM USER_PROCEDURES p
		        LEFT OUTER JOIN USER_ARGUMENTS a ON
			        ((a.PACKAGE_NAME = p.OBJECT_NAME AND a.OBJECT_NAME = p.PROCEDURE_NAME)
					        OR (a.PACKAGE_NAME IS NULL AND p.PROCEDURE_NAME IS NULL AND a.OBJECT_NAME = p.OBJECT_NAME))
				        AND a.ARGUMENT_NAME IS NULL
				        AND a.DATA_LEVEL = 0
        WHERE ((p.OBJECT_TYPE IN ('PROCEDURE', 'FUNCTION') AND PROCEDURE_NAME IS NULL) OR PROCEDURE_NAME IS NOT NULL)
        ORDER BY
	        CASE WHEN p.OBJECT_TYPE = 'PACKAGE' THEN p.OBJECT_NAME ELSE NULL END,
	        CASE WHEN p.OBJECT_TYPE = 'PACKAGE' THEN p.PROCEDURE_NAME ELSE p.OBJECT_NAME END";
        

        return DBLive.ExeQuery(sql, null, rd =>
        {
            // IMPORTANT: reader calls must be ordered to support SequentialAccess
            var schema = rd.GetString(0);
            var isDefault = rd.GetInt32(1) != 0;
            var overload = rd.IsDBNull(2) ? null : rd.GetString(2);
            var packageName = rd.IsDBNull(3) ? null : rd.GetString(3);
            var procedureName = rd.GetString(4);
            var procedureType = rd.GetString(5);

            return new ProcedureInfo()
            {
                ProcedureID = $"{schema}.{overload}.{packageName}.{procedureName}",
                SchemaName = schema,
                PackageName = packageName,
                ProcedureName = procedureName,
                IsFunction = procedureType != "PROCEDURE",
                IsTableFunction = procedureType == "TABLE_FUNCTION",
                IsDefaultSchema = isDefault
            };
        }).ToList();
    }
    public override List<ProcedureParameterInfo> GetProcedureParameters(IEnumerable<ProcedureInfo> procedures, GetSchemaOptions options)
    {

        var    sql = @"SELECT
	        USER           AS Owner,
	        PACKAGE_NAME   AS PackageName,
	        OBJECT_NAME    AS ProcedureName,
	        OVERLOAD       AS Overload,
	        IN_OUT         AS Direction,
	        DATA_LENGTH    AS DataLength,
	        ARGUMENT_NAME  AS Name,
	        DATA_TYPE      AS Type,
	        POSITION       AS Ordinal,
	        DATA_PRECISION AS Precision,
	        DATA_SCALE     AS Scale
        FROM ALL_ARGUMENTS
        WHERE SEQUENCE > 0 AND DATA_LEVEL = 0 AND OWNER = USER
	        AND (DATA_TYPE <> 'TABLE' OR IN_OUT <> 'OUT' OR POSITION <> 0)";

        

        return DBLive.ExeQuery(sql, null, rd =>
        {
            // IMPORTANT: reader calls must be ordered to support SequentialAccess
            var schema = rd.GetString(0);
            var packageName = rd.IsDBNull(1) ? null : rd.GetString(1);
            var procedureName = rd.GetString(2);
            var overload = rd.IsDBNull(3) ? null : rd.GetString(3);
            // IN, OUT, IN/OUT
            var direction = rd.GetString(4);
            var length = rd.IsDBNull(5) ? (int?)null : rd.GetInt32(5);
            var name = rd.IsDBNull(6) ? null : rd.GetString(6);
            var dataType = rd.GetString(7);
            // 0 - return value
            var ordinal = rd.GetInt32(8);
            var precision = rd.IsDBNull(9) ? (int?)null : rd.GetInt32(9);
            var scale = rd.IsDBNull(10) ? (int?)null : rd.GetInt32(10);

            return new ProcedureParameterInfo()
            {
                ProcedureID = $"{schema}.{overload}.{packageName}.{procedureName}",
                Ordinal = ordinal,
                ParameterName = name,
                DataType = dataType,
                Length = length,
                Precision = precision,
                Scale = scale,
                IsIn = direction.StartsWith("IN"),
                IsOut = direction.EndsWith("OUT"),
                IsResult = ordinal == 0,
                IsNullable = true
            };
        }).ToList();
    }

}
