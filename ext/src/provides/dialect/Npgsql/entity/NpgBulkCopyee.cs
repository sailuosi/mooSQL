using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Spreadsheet;
using Npgsql;
using NPOI.SS.Formula.Functions;

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

        public NpgsqlConnection conn;
        private readonly string[] _columnNames;
        private NpgsqlBinaryImporter _importer;
        private NpgsqlTransaction _transaction;

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
        /// 
        /// </summary>
        /// <param name="rows"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
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
