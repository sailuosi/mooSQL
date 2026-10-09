using FluentAssertions;
using mooSQL.data;
using System.Linq;

namespace mooSQL.Pure.Tests.TestHelpers
{
    /// <summary>api8 用法测试共用：EnsureLiveParasResolved + 按 holder 长度降序展开参数。</summary>
    public static class Api8UsageSqlAssert
    {
        public static string ExactSql(SQLCmd cmd)
        {
            cmd.EnsureLiveParasResolved();
            var sql = cmd.sql ?? "";
            if (cmd.para?.value == null || cmd.para.value.Count == 0)
                return sql;

            foreach (var item in cmd.para.value.OrderByDescending(kv => (kv.Value?.holder ?? kv.Key).Length))
            {
                var holder = item.Value?.holder;
                var lit = "'" + item.Value?.val + "'";
                if (!string.IsNullOrEmpty(holder) && sql.Contains(holder))
                    sql = sql.Replace(holder, lit);
                else
                    sql = sql.Replace("@" + item.Key, lit);
            }
            return sql;
        }

        public static void AssertExactSql(SQLCmd cmd, string expected) =>
            ExactSql(cmd).Should().Be(expected);
    }
}
