using mooSQL.config;
using mooSQL.data;

/// <summary>
/// Provider 包显式注册（模型 1）+ 双通道识别演示。
/// </summary>
Console.WriteLine("=== mooSQL Provider Demo ===");
Console.WriteLine("双通道：DataBaseType 枚举 + dialectKey 字符串");
Console.WriteLine("注册：引用 Provider 包后调用 UseXxx()\n");

var cash = new DBClientBuilder()
    .UseDuckDB()
    .UseAurora()
    .UseCockroachDB()
    .UseEDB()
    .UseTurso()
    .useDataBase(new List<DBPosition>
    {
        new DBPosition
        {
            Position = 0,
            Name = "duck-mem",
            DbType = "DuckDB",
            ConnectString = "Data Source=:memory:"
        }
    })
    .doBuild();

var duck = cash.getInstance(0);
var one = duck.useSQL().select("1").from("(SELECT 1) t").queryScalar<long>();
Console.WriteLine($"[DuckDB] SELECT 1 => {one}");

var factory = cash.dialectFactory;
var auroraMy = factory.getDialect(new DataBase
{
    dialectKey = "aurora-mysql",
    DBConnectStr = ""
});
var crdb = factory.getDialect(new DataBase
{
    dialectKey = "CRDB",
    DBConnectStr = ""
});
var edb = factory.getDialect(new DataBase
{
    dbType = DataBaseType.EDB,
    DBConnectStr = ""
});
var turso = factory.getDialect(new DataBase
{
    dialectKey = DialectKeys.Turso,
    DBConnectStr = ""
});

Console.WriteLine($"[AuroraMySQL]     => {auroraMy.GetType().Name}");
Console.WriteLine($"[CockroachDB/CRDB]=> {crdb.GetType().Name}");
Console.WriteLine($"[EDB enum]        => {edb.GetType().Name}");
Console.WriteLine($"[Turso key]       => {turso.GetType().Name}");

Console.WriteLine("\nDone. 复审要点：");
Console.WriteLine("  1) Ext 默认 DialectFactory 不再注册 DuckDB");
Console.WriteLine("  2) 少见库在 provides/*，应用显式 UseXxx");
Console.WriteLine("  3) dialectKey / DataBaseType 双通道均可解析");
