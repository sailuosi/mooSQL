using FluentAssertions;
using mooSQL.data;
using mooSQL.Pure.Tests.TestHelpers;
using System.Text.RegularExpressions;

namespace TestMooSQL.src;

/// <summary>
/// 各方言 CreateTable preview 共用：同一套调用，仅 DBInstance 不同。
/// </summary>
public static class CreateTablePreviewAssert
{
    public static string PreviewCreateTableSql(DBInstance db)
        => db.useSQL().useDBInit().preview().createTable<SysConfig>().Result;

    public static string NormalizeWhitespace(string s)
        => Regex.Replace(s ?? string.Empty, @"\s+", string.Empty);

    public static void AssertSqlEqual(string actual, string expected)
    {
        NormalizeWhitespace(actual).Should().Be(
            NormalizeWhitespace(expected),
            because: "create-table preview SQL must match expected (whitespace ignored only)");
    }
}
