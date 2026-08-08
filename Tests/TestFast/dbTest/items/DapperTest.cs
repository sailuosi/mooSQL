using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
namespace dbTest.items
{
    public class DapperTest : ITest
    {
        public override void testQueryAnonymousResult()
        {
            using (var _connection = new SqliteConnection(sqlLiteDb))
            {
                _connection.Open();
                var sql = $"select * from TestEntity limit {listTake}";
                var list = _connection.Query(sql);
            }
        }

        public override string testQueryCondition()
        {
            return "";
        }

        public override void testQueryResult()
        {
            using (var _connection = new SqliteConnection(sqlLiteDb))
            {
                _connection.Open();
                var sql = $"select * from TestEntity limit {listTake}";
                var list = _connection.Query<TestEntity>(sql);
            }
        }
        public override void testQueryJoin()
        {

        }
        public override void testQueryLoop()
        {
            for (var i = 0; i < 20; i++)
            {
                using (var _connection = new SqliteConnection(sqlLiteDb))
                {
                    _connection.Open();
                    var sql = $"select * from TestEntity where id={i}";
                    var list = _connection.Query(sql);
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
