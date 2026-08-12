using mooSQL.data;
using mooSQL.data.context;
using mooSQL.data.Mapping;
using System;
using System.Collections.Concurrent;

namespace TestMooSQL.src;

public partial class DBTest
{
    private static int _runPosition = 0;
    private static readonly ConcurrentDictionary<DataBaseType, DBInstance> _dialectKits = new();

    /// <summary>本地 SQLite（槽位 0，与专项测试共用同一库文件）。</summary>
    public static DBInstance useSQLiteDB() => GetDBInstance(0);

    /// <summary>执行用库。默认槽位 0（本地 SQLite），可通过 setRunDB 切换。</summary>
    public static DBInstance useRunDB() => GetDBInstance(_runPosition);

    /// <summary>当前执行库连接位。</summary>
    public static int RunDBPosition => _runPosition;

    /// <summary>切换执行库连接位（需已在 loadDBConfig / addMoreDB 中注册）。</summary>
    public static void setRunDB(int position) => _runPosition = position;

    /// <summary>按已注册槽位的 dbType 切换执行库；找不到则抛出。</summary>
    public static void setRunDB(DataBaseType dbType)
    {
        if (cash == null)
            initFactory();

        // 槽位 0 常为默认本地库
        try
        {
            var slot0 = cash.getInstance(0);
            if (slot0?.config?.dbType == dbType)
            {
                _runPosition = 0;
                return;
            }
        }
        catch
        {
            // ignore and scan
        }

        for (var i = 1; i < 32; i++)
        {
            try
            {
                var inst = cash.getInstance(i);
                if (inst?.config?.dbType == dbType)
                {
                    _runPosition = i;
                    return;
                }
            }
            catch
            {
                // slot missing
            }
        }

        throw new InvalidOperationException($"未找到已注册的 DataBaseType={dbType} 连接位，无法 setRunDB。");
    }

    /// <summary>执行库是否可用（对 useRunDB 做 SELECT 1）。</summary>
    public static bool IsRunAvailable() => IsAvailable(_runPosition);

    /// <summary>
    /// 业务库槽位（addMoreDB 注册的本机 netapi MSSQL）。依赖业务表结构的执行测试应使用此入口。
    /// </summary>
    public const int BusinessRunSlot = 1;

    /// <summary>
    /// 业务库是否可用（连接成功且存在 HH_DutyItem）。
    /// 不可用时依赖 HH_* / UCML_* 等业务表的测试应直接 return。
    /// </summary>
    public static bool IsBusinessRunAvailable()
    {
        try
        {
            var db = GetDBInstance(BusinessRunSlot);
            if (db?.config == null || db.config.dbType == DataBaseType.SQLite)
                return false;
            db.ExeQueryScalar<object>("SELECT 1", null);
            // 探测业务表：仅连通但无 schema 时仍视为不可用，避免误跑失败
            db.ExeQueryScalar<object>("SELECT TOP 1 1 FROM HH_DutyItem", null);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>业务库 DBInstance（槽位 <see cref="BusinessRunSlot"/>）。</summary>
    public static DBInstance useBusinessRunDB() => GetDBInstance(BusinessRunSlot);

    /// <summary>MySQL 方言空连接实例（仅 SQL 产物，不执行）。</summary>
    public static DBInstance useMySQLDB() => DialectKit(DataBaseType.MySQL);

    /// <summary>MSSQL 方言空连接实例（仅 SQL 产物，不执行）。</summary>
    public static DBInstance useMSSQLDB() => DialectKit(DataBaseType.MSSQL);

    /// <summary>Oracle 方言空连接实例（仅 SQL 产物，不执行）。</summary>
    public static DBInstance useOracleDB() => DialectKit(DataBaseType.Oracle);

    /// <summary>PostgreSQL 方言空连接实例（仅 SQL 产物，不执行）。</summary>
    public static DBInstance usePostgreSQLDB() => DialectKit(DataBaseType.PostgreSQL);

    /// <summary>Taos 方言空连接实例（仅 SQL 产物，不执行）。</summary>
    public static DBInstance useTaosDB() => DialectKit(DataBaseType.Taos);

    /// <summary>GBase8a 方言空连接实例（仅 SQL 产物，不执行）。</summary>
    public static DBInstance useGBase8aDB() => DialectKit(DataBaseType.GBase8a);

    /// <summary>OceanBase 方言空连接实例（仅 SQL 产物，不执行）。</summary>
    public static DBInstance useOceanBaseDB() => DialectKit(DataBaseType.OceanBase);

    /// <summary>Oscar 方言空连接实例（仅 SQL 产物，不执行）。</summary>
    public static DBInstance useOscarDB() => DialectKit(DataBaseType.Oscar);

    static DBInstance DialectKit(DataBaseType dbType) =>
        _dialectKits.GetOrAdd(dbType, t => BuildStandaloneInstance(t, string.Empty));

    /// <summary>
    /// 创建仅用于方言 SQL 生成的 DBInstance（默认空连接串，不进入 DBInsCash）。
    /// 每次新建，便于测试改写 version 等配置而不污染别名缓存。
    /// </summary>
    public static DBInstance CreateDialectInstance(DataBaseType dbType, string? connectionString = null)
        => BuildStandaloneInstance(dbType, connectionString ?? string.Empty);

    /// <summary>
    /// 构建独立 DBInstance（与 TestDatabaseHelper / 方言别名共用），不进入连接位缓存。
    /// </summary>
    public static DBInstance BuildStandaloneInstance(DataBaseType dbType, string connectionString)
    {
        var client = new MooClient();
        client.dialectFactory = new DialectFactory();
        var factory = new BaseEntityAnalyseFactory();
        factory.register(new MooEntityAnalyser());
        client.entityAnalyseFactory = factory;

        var dbConfig = new DataBase
        {
            dbType = dbType,
            DBConnectStr = connectionString
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
}
