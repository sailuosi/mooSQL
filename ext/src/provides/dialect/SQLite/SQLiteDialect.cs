
#if NET5_0_OR_GREATER
using Microsoft.Data.Sqlite;
#else
using System.Data.SQLite;
#endif

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    class SQLiteDialect : Dialect
    {
        public SQLiteDialect()
        {
            expression = new SQLiteExpress(this);
            sentence = new SQLiteSentence(this);

            function = new SQLLiteFunction();

            this.initVersions();
        }
#if NET5_0_OR_GREATER
        public override DbCommand getCommand()
        {
            return new SqliteCommand();
        }

        public override DbConnection getConnection()
        {
            return new SqliteConnection(db.DBConnectStr);
        }

        public override DbCommandBuilder getCmdBuilder()
        {
            return new SooSQLiteCommandBuilder();
        }

        public override DbDataAdapter getDataAdapter()
        {
            return new SooSQLiteDataAdapter();
        }
        public override DbBulkCopy GetBulkCopy()
        {
            return new DbBulkCopyFallback(this.dbInstance);
        }
        public override DbParameter AddCmdPara(DbCommand cmd, Parameter para)
        {
            if (cmd is SqliteCommand cmdda)
            {
                return cmdda.Parameters.AddWithValue(para.key, para.val);
            }
            return null;
        }
        public override DbParameter AddCmdPara(DbCommand cmd, string parameterName, Type type, int size, string sourceColumn)
        {
            if (cmd is SqliteCommand scmd)
            {
                var parameter = new SqliteParameter();
                parameter.ParameterName = parameterName;
                parameter.DbType = GetDBTypeComm(type);
                parameter.Size = size;
                parameter.SourceColumn = sourceColumn;
                return scmd.Parameters.Add(parameter);
            }
            int i = cmd.Parameters.Add(parameterName);
            return cmd.Parameters[i];
        }

        private DbType GetDBTypeComm(System.Type theType)
        {
            SqliteParameter p1;
            System.ComponentModel.TypeConverter tc;
            p1 = new SqliteParameter();
            tc = System.ComponentModel.TypeDescriptor.GetConverter(p1.DbType);
            if (tc.CanConvertFrom(theType))
            {
                p1.DbType = (DbType)tc.ConvertFrom(theType.Name);
            }
            else
            {
                //Try brute force
                try
                {
                    p1.DbType = (DbType)tc.ConvertFrom(theType.Name);
                }
                catch (Exception)
                {
                    //Do Nothing; will return NVarChar as default
                }
            }
            return p1.DbType;
        }
#else
        public override DbCommand getCommand()
        {
            return new SQLiteCommand();
        }

        public override DbConnection getConnection()
        {
            return new SQLiteConnection(db.DBConnectStr);
        }

        public override DbCommandBuilder getCmdBuilder()
        {
            return new SQLiteCommandBuilder();
        }
        public override DbDataAdapter getDataAdapter()
        {
            return new SQLiteDataAdapter();
        }
        public override DbBulkCopy GetBulkCopy()
        {
            return new DbBulkCopyFallback(this.dbInstance);
        }
        public override DbParameter AddCmdPara(DbCommand cmd, Parameter para)
        {
            if (cmd is SQLiteCommand cmdda)
            {
                return cmdda.Parameters.AddWithValue(para.key, para.val);
            }
            return null;
        }

        public override DbParameter AddCmdPara(DbCommand cmd, string parameterName, Type type, int size, string sourceColumn)
        {
            if (cmd is SQLiteCommand scmd)
            {
                var parameter = new SQLiteParameter();
                parameter.ParameterName = parameterName;
                parameter.DbType = GetDBTypeComm(type);
                parameter.Size = size;
                parameter.SourceColumn = sourceColumn;
                scmd.Parameters.Add(parameter);
                return parameter;
            }
            int i = cmd.Parameters.Add(parameterName);
            return cmd.Parameters[i];
        }

        private DbType GetDBTypeComm(System.Type theType)
        {
            SQLiteParameter p1;
            System.ComponentModel.TypeConverter tc;
            p1 = new SQLiteParameter();
            tc = System.ComponentModel.TypeDescriptor.GetConverter(p1.DbType);
            if (tc.CanConvertFrom(theType))
            {
                p1.DbType = (DbType)tc.ConvertFrom(theType.Name);
            }
            else
            {
                //Try brute force
                try
                {
                    p1.DbType = (DbType)tc.ConvertFrom(theType.Name);
                }
                catch (Exception)
                {
                    //Do Nothing; will return NVarChar as default
                }
            }
            return p1.DbType;
        }
#endif



        private List<DBVersion> initVersions() {

            var tar= new List<DBVersion> {
                new DBVersion(){
                    VersionCode = "3.0",
                    VersionName = "SQLite 3.0",
                    MatchRegex = "\\.0\\.[0-9]+$",
                    ReleaseTime = new DateTime(2004, 6, 1),
                    Idx = 1,
                    Year = 2004,
                    Note = "首个3.x版本，全新文件格式",
                    VersionNumber = 3.0
                },
                new DBVersion(){
                    VersionCode = "3.3",
                    VersionName = "SQLite 3.3",
                    MatchRegex = "\\.3\\.[0-9]+$",
                    ReleaseTime = new DateTime(2006, 1, 1),
                    Idx = 2,
                    Year = 2006,
                    Note = "支持外键约束",
                    VersionNumber = 3.3
                },
                new DBVersion(){
                    VersionCode = "3.6",
                    VersionName = "SQLite 3.6",
                    MatchRegex = "\\.6\\.[0-9]+$",
                    ReleaseTime = new DateTime(2008, 7, 1),
                    Idx = 3,
                    Year = 2008,
                    Note = "引入WAL日志模式",
                    VersionNumber = 3.6
                },
                new DBVersion(){
                    VersionCode = "3.7",
                    VersionName = "SQLite 3.7",
                    MatchRegex = "\\.7\\.[0-9]+$",
                    ReleaseTime = new DateTime(2010, 7, 1),
                    Idx = 4,
                    Year = 2010,
                    Note = "支持多线程",
                    VersionNumber = 3.7
                },
                new DBVersion(){
                    VersionCode = "3.8",
                    VersionName = "SQLite 3.8",
                    MatchRegex = "\\.8\\.[0-9]+$",
                    ReleaseTime = new DateTime(2013, 8, 1),
                    Idx = 5,
                    Year = 2013,
                    Note = "JSON1扩展支持",
                    VersionNumber = 3.8
                },
                new DBVersion(){
                    VersionCode = "3.24",
                    VersionName = "SQLite 3.24",
                    MatchRegex = "\\.24\\.[0-9]+$",
                    ReleaseTime = new DateTime(2018, 6, 1),
                    Idx = 6,
                    Year = 2018,
                    Note = "窗口函数支持",
                    VersionNumber = 3.24
                },
                new DBVersion(){
                    VersionCode = "3.35",
                    VersionName = "SQLite 3.35",
                    MatchRegex = "\\.35\\.[0-9]+$",
                    ReleaseTime = new DateTime(2021, 3, 1),
                    Idx = 7,
                    Year = 2021,
                    Note = "支持ALTER TABLE DROP COLUMN",
                    VersionNumber = 3.35
                },
                new DBVersion(){
                    VersionCode = "3.39",
                    VersionName = "SQLite 3.39",
                    MatchRegex = "\\.39\\.[0-9]+$",
                    ReleaseTime = new DateTime(2022, 6, 1),
                    Idx = 8,
                    Year = 2022,
                    Note = "JSONB二进制格式支持",
                    VersionNumber = 3.39
                },
                new DBVersion(){
                    VersionCode = "3.42",
                    VersionName = "SQLite 3.42",
                    MatchRegex = "\\.42\\.[0-9]+$",
                    ReleaseTime = new DateTime(2023, 5, 1),
                    Idx = 9,
                    Year = 2023,
                    Note = "增强的SQL函数",
                    VersionNumber = 3.42
                },
                new DBVersion(){
                    VersionCode = "3.45",
                    VersionName = "SQLite 3.45",
                    MatchRegex = "\\.45\\.[0-9]+$",
                    ReleaseTime = new DateTime(2024, 1, 1),
                    Idx = 10,
                    Year = 2024,
                    Note = "最新稳定版，性能优化",
                    VersionNumber = 3.45
                }
            };
            this.Versions = tar;
            return tar;
        }





    }
}
