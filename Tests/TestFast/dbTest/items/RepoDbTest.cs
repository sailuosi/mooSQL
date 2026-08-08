using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using RepoDb;
namespace dbTest.items
{
    class RepoDbTest : ITest
    {
        public RepoDbTest()
        {
            SqliteBootstrap.Initialize();
        }
        public override void testQueryAnonymousResult()
        {
            using (var _connection = new SqliteConnection(sqlLiteDb))
            {
                _connection.Open();
                var sql = $"select * from TestEntity limit {listTake}";
                var list = _connection.ExecuteQuery(sql);
            }
        }

        public override string testQueryCondition()
        {
            var filter = GetMethodFilter();
            var sql = QueryGroup.Parse(filter).ToString();
            return sql;
        }

        public override void testQueryResult()
        {
            using (var _connection = new SqliteConnection(sqlLiteDb))
            {
                _connection.Open();
                var sql = $"select * from TestEntity limit {listTake}";
                var list = _connection.ExecuteQuery<TestEntity>(sql);//throw System.InvalidOperationException:“Compiler.DataReader.IsDbNull.FalseExpression: Failed to convert the value expression into its destination .NET CLR Type 'System.Boolean'. PropertyInfo: F_Bool (System.Boolean), DeclaringType: dbTest.TestEntity”
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
                    //var sql = $"select * from TestEntity where id={i}";
                    var list = _connection.Query<TestEntity>(b => b.Id == i);
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
