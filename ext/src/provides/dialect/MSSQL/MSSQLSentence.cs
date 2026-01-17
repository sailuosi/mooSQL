// 基础功能说明：


using mooSQL.data;
using mooSQL.data.model;
using mooSQL.linq;

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data;

public class MSSQLSentence : SQLSentence
{
    private int? _CompatibilityLevel;

    public int CompatibilityLevel
    {
        get {
            if (_CompatibilityLevel == null) {
                _CompatibilityLevel = DBLive.ExeQueryScalar<int>("SELECT compatibility_level FROM sys.databases WHERE name = db_name()", null);
            }
            return _CompatibilityLevel.Value;
        }
    }

    public MSSQLSentence(Dialect dia) : base(dia) { 
    
    }
    public override SQLBuilder getTableColumns(string tableName, SQLBuilder builder)
    {
        // sql = "SELECT column_name as FName, data_type as FType, CHARACTER_MAXIMUM_LENGTH as FLen , column_name as
        // FDesc FROM information_schema.columns where table_name='"
        //                        + tableName + "'";
        builder.from("t", (t) =>
        {
            t.select("co.column_name as columnName, co.data_type as columnType, co.CHARACTER_MAXIMUM_LENGTH as columnLen , " +
                "(select MAX(c.value)FROM sys.tables A INNER JOIN sys.columns B ON B.object_id = A.object_id LEFT JOIN sys.extended_properties C ON C.major_id = B.object_id AND C.minor_id = B.column_id" +
                " where c.name='MS_Description' and a.name=co.TABLE_NAME and b.name=co.COLUMN_NAME)  as columnDesc")
                .from("information_schema.columns co")
                .where("table_name", tableName);
        });

        return builder;
    }



    public override bool CreateDatabase(string DatabaseName, string databaseDirectory = null)
    {
        bool result = false;
        if (!string.IsNullOrWhiteSpace(databaseDirectory))
        {
            databaseDirectory = databaseDirectory.TrimEnd('\\').TrimEnd('/');
        }
        if (!databaseDirectory.IsNullOrWhiteSpace())
        {
            try
            {
                databaseDirectory = databaseDirectory.TrimEnd('\\').TrimEnd('/');
                if (Directory.Exists(databaseDirectory))
                {
                    Directory.CreateDirectory(databaseDirectory);
                }
            }
            catch
            {
            }
        }

        if (!GetDataBaseList().Any((string it) => it.Equals(DatabaseName, StringComparison.CurrentCultureIgnoreCase)))
        {
            string text = "create database {0}";
            if (!databaseDirectory.IsNullOrWhiteSpace())
            {
                text += " on primary      (     name = N'{0}',        filename = N'{1}\\{0}.mdf', size = 10mb,  maxsize = 100mb,  filegrowth = 1mb  ), ( name = N'{0}_ndf',  filename = N'{1}\\{0}.ndf', size = 10mb,  maxsize = 100mb,  filegrowth = 10 %  )  log on    (  name = N'{0}_log',  filename = N'{1}\\{0}.ldf',  size = 100mb,   maxsize = 1gb,  filegrowth = 10mb  ); ";
            }
            DBLive.ExeNonQuery(string.Format(text,WordBuilder.wrapTable (DatabaseName), databaseDirectory), null);
            result = true;
        }

        return result;
    }




    //protected override void SetDbType(EntityColumn item, DbColumnInfo result)
    //{
    //    if (!Enum.IsDefined(typeof(DataType), item.DataType))
    //    {
    //        item.DataType = DataType.VarChar ;
    //    }
    //    result.DbType= item.DbType;
    //    result.DbTypeTextFull=result.DbType.ToDBString();
    //}

    //protected override bool IsSamgeType(EntityColumn ec, DbColumnInfo dc)
    //{
    //    return ec.DbType.IsSame(dc.DbType);
    //}

