
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using mooSQL.data.model;

namespace mooSQL.data
{
    /// <summary>
    /// Jet SQL (Access / Excel) 语句方言：通过 OleDb GetSchema 获取表与列信息，无独立“数据库列表”。
    /// </summary>
    public class JetSQLSentence : SQLSentence
    {
        public JetSQLSentence(Dialect dia) : base(dia) { }

        public override string GetDataBaseSql => "SELECT '' AS NAME";

        public override string GetTableInfoListSql => null;

        public override string GetViewInfoListSql => null;

        public override string GetColumnInfosByTableNameSql => null;

        public override List<DbColumnInfo> GetDbColumnsByTableName(string tableName)
        {
            var conn = dialect.getConnection();
            if (conn == null || !(conn is DbConnection dbConn))
                return base.GetDbColumnsByTableName(tableName);
            try
            {
                if (conn.State != ConnectionState.Open)
                    conn.Open();
                using (var schema = dbConn.GetSchema("Columns", new string[] { null, null, tableName, null }))
                {
                    var list = new List<DbColumnInfo>();
                    foreach (DataRow row in schema.Rows)
                    {
                        var info = new DbColumnInfo
                        {
                            Name = GetString(row, "COLUMN_NAME"),
                            DbTypeText = GetTypeName(row),
                            MaxLength = GetInt(row, "CHARACTER_MAXIMUM_LENGTH") ?? GetInt(row, "COLUMN_SIZE") ?? 0,
                            Scale = GetInt(row, "NUMERIC_SCALE") ?? 0,
                            Precision = GetInt(row, "NUMERIC_PRECISION") ?? 0,
                            IsNullable = GetString(row, "IS_NULLABLE")?.Equals("YES", StringComparison.OrdinalIgnoreCase) ?? true,
                            Position = GetInt(row, "ORDINAL_POSITION") ?? 0,
                            Comment = GetString(row, "DESCRIPTION")
                        };
                        if (!string.IsNullOrWhiteSpace(info.DbTypeText))
                            info.FieldType = dialect.mapping.GetDataType(info.DbTypeText);
                        list.Add(info);
                    }
                    return list.OrderBy(c => c.Position).ToList();
                }
            }
            finally
            {
                if (conn.State != ConnectionState.Closed)
                    conn.Close();
            }
        }

        private static string GetString(DataRow row, string col)
        {
            if (!row.Table.Columns.Contains(col)) return null;
            var v = row[col];
            return v == null || v == DBNull.Value ? null : v.ToString();
        }

        private static int? GetInt(DataRow row, string col)
        {
            if (!row.Table.Columns.Contains(col)) return null;
            var v = row[col];
            if (v == null || v == DBNull.Value) return null;
            if (v is int i) return i;
            if (v is short s) return s;
            if (v is long l) return (int)l;
            return int.TryParse(v.ToString(), out var n) ? n : (int?)null;
        }

        private static string GetTypeName(DataRow row)
        {
            if (row.Table.Columns.Contains("DATA_TYPE") && row["DATA_TYPE"] != DBNull.Value)
            {
                var dt = row["DATA_TYPE"];
                if (dt is int typeNum)
                    return OleDbTypeName(typeNum);
                if (dt is string s && !string.IsNullOrWhiteSpace(s))
                    return s.Trim();
            }
            if (row.Table.Columns.Contains("TYPE_NAME"))
            {
                var t = GetString(row, "TYPE_NAME");
                if (!string.IsNullOrWhiteSpace(t)) return t;
            }
            return "VARCHAR";
        }

        private static string OleDbTypeName(int oleDbType)
        {
            switch (oleDbType)
            {
                case 2: return "SmallInt";
                case 3: return "Integer";
                case 4: return "Single";
                case 5: return "Double";
                case 6: return "Currency";
                case 7: return "DateTime";
                case 11: return "Boolean";
                case 17: return "TinyInt";
                case 20: return "BigInt";
                case 72: return "GUID";
                case 128: return "Binary";
                case 129: return "Char";
                case 130: return "WChar";
                case 131: return "Numeric";
                case 200: return "VarChar";
                case 201: return "LongText";
                case 202: return "VarWChar";
                case 203: return "LongText";
                case 204: return "VarBinary";
                case 205: return "LongText";
                default: return "VarChar";
            }
        }

