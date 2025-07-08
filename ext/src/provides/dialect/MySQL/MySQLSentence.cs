// 基础功能说明：

using mooSQL.data;
using mooSQL.data.model;
using mooSQL.utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data;
public class MySQLSentence :SQLSentence
{

    public MySQLSentence(Dialect dia) : base(dia) { 
    
    }

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


    public override string GetDataBaseSql => "SHOW DATABASES";

    public override string GetColumnInfosByTableNameSql => "SELECT  column_name AS Name," +
        " CASE WHEN  left(COLUMN_TYPE,LOCATE('(',COLUMN_TYPE)-1)='' THEN COLUMN_TYPE ELSE  left(COLUMN_TYPE,LOCATE('(',COLUMN_TYPE)-1) END   AS DbTypeText," +
        "CAST(SUBSTRING(COLUMN_TYPE,LOCATE('(',COLUMN_TYPE)+1,LOCATE(')',COLUMN_TYPE)-LOCATE('(',COLUMN_TYPE)-1) AS signed) AS MaxLength," +
        " numeric_scale as Scale,column_default  AS  `DefaultValue`,column_comment  AS  `Comment`," +
        "CASE WHEN COLUMN_KEY = 'PRI' THEN true ELSE false END AS `IsPrimary`," +
        " CASE WHEN EXTRA='auto_increment' THEN true ELSE false END as IsIdentity," +
        "CASE WHEN is_nullable = 'YES' THEN true ELSE false END AS `IsNullable` " +
        "FROM Information_schema.columns where TABLE_NAME='{0}' and  TABLE_SCHEMA=(select database()) ORDER BY TABLE_NAME";

    public override string GetTableInfoListSql => "select TABLE_NAME as Name,TABLE_COMMENT as Comment from information_schema.tables" +
        " where  TABLE_SCHEMA=(select database())  AND TABLE_TYPE='BASE TABLE'";

    public override string GetViewInfoListSql => "select TABLE_NAME as Name,TABLE_COMMENT as Description from information_schema.tables " +
        "where  TABLE_SCHEMA=(select database()) AND TABLE_TYPE='VIEW'";


    public override SQLCmd buildHasTable(string TableName)
    {
        var cmd = DBLive.useSQL()
            .from("information_schema.tables")
            .where("table_name", TableName)
            .where("table_schema = DATABASE()")
            .toSelectCount();
        return cmd;
    }

    public override List<TableInfo> GetTables(GetSchemaOptions options)
    {
        // https://dev.mysql.com/doc/refman/8.0/en/tables-table.html
        // all selected columns are not nullable
        var sql = @"
SELECT
		TABLE_SCHEMA,
		TABLE_NAME,
		TABLE_TYPE,
		TABLE_COMMENT
	FROM INFORMATION_SCHEMA.TABLES
	WHERE TABLE_SCHEMA = DATABASE()";

        return DBLive
            .ExeQuery(sql, null, rd =>
            {
                // IMPORTANT: reader calls must be ordered to support SequentialAccess
                var catalog = rd.GetString(0);
                var name = rd.GetString(1);
                // BASE TABLE/VIEW/SYSTEM VIEW
                var type = rd.GetString(2);
                return new TableInfo()
                {
                    // The latest MySql returns FK information with lowered schema names.
                    //
                    TableID = catalog.ToLowerInvariant() + ".." + name,
                    CatalogName = catalog,
                    TableName = name,
                    IsDefaultSchema = true,
                    IsView = type == "VIEW" || type == "SYSTEM VIEW",
                    IsProviderSpecific = type == "SYSTEM VIEW" || catalog.Equals("sys", StringComparison.OrdinalIgnoreCase),
                    Description = rd.GetString(3)
                };
            })
            .ToList();
    }

