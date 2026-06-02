using System;
using System.Collections;

using System.Data;
using System.Data.Common;
using System.IO;

using System.Threading;
using System.Threading.Tasks;

namespace mooSQL.data.context
{
    /// <summary>
    /// 类型 DataReaderWrapper。
    /// </summary>
    public class DataReaderWrapper : DbDataReader
    {
        /// <summary>
        /// 属性 SourceDataReader（DbDataReader）。
        /// </summary>
        public DbDataReader SourceDataReader { get; }

        /// <summary>
        /// 属性 command（DbCommand）。
        /// </summary>
        public DbCommand command { get; set; }
        /// <summary>
        /// 执行上下文，由于用户自定义读取时，需要手动自己关闭
        /// </summary>
        public ExeContext context { get; set; }

        /// <summary>
        /// 按列序号读取字段值（转发内部 DataReader）。
        /// </summary>
        public override object this[int ordinal] => SourceDataReader[ordinal];

        /// <summary>
        /// 按列名读取字段值（转发内部 DataReader）。
        /// </summary>
        public override object this[string name] => SourceDataReader[name];

        /// <summary>
        /// 转发底层 DataReader 的 Depth。
        /// </summary>
        public override int Depth => SourceDataReader.Depth;

        /// <summary>
        /// 转发底层 DataReader 的 FieldCount。
        /// </summary>
        public override int FieldCount => SourceDataReader.FieldCount;

        /// <summary>
        /// 转发底层 DataReader 的 HasRows。
        /// </summary>
        public override bool HasRows => SourceDataReader.HasRows;

        /// <summary>
        /// 转发底层 DataReader 的 IsClosed。
        /// </summary>
        public override bool IsClosed => SourceDataReader.IsClosed;

        /// <summary>
        /// 转发底层 DataReader 的 RecordsAffected。
        /// </summary>
        public override int RecordsAffected => SourceDataReader.RecordsAffected;

        /// <summary>
        /// 属性 ResultIndex（int）。
        /// </summary>
        public int ResultIndex { get; private set; }

        /// <summary>
        /// 初始化 DataReaderWrapper（构造）。
        /// </summary>
        public DataReaderWrapper(DbDataReader dbDataReader)
        {
            SourceDataReader = dbDataReader;
        }
        /// <summary>
        /// 初始化 DataReaderWrapper（构造）。
        /// </summary>
        public DataReaderWrapper(DbDataReader dbDataReader,DbCommand cmd)
        {
            SourceDataReader = dbDataReader;
            this.command = cmd;
        }

        /// <summary>
        /// 初始化 DataReaderWrapper（构造）。
        /// </summary>
        public DataReaderWrapper(DbDataReader dbDataReader, DbCommand cmd,ExeContext cont)
        {
            SourceDataReader = dbDataReader;
            this.command = cmd;
            this.context = cont;
        }

        /// <summary>
        /// NextResult 方法（返回 bool）。
        /// </summary>
        public override bool NextResult()
        {
            ResultIndex++;
            return SourceDataReader.NextResult();
        }

        /// <summary>
        /// NextResultAsync 方法（返回 Task<bool>）。
        /// </summary>
        public override Task<bool> NextResultAsync(CancellationToken cancellationToken)
        {
            ResultIndex++;
            return SourceDataReader.NextResultAsync(cancellationToken);
        }

        /// <summary>
        /// 获取Boolean。
        /// </summary>
        public override bool GetBoolean(int ordinal) => SourceDataReader.GetBoolean(ordinal);

        /// <summary>
        /// 获取Byte。
        /// </summary>
        public override byte GetByte(int ordinal) => SourceDataReader.GetByte(ordinal);

        /// <summary>
        /// 获取Bytes。
        /// </summary>
        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
            => SourceDataReader.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);

        /// <summary>
        /// 获取Char。
        /// </summary>
        public override char GetChar(int ordinal) => SourceDataReader.GetChar(ordinal);