    public override string GetDataBaseSql => "SELECT NAME FROM MASTER.DBO.SYSDATABASES ORDER BY NAME";
    /*
    protected override string GetColumnInfosByTableNameSql => "SELECT \r\n                           syscolumns.name AS ColumnName,\r\n                           systypes.name AS DataType,\r\n                           COLUMNPROPERTY(syscolumns.id,syscolumns.name,'PRECISION') as ColumnLength,                           \r\n\t\t\t\t\t\t   isnull(COLUMNPROPERTY(syscolumns.id,syscolumns.name,'Scale'),0) as DecimalDigits,\r\n                           sys.extended_properties.[value] AS ColumnDescription,                          \r\n                           syscolumns.isnullable AS IsNullable,\r\n\t                       columnproperty(syscolumns.id,syscolumns.name,'IsIdentity')as IsIdentity,\r\n                           (CASE\r\n                                WHEN EXISTS\r\n                                       ( \r\n                                             \tselect 1\r\n\t\t\t\t\t\t\t\t\t\t\t\tfrom sysindexes i\r\n\t\t\t\t\t\t\t\t\t\t\t\tjoin sysindexkeys k on i.id = k.id and i.indid = k.indid\r\n\t\t\t\t\t\t\t\t\t\t\t\tjoin sysobjects o on i.id = o.id\r\n\t\t\t\t\t\t\t\t\t\t\t\tjoin syscolumns c on i.id=c.id and k.colid = c.colid\r\n\t\t\t\t\t\t\t\t\t\t\t\twhere o.xtype = 'U' \r\n\t\t\t\t\t\t\t\t\t\t\t\tand exists(select 1 from sysobjects where xtype = 'PK' and name = i.name) \r\n\t\t\t\t\t\t\t\t\t\t\t\tand o.name=sysobjects.name and c.name=syscolumns.name\r\n                                       ) THEN 1\r\n                                ELSE 0\r\n                            END) AS IsPrimaryKey " +
        " FROM syscolumns  INNER JOIN systypes ON syscolumns.xtype = systypes.xtype " +
        " LEFT JOIN sysobjects ON syscolumns.id = sysobjects.id " +
        " LEFT OUTER JOIN sys.extended_properties ON (sys.extended_properties.minor_id = syscolumns.colid  AND sys.extended_properties.major_id = syscolumns.id) " +
        " LEFT OUTER JOIN syscomments ON syscolumns.cdefault = syscomments.id " +
        " WHERE syscolumns.id IN (SELECT id  FROM sysobjects  WHERE upper(xtype) IN('U', 'V') )" +
        " AND (systypes.name <> 'sysname') " +
        " AND sysobjects.name='{0}' " +
        " AND systypes.name<>'geometry' " +
        " AND systypes.name<>'geography'" +
        " ORDER BY syscolumns.colid";
    */
    public override string GetTableInfoListSql => "SELECT s.Name,Convert(varchar(max),tbp.value) as Comment   FROM sysobjects s LEFT JOIN sys.extended_properties as tbp ON s.id=tbp.major_id and tbp.minor_id=0 AND (tbp.Name='MS_Description' OR tbp.Name is null)  WHERE s.xtype IN('U') order by s.Name";

    public override string GetViewInfoListSql => "SELECT s.Name,Convert(varchar(max),tbp.value) as Comment FROM sysobjects s LEFT JOIN sys.extended_properties as tbp ON s.id=tbp.major_id and tbp.minor_id=0  AND (tbp.Name='MS_Description' OR tbp.Name is null) WHERE s.xtype IN('V')  ";