    public override IReadOnlyCollection<PrimaryKeyInfo> GetPrimaryKeys(IEnumerable<TableSchema> tables, GetSchemaOptions options)
    {
        return DBLive.ExeQuery<PrimaryKeyInfo>(@"
			SELECT
					CONCAT(lower(k.CONSTRAINT_SCHEMA),'..',k.TABLE_NAME) as TableID,
					k.CONSTRAINT_NAME                                    as PrimaryKeyName,
					k.COLUMN_NAME                                        as ColumnName,
					k.ORDINAL_POSITION                                   as Ordinal
				FROM
					INFORMATION_SCHEMA.KEY_COLUMN_USAGE k
					JOIN
						INFORMATION_SCHEMA.TABLE_CONSTRAINTS c
					ON
						k.CONSTRAINT_CATALOG = c.CONSTRAINT_CATALOG AND
						k.CONSTRAINT_SCHEMA  = c.CONSTRAINT_SCHEMA AND
						k.CONSTRAINT_NAME    = c.CONSTRAINT_NAME AND
						k.TABLE_NAME         = c.TABLE_NAME
				WHERE
					c.CONSTRAINT_TYPE   ='PRIMARY KEY' AND
					c.CONSTRAINT_SCHEMA = database()", null)
        .ToList();
    }

    public override List<ColumnInfo> GetColumns(GetSchemaOptions options)
    {
        // https://dev.mysql.com/doc/refman/8.0/en/columns-table.html
        // nullable columns:
        // CHARACTER_MAXIMUM_LENGTH
        // NUMERIC_PRECISION
        // NUMERIC_SCALE
        var sql = @"
SELECT
		DATA_TYPE,
		COLUMN_TYPE,
		TABLE_SCHEMA,
		TABLE_NAME,
		COLUMN_NAME,
		IS_NULLABLE,
		ORDINAL_POSITION,
		CHARACTER_MAXIMUM_LENGTH,
		NUMERIC_PRECISION,
		NUMERIC_SCALE,
		EXTRA,
		COLUMN_COMMENT
	FROM INFORMATION_SCHEMA.COLUMNS
	WHERE TABLE_SCHEMA = DATABASE()";

        return DBLive
            .ExeQuery(sql, null, rd =>
            {
                // IMPORTANT: reader calls must be ordered to support SequentialAccess
                var dataType = rd.GetString(0);
                var columnType = rd.GetString(1);
                var tableId = rd.GetString(2).ToLowerInvariant() + ".." + rd.GetString(3);
                var name = rd.GetString(4);
                var isNullable = rd.GetString(5) == "YES";
                var ordinal = TypeAs.asInt(rd[6],0);
                var length = TypeAs.asLong(rd[7],0);
                var precision = TypeAs.asLong(rd[8], 0);
                var scale = TypeAs.asLong(rd[9],0);
                var extra = rd.GetString(10);

                return new ColumnInfo()
                {
                    TableID = tableId,
                    Name = name,
                    IsNullable = isNullable,
                    Ordinal = ordinal,
                    DataType = dataType,
                    // length could be > int.MaxLength for LONGBLOB/LONGTEXT types, but they always have fixed length and it cannot be used in type name
                    Length = length > int.MaxValue ? null : (int?)length,
                    Precision = (int?)precision,
                    Scale = (int?)scale,
                    ColumnType = columnType,
                    IsIdentity = extra.Contains("auto_increment"),
                    Description = rd.GetString(11),
                    // also starting from 5.1 we can utilise provileges column for skip properties
                    // but it sounds like a bad idea
                    SkipOnInsert = extra.Contains("VIRTUAL STORED") || extra.Contains("VIRTUAL GENERATED"),
                    SkipOnUpdate = extra.Contains("VIRTUAL STORED") || extra.Contains("VIRTUAL GENERATED")
                };
            })
            .ToList();
    }

    public override IReadOnlyCollection<ForeignKeyInfo> GetForeignKeys(IEnumerable<TableSchema> tables, GetSchemaOptions options)
    {
        // https://dev.mysql.com/doc/refman/8.0/en/key-column-usage-table.html
        // https://dev.mysql.com/doc/refman/8.0/en/table-constraints-table.html
        // nullable columns:
        // REFERECED_* columns could be null, but not for FK
        var sql = @"
SELECT
		c.TABLE_SCHEMA,
		c.TABLE_NAME,
		c.CONSTRAINT_NAME,
		c.COLUMN_NAME,
		c.REFERENCED_TABLE_SCHEMA,
		c.REFERENCED_TABLE_NAME,
		c.REFERENCED_COLUMN_NAME,
		c.ORDINAL_POSITION
	FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE c
		INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
			ON c.CONSTRAINT_SCHEMA    = tc.CONSTRAINT_SCHEMA
				AND c.CONSTRAINT_NAME = tc.CONSTRAINT_NAME
				AND c.TABLE_SCHEMA    = tc.TABLE_SCHEMA
				AND c.TABLE_NAME      = tc.TABLE_NAME
	WHERE tc.CONSTRAINT_TYPE = 'FOREIGN KEY'
		AND c.TABLE_SCHEMA   = DATABASE()";

        return DBLive
            .ExeQuery(sql, null, rd =>
            {
                // IMPORTANT: reader calls must be ordered to support SequentialAccess
                return new ForeignKeyInfo()
                {
                    ThisTableID = rd.GetString(0).ToLowerInvariant() + ".." + rd.GetString(1),
                    Name = rd.GetString(2),
                    ThisColumn = rd.GetString(3),
                    OtherTableID = rd.GetString(4).ToLowerInvariant() + ".." + rd.GetString(5),
                    OtherColumn = rd.GetString(6),
                    Ordinal =rd.GetInt32( 7),
                };
            })
            .ToList();
    }


    public override List<ProcedureInfo>? GetProcedures(GetSchemaOptions options)
    {
        // GetSchema("PROCEDURES") not used, as for MySql 5.7 (but not mariadb/mysql 5.6) it returns procedures from
        // sys database too
        var sql = "SELECT ROUTINE_SCHEMA, ROUTINE_NAME, ROUTINE_TYPE, ROUTINE_DEFINITION, ROUTINE_COMMENT FROM INFORMATION_SCHEMA.routines" +
            " WHERE ROUTINE_TYPE IN ('PROCEDURE', 'FUNCTION') AND ROUTINE_SCHEMA = database()";

        return DBLive
            .ExeQuery(sql, null, rd =>
            {
                // IMPORTANT: reader calls must be ordered to support SequentialAccess
                var catalog = TypeAs.asString(rd[0],"");
                var name = TypeAs.asString(rd[1],"");

                return new ProcedureInfo()
                {
                    ProcedureID = catalog + "." + name,
                    CatalogName = catalog,
                    ProcedureName = name,
                    IsFunction = TypeAs.asString(rd[2],"") == "FUNCTION",
                    IsDefaultSchema = true,
                    ProcedureDefinition = TypeAs.asString(rd[3], ""),
                    Description = TypeAs.asString(rd[4],""),
                };
            })
            .ToList();
    }

    public override List<ProcedureParameterInfo> GetProcedureParameters(IEnumerable<ProcedureInfo> procedures, GetSchemaOptions options)
    {
        // don't use GetSchema("PROCEDURE PARAMETERS") as MySql provider's implementation does strange stuff
        // instead of just quering of INFORMATION_SCHEMA view. It returns incorrect results and breaks provider
        var sql = "SELECT SPECIFIC_SCHEMA, SPECIFIC_NAME, PARAMETER_MODE, ORDINAL_POSITION, PARAMETER_NAME, NUMERIC_PRECISION, NUMERIC_SCALE, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, DTD_IDENTIFIER" +
            " FROM INFORMATION_SCHEMA.parameters WHERE SPECIFIC_SCHEMA = database()";

        return DBLive
            .ExeQuery(sql, null, rd =>
            {
                // IMPORTANT: reader calls must be ordered to support SequentialAccess
                var procId = rd.GetString(0) + "." + rd.GetString(1);
                var mode = TypeAs.asString(rd[2], "");
                var ordinal = TypeAs.asInt(rd[3],0);
                var name = TypeAs.asString(rd[4], "");
                var precision = TypeAs.asInt(rd[5],0);
                var scale = TypeAs.asLong(rd[6],0);
                var type = rd.GetString(7).ToUpperInvariant();
                var length = TypeAs.asLong(rd[8],0);

                return new ProcedureParameterInfo()
                {
                    ProcedureID = procId,
                    ParameterName = name,
                    IsIn = mode == "IN" || mode == "INOUT",
                    IsOut = mode == "OUT" || mode == "INOUT",
                    Precision = precision,
                    Scale = (int?)scale,
                    Ordinal = ordinal,
                    IsResult = mode == null,
                    DataType = type,
                    Length = length > int.MaxValue ? null : (int?)length,
                    DataTypeExact = TypeAs.asString(rd[9],""),
                    IsNullable = true
                };
            })
            .ToList();
    }

}
