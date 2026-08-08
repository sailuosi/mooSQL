using System;
using System.Linq.Expressions;

namespace dbTest.items
{
    public abstract class ITest
    {
        //public static string sqlLiteDb = $"Data Source={AppDomain.CurrentDomain.BaseDirectory}sqlliteTest.db;";
        public static string sqlLiteDb = "Data Source=d:\\sqlliteTest.db;";
        public virtual void testQueryResult() { }
        public virtual void testQueryAnonymousResult() { }
        public virtual string testQueryCondition() { return ""; }
        public virtual string testQueryMethodCondition()
        {
            return "";
        }
        public virtual void testQueryLoop() { }
        public virtual void testInclude()
        {

        }
        public virtual void testQueryJoin()
        {

        }
        public virtual void testInsert()
        {

        }
        public virtual void testThread()
        {

        }
        //public abstract void testQueryJoin();
        protected Expression<Func<TestEntity, bool>> GetSelectFilter()
        {
            return b => b.F_String == "111" && b.F_Decimal > 0 && b.F_Bool == true && b.F_String.StartsWith("abc");
        }
        protected Expression<Func<TestEntity, bool>> GetMethodFilter()
        {
            return b => b.F_String.StartsWith("abc") && b.F_String.EndsWith("ddd") && b.F_String.Contains("333");
        }
        protected int listTake = 100;
    }

}
