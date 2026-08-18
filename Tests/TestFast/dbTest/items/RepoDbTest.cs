using System;
using System.Globalization;
using System.Linq;
using Microsoft.Data.Sqlite;
using RepoDb;
using RepoDb.Interfaces;
using RepoDb.Options;

namespace dbTest.items
{
    /// <summary>
    /// SQLite + Microsoft.Data.Sqlite 读回类型松散：BOOLEAN→Int64、DATETIME→string 等，
    /// RepoDb 编译映射不做这些强制转换。类型级 PropertyHandler 仅作用于 RepoDb，不影响其它 ORM。
    /// </summary>
    public sealed class SqliteBooleanPropertyHandler : IPropertyHandler<object, bool>
    {
        public bool Get(object input, PropertyHandlerGetOptions options)
        {
            if (input == null || input is DBNull)
                return false;
            if (input is bool b)
                return b;
            if (input is string s)
                return s == "1"
                    || string.Equals(s, "true", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(s, "yes", StringComparison.OrdinalIgnoreCase);
            return Convert.ToInt64(input) != 0;
        }

        public object Set(bool input, PropertyHandlerSetOptions options) => input ? 1L : 0L;
    }

    public sealed class SqliteNullableDateTimePropertyHandler : IPropertyHandler<object, DateTime?>
    {
        public DateTime? Get(object input, PropertyHandlerGetOptions options)
        {
            if (input == null || input is DBNull)
                return null;
            if (input is DateTime dt)
                return dt;
            if (input is string s)
            {
                if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
                    || DateTime.TryParse(s, CultureInfo.CurrentCulture, DateTimeStyles.None, out parsed))
                    return parsed;
                return null;
            }
            return Convert.ToDateTime(input, CultureInfo.InvariantCulture);
        }

        public object Set(DateTime? input, PropertyHandlerSetOptions options) => input;
    }

    public sealed class SqliteNullableDecimalPropertyHandler : IPropertyHandler<object, decimal?>
    {
        public decimal? Get(object input, PropertyHandlerGetOptions options)
        {
            if (input == null || input is DBNull)
                return null;
            if (input is decimal d)
                return d;
            if (input is string s && decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                return parsed;
            return Convert.ToDecimal(input, CultureInfo.InvariantCulture);
        }

        public object Set(decimal? input, PropertyHandlerSetOptions options) => input;
    }

    public sealed class SqliteNullableFloatPropertyHandler : IPropertyHandler<object, float?>
    {
        public float? Get(object input, PropertyHandlerGetOptions options)
        {
            if (input == null || input is DBNull)
                return null;
            if (input is float f)
                return f;
            if (input is double db)
                return (float)db;
            return Convert.ToSingle(input, CultureInfo.InvariantCulture);
        }

        public object Set(float? input, PropertyHandlerSetOptions options) => input;
    }

    /// <summary>
    /// RepoDB 基准适配器。
    /// </summary>
    public class RepoDbTest : ITest
    {
        private static readonly object InitGate = new();
        private static bool _initialized;

        public RepoDbTest()
        {
            EnsureInitialized();
        }

        private static void EnsureInitialized()
        {
            if (_initialized)
                return;
            lock (InitGate)
            {
                if (_initialized)
                    return;

                GlobalConfiguration.Setup().UseSqlite();
                PropertyHandlerMapper.Add(typeof(bool), new SqliteBooleanPropertyHandler(), true);
                PropertyHandlerMapper.Add(typeof(DateTime?), new SqliteNullableDateTimePropertyHandler(), true);
                PropertyHandlerMapper.Add(typeof(decimal?), new SqliteNullableDecimalPropertyHandler(), true);
                PropertyHandlerMapper.Add(typeof(float?), new SqliteNullableFloatPropertyHandler(), true);
                _initialized = true;
            }
        }

        public override void testQueryAnonymousResult()
        {
            using (var connection = new SqliteConnection(sqlLiteDb))
            {
                connection.Open();
                var sql = $"select * from TestEntity limit {listTake}";
                var list = connection.ExecuteQuery(sql);
                _ = list;
            }
        }

        public override string testQueryCondition()
        {
            var filter = GetMethodFilter();
            return QueryGroup.Parse(filter).ToString();
        }

        public override void testQueryResult()
        {
            using (var connection = new SqliteConnection(sqlLiteDb))
            {
                connection.Open();
                var sql = $"select * from TestEntity limit {listTake}";
                var list = connection.ExecuteQuery<TestEntity>(sql).ToList();
                _ = list.Count;
            }
        }

        public override void testQueryJoin()
        {
        }

        public override void testQueryLoop()
        {
            for (var i = 0; i < 20; i++)
            {
                using (var connection = new SqliteConnection(sqlLiteDb))
                {
                    connection.Open();
                    var list = connection.Query<TestEntity>(b => b.Id == i).ToList();
                    _ = list.Count;
                }
            }
        }

        public override void testInclude()
        {
        }

        public override void testInsert()
        {
        }
    }
}
