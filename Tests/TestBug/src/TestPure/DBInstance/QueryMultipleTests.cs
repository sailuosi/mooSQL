using FluentAssertions;
using mooSQL.Pure.Tests.TestHelpers;
using mooSQL.data;
using mooSQL.data.context;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace mooSQL.Pure.Tests
{
    public sealed class QueryMultipleTests
    {
        private sealed class IdRow
        {
            public int id { get; set; }
        }

        private readonly DBInstance _db;

        public QueryMultipleTests()
        {
            _db = TestDatabaseHelper.CreateTestDBInstance(DataBaseType.SQLite);
        }

        [Fact]
        public void ExeQueryMultiple_TwoSelects_ReturnsListThenScalar()
        {
            var sql =new SQLCmd( "SELECT 1 AS id; SELECT 2 AS x");
            var result = _db.ExeQueryMultiple(sql, r => (r.List<IdRow>(), r.Scalar<int>()));

            result.Item1.Should().HaveCount(1);
            result.Item1[0].id.Should().Be(1);
            result.Item2.Should().Be(2);
        }

        [Fact]
        public void ExeQueryMultiple_SecondReadBeyondResults_Throws()
        {
            var sql = new SQLCmd("SELECT 1 AS id;");
            Action act = () => _db.ExeQueryMultiple(sql, r =>
            {
                _ = r.List<IdRow>();
                _ = r.List<IdRow>();
                return 0;
            });

            act.Should().Throw<Exception>()
                .Which.InnerException.Should().BeOfType<InvalidOperationException>()
                .Which.Message.Should().Contain("结果集");
        }

        [Fact]
        public void ExeQueryMultiple_UniqueRow_WithTwoRows_Throws()
        {
            var sql = new SQLCmd("SELECT 1 AS id UNION ALL SELECT 2 AS id;");
            Action act = () => _db.ExeQueryMultiple(sql, r => r.UniqueRow<IdRow>());

            act.Should().Throw<Exception>()
                .Which.InnerException.Should().BeOfType<InvalidOperationException>()
                .Which.Message.Should().Contain("至多一行");
        }

        [Fact]
        public async Task ExeQueryMultipleAsync_TwoSelects_ReturnsListThenScalar()
        {
            var sql = new SQLCmd("SELECT 3 AS id; SELECT 4 AS x");
            var result = await _db.ExeQueryMultipleAsync(sql, async r =>
            {
                var list = await Task.Run(() => r.List<IdRow>().ToList());
                var s = await Task.Run(() => r.Scalar<int>());
                return (list, s);
            });

            result.list.Should().HaveCount(1);
            result.list[0].id.Should().Be(3);
            result.s.Should().Be(4);
        }

        [Fact]
        public async Task ExeQueryMultipleAsync_SyncCallback_Works()
        {
            var sql = new SQLCmd("SELECT 5 AS id; SELECT 6 AS x");
            var result = await _db.ExeQueryMultipleAsync(sql, r => (r.List<IdRow>(), r.Scalar<int>()));

            result.Item1.Should().HaveCount(1);
            result.Item1[0].id.Should().Be(5);
            result.Item2.Should().Be(6);
        }
    }
}