    public override List<DbColumnInfo> GetDbColumnsByTableName(string tableName)
    {
        var kit = DBLive.useSQL()
            .select("syscolumns.name AS Name,systypes.name AS DbTypeText," +
            "COLUMNPROPERTY(syscolumns.id,syscolumns.name,'PRECISION') as MaxLength," +
            "isnull(COLUMNPROPERTY(syscolumns.id,syscolumns.name,'Scale'),0) as Scale," +
            "sys.extended_properties.[value] AS Comment," +
            "syscolumns.isnullable AS IsNullable," +
            "columnproperty(syscolumns.id,syscolumns.name,'IsIdentity')as IsIdentity," +
            "(CASE WHEN EXISTS(select 1 from sysindexes i join sysindexkeys k on i.id = k.id and i.indid = k.indid join sysobjects o on i.id = o.id join syscolumns c on i.id=c.id and k.colid = c.colid where o.xtype = 'U' and exists(select 1 from sysobjects where xtype = 'PK' and name = i.name)  and o.name=sysobjects.name and c.name=syscolumns.name ) THEN 1  ELSE 0 END) AS IsPrimary")
            .from("syscolumns  INNER JOIN systypes ON syscolumns.xtype = systypes.xtype")
            .join("LEFT JOIN sysobjects ON syscolumns.id = sysobjects.id")
            .join("LEFT OUTER JOIN sys.extended_properties ON (sys.extended_properties.minor_id = syscolumns.colid  AND sys.extended_properties.major_id = syscolumns.id)")
            .join("LEFT OUTER JOIN syscomments ON syscolumns.cdefault = syscomments.id")
            .where("syscolumns.id IN (SELECT id  FROM sysobjects  WHERE upper(xtype) IN('U', 'V') )")
            .where("systypes.name <> 'sysname'")
            .where("sysobjects.name", tableName)
            .where("systypes.name<>'geometry'  AND systypes.name<>'geography'")
            .orderBy("syscolumns.colid")
            ;
        //var sql = string.Format(this.GetColumnInfosByTableNameSql, tableName);
        //var res = this.DBLive.ExeQuery<DbColumnInfo>(sql);
        //return res.ToList();
        var sql = kit.toSelect();
        var tar= kit.query<DbColumnInfo>().ToList();
        foreach (var item in tar) {
            if (!string.IsNullOrWhiteSpace(item.DbTypeText)) { 
                item.FieldType= this.dialect.mapping.GetDataType(item.DbTypeText);
            }
        }
        return tar;
    }
    public override SQLCmd buildHasTable(string TableName)
    {
        var cmd = DBLive.useSQL()
            .from("sys.tables")
            .where("name", TableName)
            .toSelectCount();
        return cmd;
    }


    public override List<TableInfo> GetTables(GetSchemaOptions options)
    {
        var withTemporal = CompatibilityLevel >= 130;
        var temporalFilterStart = !withTemporal || !options.IgnoreSystemHistoryTables ? string.Empty : "(";
        var temporalFilterEnd = !withTemporal || !options.IgnoreSystemHistoryTables ? string.Empty : @"
					) AND t.temporal_type <> 1
";

        return DBLive.ExeQuery<TableInfo>(
             @"
				SELECT
					TABLE_CATALOG COLLATE DATABASE_DEFAULT + '.' + TABLE_SCHEMA + '.' + TABLE_NAME as TableID,
					TABLE_CATALOG                                                                  as CatalogName,
					TABLE_SCHEMA                                                                   as SchemaName,
					TABLE_NAME                                                                     as TableName,
					CASE WHEN TABLE_TYPE = 'VIEW' THEN 1 ELSE 0 END                                as IsView,
					ISNULL(CONVERT(varchar(8000), x.value), '')                                    as Description,
					CASE WHEN TABLE_SCHEMA = 'dbo' THEN 1 ELSE 0 END                               as IsDefaultSchema
				FROM
					INFORMATION_SCHEMA.TABLES s
					LEFT JOIN
						sys.tables t
					ON
						OBJECT_ID('[' + TABLE_CATALOG + '].[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']') = t.object_id
					LEFT JOIN
						sys.extended_properties x
					ON
						OBJECT_ID('[' + TABLE_CATALOG + '].[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']') = x.major_id AND
						x.minor_id = 0 AND
						x.name = 'MS_Description'
				WHERE
					" + temporalFilterStart + @"t.object_id IS NULL OR
					t.is_ms_shipped <> 1 AND
					(
						SELECT
							major_id
						FROM
							sys.extended_properties
						WHERE
							major_id = t.object_id AND
							minor_id = 0           AND
							class    = 1           AND
							name     = N'microsoft_database_tools_support'
					) IS NULL" + temporalFilterEnd, null)
            .ToList();
    }

