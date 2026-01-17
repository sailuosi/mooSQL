
using MySqlConnector;
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
    class MySQLDialect : Dialect
    {
        public MySQLDialect()
        {
            expression = new MySQLExpress(this);
            sentence = new MySQLSentence(this);
            function = new MySQLFunction();
            this.initDBVersion();
        }
        public override DbCommand getCommand()
        {
            return new MySqlCommand();
        }

        public override DbConnection getConnection()
        {
            return new MySqlConnection(db.DBConnectStr);
        }

        public override DbDataAdapter getDataAdapter()
        {
            return new MySqlDataAdapter();
        }

        public override DbCommandBuilder getCmdBuilder()
        {
            return new MySqlCommandBuilder();
        }

        public override DbBulkCopy GetBulkCopy()
        {
            return new MySQLBulkCopyee(this.dbInstance);
        }
        public override DbParameter AddCmdPara(DbCommand cmd, Parameter para)
        {
            if (cmd is MySqlCommand)
            {
                MySqlCommand qcmd = (MySqlCommand)cmd;
                return qcmd.Parameters.AddWithValue(para.key, para.val);
            }
            return null;
        }
        public override DbParameter AddCmdPara(DbCommand cmd, string parameterName, Type type, int size, string sourceColumn)
        {
            if (cmd is MySqlCommand)
            {
                MySqlCommand qcmd = (MySqlCommand)cmd;
                var parameter = new MySqlParameter();
                parameter.ParameterName = parameterName;
                parameter.DbType = GetDBTypeComm(type);
                parameter.Size = size;
                parameter.SourceColumn = sourceColumn;
                return qcmd.Parameters.Add(parameter);
            }
            int i= cmd.Parameters.Add(parameterName);
            return cmd.Parameters[i];
        }

        private DbType GetDBTypeComm(System.Type theType)
        {
            MySqlParameter p1;
            System.ComponentModel.TypeConverter tc;
            p1 = new MySqlParameter();
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

        public override int BulkInsert(BulkBase bk) {
            int cc = 0;
            try { 
                var conn = this.getConnection() as MySqlConnection;
                using (conn) {
                    MySqlBulkLoader bulkLoader = GetBulkLoader(conn, bk);
                    cc=bulkLoader.Load();
                }            
            }
            catch (Exception ex)
            {
                cc = this.BulkInsertByInsertValues(bk);
            }

            return cc;
        }



        public String SecureFilePriv { get; set; }
        public String DateTimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
        public List<string> Expressions { get; } = new List<string>();
        private string _fieldTerminator = ",";
        private char _fieldQuotationCharacter = '"';
        private char _escapeCharacter = '"';
        private string _lineTerminator = "\r\n";
        private MySqlBulkLoader GetBulkLoader(MySqlConnection conn,BulkBase bulk)
        {
            var bulkLoader = new MySqlBulkLoader(conn)
            {
                FieldTerminator = _fieldTerminator,
                FieldQuotationCharacter = _fieldQuotationCharacter,
                EscapeCharacter = _escapeCharacter,
                LineTerminator = _lineTerminator,
                FileName = ToCSV(bulk),
                NumberOfLinesToSkip = 0,
                TableName = bulk.tableName
            };
            foreach (DataColumn dbCol in bulk.bulkTarget.Columns)
            {
                bulkLoader.Columns.Add(dbCol.ColumnName);
            }

            bulkLoader.Expressions.AddRange(Expressions);

            return bulkLoader;
        }

        private string ToCSV(BulkBase bulk)
        {

            const string NULL_VALUE = "NULL";
            StringBuilder dataBuilder = new StringBuilder();
            foreach (DataRow row in bulk.bulkTarget.Rows)
            {
                var colIndex = 0;
                foreach (DataColumn dataColumn in bulk.bulkTarget.Columns)
                {
                    if (colIndex != 0) dataBuilder.Append(_fieldTerminator);

                    if (dataColumn.DataType == typeof(string)
                        && !row.IsNull(dataColumn)
                        && row[dataColumn].ToString().Contains(_fieldTerminator))
                    {
                        dataBuilder.AppendFormat("\"{0}\"", row[dataColumn].ToString().Replace("\"", "\"\""));
                    }
                    else if (dataColumn.DataType == typeof(DateTime) || dataColumn.DataType == typeof(DateTime?))
                    {
                        var originCell = row[dataColumn];
                        if (originCell is DBNull)
                        {
                            dataBuilder.Append(NULL_VALUE);
                        }
                        else
                        {
                            var dateCell = (DateTime)originCell;
                            var dateCellTime = dateCell.ToString(DateTimeFormat);
                            dataBuilder.Append(dateCellTime);
                        }
                    }
                    else if (dataColumn.DataType == typeof(bool) || dataColumn.DataType == typeof(bool?))
                    {
                        var originCell = row[dataColumn];
                        if (originCell is DBNull)
                        {
                            dataBuilder.Append(NULL_VALUE);
                        }
                        else
                        {
                            dataBuilder.Append(Convert.ToByte(originCell));
                        }
                    }
                    else if (row[dataColumn] is DBNull || dataColumn.AutoIncrement)
                    {
                        dataBuilder.Append(NULL_VALUE);
                    }
                    else
                    {
                        var colValStr = row[dataColumn]?.ToString() ?? NULL_VALUE;
                        dataBuilder.Append(colValStr);
                    }

                    colIndex++;
                }

                dataBuilder.Append(_lineTerminator);
            }

            var fileName = Guid.NewGuid().ToString("N") + ".csv";
            var fileDir = SecureFilePriv ?? AppDomain.CurrentDomain.BaseDirectory;
            fileName = Path.Combine(fileDir, fileName);
            File.WriteAllText(fileName, dataBuilder.ToString());
            return fileName;
        }

        private List<DBVersion> initDBVersion() {
            var tar= new List<DBVersion> {
                new DBVersion(){
                    VersionCode = "3.23",
                    VersionName = "MySQL 3.23",
                    MatchRegex = "^3\\.23\\.[0-9]+$",
                    ReleaseTime = new DateTime(2001, 1, 1),
                    Idx = 1,
                    Year = 2001,
                    Note = "首个稳定版，支持存储过程、触发器",
                    VersionNumber = 3.23
                },
                new DBVersion(){
                    VersionCode = "4.0",
                    VersionName = "MySQL 4.0",
                    MatchRegex = "^4\\.0\\.[0-9]+$",
                    ReleaseTime = new DateTime(2003, 3, 1),
                    Idx = 2,
                    Year = 2003,
                    Note = "集成InnoDB存储引擎，支持事务",
                    VersionNumber = 4.0
                },
                new DBVersion(){
                    VersionCode = "5.0",
                    VersionName = "MySQL 5.0",
                    MatchRegex = "^5\\.0\\.[0-9]+$",
                    ReleaseTime = new DateTime(2005, 10, 1),
                    Idx = 3,
                    Year = 2005,
                    Note = "引入视图、游标、XA事务",
                    VersionNumber = 5.0
                },
                new DBVersion(){
                    VersionCode = "5.1",
                    VersionName = "MySQL 5.1",
                    MatchRegex = "^5\\.1\\.[0-9]+$",
                    ReleaseTime = new DateTime(2008, 11, 1),
                    Idx = 4,
                    Year = 2008,
                    Note = "支持分区表、事件调度器",
                    VersionNumber = 5.1
                },
                new DBVersion(){
                    VersionCode = "5.5",
                    VersionName = "MySQL 5.5",
                    MatchRegex = "^5\\.5\\.[0-9]+$",
                    ReleaseTime = new DateTime(2010, 12, 1),
                    Idx = 5,
                    Year = 2010,
                    Note = "InnoDB成为默认引擎，支持UTF8MB4",
                    VersionNumber = 5.5
                },
                new DBVersion(){
                    VersionCode = "5.6",
                    VersionName = "MySQL 5.6",
                    MatchRegex = "^5\\.6\\.[0-9]+$",
                    ReleaseTime = new DateTime(2013, 2, 1),
                    Idx = 6,
                    Year = 2013,
                    Note = "GTID复制、NoSQL接口支持",
                    VersionNumber = 5.6
                },
                new DBVersion(){
                    VersionCode = "5.7",
                    VersionName = "MySQL 5.7",
                    MatchRegex = "^5\\.7\\.[0-9]+$",
                    ReleaseTime = new DateTime(2015, 10, 1),
                    Idx = 7,
                    Year = 2015,
                    Note = "引入JSON数据类型、多源复制",
                    VersionNumber = 5.7
                },
                new DBVersion(){
                    VersionCode = "8.0",
                    VersionName = "MySQL 8.0",
                    MatchRegex = "^8\\.0\\.[0-9]+$",
                    ReleaseTime = new DateTime(2018, 4, 1),
                    Idx = 8,
                    Year = 2018,
                    Note = "窗口函数、CTE、数据字典",
                    VersionNumber = 8.0
                },
                new DBVersion(){
                    VersionCode = "8.4",
                    VersionName = "MySQL 8.4",
                    MatchRegex = "^8\\.4\\.[0-9]+$",
                    ReleaseTime = new DateTime(2024, 4, 1),
                    Idx = 9,
                    Year = 2024,
                    Note = "LTS版本，优化JSON和GIS功能",
                    VersionNumber = 8.4
                },
                new DBVersion(){
                    VersionCode = "9.0",
                    VersionName = "MySQL 9.0",
                    MatchRegex = "^9\\.0\\.[0-9]+$",
                    ReleaseTime = new DateTime(2024, 10, 1),
                    Idx = 10,
                    Year = 2024,
                    Note = "创新版本，增强云原生支持",
                    VersionNumber = 9.0
                }
            };
            this.Versions = tar;
            return tar;
        }
    }


}
