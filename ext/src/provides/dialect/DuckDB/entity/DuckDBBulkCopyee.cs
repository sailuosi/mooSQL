#if NET6_0_OR_GREATER
using DuckDB.NET.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// DuckDB 批量写入：优先 <see cref="DuckDBConnection.CreateAppender"/>，失败时回退到多行 INSERT。
    /// </summary>
    public class DuckDBBulkCopyee : DbBulkCopy
    {
        public DuckDBBulkCopyee(DBInstance DB) : base(DB) { }

        public override BulkCopyResult WriteToServer(DataRow[] rows)
        {
            if (rows == null || rows.Length == 0)
                return new BulkCopyResult { count = 0 };
            return WriteViaAppender(rows[0].Table.Columns, rows.Length, i => rows[i]);
        }

        public override BulkCopyResult WriteToServer(DataTable table)
        {
            if (table == null || table.Rows.Count == 0)
                return new BulkCopyResult { count = 0 };
            return WriteViaAppender(table.Columns, table.Rows.Count, i => table.Rows[i]);
        }

        public override BulkCopyResult WriteToServer(IDataReader reader)
        {
            if (reader == null)
                return new BulkCopyResult { count = 0 };

            var table = new DataTable();
            table.Load(reader);
            return WriteToServer(table);
        }

        BulkCopyResult WriteViaAppender(DataColumnCollection columns, int rowCount, Func<int, DataRow> rowAt)
        {
            var fieldMap = BuildFieldMap(columns);
            if (fieldMap.Count == 0)
                return new BulkCopyResult { count = 0 };

            DuckDBConnection conn = null;
            var ownConn = false;
            try
            {
                conn = DB.dialect.getConnection() as DuckDBConnection;
                if (conn == null)
                    return FallbackInsert(columns, rowCount, rowAt, fieldMap);

                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();
                    ownConn = true;
                }

                using (var appender = conn.CreateAppender(TargetTableName))
                {
                    long total = 0;
                    for (int i = 0; i < rowCount; i++)
                    {
                        var row = rowAt(i);
                        var appRow = appender.CreateRow();
                        foreach (var map in fieldMap)
                        {
                            var v = row[map.Value];
                            if (v == null || v == DBNull.Value)
                                appRow.AppendNullValue();
                            else
                                AppendObject(appRow, v);
                        }
                        appRow.EndRow();
                        total++;
                    }
                    return new BulkCopyResult { count = total };
                }
            }
            catch (Exception)
            {
                // Appender 失败时回退多行 INSERT（不吞掉连接问题以外的诊断：由调用方看 count）
                return FallbackInsert(columns, rowCount, rowAt, fieldMap);
            }
            finally
            {
                if (ownConn && conn != null)
                    conn.Dispose();
            }
        }

        static void AppendObject(IDuckDBAppenderRow appRow, object v)
        {
            switch (v)
            {
                case bool b:
                    appRow.AppendValue(b);
                    break;
                case byte by:
                    appRow.AppendValue(by);
                    break;
                case short s:
                    appRow.AppendValue(s);
                    break;
                case int i:
                    appRow.AppendValue(i);
                    break;
                case long l:
                    appRow.AppendValue(l);
                    break;
                case float f:
                    appRow.AppendValue(f);
                    break;
                case double d:
                    appRow.AppendValue(d);
                    break;
                case decimal m:
                    appRow.AppendValue(m);
                    break;
                case DateTime dt:
                    appRow.AppendValue(dt);
                    break;
                case Guid g:
                    appRow.AppendValue(g);
                    break;
                case string str:
                    appRow.AppendValue(str);
                    break;
                case byte[] blob:
                    appRow.AppendValue(blob);
                    break;
                default:
                    appRow.AppendValue(Convert.ToString(v));
                    break;
            }
        }

        BulkCopyResult FallbackInsert(DataColumnCollection columns, int rowCount, Func<int, DataRow> rowAt, List<KeyValuePair<string, int>> fieldMap)
        {
            var kit = DB.useSQL();
            kit.setTable(TargetTableName);
            int total = 0;
            int batch = BatchSize > 0 ? BatchSize : 100;
            int cc = 0;
            for (int i = 0; i < rowCount; i++)
            {
                var row = rowAt(i);
                var k = kit.newRow();
                foreach (var map in fieldMap)
                {
                    var v = row[map.Value];
                    if (v == null || v == DBNull.Value)
                        k.set(map.Key, "null", false);
                    else
                        k.set(map.Key, v);
                }
                cc++;
                if (cc >= batch)
                {
                    total += kit.doInsert();
                    kit.clear();
                    kit.setTable(TargetTableName);
                    cc = 0;
                }
            }
            if (cc > 0)
                total += kit.doInsert();
            return new BulkCopyResult { count = total };
        }

        List<KeyValuePair<string, int>> BuildFieldMap(DataColumnCollection columns)
        {
            var res = new List<KeyValuePair<string, int>>();
            if (MapBag?.Maps != null && MapBag.Maps.Count > 0)
            {
                foreach (var map in MapBag.Maps)
                {
                    var field = map.tarName;
                    int srcIndex = map.srcIndex;
                    if (string.IsNullOrWhiteSpace(field))
                    {
                        if (map.tarIndex >= 0)
                            field = columns[map.tarIndex].ColumnName;
                        else if (srcIndex >= 0)
                            field = columns[srcIndex].ColumnName;
                        else if (!string.IsNullOrWhiteSpace(map.srcName))
                            field = map.srcName;
                        else
                            continue;
                    }
                    if (srcIndex < 0 && !string.IsNullOrWhiteSpace(map.srcName))
                        srcIndex = columns.IndexOf(map.srcName);
                    if (srcIndex < 0)
                        srcIndex = columns.IndexOf(field);
                    if (srcIndex < 0)
                        continue;
                    res.Add(new KeyValuePair<string, int>(field, srcIndex));
                }
                return res;
            }

            for (int i = 0; i < columns.Count; i++)
                res.Add(new KeyValuePair<string, int>(columns[i].ColumnName, i));
            return res;
        }
    }
}
#endif