    public override IReadOnlyCollection<PrimaryKeyInfo> GetPrimaryKeys(IEnumerable<TableSchema> tables, GetSchemaOptions options)
    {
        return DBLive.ExeQuery<PrimaryKeyInfo>(
            @"
				SELECT
					k.TABLE_CATALOG COLLATE DATABASE_DEFAULT + '.' + k.TABLE_SCHEMA + '.' + k.TABLE_NAME as TableID,
					k.CONSTRAINT_NAME                                                                    as PrimaryKeyName,
					k.COLUMN_NAME                                                                        as ColumnName,
					k.ORDINAL_POSITION                                                                   as Ordinal
				FROM
					INFORMATION_SCHEMA.KEY_COLUMN_USAGE k
					JOIN
						INFORMATION_SCHEMA.TABLE_CONSTRAINTS c
					ON
						k.CONSTRAINT_CATALOG = c.CONSTRAINT_CATALOG AND
						k.CONSTRAINT_SCHEMA  = c.CONSTRAINT_SCHEMA AND
						k.CONSTRAINT_NAME    = c.CONSTRAINT_NAME
				WHERE
					c.CONSTRAINT_TYPE='PRIMARY KEY'", null)
            .ToList();
    }

    public override List<ColumnInfo> GetColumns(GetSchemaOptions options)
    {
        var withTemporal = CompatibilityLevel >= 130;

        // column is from/to field (GeneratedAlwaysType)
        // or belongs to SYSTEM_VERSIONED_TEMPORAL_TABLE
        var temporalClause = !withTemporal ? string.Empty : @"
						OR COLUMNPROPERTY(object_id('[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']'), COLUMN_NAME, 'GeneratedAlwaysType') <> 0
						OR t.temporal_type = 1
";
        var temporalJoin = !withTemporal ? string.Empty : @"
					LEFT JOIN sys.tables t ON OBJECT_ID('[' + TABLE_CATALOG + '].[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']') = t.object_id";

        return DBLive.ExeQuery<ColumnInfo>(
             @"
				SELECT
					TABLE_CATALOG COLLATE DATABASE_DEFAULT + '.' + TABLE_SCHEMA + '.' + TABLE_NAME                      as TableID,
					COLUMN_NAME                                                                                         as Name,
					CASE WHEN IS_NULLABLE = 'YES' THEN 1 ELSE 0 END                                                     as IsNullable,
					ORDINAL_POSITION                                                                                    as Ordinal,
					c.DATA_TYPE                                                                                         as DataType,
					CHARACTER_MAXIMUM_LENGTH                                                                            as Length,
					ISNULL(NUMERIC_PRECISION, DATETIME_PRECISION)                                                       as [Precision],
					NUMERIC_SCALE                                                                                       as Scale,
					ISNULL(CONVERT(varchar(8000), x.value), '')                                                         as [Description],
					COLUMNPROPERTY(object_id('[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']'), COLUMN_NAME, 'IsIdentity') as IsIdentity,
					CASE WHEN c.DATA_TYPE = 'timestamp'
						OR COLUMNPROPERTY(object_id('[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']'), COLUMN_NAME, 'IsComputed') = 1" + temporalClause + @"
						THEN 1 ELSE 0 END as SkipOnInsert,
					CASE WHEN c.DATA_TYPE = 'timestamp'
						OR COLUMNPROPERTY(object_id('[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']'), COLUMN_NAME, 'IsComputed') = 1" + temporalClause + @"
						THEN 1 ELSE 0 END as SkipOnUpdate
				FROM
					INFORMATION_SCHEMA.COLUMNS c
					LEFT JOIN
						sys.extended_properties x
					ON
						--OBJECT_ID('[' + TABLE_CATALOG + '].[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']') = x.major_id AND
						OBJECT_ID('[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']') = x.major_id AND
						COLUMNPROPERTY(OBJECT_ID('[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']'), COLUMN_NAME, 'ColumnID') = x.minor_id AND
						x.name = 'MS_Description' AND x.class = 1" + temporalJoin, null)
            .Select(c =>
            {
                switch (c.DataType)
                {
                    case "geometry":
                    case "geography":
                    case "hierarchyid":
                    case "float":
                        c.Length = null;
                        c.Precision = null;
                        c.Scale = null;
                        break;
                }

                return c;
            })
            .ToList();
    }

    public override IReadOnlyCollection<ForeignKeyInfo> GetForeignKeys(IEnumerable<TableSchema> tables, GetSchemaOptions options)
    {
        return DBLive.ExeQuery<ForeignKeyInfo>(@"
				SELECT
					fk.name                                                     as Name,
					DB_NAME() + '.' + SCHEMA_NAME(po.schema_id) + '.' + po.name as ThisTableID,
					pc.name                                                     as ThisColumn,
					DB_NAME() + '.' + SCHEMA_NAME(fo.schema_id) + '.' + fo.name as OtherTableID,
					fc.name                                                     as OtherColumn,
					fkc.constraint_column_id                                    as Ordinal
				FROM sys.foreign_keys fk
					inner join sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
					inner join sys.columns             pc  ON fkc.parent_column_id = pc.column_id and fkc.parent_object_id = pc.object_id
					inner join sys.objects             po  ON fk.parent_object_id = po.object_id
					inner join sys.columns             fc  ON fkc.referenced_column_id = fc.column_id and fkc.referenced_object_id = fc.object_id
					inner join sys.objects             fo  ON fk.referenced_object_id = fo.object_id
				ORDER BY
					ThisTableID,
					Ordinal")
            .ToList();
    }

    public override List<ProcedureInfo>? GetProcedures(GetSchemaOptions options)
    {
        return DBLive.ExeQuery<ProcedureInfo>(
            @"SELECT
					SPECIFIC_CATALOG COLLATE DATABASE_DEFAULT + '.' + SPECIFIC_SCHEMA + '.' + SPECIFIC_NAME as ProcedureID,
					SPECIFIC_CATALOG                                                                        as CatalogName,
					SPECIFIC_SCHEMA                                                                         as SchemaName,
					SPECIFIC_NAME                                                                           as ProcedureName,
					CASE WHEN ROUTINE_TYPE = 'FUNCTION'                         THEN 1 ELSE 0 END           as IsFunction,
					CASE WHEN ROUTINE_TYPE = 'FUNCTION' AND DATA_TYPE = 'TABLE' THEN 1 ELSE 0 END           as IsTableFunction,
					CASE WHEN EXISTS(SELECT * FROM sys.objects where name = SPECIFIC_NAME AND type='AF')
					                                                            THEN 1 ELSE 0 END           as IsAggregateFunction,
					CASE WHEN SPECIFIC_SCHEMA = 'dbo'                           THEN 1 ELSE 0 END           as IsDefaultSchema,
					ISNULL(CONVERT(varchar(8000), x.value), '')                                             as Description
				FROM
					INFORMATION_SCHEMA.ROUTINES
					LEFT JOIN sys.extended_properties x
						ON OBJECT_ID('[' + SPECIFIC_SCHEMA + '].[' + SPECIFIC_NAME + ']') = x.major_id AND
							x.name = 'MS_Description' AND x.class = 1
				ORDER BY SPECIFIC_CATALOG, SPECIFIC_SCHEMA, SPECIFIC_NAME")
            .ToList();
    }

    public override List<ProcedureParameterInfo> GetProcedureParameters(IEnumerable<ProcedureInfo> procedures, GetSchemaOptions options)
    {
        return DBLive.ExeQuery<ProcedureParameterInfo>(
            @"SELECT
					SPECIFIC_CATALOG COLLATE DATABASE_DEFAULT + '.' + SPECIFIC_SCHEMA + '.' + SPECIFIC_NAME as ProcedureID,
					ORDINAL_POSITION                                                                        as Ordinal,
					PARAMETER_MODE                                                                          as Mode,
					PARAMETER_NAME                                                                          as ParameterName,
					DATA_TYPE                                                                               as DataType,
					CHARACTER_MAXIMUM_LENGTH                                                                as Length,
					NUMERIC_PRECISION                                                                       as [Precision],
					NUMERIC_SCALE                                                                           as Scale,
					CASE WHEN PARAMETER_MODE = 'IN'  OR PARAMETER_MODE = 'INOUT' THEN 1 ELSE 0 END          as IsIn,
					CASE WHEN PARAMETER_MODE = 'OUT' OR PARAMETER_MODE = 'INOUT' THEN 1 ELSE 0 END          as IsOut,
					CASE WHEN IS_RESULT      = 'YES'                             THEN 1 ELSE 0 END          as IsResult,
					USER_DEFINED_TYPE_CATALOG                                                               as UDTCatalog,
					USER_DEFINED_TYPE_SCHEMA                                                                as UDTSchema,
					USER_DEFINED_TYPE_NAME                                                                  as UDTName,
					1                                                                                       as IsNullable,
					ISNULL(CONVERT(varchar(8000), x.value), '')                                             as Description
				FROM
					INFORMATION_SCHEMA.PARAMETERS
					LEFT JOIN sys.extended_properties x
						ON OBJECT_ID('[' + SPECIFIC_SCHEMA + '].[' + SPECIFIC_NAME + ']') = x.major_id AND
							ORDINAL_POSITION = x.minor_id AND
							x.name = 'MS_Description' AND x.class = 2")
            .ToList();
    }

}
