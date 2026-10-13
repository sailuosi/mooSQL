using mooSQL.Pure.Tests.TestHelpers;
using TestMooSQL.src;

var t = typeof(SysConfig);
Console.WriteLine("GetProperties:");
foreach (var p in t.GetProperties())
    Console.WriteLine($"  {p.Name} (token={p.MetadataToken})");

var en = DBTest.useMySQLDB().client.EntityCash.getEntityInfo<SysConfig>();
Console.WriteLine("EntityInfo.Columns:");
foreach (var c in en.Columns)
    Console.WriteLine($"  {c.PropertyName}");

var sql = CreateTablePreviewAssert.PreviewCreateTableSql(DBTest.useMySQLDB());
var inner = sql.Split('(')[1].Split("PRIMARY")[0];
Console.WriteLine("DDL order:");
foreach (var part in inner.Split(','))
{
    var name = part.Trim().Split(' ')[0];
    if (!string.IsNullOrEmpty(name)) Console.WriteLine($"  {name}");
}