        /// <summary>
        /// 获取Chars。
        /// </summary>
        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
        => SourceDataReader.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);

        /// <summary>
        /// 获取DataTypeName。
        /// </summary>
        public override string GetDataTypeName(int ordinal) => SourceDataReader.GetDataTypeName(ordinal);

        /// <summary>
        /// 获取DateTime。
        /// </summary>
        public override DateTime GetDateTime(int ordinal) => SourceDataReader.GetDateTime(ordinal);

        /// <summary>
        /// 获取Decimal。
        /// </summary>
        public override decimal GetDecimal(int ordinal) => SourceDataReader.GetDecimal(ordinal);

        /// <summary>
        /// 获取Double。
        /// </summary>
        public override double GetDouble(int ordinal) => SourceDataReader.GetDouble(ordinal);

        /// <summary>
        /// 获取Enumerator。
        /// </summary>
        public override IEnumerator GetEnumerator() => SourceDataReader.GetEnumerator();

        /// <summary>
        /// 获取FieldType。
        /// </summary>
        public override Type GetFieldType(int ordinal) => SourceDataReader.GetFieldType(ordinal);

        /// <summary>
        /// 获取Float。
        /// </summary>
        public override float GetFloat(int ordinal) => SourceDataReader.GetFloat(ordinal);

        /// <summary>
        /// 获取Guid。
        /// </summary>
        public override Guid GetGuid(int ordinal) => SourceDataReader.GetGuid(ordinal);

        /// <summary>
        /// 获取Int16。
        /// </summary>
        public override short GetInt16(int ordinal) => SourceDataReader.GetInt16(ordinal);

        /// <summary>
        /// 获取Int32。
        /// </summary>
        public override int GetInt32(int ordinal) => SourceDataReader.GetInt32(ordinal);

        /// <summary>
        /// 获取Int64。
        /// </summary>
        public override long GetInt64(int ordinal) => SourceDataReader.GetInt64(ordinal);

        /// <summary>
        /// 获取Name。
        /// </summary>
        public override string GetName(int ordinal) => SourceDataReader.GetName(ordinal);

        /// <summary>
        /// 获取Ordinal。
        /// </summary>
        public override int GetOrdinal(string name) => SourceDataReader.GetOrdinal(name);

        /// <summary>
        /// 获取String。
        /// </summary>
        public override string GetString(int ordinal) => SourceDataReader.GetString(ordinal);

        /// <summary>
        /// 获取Value。
        /// </summary>
        public override object GetValue(int ordinal) => SourceDataReader.GetValue(ordinal);

        /// <summary>
        /// 获取Values。
        /// </summary>
        public override int GetValues(object[] values) => SourceDataReader.GetValues(values);

        /// <summary>
        /// 判断是否为DBNull。
        /// </summary>
        public override bool IsDBNull(int ordinal) => SourceDataReader.IsDBNull(ordinal);

        /// <summary>
        /// Read 方法（返回 bool）。
        /// </summary>
        public override bool Read() => SourceDataReader.Read();
        /// <summary>
        /// Close 方法。
        /// </summary>
        public override void Close() => SourceDataReader.Close();
        /// <summary>
        /// Equals 方法（返回 bool）。
        /// </summary>
        public override bool Equals(object obj) => SourceDataReader.Equals(obj);
        /// <summary>
        /// 转发底层 DataReader 的成员。
        /// </summary>
        public override T GetFieldValue<T>(int ordinal) => SourceDataReader.GetFieldValue<T>(ordinal);
        /// <summary>
        /// 转发底层 DataReader 的成员。
        /// </summary>
        public override Task<T> GetFieldValueAsync<T>(int ordinal, CancellationToken cancellationToken) => SourceDataReader.GetFieldValueAsync<T>(ordinal, cancellationToken);
        /// <summary>
        /// 获取HashCode。
        /// </summary>
        public override int GetHashCode() => SourceDataReader.GetHashCode();
        /// <summary>
        /// 获取ProviderSpecificFieldType。
        /// </summary>
        public override Type GetProviderSpecificFieldType(int ordinal) => SourceDataReader.GetProviderSpecificFieldType(ordinal);
        /// <summary>
        /// 获取ProviderSpecificValue。
        /// </summary>
        public override object GetProviderSpecificValue(int ordinal) => SourceDataReader.GetProviderSpecificValue(ordinal);
        /// <summary>
        /// 获取ProviderSpecificValues。
        /// </summary>
        public override int GetProviderSpecificValues(object[] values) => SourceDataReader.GetProviderSpecificValues(values);
        /// <summary>
        /// 获取SchemaTable。
        /// </summary>
        public override DataTable GetSchemaTable() => SourceDataReader.GetSchemaTable();
        /// <summary>
        /// 获取Stream。
        /// </summary>
        public override Stream GetStream(int ordinal) => SourceDataReader.GetStream(ordinal);
        /// <summary>
        /// 获取TextReader。
        /// </summary>
        public override TextReader GetTextReader(int ordinal) => SourceDataReader.GetTextReader(ordinal);
        /// <summary>
        /// InitializeLifetimeService 方法（返回 object）。
        /// </summary>
        public override object InitializeLifetimeService() => SourceDataReader.InitializeLifetimeService();
        /// <summary>
        /// 判断是否为DBNullAsync。
        /// </summary>
        public override Task<bool> IsDBNullAsync(int ordinal, CancellationToken cancellationToken) => SourceDataReader.IsDBNullAsync(ordinal, cancellationToken);
        /// <summary>
        /// ReadAsync 方法（返回 Task<bool>）。
        /// </summary>
        public override Task<bool> ReadAsync(CancellationToken cancellationToken) => SourceDataReader.ReadAsync(cancellationToken);
        /// <summary>
        /// 转换为String。
        /// </summary>
        public override string ToString() => SourceDataReader.ToString();
    }
}