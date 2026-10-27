using System.Collections.Generic;
using System.Text;
using Dapper;
using Microsoft.Data.Sqlite;

namespace dbTest.items
{
    /// <summary>
    /// Dapper / ADO.NET 写操作共用 SQL（SQLite：多行 INSERT / ON CONFLICT Upsert）。
    /// 对齐 LiteOrm.Benchmark 的 ExecuteBatchInsert / ExecuteBatchUpsert，方言改为 SQLite。
    /// </summary>
    internal static class SqliteWriteSql
    {
        const string InsertCols =
            "F_Byte, F_Int16, F_Int32, F_Int64, F_Double, F_Float, F_Decimal, F_Bool, F_DateTime, F_String";

        public static void BatchInsert(SqliteConnection conn, IReadOnlyList<TestEntity> rows, SqliteTransaction tx = null)
        {
            if (rows == null || rows.Count == 0) return;
            const int chunkSize = 200;
            for (int offset = 0; offset < rows.Count; offset += chunkSize)
            {
                var n = System.Math.Min(chunkSize, rows.Count - offset);
                var sb = new StringBuilder("INSERT INTO TestEntity (").Append(InsertCols).Append(") VALUES ");
                var p = new DynamicParameters();
                for (int i = 0; i < n; i++)
                {
                    if (i > 0) sb.Append(',');
                    var e = rows[offset + i];
                    sb.Append($"(@B{i},@I16{i},@I32{i},@I64{i},@D{i},@F{i},@Dec{i},@Bool{i},@Dt{i},@S{i})");
                    p.Add($"B{i}", e.F_Byte);
                    p.Add($"I16{i}", e.F_Int16);
                    p.Add($"I32{i}", e.F_Int32);
                    p.Add($"I64{i}", e.F_Int64);
                    p.Add($"D{i}", e.F_Double);
                    p.Add($"F{i}", e.F_Float);
                    p.Add($"Dec{i}", e.F_Decimal);
                    p.Add($"Bool{i}", e.F_Bool);
                    p.Add($"Dt{i}", e.F_DateTime);
                    p.Add($"S{i}", e.F_String);
                }
                conn.Execute(sb.ToString(), p, tx);
            }
        }

        public static void BatchUpsert(SqliteConnection conn, IReadOnlyList<TestEntity> rows, SqliteTransaction tx = null)
        {
            if (rows == null || rows.Count == 0) return;
            const int chunkSize = 200;
            for (int offset = 0; offset < rows.Count; offset += chunkSize)
            {
                var n = System.Math.Min(chunkSize, rows.Count - offset);
                var sb = new StringBuilder(
                    "INSERT INTO TestEntity (Id, " + InsertCols + ") VALUES ");
                var p = new DynamicParameters();
                for (int i = 0; i < n; i++)
                {
                    if (i > 0) sb.Append(',');
                    var e = rows[offset + i];
                    sb.Append($"(@Id{i},@B{i},@I16{i},@I32{i},@I64{i},@D{i},@F{i},@Dec{i},@Bool{i},@Dt{i},@S{i})");
                    p.Add($"Id{i}", e.Id);
                    p.Add($"B{i}", e.F_Byte);
                    p.Add($"I16{i}", e.F_Int16);
                    p.Add($"I32{i}", e.F_Int32);
                    p.Add($"I64{i}", e.F_Int64);
                    p.Add($"D{i}", e.F_Double);
                    p.Add($"F{i}", e.F_Float);
                    p.Add($"Dec{i}", e.F_Decimal);
                    p.Add($"Bool{i}", e.F_Bool);
                    p.Add($"Dt{i}", e.F_DateTime);
                    p.Add($"S{i}", e.F_String);
                }
                sb.Append(" ON CONFLICT(Id) DO UPDATE SET ")
                    .Append("F_String=excluded.F_String, F_Int32=excluded.F_Int32, F_Decimal=excluded.F_Decimal, F_Bool=excluded.F_Bool");
                conn.Execute(sb.ToString(), p, tx);
            }
        }

        public const string SingleInsertSql =
            "INSERT INTO TestEntity (" + InsertCols + ") VALUES (@F_Byte,@F_Int16,@F_Int32,@F_Int64,@F_Double,@F_Float,@F_Decimal,@F_Bool,@F_DateTime,@F_String)";

        public const string SingleUpdateSql =
            "UPDATE TestEntity SET F_String=@F_String, F_Int32=@F_Int32, F_Decimal=@F_Decimal WHERE Id=@Id";

        public const string SingleUpsertSql =
            "INSERT INTO TestEntity (Id, " + InsertCols + ") VALUES (@Id,@F_Byte,@F_Int16,@F_Int32,@F_Int64,@F_Double,@F_Float,@F_Decimal,@F_Bool,@F_DateTime,@F_String) " +
            "ON CONFLICT(Id) DO UPDATE SET F_String=excluded.F_String, F_Int32=excluded.F_Int32, F_Decimal=excluded.F_Decimal";
    }
}
