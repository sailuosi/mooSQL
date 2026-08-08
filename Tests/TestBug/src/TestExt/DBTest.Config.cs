using mooSQL.data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestMooSQL.src
{
    partial class DBTest
    {

        public static void addMoreDB() {
            //

            var db2 = new DataBase();
            db2.dbType = DataBaseType.MSSQL;
            db2.DBConnectStr = "Enlist=false;Data Source=10.16.10.218;Database=testme;User Id=hh;Password=mp@hh123456;Encrypt=True;TrustServerCertificate=True;";
            db2.name = "1";
            db2.version = "13.0";
            db2.versionNumber = 13.0;
            //db1.databaseName = "ZHXT_Tar";

            cash.addDataBase(1, db2);

        }

    }
}
