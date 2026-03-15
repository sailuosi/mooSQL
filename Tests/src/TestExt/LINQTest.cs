// 基础功能说明：

using HHNY.NET.Application.Entity;
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

public class LINQTest
{
    [Fact]
    public void testFieldFind() {


        Expression<Func<HHDutyItem, string>> exp = (e) => e.Di_Code;

        var kit = DBTest.useSQL(0);

        var fieldName=kit.DBLive.FindFieldName(exp);
        Assert.Equal("Di_Code", fieldName);
    }

    /// <summary>
    /// 测试同一个表达式的缓存命中情况
    /// </summary>
    [Fact]
    public void testFieldFindCache1()
    {


        Expression<Func<HHDutyItem, string>> exp = (e) => e.Di_Code;

        var kit = DBTest.useSQL(0);
        var sw = Stopwatch.StartNew();
        var fieldName = kit.DBLive.FindFieldName(exp);
        sw.Stop();
        var t1=($"执行耗时: {sw.ElapsedTicks}ms");
        sw.Restart();
        var fieldName2 = kit.DBLive.FindFieldName(exp);
        sw.Stop();
        var t2=($"执行耗时: {sw.ElapsedTicks}ms");

        Expression<Func<HHDutyItem, string>> exp3 = (e) => e.Di_Code;
        sw.Restart();
        var fieldName3 = kit.DBLive.FindFieldName(exp3);
        sw.Stop();
        var t3=($"执行耗时: {sw.ElapsedTicks}ms");
        Assert.Equal(fieldName2, fieldName);
    }
}
