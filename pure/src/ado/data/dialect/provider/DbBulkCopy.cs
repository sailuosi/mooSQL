using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 批量写入数据层驱动的父类。具有类似 SqlBulkCopy/MySQLBulkCopy等类相同的行为。
    /// 用以实现未被标准化支持的BulkCopy驱动类
    /// </summary>
    public abstract class DbBulkCopy : IDisposable
    {
        /// <summary>
        /// 数据库实例
        /// </summary>
        public DBInstance DB;

        /// <summary>
        /// 构造函数。
        /// </summary>
        public DbBulkCopy(DBInstance DB) { 
            this.DB = DB;
        }

        private bool _AutoDispose = true;
        /// <summary>
        /// 是否自动释放资源
        /// </summary>
        public bool AutoDispose
        {
            get { return _AutoDispose; }
            set
            {
                _AutoDispose = value;
            }
        }
        /// <summary>
        /// 需要释放资源时，行为由子类操作
        /// </summary>
        public virtual void Dispose()
        {
            //throw new NotImplementedException();
        }
        /// <summary>
        /// 批量执行的大小
        /// </summary>
        public int BatchSize { get; set; }
        /// <summary>
        /// 超时时间
        /// </summary>
        public int TimeOut { get; set; }
        /// <summary>
        /// 字段映射集合
        /// </summary>
        public DbBulkFieldMapBag MapBag { get; set; }
        /// <summary>
        /// 目标表名
        /// </summary>
        public string TargetTableName;
        /// <summary>
        /// 变更通知
        /// </summary>
        public int NotifyAfter {  get; set; }

        /// <summary>
        /// 抽象方法 WriteToServer（返回 BulkCopyResult），由子类实现。
        /// </summary>
        public abstract BulkCopyResult WriteToServer(DataRow[] rows);

        /// <summary>
        /// 抽象方法 WriteToServer（返回 BulkCopyResult），由子类实现。
        /// </summary>
        public abstract BulkCopyResult WriteToServer(DataTable table);

        /// <summary>
        /// 抽象方法 WriteToServer（返回 BulkCopyResult），由子类实现。
        /// </summary>
        public abstract BulkCopyResult WriteToServer(IDataReader reader);

        /// <summary>
        /// WriteToServerAsync 方法（返回 Task<BulkCopyResult>）。
        /// </summary>
        public virtual Task<BulkCopyResult> WriteToServerAsync(DataRow[] rows,CancellationToken token) {
            var cc= WriteToServer(rows);
            return Task.FromResult(cc);
        }
        /// <summary>
        /// WriteToServerAsync 方法（返回 Task<BulkCopyResult>）。
        /// </summary>
        public virtual Task<BulkCopyResult> WriteToServerAsync(DataTable table, CancellationToken token)
        {
            var cc = WriteToServer(table);
            return Task.FromResult(cc);
        }
        /// <summary>
        /// WriteToServerAsync 方法（返回 Task<BulkCopyResult>）。
        /// </summary>
        public virtual Task<BulkCopyResult> WriteToServerAsync(IDataReader reader, CancellationToken token)
        {
            var cc = WriteToServer(reader);
            return Task.FromResult(cc);
        }
    }
}