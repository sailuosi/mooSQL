// 基础功能说明：

using HHNY.NET.Application.Entity;
using mooSQL.data;
using mooSQL.Pure.Tests.TestHelpers;
using mooSQL.utils;
using mooSQL.data.clip;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace TestMooSQL.src;

public class LINQTest : IClassFixture<LinqSqliteTestFixture>
{
    readonly LinqSqliteTestFixture _sqlite;

    public LINQTest(LinqSqliteTestFixture sqlite) => _sqlite = sqlite;

    [Fact]
    public void testFieldFind()
    {
        Expression<Func<HHDutyItem, string>> exp = (e) => e.Di_Code;

        var db = LinqSqliteTestHelper.CreateSugarDatabase(out var path);
        try
        {
            var kit = db.useSQL();
            var fieldName = kit.DBLive.FindFieldName(exp);
            Assert.Equal("Di_Code", fieldName);
        }
        finally
        {
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }
    }

    /// <summary>
    /// 测试同一个表达式的缓存命中情况
    /// </summary>
    [Fact]
    public void testFieldFindCache1()
    {
        Expression<Func<HHDutyItem, string>> exp = (e) => e.Di_Code;

        var db = LinqSqliteTestHelper.CreateSugarDatabase(out var path);
        try
        {
            var kit = db.useSQL();
            var sw = Stopwatch.StartNew();
            var fieldName = kit.DBLive.FindFieldName(exp);
            sw.Stop();
            var t1 = ($"执行耗时: {sw.ElapsedTicks}ms");
            sw.Restart();
            var fieldName2 = kit.DBLive.FindFieldName(exp);
            sw.Stop();
            var t2 = ($"执行耗时: {sw.ElapsedTicks}ms");

            Expression<Func<HHDutyItem, string>> exp3 = (e) => e.Di_Code;
            sw.Restart();
            var fieldName3 = kit.DBLive.FindFieldName(exp3);
            sw.Stop();
            var t3 = ($"执行耗时: {sw.ElapsedTicks}ms");
            Assert.Equal(fieldName2, fieldName);
        }
        finally
        {
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }
    }

    [Fact]
    public void useBus1()
    {
        var kit = LinqSqliteTestHelper.CreateBus<SQLiteTestUser>(_sqlite.Db);

        var rows = kit.Where(u => u.Age > 20).ToList();
        Assert.True(rows.Count >= 2);
        Assert.All(rows, u => Assert.True(u.Age > 20));
    }
}
