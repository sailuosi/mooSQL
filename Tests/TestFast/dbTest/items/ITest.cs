using System;
using System.IO;
using System.Linq.Expressions;

namespace dbTest.items
{
    public abstract class ITest
    {
        /// <summary>
        /// 固定绝对路径（%TEMP%），供主进程 InitData 与 BenchmarkDotNet 子进程共用。
        /// 勿写死盘符；也勿用 BaseDirectory（BDN 工作目录不同，会连到空库 → no such table）。
        /// </summary>
        public static string sqlLiteDb =
            "Data Source=" + Path.Combine(Path.GetTempPath(), "mooSQL_dbTest_sqlite.db") + ";Mode=ReadWriteCreate";
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
