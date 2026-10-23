using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

using Npgsql;


namespace mooSQL.data
{

    /// <summary>
    /// postgre的批量写入 使用NpgsqlBinaryImporter
    /// </summary>
    public class NpgBulkCopyee : DbBulkCopy
    {
        /// <summary>
        /// g
        /// </summary>
        /// <param name="DB"></param>
        public NpgBulkCopyee(DBInstance DB) : base(DB)
        {

        }
        /// <summary>
        /// conn
        /// </summary>

        public NpgsqlConnection conn;
        private readonly string[] _columnNames;
        private NpgsqlBinaryImporter _importer;
        private NpgsqlTransaction _transaction;
        /// <summary>
        /// prepareRun
        /// </summary>

        public void prepareRun() {
            if (conn == null) { 
                conn= (NpgsqlConnection)this.DB.dialect.getConnection();
            }
            if (conn.State != ConnectionState.Open) { 
                conn.Open();
            }
            var names = this.getFieldNames();
            if (_importer == null)
            {
                _importer = conn.BeginBinaryImport($"COPY {TargetTableName} ({string.Join(", ", names)}) FROM STDIN (FORMAT BINARY)");
            }

        }
        /// <summary>
        /// DoRun
        /// </summary>

        protected ulong DoRun(Func<NpgsqlBinaryImporter,ulong> runner)
        {
            try
            {
                this.prepareRun();

                ulong total= runner(_importer);
    #if NETFRAMEWORK
                ulong res = 0;
                _importer.Complete();
                res = total;
#else
                var res= _importer.Complete();
#endif
                if (this._transaction != null) { 
                    _transaction.Commit();
                }
                return res;
            }
            catch (Exception ex)
            {
                if (this._transaction != null)
                {
                    _transaction.Rollback();
                }
                throw ex;
            }
            finally {
                if (this.AutoDispose) {
                    this.Dispose();
                }
            }

        }
        /// <summary>
        /// DoRunAsync
        /// </summary>

        protected async Task<BulkCopyResult> DoRunAsync(CancellationToken token,Func<NpgsqlBinaryImporter,Task< ulong>> runner)
        {
            try
            {
                this.prepareRun();

                ulong total =await runner(_importer);
#if NETFRAMEWORK
                ulong res = 0;
                _importer.Complete();
                res = total;
#else
                 var  res= await _importer.CompleteAsync(token);
#endif
                if (this._transaction != null)
                {
                    await _transaction.CommitAsync(token);
                }
                return new BulkCopyResult()
                {
                    count = (long)res
                };
            }
            catch (Exception ex)
            {
                if (this._transaction != null)
                {
                    await _transaction.RollbackAsync(token);
                }
                throw ex;
            }
            finally
            {
                if (this.AutoDispose)
                {
                    this.Dispose();
                }
            }

        }
/// <summary>
/// Dispose
/// </summary>
 

        public override void Dispose()
        {
            if (conn != null)
            {
                if (conn.State != ConnectionState.Closed)
                {
                    conn.Close();
                }
                conn.Dispose();
            }
            if (_importer != null)
            {
                _importer.Dispose();
                _importer = null;
            }

            if (_transaction != null)
            {
                _transaction.Dispose();
                _transaction = null;
            }
        }


