using System;
using System.Collections.Generic;
using mooSQL.auth;
using mooSQL.data;
using mooSQL.Pure.Tests.TestHelpers;
using mooSQL.utils;
using Xunit;

namespace mooSQL.Pure.Tests.src.Auth;

public class AuthOptimizationsTests
{
    private sealed class FakePipelineDialect : PipelineDialect
    {
        public override int readRoleScopeCode(WordBagDialect range, AuthWord role, AuthUser user) => 0;
    }

    private sealed class FakeAuthDialect : AuthDialect
    {
        private readonly PipelineDialect _pipeline;
        private readonly WordBagDialect _wordBag;

        public FakeAuthDialect()
        {
            _pipeline = new FakePipelineDialect();
            _wordBag = new WordBagDialect(this);
        }

        public override PipelineDialect getPipeLine() => _pipeline;
        public override WordBagDialect getWordBag() => _wordBag;

        public override AuthOrg getOrgByOID(string oid) => throw new NotImplementedException();
        public override List<AuthOrg> getOrgByOIDs(List<string> oid) => throw new NotImplementedException();
        public override List<AuthWord> getDefaultRole() => new();
        public override AuthOrg loadManOrg(AuthUser man) => throw new NotImplementedException();
        public override AuthOrg loadManDiv(AuthUser man) => throw new NotImplementedException();
        public override AuthPost loadManPost(AuthUser man) => throw new NotImplementedException();
        public override AuthUser getUser(string acount) => throw new NotImplementedException();
        public override AuthOrg getOrg(Action<SQLBuilder> whereBuilder) => throw new NotImplementedException();
        public override AuthPost getPost(Action<SQLBuilder> whereBuilder) => throw new NotImplementedException();
        public override AuthUser getUser(Action<SQLBuilder> whereBuilder) => throw new NotImplementedException();
    }

    private sealed class FakeAuthFactory : AuthFactory<FakeAuthDialect>
    {
        private readonly FakeAuthDialect _dialect = new();
        public override FakeAuthDialect GetDialect() => _dialect;
    }

    private sealed class FakeAuthorBuilder : AuthorBuilder<FakeAuthDialect>
    {
        public FakeAuthorBuilder()
        {
            factory = new FakeAuthFactory();
        }

        public override void readRoleDataScope() { }
        public override List<AuthWord> loadDataScopes() => new();
    }

    [Fact]
    public void InvokeWordPreLoader_ShouldInvokeOnLoadWord_Readers()
    {
        var builder = new FakeAuthorBuilder();

        // ensure dialect + wordBag init
        _ = builder.dialect;

        // add a lazy word into the bag
        builder.dialect.getWordBag().addLazyWord(new AuthWord
        {
            id = Guid.NewGuid().ToString("N"),
            type = "2",
            scopeCode = "x",
            parser = "any"
        });

        var called = 0;
        builder.onLoadWord((w, d) => called++);

        builder.invokeWordPreLoader();

        Assert.Equal(1, called);
    }

    [Fact]
    public void GenerateCondition_ShouldSupport_NotIn_And_Between()
    {
        var kit = TestDatabaseHelper.CreateSQLBuilder();

        kit.generateCondition(new Condition
        {
            Key = "A.Id",
            Contrast = "not in",
            Value = "1;2",
            Text = "1;2",
            Paramed = true
        });

        kit.generateCondition(new Condition
        {
            Key = "A.Score",
            Contrast = "between",
            Value = "10;20",
            Text = "10;20",
            Paramed = true
        });

        var wh = kit.buildWhereContent();
        Assert.Contains("NOT IN", wh, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("between", wh, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MooAuthExtension_WriteTo_ShouldWriteSourceIntoTarget()
    {
        var src = new List<int> { 1, 2 };
        var target = new List<int> { 2, 3 };

        src.writeTo(target);

        Assert.Contains(1, target);
        Assert.Contains(2, target);
        Assert.Contains(3, target);
        Assert.Equal(3, target.Count);
    }

    [Fact]
    public void CodeRange_AddBindValue_ShouldReturnTrue_WhenAdded()
    {
        var range = new CodeRange<AuthOrg>();
        var added = range.addBindValue(new AuthOrg { HRCode = "100", HROID = "1", OrgNo = "o1" });
        Assert.True(added);
        Assert.Single(range.bindValues);
    }

    [Fact]
    public void CodeRange_AddBindValue_ShouldReturnFalse_WhenCoveredByContainValue()
    {
        var range = new CodeRange<AuthOrg>();
        range.addContainValue(new AuthOrg { HRCode = "10", HROID = "p", OrgNo = "p" });

        var added = range.addBindValue(new AuthOrg { HRCode = "101", HROID = "c", OrgNo = "c" });
        Assert.False(added);
        Assert.Empty(range.bindValues);
    }
}

