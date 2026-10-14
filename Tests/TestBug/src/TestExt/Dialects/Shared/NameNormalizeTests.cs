using FluentAssertions;
using mooSQL.data;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// NameCase / NameQuote 规范化：方言默认、连接覆盖、KeywordsOnly / IfNeeded。
/// </summary>
public class NameNormalizeTests
{
    [Fact]
    public void PostgreSQL_Default_ToLower_IfNeeded()
    {
        var db = DBTest.CreateDialectInstance(DataBaseType.PostgreSQL);
        var exp = db.dialect.expression;
        exp.DefaultNameCase.Should().Be(NameCaseMode.ToLower);
        exp.DefaultNameQuote.Should().Be(NameQuoteMode.IfNeeded);
        exp.wrapField("UserInfo").Should().Be("userinfo");
        exp.wrapTable("HH_SysConfig").Should().Be("hh_sysconfig");
    }

    [Fact]
    public void PostgreSQL_IfNeeded_Quotes_Reserved_User()
    {
        var db = DBTest.CreateDialectInstance(DataBaseType.PostgreSQL);
        db.dialect.expression.wrapField("User").Should().Be("\"user\"");
        db.dialect.expression.wrapField("Order").Should().Be("\"order\"");
    }

    [Fact]
    public void Oracle_Default_ToUpper_IfNeeded_Quotes_RowId()
    {
        var db = DBTest.CreateDialectInstance(DataBaseType.Oracle);
        var exp = db.dialect.expression;
        exp.DefaultNameQuote.Should().Be(NameQuoteMode.IfNeeded);
        exp.wrapField("UserInfo").Should().Be("USERINFO");
        exp.wrapTable("HH_SysConfig").Should().Be("HH_SYSCONFIG");
        exp.wrapField("RowId").Should().Be("\"ROWID\"");
    }

    [Fact]
    public void Mssql_Default_Keep_Always()
    {
        var db = DBTest.CreateDialectInstance(DataBaseType.MSSQL);
        var exp = db.dialect.expression;
        exp.wrapField("UserInfo").Should().Be("[UserInfo]");
        exp.wrapTable("HH_SysConfig").Should().Be("[HH_SysConfig]");
    }

    [Fact]
    public void Mssql_KeywordsOnly_Quotes_Only_Reserved()
    {
        var db = DBTest.CreateDialectInstance(DataBaseType.MSSQL);
        db.config.setNameCase(NameCaseMode.Keep).setNameQuote(NameQuoteMode.KeywordsOnly);
        var exp = db.dialect.expression;
        exp.wrapField("UserName").Should().Be("UserName");
        exp.wrapField("User").Should().Be("[User]");
        exp.wrapField("Order").Should().Be("[Order]");
    }

    [Fact]
    public void PostgreSQL_Keep_IfNeeded_Quotes_MixedCase()
    {
        var db = DBTest.CreateDialectInstance(DataBaseType.PostgreSQL);
        db.config.setNameCase(NameCaseMode.Keep).setNameQuote(NameQuoteMode.IfNeeded);
        db.dialect.expression.wrapField("UserInfo").Should().Be("\"UserInfo\"");
        db.dialect.expression.wrapField("userinfo").Should().Be("userinfo");
    }

    [Fact]
    public void Connection_Override_Keep_Always_On_PostgreSQL()
    {
        var db = DBTest.CreateDialectInstance(DataBaseType.PostgreSQL);
        db.config.setNameCase(NameCaseMode.Keep).setNameQuote(NameQuoteMode.Always);
        db.dialect.expression.wrapField("UserInfo").Should().Be("\"UserInfo\"");
    }

    [Fact]
    public void AlreadyQuoted_Is_Not_Rewritten()
    {
        var db = DBTest.CreateDialectInstance(DataBaseType.PostgreSQL);
        db.dialect.expression.wrapField("\"UserInfo\"").Should().Be("\"UserInfo\"");
    }

    [Fact]
    public void Expression_Fragment_Is_Not_Rewritten()
    {
        var db = DBTest.CreateDialectInstance(DataBaseType.MSSQL);
        db.dialect.expression.wrapField("COUNT(*)").Should().Be("COUNT(*)");
    }

    [Fact]
    public void Qualified_Name_Segments_Are_Normalized()
    {
        var db = DBTest.CreateDialectInstance(DataBaseType.PostgreSQL);
        db.dialect.expression.wrapField("Public.UserInfo").Should().Be("public.userinfo");
        db.config.setNameCase(NameCaseMode.Keep).setNameQuote(NameQuoteMode.IfNeeded);
        db.dialect.expression.wrapField("Public.User").Should().Be("\"Public\".\"User\"");
    }

    [Fact]
    public void IsReservedWord_Is_CaseInsensitive()
    {
        var db = DBTest.CreateDialectInstance(DataBaseType.PostgreSQL);
        db.dialect.expression.IsReservedWord("USER").Should().BeTrue();
        db.dialect.expression.IsReservedWord("user").Should().BeTrue();
        db.dialect.expression.IsReservedWord("UserInfo").Should().BeFalse();
    }
}