        private List<string> getFieldNames()
        {
            var res = new List<string>();
            foreach (var map in MapBag.Maps)
            {
                var field = map.tarName;
                if (string.IsNullOrWhiteSpace(field))
                {
                    //无法匹配
                    continue;
                    
                }
                res.Add(field);
            }
            return res;
        }
        private List<DbBulkFieldMap> getFieldMap(DataColumnCollection columns)
        {
            var res = new List<DbBulkFieldMap>();
            foreach (var map in MapBag.Maps)
            {
                var field = map.tarName;
                if (string.IsNullOrWhiteSpace(field))
                {
                    //依据索引
                    if (map.tarIndex >= 0)
                    {
                        field = columns[map.tarIndex].ColumnName;
                    }
                    else if (map.srcIndex >= 0)
                    {
                        field = columns[map.srcIndex].ColumnName;
                    }
                    else if (!string.IsNullOrWhiteSpace(map.srcName))
                    {
                        field = map.srcName;
                    }
                    else
                    {
                        //无法匹配
                        continue;
                    }
                }
                //获取列索引
                if (map.srcIndex >= 0)
                {
                    res.Add(map);
                    continue;
                }
                if (!string.IsNullOrWhiteSpace(map.srcName))
                {
                    int index = columns.IndexOf(map.srcName);
                    map.srcIndex = index;
                    res.Add(map);
                    continue;
                }
            }
            return res;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="rows"></param>
        /// <returns></returns>
        public override BulkCopyResult WriteToServer(DataRow[] rows)
        {
            if (rows == null)
            {
                return new BulkCopyResult() { count = 0 };
            }

            var total= DoRun((kit) => {
                List<DbBulkFieldMap> fieldMap = null;
                ulong total = 0;
                foreach (DataRow row in rows)
                {
                    kit.StartRow();
                    if (fieldMap == null)
                    {
                        fieldMap = this.getFieldMap(row.Table.Columns);
                    }
                    for(int i = 0; i < fieldMap.Count; i++) 
                    {
                        var map=fieldMap[i];
                        var v = row[map.srcIndex];
                        
                        kit.Write(v);
                    }
                    total++;
                }
                return total;
            });

            return new BulkCopyResult()
            {
                count = (long)total,
            };
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public override BulkCopyResult WriteToServer(DataTable table)
        {
            if (table == null)
            {
                return new BulkCopyResult() { count = 0 };
            }
            List<DbBulkFieldMap> fieldMap = this.getFieldMap(table.Columns); 
            var total = DoRun((kit) => {
                ulong total = 0;
                foreach (DataRow row in table.Rows)
                {
                    kit.StartRow();
                    for (int i = 0; i < fieldMap.Count; i++)
                    {
                        var map = fieldMap[i];
                        var v = row[map.srcIndex];

                        kit.Write(v);
                    }

                    total++;
                }
                return total;
            });

            return new BulkCopyResult()
            {
                count = (long)total,
            };
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public override BulkCopyResult WriteToServer(IDataReader reader)
        {
            if (reader == null)
            {
                return new BulkCopyResult() { count = 0 };
            }

            var total = DoRun((kit) => {
                ulong total = 0;
                while (reader.Read())
                {
                    kit.StartRow();
                    foreach (var map in MapBag.Maps)
                    {
                        var field = map.tarName;
                        if (string.IsNullOrWhiteSpace(field))
                        {
                            continue;
                        }
                        //获取列索引
                        if (map.srcIndex >= 0)
                        {
                            var v = reader.GetValue(map.srcIndex);
                            kit.Write(v);
                        }
                        else if (!string.IsNullOrWhiteSpace(map.srcName))
                        {
                            var v = reader[map.srcName];
                            kit.Write(v);
                        }
                    }
                    total++;
                }
                return total;
            });

            return new BulkCopyResult()
            {
                count = (long)total,
            };
        }


        /// <summary>
        /// 异步将 DataRow 数组批量写入服务器。
        /// </summary>
        /// <param name="rows">要写入的行。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>批量写入结果。</returns>
        public override async Task<BulkCopyResult> WriteToServerAsync(DataRow[] rows, CancellationToken token)
        {
            if (rows == null)
            {
                return  new BulkCopyResult() { count = 0 };
            }

            var total = await DoRunAsync(token,async(kit) => {
                List<DbBulkFieldMap> fieldMap = null;
                ulong total = 0;
                foreach (DataRow row in rows)
                {
#if NETFRAMEWORK
                    kit.StartRow();
#else
                    await kit.StartRowAsync(token);
#endif
                    if (fieldMap == null)
                    {
                        fieldMap = this.getFieldMap(row.Table.Columns);
                    }
                    for (int i = 0; i < fieldMap.Count; i++)
                    {
                        var map = fieldMap[i];
                        var v = row[map.srcIndex];

#if NETFRAMEWORK
                        kit.Write(v);
#else
                        await kit.WriteAsync(v, token);
#endif
                    }
                    total++;
                }
                return total;
            });

            return total;
        }


        /// <summary>
        /// 异步将 DataTable 批量写入服务器。
        /// </summary>
        /// <param name="table">要写入的表。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>批量写入结果。</returns>
        public override async Task<BulkCopyResult> WriteToServerAsync(DataTable table, CancellationToken token)
        {
            if (table == null)
            {
                return new BulkCopyResult() { count = 0 };
            }
            List<DbBulkFieldMap> fieldMap = this.getFieldMap(table.Columns);
            var total =await DoRunAsync(token,async (kit) => {
                ulong total = 0;
                foreach (DataRow row in table.Rows)
                {
#if NETFRAMEWORK
                    kit.StartRow();
#else
                    await kit.StartRowAsync(token);
#endif

                    for (int i = 0; i < fieldMap.Count; i++)
                    {
                        var map = fieldMap[i];
                        var v = row[map.srcIndex];
#if NETFRAMEWORK
                        kit.Write(v);
#else
                        await kit.WriteAsync(v,token);
#endif
                    }

                    total++;
                }
                return total;
            });

            return total;
        }
        /// <summary>
        /// 异步将 IDataReader 数据批量写入服务器。
        /// </summary>
        /// <param name="reader">数据读取器。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>批量写入结果。</returns>
        public override async Task<BulkCopyResult> WriteToServerAsync(IDataReader reader, CancellationToken token)
        {
            if (reader == null)
            {
                return new BulkCopyResult() { count = 0 };
            }

            var total =await DoRunAsync(token,async(kit) => {
                ulong total = 0;
                while (reader.Read())
                {
#if NETFRAMEWORK
                    kit.StartRow();
#else
                    await kit.StartRowAsync(token);
#endif
                    foreach (var map in MapBag.Maps)
                    {
                        var field = map.tarName;
                        if (string.IsNullOrWhiteSpace(field))
                        {
                            continue;
                        }
                        //获取列索引
                        if (map.srcIndex >= 0)
                        {
                            var v = reader.GetValue(map.srcIndex);
#if NETFRAMEWORK
                            kit.Write(v);
#else
                            await kit.WriteAsync(v, token);
#endif
                        }
                        else if (!string.IsNullOrWhiteSpace(map.srcName))
                        {
                            var v = reader[map.srcName];
#if NETFRAMEWORK
                            kit.Write(v);
#else
                            await kit.WriteAsync(v, token);
#endif
                        }
                    }
                    total++;
                }
                return total;
            });

            return total;
        }
    }
}
