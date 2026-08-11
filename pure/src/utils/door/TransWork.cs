using System;
using System.Data;
using mooSQL.data.clip;
using mooSQL.data.richRepo;

namespace mooSQL.data
{
    /// <summary>
    /// 同库事务工作门面：绑定同一 <see cref="DBExecutor"/>，供 useRepo / useRichRepo / useSQL / useClip 共享。
    /// </summary>
    public sealed class TransWork
    {
        /// <summary>所属数据库实例。</summary>
        public DBInstance DB { get; }

        /// <summary>已开启事务的执行器。</summary>
        public DBExecutor Executor { get; }

        internal TransWork(DBInstance db, DBExecutor executor)
        {
            DB = db ?? throw new ArgumentNullException(nameof(db));
            Executor = executor ?? throw new ArgumentNullException(nameof(executor));
        }

        /// <summary>绑定事务的 SQLBuilder。</summary>
        public SQLBuilder useSQL()
        {
            var kit = DB.useSQL();
            kit.useTransaction(Executor);
            return kit;
        }

        /// <summary>绑定事务的 SQLClip。</summary>
        public SQLClip useClip(SQLBuilder kit = null)
        {
            var c = DB.useClip(kit);
            c.useTransaction(Executor);
            return c;
        }

        /// <summary>绑定事务的薄仓储。</summary>
        public SooRepository<T> useRepo<T>() where T : class, new()
            => DB.useRepo<T>().useTransaction(Executor);

        /// <summary>绑定事务的富仓储。</summary>
        public SooRichRepo<T> useRichRepo<T>() where T : class, new()
        {
            var repo = DB.useRichRepo<T>();
            repo.useTransaction(Executor);
            return repo;
        }
    }

    /// <summary>
    /// Schema 同步客户端级默认（由 <see cref="MooClient.configureSchema"/> 写入）。
    /// </summary>
    public sealed class SchemaClientOptions
    {
        /// <summary>是否允许结构同步；生产建议 false。</summary>
        public bool AllowSchemaSync { get; set; } = true;

        /// <summary>是否允许 DROP 多余列（仍须 Options.AllowDropColumn）。</summary>
        public bool AllowDropColumn { get; set; } = false;
    }
}