        public override List<string> GetDataBaseList()
        {
            return new List<string>();
        }

        public override List<DbTableInfo> GetDbTableList()
        {
            var conn = dialect.getConnection();
            if (conn == null || !(conn is DbConnection dbConn))
                return base.GetDbTableList();
            try
            {
                if (conn.State != ConnectionState.Open)
                    conn.Open();
                using (var schema = dbConn.GetSchema("Tables", new string[] { null, null, null, "TABLE" }))
                {
                    var list = new List<DbTableInfo>();
                    foreach (DataRow row in schema.Rows)
                    {
                        var name = GetString(row, "TABLE_NAME");
                        if (string.IsNullOrWhiteSpace(name)) continue;
                        list.Add(new DbTableInfo { Name = name, Comment = GetString(row, "DESCRIPTION") });
                    }
                    return list;
                }
            }
            finally
            {
                if (conn.State != ConnectionState.Closed)
                    conn.Close();
            }
        }

        public override List<DbTableInfo> GetDbViewList()
        {
            var conn = dialect.getConnection();
            if (conn == null || !(conn is DbConnection dbConn))
                return base.GetDbViewList();
            try
            {
                if (conn.State != ConnectionState.Open)
                    conn.Open();
                using (var schema = dbConn.GetSchema("Views", new string[] { null, null, null }))
                {
                    var list = new List<DbTableInfo>();
                    foreach (DataRow row in schema.Rows)
                    {
                        var name = GetString(row, "TABLE_NAME");
                        if (string.IsNullOrWhiteSpace(name)) continue;
                        list.Add(new DbTableInfo { Name = name, Comment = GetString(row, "DESCRIPTION") });
                    }
                    return list;
                }
            }
            finally
            {
                if (conn.State != ConnectionState.Closed)
                    conn.Close();
            }
        }

        public override bool? IsView(string tabelOrViewName, string dbName = null)
        {
            if (string.IsNullOrWhiteSpace(tabelOrViewName)) return null;
            var conn = dialect.getConnection();
            if (conn == null || !(conn is DbConnection dbConn))
                return null;
            try
            {
                if (conn.State != ConnectionState.Open)
                    conn.Open();
                using (var schema = dbConn.GetSchema("Views", new string[] { null, null, null }))
                {
                    foreach (DataRow row in schema.Rows)
                    {
                        var name = GetString(row, "TABLE_NAME");
                        if (string.Equals(name, tabelOrViewName, StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                    return false;
                }
            }
            finally
            {
                if (conn.State != ConnectionState.Closed)
                    conn.Close();
            }
        }

        public override bool? IsExitsTableCol(string table, string col)
        {
            if (string.IsNullOrWhiteSpace(table) || string.IsNullOrWhiteSpace(col)) return null;
            var conn = dialect.getConnection();
            if (conn == null || !(conn is DbConnection dbConn))
                return null;
            try
            {
                if (conn.State != ConnectionState.Open)
                    conn.Open();
                using (var schema = dbConn.GetSchema("Columns", new string[] { null, null, table, null }))
                {
                    foreach (DataRow row in schema.Rows)
                    {
                        var name = GetString(row, "COLUMN_NAME");
                        if (string.Equals(name, col, StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                    return false;
                }
            }
            finally
            {
                if (conn.State != ConnectionState.Closed)
                    conn.Close();
            }
        }

        public override bool IsExitsTableIndex(string table, string indexName)
        {
            if (string.IsNullOrWhiteSpace(table) || string.IsNullOrWhiteSpace(indexName)) return false;
            var conn = dialect.getConnection();
            if (conn == null || !(conn is DbConnection dbConn))
                return false;
            try
            {
                if (conn.State != ConnectionState.Open)
                    conn.Open();
                // Indexes schema: restrictions [catalog, schema, indexName, null, tableName]
                using (var schema = dbConn.GetSchema("Indexes", new string[] { null, null, null, null, table }))
                {
                    foreach (DataRow row in schema.Rows)
                    {
                        var name = GetString(row, "INDEX_NAME");
                        if (string.Equals(name, indexName, StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                    return false;
                }
            }
            finally
            {
                if (conn.State != ConnectionState.Closed)
                    conn.Close();
            }
        }
    }
}
