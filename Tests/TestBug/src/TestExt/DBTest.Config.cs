using mooSQL.data;
using System.IO;

namespace TestMooSQL.src
{
    partial class DBTest
    {
        /// <summary>
        /// 本地专项 SQLite 文件路径（与 TestDatabaseHelper / 槽位 0 共用）。
        /// </summary>
        public static string LocalSQLitePath =>
            Path.Combine(Path.GetTempPath(), "mooSQL_test_sqlite.db");

        /// <summary>
        /// 本地专项 SQLite 连接串（槽位 0 / useSQLiteDB / useRunDB 默认）。
        /// </summary>
        public static string LocalSQLiteConnStr =>
            "Data Source=" + LocalSQLitePath + ";Mode=ReadWriteCreate";

        /// <summary>
        /// 额外真实库槽位（供 setRunDB / useBusinessRunDB 切换）。不与槽位 0 绑定 failover。
        /// 槽位 1：本机业务 MSSQL（原 0 号 netapi）；槽位 2：远程备库。
        /// </summary>
        public static void addMoreDB()
        {
            var dbBiz = new DataBase();
            dbBiz.dbType = DataBaseType.MSSQL;
            dbBiz.DBConnectStr = "Enlist=false;Data Source=localhost;Database=netapi;User Id=test;Password=123456;Encrypt=True;TrustServerCertificate=True;";
            dbBiz.name = "1";
            dbBiz.version = "13.0";
            dbBiz.versionNumber = 13.0;
            cash.addDataBase(1, dbBiz);

            var db2 = new DataBase();
            db2.dbType = DataBaseType.MSSQL;
            db2.DBConnectStr = "Enlist=false;Data Source=10.16.10.218;Database=testme;User Id=hh;Password=mp@hh123456;Encrypt=True;TrustServerCertificate=True;";
            db2.name = "2";
            db2.version = "13.0";
            db2.versionNumber = 13.0;
            cash.addDataBase(2, db2);
        }
    }
}
