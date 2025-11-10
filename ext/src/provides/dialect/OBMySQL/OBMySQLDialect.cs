
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
    class OBMySQLDialect : Dialect
    {
        public OBMySQLDialect()
        {
            expression = new OBMySQLExpress(this);
            sentence = new MySQLSentence(this);

            function = new MySQLFunction();
            this.initVersions();
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
            return new DbBulkCopyFallback(this.dbInstance);
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
            int i = cmd.Parameters.Add(parameterName);
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

                try
                {
                    p1.DbType = (DbType)tc.ConvertFrom(theType.Name);
                }
                catch (Exception)
                {

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
        private MySqlBulkLoader GetBulkLoader(MySqlConnection conn, BulkBase bulk)
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

        private List<DBVersion> initVersions() { 
            var tar= new List<DBVersion> {
                new DBVersion(){
                    VersionCode = "1.0",
                    VersionName = "OceanBase 1.0",
                    MatchRegex = "\\.0\\.[0-9]+$",
                    ReleaseTime = new DateTime(2010, 6, 1),
                    Idx = 1,
                    Year = 2010,
                    Note = "首个分布式版本",
                    VersionNumber = 1.0
                },
                new DBVersion(){
                    VersionCode = "2.0",
                    VersionName = "OceanBase 2.0",
                    MatchRegex = "\\.0\\.[0-9]+$",
                    ReleaseTime = new DateTime(2016, 8, 1),
                    Idx = 2,
                    Year = 2016,
                    Note = "支持多租户架构",
                    VersionNumber = 2.0
                },
                new DBVersion(){
                    VersionCode = "3.0",
                    VersionName = "OceanBase 3.0",
                    MatchRegex = "\\.0\\.[0-9]+$",
                    ReleaseTime = new DateTime(2020, 6, 1),
                    Idx = 3,
                    Year = 2020,
                    Note = "全面兼容MySQL协议",
                    VersionNumber = 3.0
                },
                new DBVersion(){
                    VersionCode = "3.1",
                    VersionName = "OceanBase 3.1",
                    MatchRegex = "\\.1\\.[0-9]+$",
                    ReleaseTime = new DateTime(2021, 3, 1),
                    Idx = 4,
                    Year = 2021,
                    Note = "增强分布式事务性能",
                    VersionNumber = 3.1
                },
                new DBVersion(){
                    VersionCode = "3.2",
                    VersionName = "OceanBase 3.2",
                    MatchRegex = "\\.2\\.[0-9]+$",
                    ReleaseTime = new DateTime(2022, 5, 1),
                    Idx = 5,
                    Year = 2022,
                    Note = "引入HTAP混合负载能力",
                    VersionNumber = 3.2
                },
                new DBVersion(){
                    VersionCode = "4.0",
                    VersionName = "OceanBase 4.0",
                    MatchRegex = "\\.0\\.[0-9]+$",
                    ReleaseTime = new DateTime(2023, 5, 1),
                    Idx = 6,
                    Year = 2023,
                    Note = "全面兼容Oracle模式",
                    VersionNumber = 4.0
                },
                new DBVersion(){
                    VersionCode = "4.1",
                    VersionName = "OceanBase 4.1",
                    MatchRegex = "\\.1\\.[0-9]+$",
                    ReleaseTime = new DateTime(2024, 1, 1),
                    Idx = 7,
                    Year = 2024,
                    Note = "增强JSON处理能力",
                    VersionNumber = 4.1
                },
                new DBVersion(){
                    VersionCode = "4.2",
                    VersionName = "OceanBase 4.2",
                    MatchRegex = "\\.2\\.[0-9]+$",
                    ReleaseTime = new DateTime(2024, 6, 1),
                    Idx = 8,
                    Year = 2024,
                    Note = "支持自动回滚SQL生成",
                    VersionNumber = 4.2
                },
                new DBVersion(){
                    VersionCode = "4.3",
                    VersionName = "OceanBase 4.3",
                    MatchRegex = "\\.3\\.[0-9]+$",
                    ReleaseTime = new DateTime(2025, 3, 1),
                    Idx = 9,
                    Year = 2025,
                    Note = "优化LSM-Tree读放大问题",
                    VersionNumber = 4.3
                }
            };
            this.Versions = tar;
            return tar;
        }

    }
}
