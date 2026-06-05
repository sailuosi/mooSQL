using HHNY.NET.Core.MooSQL;
using mooSQL.data;
using mooSQL.data.context;
using mooSQL.data.Mapping;
using mooSQL.linq;
using mooSQL.linq.core;
using System.IO;

namespace mooSQL.Pure.Tests.TestHelpers;

/// <summary>
/// LINQ 端到端测试用的 SQLite 数据库与 DbBus 工厂。
/// </summary>
public static class LinqSqliteTestHelper
{
    /// <summary>
    /// 创建带 Moo 实体解析器的 SQLite <see cref="DBInstance"/>。
    /// </summary>
    public static DBInstance CreateDatabase(out string dbFilePath, string? connectionString = null)
    {
        dbFilePath = Path.Combine(Path.GetTempPath(), $"mooSQL_linq_{Guid.NewGuid():N}.db");
        var connStr = connectionString ?? "Data Source=" + dbFilePath + ";Mode=ReadWriteCreate";
        return TestDatabaseHelper.CreateTestDBInstance(DataBaseType.SQLite, connStr);
    }

    /// <summary>
    /// 创建带 SqlSugar 实体解析器的 SQLite 实例（用于 <c>SugarTable</c> 实体，如 HHDutyItem）。
    /// </summary>
    public static DBInstance CreateSugarDatabase(out string dbFilePath)
    {
        dbFilePath = Path.Combine(Path.GetTempPath(), $"mooSQL_linq_sugar_{Guid.NewGuid():N}.db");
        var connStr = "Data Source=" + dbFilePath + ";Mode=ReadWriteCreate";

        var client = new MooClient
        {
            dialectFactory = new DialectFactory(),
            entityAnalyseFactory = CreateSugarEntityFactory()
        };

        var dbConfig = new DataBase
        {
            dbType = DataBaseType.SQLite,
            DBConnectStr = connStr
        };

        var dbInstance = new DBInstance
        {
            config = dbConfig,
            client = client
        };

        dbInstance.dialect = client.dialectFactory.getDialect(dbConfig);
        dbInstance.dialect.dbInstance = dbInstance;
        dbInstance.dialect.db = dbConfig;
        dbInstance.cmd = new CmdExecutor(dbInstance);
        return dbInstance;
    }

    static BaseEntityAnalyseFactory CreateSugarEntityFactory()
    {
        var factory = new BaseEntityAnalyseFactory();
        factory.register(new SugarEnitiyParser());
        return factory;
    }

    /// <summary>
    /// 基于 EntityVisit 编译链创建 DbBus。
    /// </summary>
    public static DbBus<T> CreateBus<T>(DBInstance db)
    {
        var factory = new EntityVisitFactory();
        var context = new DbContext { DB = db, Factory = factory };
        return new EnDbBus<T>(context, typeof(T), factory);
    }
}
