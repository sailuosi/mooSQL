using mooSQL.data;

using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    class NpgsqlDialect : Dialect
    {
        public NpgsqlDialect()
        {
            expression = new NpgsqlExpress(this);

            sentence = new NpgSentence(this);
            function = new NpgSQLFunction();
            initVersions();
        }

        public override DbCommandBuilder getCmdBuilder()
        {
            return new NpgsqlCommandBuilder();
        }

        public override DbCommand getCommand()
        {
            return new NpgsqlCommand();
        }

        public override DbConnection getConnection()
        {
            return new NpgsqlConnection(db.DBConnectStr);
        }

        public override DbDataAdapter getDataAdapter()
        {
            return new NpgsqlDataAdapter();
        }
        public override DbBulkCopy GetBulkCopy()
        {
            return new NpgBulkCopyee(this.dbInstance);
        }
        public override DbParameter AddCmdPara(DbCommand cmd, Parameter para)
        {
            if (cmd is NpgsqlCommand)
            {
                NpgsqlCommand qcmd = (NpgsqlCommand)cmd;
                NpgsqlParameter para2 = new NpgsqlParameter();
                para2.Value = para.val;
                para2.ParameterName = para.key;
                return qcmd.Parameters.Add(para2);
            }
            return null;
        }
        public override DbParameter AddCmdPara(DbCommand cmd, string parameterName, Type type, int size, string sourceColumn)
        {
            if (cmd is NpgsqlCommand)
            {
                NpgsqlCommand qcmd = (NpgsqlCommand)cmd;
                qcmd.Parameters.Add(parameterName, GetDBType(type), size, sourceColumn);
            }
            int i = cmd.Parameters.Add(parameterName);
            return cmd.Parameters[i];
        }
        private NpgsqlDbType GetDBType(System.Type theType)
        {
            NpgsqlParameter p1;
            System.ComponentModel.TypeConverter tc;
            p1 = new NpgsqlParameter();
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
            return p1.NpgsqlDbType;
        }

        private List<DBVersion> initVersions()
        {
            var tar= new List<DBVersion> {
                new DBVersion(){
                    VersionCode = "7.1",
                    VersionName = "PostgreSQL 7.1",
                    MatchRegex = "\\.1\\.[0-9]+$",
                    ReleaseTime = new DateTime(2001, 4, 1),
                    Idx = 1,
                    Year = 2001,
                    Note = "首个支持外键约束的版本",
                    VersionNumber = 7.1
                },
                new DBVersion(){
                    VersionCode = "8.0",
                    VersionName = "PostgreSQL 8.0",
                    MatchRegex = "\\.0\\.[0-9]+$",
                    ReleaseTime = new DateTime(2005, 1, 1),
                    Idx = 2,
                    Year = 2005,
                    Note = "引入原生Windows支持",
                    VersionNumber = 8.0
                },
                new DBVersion(){
                    VersionCode = "9.0",
                    VersionName = "PostgreSQL 9.0",
                    MatchRegex = "\\.0\\.[0-9]+$",
                    ReleaseTime = new DateTime(2010, 9, 1),
                    Idx = 3,
                    Year = 2010,
                    Note = "支持流复制和热备",
                    VersionNumber = 9.0
                },
                new DBVersion(){
                    VersionCode = "10",
                    VersionName = "PostgreSQL 10",
                    MatchRegex = "\\.[0-9]+$",
                    ReleaseTime = new DateTime(2017, 10, 1),
                    Idx = 4,
                    Year = 2017,
                    Note = "引入逻辑复制和声明式分区",
                    VersionNumber = 10.0
                },
                new DBVersion(){
                    VersionCode = "11",
                    VersionName = "PostgreSQL 11",
                    MatchRegex = "\\.[0-9]+$",
                    ReleaseTime = new DateTime(2018, 10, 1),
                    Idx = 5,
                    Year = 2018,
                    Note = "支持存储过程和并行查询优化",
                    VersionNumber = 11.0
                },
                new DBVersion(){
                    VersionCode = "12",
                    VersionName = "PostgreSQL 12",
                    MatchRegex = "\\.[0-9]+$",
                    ReleaseTime = new DateTime(2019, 10, 1),
                    Idx = 6,
                    Year = 2019,
                    Note = "改进索引和分区性能",
                    VersionNumber = 12.0
                },
                new DBVersion(){
                    VersionCode = "13",
                    VersionName = "PostgreSQL 13",
                    MatchRegex = "\\.[0-9]+$",
                    ReleaseTime = new DateTime(2020, 9, 1),
                    Idx = 7,
                    Year = 2020,
                    Note = "增强并行清理和增量排序",
                    VersionNumber = 13.0
                },
                new DBVersion(){
                    VersionCode = "14",
                    VersionName = "PostgreSQL 14",
                    MatchRegex = "\\.[0-9]+$",
                    ReleaseTime = new DateTime(2021, 9, 1),
                    Idx = 8,
                    Year = 2021,
                    Note = "改进连接并发和压缩性能",
                    VersionNumber = 14.0
                },
                new DBVersion(){
                    VersionCode = "15",
                    VersionName = "PostgreSQL 15",
                    MatchRegex = "\\.[0-9]+$",
                    ReleaseTime = new DateTime(2022, 10, 1),
                    Idx = 9,
                    Year = 2022,
                    Note = "增强JSON处理和安全功能",
                    VersionNumber = 15.0
                },
                new DBVersion(){
                    VersionCode = "16",
                    VersionName = "PostgreSQL 16",
                    MatchRegex = "\\.[0-9]+$",
                    ReleaseTime = new DateTime(2023, 9, 1),
                    Idx = 10,
                    Year = 2023,
                    Note = "引入SIMD加速和逻辑复制改进",
                    VersionNumber = 16.0
                }
            };
            this.Versions = tar;
            return tar;
        }
    }
}
